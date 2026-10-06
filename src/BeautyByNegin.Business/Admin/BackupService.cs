using System.IO.Compression;
using System.Text.Json;
using BeautyByNegin.Business.Content;
using BeautyByNegin.Business.Media;
using BeautyByNegin.DataAccess;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace BeautyByNegin.Business.Admin;

public sealed record BackupFile(string Name, long SizeBytes, DateTime CreatedAtUtc);

public sealed record RestoreResult(bool Ok, string? ErrorKey = null);

public interface IBackupService
{
    /// <summary>Creates a zip with the database, all uploaded images and the encryption keys.</summary>
    Task<string> CreateAsync(bool automatic, CancellationToken ct = default);
    IReadOnlyList<BackupFile> List();
    string? GetPath(string name);
    Task<RestoreResult> RestoreAsync(Stream zip, CancellationToken ct = default);
    bool Delete(string name);
}

/// <summary>
/// Backup = the way to move the site (Iran → Turkey → Germany): download the zip, install the site on the
/// new server, restore the zip. Zip layout: site.db, uploads/**, keys/**, backup.json.
/// </summary>
public sealed class BackupService(
    AppDbContext db,
    IMediaPaths paths,
    ISiteCache cache,
    Settings.ISettingsService settings,
    ILogger<BackupService> logger) : IBackupService
{
    private string BackupDir => Directory.CreateDirectory(Path.Combine(paths.AppDataRoot, "backups")).FullName;
    private string KeysDir => Path.Combine(paths.AppDataRoot, "keys");

    private string DatabasePath
    {
        get
        {
            var cs = new SqliteConnectionStringBuilder(db.Database.GetConnectionString());
            return Path.GetFullPath(cs.DataSource);
        }
    }

    public async Task<string> CreateAsync(bool automatic, CancellationToken ct = default)
    {
        var name = $"backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}{(automatic ? "-auto" : "")}.zip";
        var zipPath = Path.Combine(BackupDir, name);
        var tempDb = Path.Combine(Path.GetTempPath(), $"bbn-{Guid.NewGuid():N}.db");

        try
        {
            // Consistent copy of the live database (works while the site is running).
            await using (var source = new SqliteConnection($"Data Source={DatabasePath}"))
            await using (var target = new SqliteConnection($"Data Source={tempDb};Pooling=False"))
            {
                await source.OpenAsync(ct);
                await target.OpenAsync(ct);
                source.BackupDatabase(target);
            }

            await using (var fs = File.Create(zipPath))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(tempDb, "site.db", CompressionLevel.Optimal);
                AddFolder(zip, paths.UploadsRoot, "uploads");
                AddFolder(zip, KeysDir, "keys");
                var manifest = zip.CreateEntry("backup.json");
                await using var ms = manifest.Open();
                await JsonSerializer.SerializeAsync(ms, new
                {
                    app = "BeautyByNegin",
                    createdAtUtc = DateTime.UtcNow,
                    automatic,
                    migration = (await db.Database.GetAppliedMigrationsAsync(ct)).LastOrDefault()
                }, cancellationToken: ct);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            TryDelete(tempDb);
        }

        if (automatic) PruneAutomatic((await settings.GetAsync(ct)).Int(SettingKeys.AutoBackupKeep) ?? 7);
        logger.LogInformation("Backup {Name} created", name);
        return zipPath;
    }

    private static void AddFolder(ZipArchive zip, string folder, string prefix)
    {
        if (!Directory.Exists(folder)) return;
        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(folder, file).Replace('\\', '/');
            zip.CreateEntryFromFile(file, $"{prefix}/{rel}", file.EndsWith(".webp") ? CompressionLevel.NoCompression : CompressionLevel.Optimal);
        }
    }

    private void PruneAutomatic(int keep)
    {
        foreach (var old in List().Where(b => b.Name.Contains("-auto")).Skip(Math.Max(1, keep)))
            TryDelete(Path.Combine(BackupDir, old.Name));
    }

    public IReadOnlyList<BackupFile> List() => new DirectoryInfo(BackupDir).GetFiles("backup-*.zip")
        .OrderByDescending(f => f.CreationTimeUtc)
        .Select(f => new BackupFile(f.Name, f.Length, f.CreationTimeUtc)).ToList();

    public string? GetPath(string name)
    {
        // Only plain file names from our own folder (no path traversal).
        if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name) || !name.EndsWith(".zip")) return null;
        var path = Path.Combine(BackupDir, name);
        return File.Exists(path) ? path : null;
    }

    public bool Delete(string name)
    {
        var path = GetPath(name);
        if (path is null) return false;
        TryDelete(path);
        return true;
    }

    public async Task<RestoreResult> RestoreAsync(Stream zipStream, CancellationToken ct = default)
    {
        var work = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"bbn-restore-{Guid.NewGuid():N}")).FullName;
        try
        {
            // 1) Extract safely to a temp folder.
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                if (zip.GetEntry("site.db") is null) return new RestoreResult(false, "backup.invalid");
                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    var dest = Path.GetFullPath(Path.Combine(work, entry.FullName));
                    if (!dest.StartsWith(work + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return new RestoreResult(false, "backup.invalid");
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    entry.ExtractToFile(dest, overwrite: true);
                }
            }

            // 2) Check that the database really is one of ours.
            var newDb = Path.Combine(work, "site.db");
            try
            {
                await using var check = new SqliteConnection($"Data Source={newDb};Mode=ReadOnly;Pooling=False");
                await check.OpenAsync(ct);
                await using var cmd = check.CreateCommand();
                cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE name IN ('Services','SiteSettings','AspNetUsers')";
                if (Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) < 3) return new RestoreResult(false, "backup.invalid");
            }
            catch (SqliteException) { return new RestoreResult(false, "backup.invalid"); }

            // 3) Safety net: back up the current state first.
            await CreateAsync(automatic: false, ct);

            // 4) Replace database, images and keys.
            SqliteConnection.ClearAllPools();
            var dbPath = DatabasePath;
            TryDelete(dbPath + "-wal");
            TryDelete(dbPath + "-shm");
            File.Copy(newDb, dbPath, overwrite: true);

            ReplaceFolder(Path.Combine(work, "uploads"), paths.UploadsRoot);
            if (Directory.Exists(Path.Combine(work, "keys"))) ReplaceFolder(Path.Combine(work, "keys"), KeysDir);

            // 5) Bring an older backup up to the current schema, then refresh caches.
            await db.Database.MigrateAsync(ct);
            cache.InvalidateAll();
            logger.LogWarning("Backup restored");
            return new RestoreResult(true);
        }
        catch (InvalidDataException)
        {
            return new RestoreResult(false, "backup.invalid");
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch { /* temp */ }
        }
    }

    private static void ReplaceFolder(string from, string to)
    {
        if (!Directory.Exists(from)) return;
        if (Directory.Exists(to))
            foreach (var f in Directory.EnumerateFiles(to, "*", SearchOption.AllDirectories)) TryDelete(f);
        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var dest = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { }
    }
}

/// <summary>Daily jobs: automatic backup (keeps the last N) and emptying expired Trash items.</summary>
public sealed class MaintenanceWorker(IServiceScopeFactory scopeFactory, ILogger<MaintenanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); // let the site start first
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;
                var settings = await sp.GetRequiredService<Settings.ISettingsService>().GetAsync(stoppingToken);
                var backups = sp.GetRequiredService<IBackupService>();
                var lastAuto = backups.List().FirstOrDefault(b => b.Name.Contains("-auto"));
                if (settings.Bool(SettingKeys.AutoBackupEnabled) && (lastAuto is null || DateTime.UtcNow - lastAuto.CreatedAtUtc > TimeSpan.FromHours(23)))
                    await backups.CreateAsync(automatic: true, stoppingToken);

                var purged = await sp.GetRequiredService<ITrashService>().PurgeExpiredAsync(stoppingToken);
                if (purged > 0) logger.LogInformation("{Count} expired Trash items removed", purged);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Daily maintenance failed");
            }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
