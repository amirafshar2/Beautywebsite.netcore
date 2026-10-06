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
            [SettingKeys.SmtpPort] = "587",
            [SettingKeys.SmtpUseSsl] = "true",
            [SettingKeys.SmtpFromName] = "Beauty by Negin",
            [SettingKeys.TelegramEnabled] = "false",
            [SettingKeys.TelegramNotifyAppointments] = "true",
            [SettingKeys.TelegramNotifyMessages] = "true",
            [SettingKeys.TelegramNotifyChat] = "true",
            [SettingKeys.TelegramNotifyNewsletter] = "true",
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
                ("fa", "حریم خصوصی", "privacy", PrivacyFa),
                ("tr", "Gizlilik Politikası", "gizlilik", PrivacyTr),
                ("de", "Datenschutzerklärung", "datenschutz", PrivacyDe),
                ("en", "Privacy Policy", "privacy", PrivacyEn),
                ("ar", "سياسة الخصوصية", "privacy", PrivacyAr)
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
    private const string PrivacyAr =
        "<h2>١. الجهة المسؤولة</h2><p>[الاسم الكامل]، Beauty by Negin، [العنوان]، [البريد الإلكتروني]</p>" +
        "<h2>٢. لا تتبّع</h2><p>لا يستخدم هذا الموقع أي خدمات تحليل أو تتبّع أو ملفات تعريف ارتباط إعلانية أو محتوى من أطراف ثالثة. تُستخدم ملفات تعريف الارتباط الضرورية فقط لأمان النماذج والدردشة.</p>" +
        "<h2>٣. طلبات التواصل والحجز والدردشة</h2><p>تُستخدم البيانات التي ترسلها عبر النماذج أو الدردشة (الاسم، الهاتف، البريد الإلكتروني، الرسالة) فقط للرد على طلبك. قد تصلنا إشعارات الطلبات الجديدة عبر البريد الإلكتروني أو تيليجرام.</p>" +
        "<h2>٤. النشرة الإخبارية</h2><p>نحفظ بريدك الإلكتروني للنشرة بناءً على موافقتك، ويمكنك إلغاء الاشتراك في أي وقت.</p>";

    private const string PrivacyDe =
        "<h2>1. Verantwortliche Stelle</h2><p>[Vor- und Nachname], Beauty by Negin, [Anschrift], [E-Mail-Adresse]</p>" +
        "<h2>2. Keine Tracking- oder Analysedienste</h2><p>Diese Website verwendet keine Analyse- oder Trackingdienste, keine Werbe-Cookies und bindet keine externen Inhalte (z. B. Schriftarten, Karten, Social-Media-Plugins) von Drittanbietern ein. Technisch notwendige Cookies werden nur für die Sicherheit von Formularen und den Chat verwendet.</p>" +
        "<h2>3. Kontakt-, Termin- und Chatanfragen</h2><p>Wenn Sie uns über ein Formular oder den Chat kontaktieren, verarbeiten wir Ihre Angaben (z. B. Name, Telefonnummer, E-Mail-Adresse, Nachricht) ausschließlich zur Bearbeitung Ihrer Anfrage (Art. 6 Abs. 1 lit. b DSGVO). Benachrichtigungen über neue Anfragen können per E-Mail bzw. über Telegram an uns weitergeleitet werden.</p>" +
        "<h2>4. Newsletter</h2><p>Für den Newsletter speichern wir Ihre E-Mail-Adresse auf Grundlage Ihrer Einwilligung (Art. 6 Abs. 1 lit. a DSGVO). Sie können sich jederzeit abmelden.</p>" +
        "<h2>5. Server-Logfiles</h2><p>Beim Aufruf der Website werden technisch notwendige Daten (z. B. IP-Adresse, Zeitpunkt, aufgerufene Seite) in Logdateien gespeichert und nach spätestens 30 Tagen gelöscht.</p>" +
        "<h2>6. Ihre Rechte</h2><p>Sie haben das Recht auf Auskunft, Berichtigung, Löschung, Einschränkung der Verarbeitung, Datenübertragbarkeit und Widerspruch sowie das Recht auf Beschwerde bei einer Aufsichtsbehörde.</p>";
    private const string PrivacyEn =
        "<h2>1. Controller</h2><p>[Full name], Beauty by Negin, [Address], [Email address]</p>" +
        "<h2>2. No tracking</h2><p>This website does not use analytics or tracking services, advertising cookies or external third-party content. Technically necessary cookies are only used for form security and the chat.</p>" +
        "<h2>3. Contact, booking and chat requests</h2><p>When you contact us via a form or the chat, we process your details (e.g. name, phone, email, message) solely to handle your request. Notifications about new requests may be forwarded to us by email or Telegram.</p>" +
        "<h2>4. Newsletter</h2><p>We store your email address for the newsletter based on your consent. You can unsubscribe at any time.</p>" +
        "<h2>5. Your rights</h2><p>You have the right to access, rectification, erasure, restriction, data portability and objection, and the right to lodge a complaint with a supervisory authority.</p>";
    private const string PrivacyTr =
        "<h2>1. Sorumlu</h2><p>[Ad Soyad], Beauty by Negin, [Adres], [E-posta adresi]</p>" +
        "<h2>2. Takip yok</h2><p>Bu web sitesi analiz veya takip hizmetleri, reklam çerezleri ya da üçüncü taraf içerikleri kullanmaz. Teknik olarak gerekli çerezler yalnızca form güvenliği ve sohbet için kullanılır.</p>" +
        "<h2>3. İletişim, randevu ve sohbet talepleri</h2><p>Bize bir form veya sohbet üzerinden ulaştığınızda bilgileriniz (ad, telefon, e-posta, mesaj) yalnızca talebinizi yanıtlamak için işlenir. Yeni taleplerle ilgili bildirimler bize e-posta veya Telegram ile iletilebilir.</p>" +
        "<h2>4. Bülten</h2><p>Bülten için e-posta adresinizi onayınıza dayanarak saklarız. Aboneliğinizi istediğiniz zaman iptal edebilirsiniz.</p>";
    private const string PrivacyFa =
        "<h2>۱. مسئول</h2><p>[نام و نام خانوادگی]، Beauty by Negin، [آدرس]، [ایمیل]</p>" +
        "<h2>۲. بدون ردیابی</h2><p>این سایت از هیچ سرویس آمار، ردیابی، کوکی تبلیغاتی یا محتوای شخص ثالث استفاده نمی‌کند. کوکی‌های ضروری فقط برای امنیت فرم‌ها و چت به کار می‌روند.</p>" +
        "<h2>۳. فرم تماس، رزرو و چت</h2><p>اطلاعاتی که از طریق فرم‌ها یا چت می‌فرستید (نام، تلفن، ایمیل، پیام) فقط برای پاسخ به درخواست شما استفاده می‌شود. اعلان درخواست‌های جدید ممکن است از طریق ایمیل یا تلگرام برای ما ارسال شود.</p>" +
        "<h2>۴. خبرنامه</h2><p>ایمیل شما برای خبرنامه با رضایت خودتان ذخیره می‌شود و هر زمان می‌توانید عضویت را لغو کنید.</p>";
}
