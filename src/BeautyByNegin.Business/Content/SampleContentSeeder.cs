using BeautyByNegin.Business.Media;
using BeautyByNegin.DataAccess;
using BeautyByNegin.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeautyByNegin.Business.Content;

/// <summary>
/// Adds the starter images (brand-palette artwork, to be replaced by real photos in the panel),
/// sample gallery and Instagram items and the "aftercare" example page.
/// Runs ONCE per database (also on databases created by an older version) and only fills empty places:
/// whatever the admin later deletes or replaces is never added again.
/// </summary>
public sealed class SampleContentSeeder(AppDbContext db, IMediaService media, ISiteCache cache, ILogger<SampleContentSeeder> logger)
{
    private const string Version = "1";
    private static readonly string[] Langs = ["fa", "tr", "de", "en", "ar"];

    public async Task SeedAsync(string sampleFolder, CancellationToken ct = default)
    {
        var flag = await db.SiteSettings.FirstOrDefaultAsync(s => s.Key == SettingKeys.SampleContentVersion, ct);
        if (flag?.Value == Version) return;
        if (!Directory.Exists(sampleFolder)) { logger.LogWarning("Sample content folder not found: {Folder}", sampleFolder); return; }

        async Task<int?> Image(string file, string[] alt)
        {
            var path = Path.Combine(sampleFolder, file);
            if (!File.Exists(path)) return null;
            await using var stream = File.OpenRead(path);
            var result = await media.SaveAsync(stream, file, null, 1600, ct);
            if (!result.Ok || result.Image is null) return null;
            await media.SetAltTextAsync(result.Image.Id, Langs.Zip(alt).ToDictionary(x => x.First, x => (string?)x.Second), ct);
            return result.Image.Id;
        }

        // ---- page images (settings)
        var settings = await db.SiteSettings.ToDictionaryAsync(s => s.Key, ct);
        async Task SetImage(string key, string file, string[] alt)
        {
            if (settings.TryGetValue(key, out var s) && !string.IsNullOrWhiteSpace(s.Value)) return;
            var id = await Image(file, alt);
            if (id is null) return;
            if (s is null) db.SiteSettings.Add(s = new SiteSetting { Key = key });
            s.Value = id.Value.ToString();
        }
        await SetImage(SettingKeys.HeroImageId, "hero.jpg", Alt.Hero);
        await SetImage(SettingKeys.AboutImageId, "about.jpg", Alt.About);
        await SetImage(SettingKeys.ConsultationImageId, "consultation.jpg", Alt.Consultation);
        await SetImage(SettingKeys.ShareImageId, "share.jpg", Alt.Share);
        await db.SaveChangesAsync(ct);

        // ---- cover image per service (only services that have none)
        var services = await db.Services.Include(s => s.Translations).ToListAsync(ct);
        foreach (var service in services.Where(s => s.CoverImageId is null))
        {
            var slug = service.Translations.Select(t => t.Slug).FirstOrDefault(s => ServiceFiles.ContainsKey(s));
            if (slug is null) continue;
            var name = service.Translations.First().Name;
            var id = await Image(ServiceFiles[slug], Langs.Select(l => Alt.ServiceAlt(l, name)).ToArray());
            if (id is not null) service.CoverImageId = id.Value;
        }
        await db.SaveChangesAsync(ct);

        // ---- gallery (only when it is completely empty)
        if (!await db.GalleryItems.IgnoreQueryFilters().AnyAsync(ct))
        {
            var categories = await db.GalleryCategories.OrderBy(c => c.SortOrder).Select(c => c.Id).ToListAsync(ct);
            // category index: 0 work, 1 results, 2 space, 3 products, 4 facial (no fake "results")
            (string File, int Cat, bool Home, string[] Caption)[] items =
            [
                ("gallery-0.jpg", 3, true, Alt.G0), ("gallery-1.jpg", 2, true, Alt.G1), ("gallery-2.jpg", 3, true, Alt.G2), ("gallery-3.jpg", 4, true, Alt.G3),
                ("gallery-4.jpg", 4, true, Alt.G4), ("gallery-5.jpg", 2, true, Alt.G5), ("gallery-6.jpg", 0, false, Alt.G6), ("gallery-7.jpg", 0, false, Alt.G7),
            ];
            var order = 0;
            foreach (var g in items)
            {
                var id = await Image(g.File, g.Caption);
                if (id is null) continue;
                var item = new GalleryItem { ImageId = id.Value, SortOrder = ++order, ShowOnHome = g.Home, CategoryId = categories.Count > g.Cat ? categories[g.Cat] : null };
                for (var i = 0; i < Langs.Length; i++) item.Translations.Add(new GalleryItemTranslation { LanguageCode = Langs[i], Caption = g.Caption[i] });
                db.GalleryItems.Add(item);
            }
            await db.SaveChangesAsync(ct);
        }

        // ---- Instagram block (only when empty)
        if (!await db.InstagramPosts.IgnoreQueryFilters().AnyAsync(ct))
        {
            for (var k = 0; k < 6; k++)
            {
                var id = await Image($"insta-{k}.jpg", Alt.Insta);
                if (id is not null) db.InstagramPosts.Add(new InstagramPost { ImageId = id.Value, SortOrder = k + 1 });
            }
            await db.SaveChangesAsync(ct);
        }

        // ---- example custom page: aftercare tips (only when no page uses that address yet)
        if (!await db.PageTranslations.AnyAsync(t => t.Slug == "aftercare" || t.Slug == "pflegetipps" || t.Slug == "bakim-sonrasi", ct))
        {
            var page = new Page { SortOrder = 10, ShowInFooter = true, IsVisible = true };
            foreach (var (lang, title, slug, html) in Aftercare.All)
                page.Translations.Add(new PageTranslation { LanguageCode = lang, Title = title, Slug = slug, Content = html });
            db.Pages.Add(page);
            await db.SaveChangesAsync(ct);
        }

        if (flag is null) db.SiteSettings.Add(new SiteSetting { Key = SettingKeys.SampleContentVersion, Value = Version });
        else flag.Value = Version;
        await db.SaveChangesAsync(ct);
        cache.InvalidateAll();
        logger.LogInformation("Sample images and content added");
    }

    private static readonly Dictionary<string, string> ServiceFiles = new()
    {
        ["carboxy-facial"] = "service-01.jpg", ["classic-facial"] = "service-02.jpg", ["acid-therapy"] = "service-03.jpg",
        ["plagen-treatment"] = "service-04.jpg", ["dermaplaning"] = "service-05.jpg", ["enzyme-mask"] = "service-06.jpg",
        ["microcurrent"] = "service-07.jpg", ["derma-f"] = "service-08.jpg", ["pore-tightening"] = "service-09.jpg",
        ["facial-contouring"] = "service-10.jpg",
    };

    /// <summary>Alternative texts / captions (fa, tr, de, en, ar).</summary>
    private static class Alt
    {
        public static readonly string[] Hero = ["محصولات مراقبت از پوست در نور ملایم صبح", "Sabah ışığında cilt bakım ürünleri", "Hautpflegeprodukte im sanften Morgenlicht", "Skincare products in soft morning light", "منتجات العناية بالبشرة في ضوء الصباح الهادئ"];
        public static readonly string[] About = ["فضای آرام Beauty by Negin", "Beauty by Negin'in huzurlu ortamı", "Die ruhige Atmosphäre von Beauty by Negin", "The calm atmosphere of Beauty by Negin", "الأجواء الهادئة في Beauty by Negin"];
        public static readonly string[] Consultation = ["مشاوره‌ی شخصی پوست", "Kişiye özel cilt danışmanlığı", "Persönliche Hautberatung", "Personal skin consultation", "استشارة شخصية للبشرة"];
        public static readonly string[] Share = ["Beauty by Negin – فیشیال و مراقبت پوست", "Beauty by Negin – Facial ve cilt bakımı", "Beauty by Negin – Facials & Hautpflege", "Beauty by Negin – Facial & Skincare", "Beauty by Negin – العناية بالوجه والبشرة"];
        public static readonly string[] Insta = ["پستی از اینستاگرام Beauty by Negin", "Beauty by Negin Instagram gönderisi", "Instagram-Beitrag von Beauty by Negin", "Instagram post by Beauty by Negin", "منشور من إنستغرام Beauty by Negin"];

        public static string ServiceAlt(string lang, string name) => lang switch
        {
            "fa" => $"{name} – مراقبت تخصصی پوست",
            "tr" => $"{name} – profesyonel cilt bakımı",
            "de" => $"{name} – professionelle Hautpflege",
            "ar" => $"{name} – عناية احترافية بالبشرة",
            _ => $"{name} – professional skincare",
        };

        public static readonly string[] G0 = ["سرم‌ها و کرم‌های منتخب", "Seçili serumlar ve kremler", "Ausgewählte Seren und Cremes", "Selected serums and creams", "أمصال وكريمات مختارة"];
        public static readonly string[] G1 = ["حوله‌های تمیز و آماده برای هر جلسه", "Her seans için temiz havlular", "Frische Handtücher für jede Sitzung", "Fresh towels for every session", "مناشف نظيفة لكل جلسة"];
        public static readonly string[] G2 = ["محصولاتی که در جلسات استفاده می‌کنیم", "Seanslarda kullandığımız ürünler", "Produkte, die wir in unseren Behandlungen verwenden", "Products we use in our treatments", "منتجات نستخدمها في جلساتنا"];
        public static readonly string[] G3 = ["ابزار ماساژ و کانتورینگ صورت", "Yüz masajı ve konturlama araçları", "Werkzeuge für Gesichtsmassage und Konturierung", "Facial massage and contouring tools", "أدوات تدليك ونحت الوجه"];
        public static readonly string[] G4 = ["آماده‌سازی ماسک برای فیشیال", "Facial için maske hazırlığı", "Vorbereitung der Maske für das Facial", "Preparing the mask for a facial", "تحضير القناع لجلسة العناية بالوجه"];
        public static readonly string[] G5 = ["گوشه‌ای از فضای کار", "Çalışma ortamımızdan bir köşe", "Ein Blick in unsere Räume", "A corner of our space", "زاوية من مكان عملنا"];
        public static readonly string[] G6 = ["سرم مخصوص فیشیال کربوکسی", "Carboxy facial serumu", "Serum für das Carboxy Facial", "Serum for the Carboxy Facial", "مصل جلسة الكربوكسي"];
        public static readonly string[] G7 = ["دستگاه میکروکارنت و محصولات همراه", "Microcurrent cihazı ve ürünleri", "Microcurrent-Gerät und passende Pflege", "Microcurrent device and matching care", "جهاز الميكروكرنت ومنتجاته"];
    }

    private static class Aftercare
    {
        public static readonly (string Lang, string Title, string Slug, string Html)[] All =
        [
            ("fa", "مراقبت‌های بعد از فیشیال", "aftercare",
                "<p>بعد از هر جلسه، پوست شما به چند روز مراقبت ملایم نیاز دارد تا نتیجه‌ی بهتری بگیرید. این توصیه‌ها عمومی هستند؛ برای پوست خودتان همیشه توصیه‌های شخصی جلسه را در اولویت بگذارید.</p>" +
                "<h2>۲۴ ساعت اول</h2><ul><li>تا چند ساعت آرایش نکنید تا پوست نفس بکشد.</li><li>صورت را با آب ولرم و یک شوینده‌ی ملایم بشویید و به آرامی خشک کنید.</li><li>از سونا، استخر و ورزش سنگین خودداری کنید.</li></ul>" +
                "<h2>چند روز بعد</h2><ul><li>هر روز ضدآفتاب با SPF مناسب بزنید، حتی در روزهای ابری.</li><li>تا چند روز از لایه‌بردارها و محصولات اسیدی در خانه استفاده نکنید.</li><li>پوست را با یک مرطوب‌کننده‌ی ساده و بدون عطر مرطوب نگه دارید.</li><li>آب کافی بنوشید.</li></ul>" +
                "<h2>چه زمانی با ما تماس بگیرید؟</h2><p>اگر قرمزی یا حساسیت بیشتر از یکی دو روز طول کشید، یا سؤالی داشتید، از طریق چت سایت، واتس‌اپ یا تلفن با ما در ارتباط باشید.</p>"),
            ("tr", "Facial sonrası bakım önerileri", "bakim-sonrasi",
                "<p>Her seanstan sonra cildiniz, en iyi sonucu vermesi için birkaç gün nazik bir bakıma ihtiyaç duyar. Bu öneriler geneldir; seansta size özel verilen tavsiyeler her zaman önceliklidir.</p>" +
                "<h2>İlk 24 saat</h2><ul><li>Cildinizin nefes alması için birkaç saat makyaj yapmayın.</li><li>Yüzünüzü ılık su ve hafif bir temizleyiciyle yıkayın, nazikçe kurulayın.</li><li>Sauna, havuz ve yoğun spordan kaçının.</li></ul>" +
                "<h2>Sonraki günler</h2><ul><li>Bulutlu günlerde bile her gün uygun SPF'li güneş koruyucu kullanın.</li><li>Birkaç gün evde peeling ve asitli ürünler kullanmayın.</li><li>Cildinizi sade, parfümsüz bir nemlendiriciyle nemli tutun.</li><li>Yeterince su için.</li></ul>" +
                "<h2>Ne zaman bize ulaşmalısınız?</h2><p>Kızarıklık veya hassasiyet bir iki günden uzun sürerse ya da bir sorunuz olursa site sohbeti, WhatsApp veya telefonla bize ulaşın.</p>"),
            ("de", "Pflegetipps nach dem Facial", "pflegetipps",
                "<p>Nach jeder Behandlung freut sich Ihre Haut über ein paar Tage sanfte Pflege – so kommt das Ergebnis am besten zur Geltung. Diese Hinweise sind allgemein; die persönlichen Empfehlungen aus Ihrer Sitzung haben immer Vorrang.</p>" +
                "<h2>Die ersten 24 Stunden</h2><ul><li>Einige Stunden auf Make-up verzichten, damit die Haut atmen kann.</li><li>Das Gesicht mit lauwarmem Wasser und einem milden Reinigungsprodukt waschen und sanft abtupfen.</li><li>Sauna, Schwimmbad und intensiven Sport meiden.</li></ul>" +
                "<h2>Die folgenden Tage</h2><ul><li>Täglich Sonnenschutz mit passendem LSF verwenden – auch an bewölkten Tagen.</li><li>Einige Tage keine Peelings oder säurehaltigen Produkte zu Hause anwenden.</li><li>Die Haut mit einer einfachen, parfümfreien Feuchtigkeitspflege versorgen.</li><li>Ausreichend Wasser trinken.</li></ul>" +
                "<h2>Wann sollten Sie sich melden?</h2><p>Wenn Rötungen oder Empfindlichkeit länger als ein, zwei Tage anhalten oder Sie Fragen haben, erreichen Sie uns über den Chat, WhatsApp oder telefonisch.</p>"),
            ("en", "Aftercare tips", "aftercare",
                "<p>After every treatment your skin enjoys a few days of gentle care – that is how the result shows at its best. These tips are general; the personal advice from your session always comes first.</p>" +
                "<h2>The first 24 hours</h2><ul><li>Skip make-up for a few hours so your skin can breathe.</li><li>Wash your face with lukewarm water and a mild cleanser and pat it dry gently.</li><li>Avoid sauna, swimming pools and intense workouts.</li></ul>" +
                "<h2>The following days</h2><ul><li>Use sunscreen with a suitable SPF every day – even when it is cloudy.</li><li>Pause home peels and acid products for a few days.</li><li>Keep your skin hydrated with a simple, fragrance-free moisturiser.</li><li>Drink enough water.</li></ul>" +
                "<h2>When should you contact us?</h2><p>If redness or sensitivity lasts longer than a day or two, or if you have any questions, reach us via the website chat, WhatsApp or phone.</p>"),
            ("ar", "نصائح العناية بعد الجلسة", "aftercare",
                "<p>بعد كل جلسة تحتاج بشرتك إلى بضعة أيام من العناية اللطيفة لتظهر النتيجة بأفضل شكل. هذه النصائح عامة؛ وتبقى التوصيات الشخصية التي تحصلين عليها في الجلسة هي الأولى دائماً.</p>" +
                "<h2>أول ٢٤ ساعة</h2><ul><li>تجنّبي المكياج لبضع ساعات لتتنفس البشرة.</li><li>اغسلي الوجه بماء فاتر ومنظّف لطيف وجفّفيه برفق.</li><li>تجنّبي الساونا والمسبح والرياضة المجهدة.</li></ul>" +
                "<h2>الأيام التالية</h2><ul><li>استخدمي واقي الشمس بعامل حماية مناسب يومياً حتى في الأيام الغائمة.</li><li>توقّفي عن المقشّرات والمنتجات الحمضية في المنزل لبضعة أيام.</li><li>حافظي على ترطيب البشرة بمرطّب بسيط خالٍ من العطور.</li><li>اشربي كمية كافية من الماء.</li></ul>" +
                "<h2>متى تتواصلين معنا؟</h2><p>إذا استمر الاحمرار أو التحسس أكثر من يوم أو يومين، أو كان لديك أي سؤال، تواصلي معنا عبر دردشة الموقع أو واتساب أو الهاتف.</p>"),
        ];
    }
}
