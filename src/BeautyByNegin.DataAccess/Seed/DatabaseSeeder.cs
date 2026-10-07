using BeautyByNegin.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BeautyByNegin.DataAccess.Seed;

/// <summary>
/// Inserts initial data. Every step is idempotent and additive: it only adds what is missing
/// and never overwrites content the admin has edited, so it is safe to run on every start.
/// </summary>
public sealed class DatabaseSeeder(AppDbContext db, RoleManager<IdentityRole> roles)
{
    private static readonly string[] Langs = ["fa", "tr", "de", "en", "ar"];

    public async Task SeedAsync()
    {
        await SeedLanguagesAsync();
        await SeedRolesAsync();
        await SeedSettingsAsync();
        await SeedTextsAsync();
        await SeedHomeSectionsAsync();
        await SeedOpeningHoursAsync();

        // Content seed runs only once, on an empty database (deleted items must stay deleted).
        if (!await db.Services.IgnoreQueryFilters().AnyAsync())
        {
            await SeedServicesAsync();
            await SeedTimeSlotsAsync();
            await SeedGalleryCategoriesAsync();
            await SeedListItemsAsync();
            await SeedPagesAsync();
        }
    }

    private async Task SeedLanguagesAsync()
    {
        Language[] all =
        [
            new() { Code = "fa", CultureName = "fa-IR", NativeName = "فارسی", ShortLabel = "FA", IsRtl = true, IsDefault = true, SortOrder = 1, UseNativeDigits = true },
            new() { Code = "tr", CultureName = "tr-TR", NativeName = "Türkçe", ShortLabel = "TR", SortOrder = 2 },
            new() { Code = "de", CultureName = "de-DE", NativeName = "Deutsch", ShortLabel = "DE", SortOrder = 3 },
            new() { Code = "en", CultureName = "en-US", NativeName = "English", ShortLabel = "EN", SortOrder = 4 },
            new() { Code = "ar", CultureName = "ar-AE", NativeName = "العربية", ShortLabel = "AR", IsRtl = true, SortOrder = 5 },
        ];
        var existing = await db.Languages.Select(l => l.Code).ToListAsync();
        var hasDefault = await db.Languages.AnyAsync(l => l.IsDefault);
        foreach (var lang in all.Where(l => !existing.Contains(l.Code)))
        {
            if (hasDefault) lang.IsDefault = false;
            db.Languages.Add(lang);
        }
        await db.SaveChangesAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in AppRoles.All)
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));
    }

    private async Task SeedSettingsAsync()
    {
        var defaults = new Dictionary<string, string?>
        {
            [SettingKeys.SetupCompleted] = "false",
            [SettingKeys.BrandName] = "Beauty by Negin",
            [SettingKeys.CountryCode] = "IR",
            [SettingKeys.TimeZoneId] = "Asia/Tehran",
            [SettingKeys.AddCityToTitles] = "false",
            [SettingKeys.MaintenanceMode] = "false",
            [SettingKeys.HttpsRedirect] = "false",
            [SettingKeys.Hsts] = "false",
            [SettingKeys.ShowPrices] = "false",
            [SettingKeys.AllowVisitorReviews] = "false",
            [SettingKeys.PrivacyConsentRequired] = "true",
            [SettingKeys.NewsletterEnabled] = "true",
            [SettingKeys.ChatEnabled] = "true",
            [SettingKeys.AccountsEnabled] = "true",
            [SettingKeys.AccountsShowBookings] = "true",
            [SettingKeys.ReviewsRequireLogin] = "true",
            [SettingKeys.SmtpEnabled] = "false",
            [SettingKeys.SmtpHost] = "smtp.gmail.com",
            [SettingKeys.SmtpPort] = "587",
            [SettingKeys.SmtpUseSsl] = "true",
            [SettingKeys.SmtpFromName] = "Beauty by Negin",
            [SettingKeys.TelegramEnabled] = "false",
            [SettingKeys.TelegramNotifyAppointments] = "true",
            [SettingKeys.TelegramNotifyMessages] = "true",
            [SettingKeys.TelegramNotifyChat] = "true",
            [SettingKeys.TelegramNotifyNewsletter] = "true",
            [SettingKeys.AiModel] = "gemini-3.5-flash",
            [SettingKeys.AutoBackupEnabled] = "true",
            [SettingKeys.AutoBackupKeep] = "7",
        };

        var existing = await db.SiteSettings.Select(s => s.Key).ToListAsync();
        foreach (var (key, value) in defaults)
            if (!existing.Contains(key))
                db.SiteSettings.Add(new SiteSetting { Key = key, Value = value });
        await db.SaveChangesAsync();
    }

    private async Task SeedTextsAsync()
    {
        var texts = await db.SiteTexts.Include(t => t.Translations).ToDictionaryAsync(t => t.Key);
        var order = 0;
        foreach (var e in TextSeed.All)
        {
            order++;
            if (!texts.TryGetValue(e.Key, out var text))
            {
                text = new SiteText { Key = e.Key, Group = e.Group, Kind = e.Kind, Hint = e.Hint, SortOrder = order };
                db.SiteTexts.Add(text);
            }
            else
            {
                // Metadata (not the values) may be improved in new versions.
                text.Group = e.Group; text.Kind = e.Kind; text.Hint = e.Hint; text.SortOrder = order;
            }

            foreach (var (lang, value) in new[] { ("fa", e.Fa), ("tr", e.Tr), ("de", e.De), ("en", e.En), ("ar", TextSeedAr.Get(e.Key, e.En)) })
                if (text.Translations.All(t => t.LanguageCode != lang))
                    text.Translations.Add(new SiteTextTranslation { SiteTextKey = e.Key, LanguageCode = lang, Value = value });
        }
        await db.SaveChangesAsync();
    }

    private async Task SeedHomeSectionsAsync()
    {
        var existing = await db.HomeSections.Select(s => s.Key).ToListAsync();
        var order = await db.HomeSections.Select(s => (int?)s.SortOrder).MaxAsync() ?? 0;
        foreach (var key in HomeSectionKeys.All)
            if (!existing.Contains(key))
                db.HomeSections.Add(new HomeSection { Key = key, SortOrder = ++order, IsVisible = true });
        await db.SaveChangesAsync();
    }

    private async Task SeedOpeningHoursAsync()
    {
        if (await db.OpeningHours.AnyAsync()) return;
        // Iranian week starts on Saturday; Friday closed by default. Editable in the panel.
        DayOfWeek[] week = [DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
        for (var i = 0; i < week.Length; i++)
        {
            var closed = week[i] == DayOfWeek.Friday;
            db.OpeningHours.Add(new OpeningHour
            {
                Day = week[i],
                SortOrder = i + 1,
                IsClosed = closed,
                Opens = closed ? null : new TimeOnly(10, 0),
                Closes = closed ? null : new TimeOnly(19, 0)
            });
        }
        await db.SaveChangesAsync();
    }

    private async Task SeedServicesAsync()
    {
        var order = 0;
        foreach (var item in ServiceSeed.Items)
        {
            var service = new Service { SortOrder = ++order, IsVisible = true, ShowOnHome = item.ShowOnHome };
            var texts = new Dictionary<string, ServiceSeed.Text>(item.Texts) { ["ar"] = ServiceSeedAr.Texts[item.Slug] };
            foreach (var (lang, t) in texts)
            {
                service.Translations.Add(new ServiceTranslation
                {
                    LanguageCode = lang,
                    Name = item.Name,
                    Slug = item.Slug,
                    Subtitle = t.Subtitle,
                    ShortDescription = ServiceSeed.ShortDescription(t.Description),
                    Description = ServiceSeed.ToHtml(t.Description),
                    SuitableFor = t.SuitableFor,
                    ExpectedResult = t.ExpectedResult
                });
            }
            db.Services.Add(service);
        }
        await db.SaveChangesAsync();
    }

    private async Task SeedTimeSlotsAsync()
    {
        string[][] slots =
        [
            ["صبح (۱۰ تا ۱۳)", "Sabah (10:00–13:00)", "Vormittag (10–13 Uhr)", "Morning (10am–1pm)", "صباحاً (10–13)"],
            ["بعدازظهر (۱۳ تا ۱۶)", "Öğleden sonra (13:00–16:00)", "Nachmittag (13–16 Uhr)", "Afternoon (1–4pm)", "بعد الظهر (13–16)"],
            ["عصر (۱۶ تا ۱۹)", "Akşam (16:00–19:00)", "Später Nachmittag (16–19 Uhr)", "Evening (4–7pm)", "مساءً (16–19)"]
        ];
        var order = 0;
        foreach (var labels in slots)
        {
            var slot = new TimeSlot { SortOrder = ++order };
            for (var i = 0; i < Langs.Length; i++)
                slot.Translations.Add(new TimeSlotTranslation { LanguageCode = Langs[i], Label = labels[i] });
            db.TimeSlots.Add(slot);
        }
        await db.SaveChangesAsync();
    }

    private async Task SeedGalleryCategoriesAsync()
    {
        string[][] categories =
        [
            ["نمونه‌کارها", "Çalışmalar", "Arbeiten", "Our Work", "أعمالنا"],
            ["نتایج", "Sonuçlar", "Ergebnisse", "Results", "النتائج"],
            ["فضای کار", "Çalışma Ortamı", "Räumlichkeiten", "Our Space", "المكان"],
            ["محصولات", "Ürünler", "Produkte", "Products", "المنتجات"],
            ["فیشیال", "Facial", "Facial", "Facial", "العناية بالوجه"]
        ];
        var order = 0;
        foreach (var names in categories)
        {
            var cat = new GalleryCategory { SortOrder = ++order };
            for (var i = 0; i < Langs.Length; i++)
                cat.Translations.Add(new GalleryCategoryTranslation { LanguageCode = Langs[i], Name = names[i] });
            db.GalleryCategories.Add(cat);
        }
        await db.SaveChangesAsync();
    }

    private async Task SeedListItemsAsync()
    {
        string[][] expertise =
        [
            ["فیشیال‌های تخصصی و شخصی‌سازی‌شده", "Kişiye özel profesyonel facial bakımları", "Individuelle, professionelle Facials", "Personalised professional facials", "جلسات عناية احترافية مخصّصة للوجه"],
            ["بررسی و شناخت وضعیت پوست", "Cilt analizi ve değerlendirmesi", "Hautanalyse und Hautberatung", "Skin analysis and consultation", "تحليل البشرة والاستشارة"],
            ["لایه‌برداری‌های ملایم و تخصصی", "Nazik ve profesyonel peeling uygulamaları", "Sanfte und professionelle Peelings", "Gentle and professional exfoliation", "تقشير لطيف واحترافي"],
            ["میکروکارنت و کانتورینگ صورت", "Microcurrent ve yüz konturlama", "Microcurrent und Gesichtskonturierung", "Microcurrent and facial contouring", "الميكروكرنت ونحت ملامح الوجه"]
        ];
        var order = 0;
        foreach (var texts in expertise)
        {
            var item = new ListItem { ListKey = ListKeys.Expertise, SortOrder = ++order };
            for (var i = 0; i < Langs.Length; i++)
                item.Translations.Add(new ListItemTranslation { LanguageCode = Langs[i], Text = texts[i] });
            db.ListItems.Add(item);
        }
        await db.SaveChangesAsync();
    }

    private async Task SeedPagesAsync()
    {
        db.Pages.Add(LegalPage(SystemPages.Impressum, 1,
            [
                ("fa", "Impressum", "impressum", ImpressumFa),
                ("tr", "Künye (Impressum)", "impressum", ImpressumTr),
                ("de", "Impressum", "impressum", ImpressumDe),
                ("en", "Legal Notice (Impressum)", "impressum", ImpressumEn),
                ("ar", "البيانات القانونية", "impressum", ImpressumAr)
            ]));
        db.Pages.Add(LegalPage(SystemPages.Privacy, 2,
            [
                ("fa", "حریم خصوصی", "privacy", LegalTexts.Privacy["fa"]),
                ("tr", "Gizlilik Politikası", "gizlilik", LegalTexts.Privacy["tr"]),
                ("de", "Datenschutzerklärung", "datenschutz", LegalTexts.Privacy["de"]),
                ("en", "Privacy Policy", "privacy", LegalTexts.Privacy["en"]),
                ("ar", "سياسة الخصوصية", "privacy", LegalTexts.Privacy["ar"])
            ]));
        await db.SaveChangesAsync();
    }

    private static Page LegalPage(string key, int order, (string Lang, string Title, string Slug, string Html)[] t)
    {
        var page = new Page { SystemKey = key, SortOrder = order, ShowInFooter = true, IsVisible = true };
        foreach (var x in t)
            page.Translations.Add(new PageTranslation { LanguageCode = x.Lang, Title = x.Title, Slug = x.Slug, Content = x.Html });
        return page;
    }

    // ---- Draft legal texts. The panel shows a note: "Have this text checked by an expert."
    private const string ImpressumDe =
        "<h2>Angaben gemäß § 5 DDG</h2><p>[Vor- und Nachname]<br>Beauty by Negin<br>[Straße und Hausnummer]<br>[PLZ Ort]<br>Deutschland</p>" +
        "<h2>Kontakt</h2><p>Telefon: [Telefonnummer]<br>E-Mail: [E-Mail-Adresse]</p>" +
        "<h2>Umsatzsteuer</h2><p>[Umsatzsteuer-Identifikationsnummer gemäß § 27a UStG, falls vorhanden]</p>" +
        "<h2>Verantwortlich für den Inhalt</h2><p>[Vor- und Nachname, Anschrift wie oben]</p>" +
        "<h2>Verbraucherstreitbeilegung</h2><p>Wir sind nicht bereit oder verpflichtet, an Streitbeilegungsverfahren vor einer Verbraucherschlichtungsstelle teilzunehmen.</p>";
    private const string ImpressumEn =
        "<p>Legal information pursuant to German law (§ 5 DDG).</p><p>[Full name]<br>Beauty by Negin<br>[Street and number]<br>[Postcode, City]<br>[Country]</p>" +
        "<h2>Contact</h2><p>Phone: [Phone number]<br>Email: [Email address]</p>" +
        "<h2>Responsible for content</h2><p>[Full name, address as above]</p>";
    private const string ImpressumTr =
        "<p>Alman mevzuatına (§ 5 DDG) göre yasal bilgiler.</p><p>[Ad Soyad]<br>Beauty by Negin<br>[Sokak ve numara]<br>[Posta kodu, Şehir]<br>[Ülke]</p>" +
        "<h2>İletişim</h2><p>Telefon: [Telefon numarası]<br>E-posta: [E-posta adresi]</p>";
    private const string ImpressumFa =
        "<p>اطلاعات حقوقی صاحب سایت (طبق قوانین آلمان، § 5 DDG).</p><p>[نام و نام خانوادگی]<br>Beauty by Negin<br>[آدرس]<br>[کد پستی، شهر]<br>[کشور]</p>" +
        "<h2>تماس</h2><p>تلفن: [شماره تلفن]<br>ایمیل: [آدرس ایمیل]</p>";

    private const string ImpressumAr =
        "<p>البيانات القانونية لصاحب الموقع (وفقاً للقانون الألماني، § 5 DDG).</p><p>[الاسم الكامل]<br>Beauty by Negin<br>[العنوان]<br>[الرمز البريدي، المدينة]<br>[الدولة]</p>" +
        "<h2>التواصل</h2><p>الهاتف: [رقم الهاتف]<br>البريد الإلكتروني: [البريد الإلكتروني]</p>";

}
