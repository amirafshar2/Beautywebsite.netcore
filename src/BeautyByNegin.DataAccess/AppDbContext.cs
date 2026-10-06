using System.Linq.Expressions;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeautyByNegin.DataAccess;

/// <summary>
/// Single EF Core context for the whole site. Provider-agnostic: nothing here is SQLite specific,
/// so switching to SQL Server / PostgreSQL only means changing the provider call in DataAccessSetup.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Language> Languages => Set<Language>();

    public DbSet<MediaImage> MediaImages => Set<MediaImage>();
    public DbSet<MediaImageTranslation> MediaImageTranslations => Set<MediaImageTranslation>();

    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceTranslation> ServiceTranslations => Set<ServiceTranslation>();
    public DbSet<ServiceImage> ServiceImages => Set<ServiceImage>();
    public DbSet<ServiceSlugHistory> ServiceSlugHistory => Set<ServiceSlugHistory>();

    public DbSet<GalleryCategory> GalleryCategories => Set<GalleryCategory>();
    public DbSet<GalleryCategoryTranslation> GalleryCategoryTranslations => Set<GalleryCategoryTranslation>();
    public DbSet<GalleryItem> GalleryItems => Set<GalleryItem>();
    public DbSet<GalleryItemTranslation> GalleryItemTranslations => Set<GalleryItemTranslation>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<AppointmentRequest> AppointmentRequests => Set<AppointmentRequest>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<TimeSlotTranslation> TimeSlotTranslations => Set<TimeSlotTranslation>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<ChatVisitor> ChatVisitors => Set<ChatVisitor>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageTranslation> PageTranslations => Set<PageTranslation>();
    public DbSet<HomeSection> HomeSections => Set<HomeSection>();
    public DbSet<ListItem> ListItems => Set<ListItem>();
    public DbSet<ListItemTranslation> ListItemTranslations => Set<ListItemTranslation>();
    public DbSet<InstagramPost> InstagramPosts => Set<InstagramPost>();
    public DbSet<OpeningHour> OpeningHours => Set<OpeningHour>();

    public DbSet<SiteText> SiteTexts => Set<SiteText>();
    public DbSet<SiteTextTranslation> SiteTextTranslations => Set<SiteTextTranslation>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Language>(e =>
        {
            e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasMaxLength(5);
            e.Property(x => x.CultureName).HasMaxLength(10).IsRequired();
            e.Property(x => x.NativeName).HasMaxLength(50).IsRequired();
            e.Property(x => x.ShortLabel).HasMaxLength(5).IsRequired();
        });

        b.Entity<AppUser>(e =>
        {
            e.Property(x => x.DisplayName).HasMaxLength(100);
            e.Property(x => x.PanelLanguage).HasMaxLength(5);
        });

        // ---------- Media
        b.Entity<MediaImage>(e =>
        {
            e.Property(x => x.StorageKey).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.StorageKey).IsUnique();
            e.Property(x => x.Widths).HasMaxLength(50);
            e.Property(x => x.OriginalFileName).HasMaxLength(255);
        });
        Translation<MediaImage, MediaImageTranslation>(b, t => t.MediaImageId);
        b.Entity<MediaImageTranslation>().Property(x => x.AltText).HasMaxLength(300);

        // ---------- Services
        b.Entity<Service>(e =>
        {
            e.Property(x => x.Price).HasPrecision(12, 2);
            e.HasOne(x => x.CoverImage).WithMany().HasForeignKey(x => x.CoverImageId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Images).WithOne().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Cascade);
        });
        Translation<Service, ServiceTranslation>(b, t => t.ServiceId);
        b.Entity<ServiceTranslation>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Subtitle).HasMaxLength(250);
            e.Property(x => x.ShortDescription).HasMaxLength(1000);
            e.Property(x => x.SuitableFor).HasMaxLength(1000);
            e.Property(x => x.ExpectedResult).HasMaxLength(1000);
            e.Property(x => x.Duration).HasMaxLength(100);
            e.Property(x => x.Slug).HasMaxLength(150).IsRequired();
            e.Property(x => x.MetaTitle).HasMaxLength(150);
            e.Property(x => x.MetaDescription).HasMaxLength(300);
            e.HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
        });
        b.Entity<ServiceImage>(e =>
        {
            e.HasOne(x => x.MediaImage).WithMany().HasForeignKey(x => x.MediaImageId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ServiceSlugHistory>(e =>
        {
            e.Property(x => x.LanguageCode).HasMaxLength(5);
            e.Property(x => x.OldSlug).HasMaxLength(150);
            e.HasIndex(x => new { x.LanguageCode, x.OldSlug });
            e.HasOne<Service>().WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Gallery
        Translation<GalleryCategory, GalleryCategoryTranslation>(b, t => t.GalleryCategoryId);
        b.Entity<GalleryCategoryTranslation>().Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Entity<GalleryItem>(e =>
        {
            e.HasOne(x => x.Category).WithMany(c => c.Items).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AfterImage).WithMany().HasForeignKey(x => x.AfterImageId).OnDelete(DeleteBehavior.SetNull);
        });
        Translation<GalleryItem, GalleryItemTranslation>(b, t => t.GalleryItemId);
        b.Entity<GalleryItemTranslation>().Property(x => x.Caption).HasMaxLength(300);

        // ---------- Reviews
        b.Entity<Review>(e =>
        {
            e.Property(x => x.AuthorName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Text).HasMaxLength(3000).IsRequired();
            e.Property(x => x.LanguageCode).HasMaxLength(5);
            e.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.Status);
        });

        // ---------- Inbox
        b.Entity<AppointmentRequest>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(40).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.ServiceNameSnapshot).HasMaxLength(150);
            e.Property(x => x.TimeSlotSnapshot).HasMaxLength(100);
            e.Property(x => x.Message).HasMaxLength(3000);
            e.Property(x => x.LanguageCode).HasMaxLength(5);
            e.Property(x => x.AdminNotes).HasMaxLength(3000);
            e.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.TimeSlot).WithMany().HasForeignKey(x => x.TimeSlotId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CreatedAtUtc);
        });
        Translation<TimeSlot, TimeSlotTranslation>(b, t => t.TimeSlotId);
        b.Entity<TimeSlotTranslation>().Property(x => x.Label).HasMaxLength(100).IsRequired();
        b.Entity<ContactMessage>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.ContactInfo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Message).HasMaxLength(3000).IsRequired();
            e.Property(x => x.LanguageCode).HasMaxLength(5);
            e.Property(x => x.AdminNotes).HasMaxLength(3000);
            e.HasIndex(x => x.CreatedAtUtc);
        });
        b.Entity<NewsletterSubscriber>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.LanguageCode).HasMaxLength(5);
            e.HasIndex(x => x.Email);
        });

        // ---------- Chat
        b.Entity<ChatVisitor>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.LanguageCode).HasMaxLength(5);
            e.Property(x => x.VerificationCodeHash).HasMaxLength(100);
            e.Property(x => x.SessionTokenHash).HasMaxLength(100);
            e.Property(x => x.AdminNotes).HasMaxLength(3000);
            e.HasIndex(x => x.Email);
            e.HasIndex(x => x.SessionTokenHash);
            e.HasIndex(x => x.LastMessageAtUtc);
            e.HasMany(x => x.Messages).WithOne(m => m.Visitor).HasForeignKey(m => m.VisitorId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ChatMessage>(e =>
        {
            e.Property(x => x.Text).HasMaxLength(2000).IsRequired();
            e.Property(x => x.PageUrl).HasMaxLength(500);
            e.HasIndex(x => new { x.VisitorId, x.CreatedAtUtc });
            // Messages of a visitor in the Trash are hidden together with the visitor.
            e.HasQueryFilter(m => m.Visitor!.DeletedAtUtc == null);
        });

        // ---------- Pages & blocks
        b.Entity<Page>(e =>
        {
            e.Property(x => x.SystemKey).HasMaxLength(30);
            e.HasIndex(x => x.SystemKey).IsUnique();
        });
        Translation<Page, PageTranslation>(b, t => t.PageId);
        b.Entity<PageTranslation>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(150).IsRequired();
            e.Property(x => x.MetaTitle).HasMaxLength(150);
            e.Property(x => x.MetaDescription).HasMaxLength(300);
            e.HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
        });
        b.Entity<HomeSection>(e =>
        {
            e.Property(x => x.Key).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.Key).IsUnique();
        });
        b.Entity<ListItem>(e =>
        {
            e.Property(x => x.ListKey).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.ListKey);
        });
        Translation<ListItem, ListItemTranslation>(b, t => t.ListItemId);
        b.Entity<ListItemTranslation>(e =>
        {
            e.Property(x => x.Text).HasMaxLength(300).IsRequired();
            e.Property(x => x.Detail).HasMaxLength(300);
        });
        b.Entity<InstagramPost>(e =>
        {
            e.Property(x => x.LinkUrl).HasMaxLength(500);
            e.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<OpeningHour>(e => e.HasIndex(x => x.Day).IsUnique());

        // ---------- Texts & settings
        b.Entity<SiteText>(e =>
        {
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(100);
            e.Property(x => x.Group).HasMaxLength(50).IsRequired();
            e.Property(x => x.Hint).HasMaxLength(300);
            e.HasMany(x => x.Translations).WithOne().HasForeignKey(x => x.SiteTextKey).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<SiteTextTranslation>(e =>
        {
            e.Property(x => x.LanguageCode).HasMaxLength(5).IsRequired();
            e.HasIndex(x => new { x.SiteTextKey, x.LanguageCode }).IsUnique();
        });
        b.Entity<SiteSetting>(e =>
        {
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(100);
        });

        ApplySoftDeleteFilters(b);
    }

    /// <summary>Common configuration for a translation table: FK, cascade delete, one row per language.</summary>
    private static void Translation<TParent, TTranslation>(ModelBuilder b, Expression<Func<TTranslation, object?>> foreignKey)
        where TParent : class, ITranslatable<TTranslation>
        where TTranslation : TranslationBase
    {
        b.Entity<TParent>()
            .HasMany(p => p.Translations)
            .WithOne()
            .HasForeignKey(foreignKey)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TTranslation>(e =>
        {
            e.Property(x => x.LanguageCode).HasMaxLength(5).IsRequired();
            var fkName = ((MemberExpression)(foreignKey.Body is UnaryExpression u ? u.Operand : foreignKey.Body)).Member.Name;
            e.HasIndex(fkName, nameof(TranslationBase.LanguageCode)).IsUnique();
        });
    }

    /// <summary>Soft-deleted rows (in the Trash) are hidden from every normal query.</summary>
    private static void ApplySoftDeleteFilters(ModelBuilder b)
    {
        foreach (var type in b.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(type.ClrType) || type.BaseType is not null) continue;
            var param = Expression.Parameter(type.ClrType, "e");
            var body = Expression.Equal(
                Expression.Property(param, nameof(ISoftDelete.DeletedAtUtc)),
                Expression.Constant(null, typeof(DateTime?)));
            type.SetQueryFilter(Expression.Lambda(body, param));
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        Touch();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        Touch();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private void Touch()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ITimestamped>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAtUtc = now;
            if (entry.State is EntityState.Added or EntityState.Modified) entry.Entity.UpdatedAtUtc = now;
        }
    }
}
