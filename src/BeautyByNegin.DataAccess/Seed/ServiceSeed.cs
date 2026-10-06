namespace BeautyByNegin.DataAccess.Seed;

/// <summary>
/// The 10 initial treatments. Persian is the original text (Appendix A of the brief);
/// Turkish, German and English are free, brand-tone translations. Wording deliberately avoids
/// medical promises ("can help", "appearance") — required in Germany (Heilmittelwerbegesetz).
/// Durations are left empty on purpose (filled in by Negin in the panel).
/// </summary>
internal static class ServiceSeed
{
    internal sealed record Text(string Subtitle, string Description, string SuitableFor, string ExpectedResult);

    internal sealed record Item(string Name, string Slug, bool ShowOnHome, Dictionary<string, Text> Texts);

    internal static readonly Item[] Items =
    [
        new("Carboxy Facial", "carboxy-facial", true, new()
        {
            ["fa"] = new("اکسیژن‌رسانی و شادابی پوست",
                "کربوکسی فیشال یک درمان غیرتهاجمی برای کمک به شاداب‌تر، روشن‌تر و یکدست‌تر دیده شدن پوست است. این درمان با استفاده از CO₂ موضعی انجام می‌شود و می‌تواند به بهبود ظاهر پوست کدر و خسته کمک کند.",
                "پوست‌های کدر، خسته و بی‌روح",
                "ظاهری شاداب‌تر، روشن‌تر و باطراوت‌تر"),
            ["tr"] = new("Cilde oksijen ve canlılık",
                "Carboxy Facial, cildin daha canlı, daha aydınlık ve daha eşit tonlu görünmesine yardımcı olan, girişimsel olmayan bir bakımdır. Lokal CO₂ ile uygulanır ve donuk, yorgun görünen ciltlerin görünümünü iyileştirmeye yardımcı olabilir.",
                "Donuk, yorgun ve cansız görünen ciltler",
                "Daha canlı, daha aydınlık ve daha taze bir görünüm"),
            ["de"] = new("Sauerstoff und Frische für die Haut",
                "Das Carboxy Facial ist eine nicht-invasive Behandlung, die dazu beitragen kann, dass die Haut frischer, strahlender und ebenmäßiger wirkt. Sie wird mit topischem CO₂ durchgeführt und kann das Erscheinungsbild fahler, müder Haut sichtbar verfeinern.",
                "Fahle, müde und glanzlose Haut",
                "Ein frischeres, strahlenderes und lebendigeres Hautbild"),
            ["en"] = new("Oxygenating care for a fresh complexion",
                "The Carboxy Facial is a non-invasive treatment designed to help the skin look fresher, brighter and more even. Performed with topical CO₂, it can help refresh the appearance of dull, tired-looking skin.",
                "Dull, tired and lacklustre skin",
                "A fresher, brighter and more radiant look")
        }),

        new("Classic Facial", "classic-facial", true, new()
        {
            ["fa"] = new("پاکسازی و مراقبت کامل از پوست",
                "فیشال کلاسیک یک روتین کامل مراقبت از پوست است که با توجه به نوع و شرایط پوست انجام می‌شود. پاکسازی، لایه‌برداری ملایم، آماده‌سازی پوست، ماسک و مراقبت نهایی، بخش‌هایی از این درمان هستند.",
                "انواع پوست",
                "پوست تمیزتر، نرم‌تر و شاداب‌تر"),
            ["tr"] = new("Derinlemesine temizlik ve eksiksiz bakım",
                "Classic Facial, cilt tipinize ve cildinizin o günkü ihtiyacına göre uygulanan eksiksiz bir bakım ritüelidir. Temizleme, nazik peeling, cildin hazırlanması, maske ve son bakım bu ritüelin adımlarıdır.",
                "Tüm cilt tipleri",
                "Daha temiz, daha yumuşak ve daha canlı bir cilt"),
            ["de"] = new("Reinigung und umfassende Pflege",
                "Das Classic Facial ist ein vollständiges Pflegeritual, das individuell auf Hauttyp und Hautzustand abgestimmt wird. Reinigung, sanftes Peeling, Vorbereitung der Haut, Maske und abschließende Pflege gehören zu dieser Behandlung.",
                "Alle Hauttypen",
                "Ein reineres, weicheres und frischeres Hautgefühl"),
            ["en"] = new("Deep cleansing and complete care",
                "The Classic Facial is a complete skincare ritual tailored to your skin type and condition. Cleansing, gentle exfoliation, skin preparation, a mask and finishing care are all part of the treatment.",
                "All skin types",
                "Cleaner, softer and fresher-looking skin")
        }),

        new("Acid Therapy", "acid-therapy", true, new()
        {
            ["fa"] = new("لایه‌برداری تخصصی برای پوستی صاف‌تر و روشن‌تر",
                "اسیدتراپی با استفاده از اسیدهای مناسب پوست، به حذف سلول‌های مرده سطح پوست و بهبود ظاهر بافت و شفافیت آن کمک می‌کند. نوع و غلظت محصول باید بر اساس شرایط پوست انتخاب شود.",
                "پوست‌های کدر، ناهموار و دارای بافت نامنظم",
                "پوست صاف‌تر، روشن‌تر و یکدست‌تر"),
            ["tr"] = new("Daha pürüzsüz ve aydınlık bir cilt için profesyonel peeling",
                "Acid Therapy, cilde uygun asitlerle yüzeydeki ölü hücrelerin uzaklaştırılmasına ve cilt dokusu ile berraklığının görünümünü iyileştirmeye yardımcı olur. Ürünün türü ve yoğunluğu cildin durumuna göre seçilir.",
                "Donuk, pürüzlü ve düzensiz dokulu ciltler",
                "Daha pürüzsüz, daha aydınlık ve daha eşit tonlu bir cilt"),
            ["de"] = new("Professionelles Peeling für ein glatteres, klareres Hautbild",
                "Bei der Acid Therapy helfen sorgfältig ausgewählte Säuren, abgestorbene Hautschüppchen zu lösen und Struktur sowie Klarheit der Haut optisch zu verfeinern. Art und Konzentration des Produkts werden individuell nach dem Hautzustand gewählt.",
                "Fahle, unebene Haut mit unregelmäßiger Struktur",
                "Ein glatteres, klareres und ebenmäßigeres Hautbild"),
            ["en"] = new("Professional exfoliation for smoother, brighter skin",
                "Acid Therapy uses skin-appropriate acids to help remove dead surface cells and refine the look of texture and clarity. The type and strength of the product are always chosen according to your skin's condition.",
                "Dull, uneven skin with irregular texture",
                "Smoother, brighter and more even-looking skin")
        }),

        new("Plagen Treatment", "plagen-treatment", true, new()
        {
            ["fa"] = new("مراقبت و تقویت ظاهر پوست",
                "یک درمان مراقبتی برای افزایش حس نرمی و شادابی پوست که با استفاده از محصولات و ماسک‌های تخصصی انجام می‌شود.\nاین درمان می‌تواند به پوست ظاهری نرم‌تر، شاداب‌تر و باطراوت‌تر بدهد.",
                "پوست‌های خشک، کدر و کم‌آب",
                "ظاهری نرم، شاداب و درخشان"),
            ["tr"] = new("Cildin görünümünü destekleyen bakım",
                "Profesyonel ürünler ve özel maskelerle uygulanan, cildin yumuşaklık ve canlılık hissini artırmaya yönelik bir bakımdır.\nCildinize daha yumuşak, daha canlı ve daha taze bir görünüm kazandırabilir.",
                "Kuru, donuk ve nem ihtiyacı olan ciltler",
                "Yumuşak, canlı ve ışıltılı bir görünüm"),
            ["de"] = new("Pflege, die das Hautbild stärkt",
                "Eine pflegende Behandlung mit professionellen Produkten und speziellen Masken, die das Gefühl von Geschmeidigkeit und Frische der Haut unterstützt.\nSie kann der Haut ein weicheres, frischeres und vitaleres Aussehen verleihen.",
                "Trockene, fahle und feuchtigkeitsarme Haut",
                "Ein geschmeidiges, frisches und strahlendes Aussehen"),
            ["en"] = new("Care that supports the look of your skin",
                "A nourishing treatment using professional products and specialised masks to enhance the feeling of softness and freshness in the skin.\nIt can give the skin a softer, fresher and more revitalised appearance.",
                "Dry, dull and dehydrated skin",
                "A soft, fresh and luminous look")
        }),

        new("Dermaplaning", "dermaplaning", true, new()
        {
            ["fa"] = new("لایه‌برداری سطحی و ایجاد ظاهری صاف و یکدست",
                "در درماپلنینگ، سلول‌های مرده سطح پوست و موهای کرکی صورت با یک ابزار تخصصی به‌آرامی برداشته می‌شوند.\nاین کار می‌تواند سطح پوست را صاف‌تر کرده و ظاهر آرایش را نیز یکدست‌تر کند.",
                "افرادی که به دنبال سطح پوستی صاف‌تر و شفاف‌تر هستند.",
                "پوست نرم‌تر، صاف‌تر و درخشان‌تر"),
            ["tr"] = new("Yüzeysel peeling ile pürüzsüz ve eşit bir görünüm",
                "Dermaplaning'de cilt yüzeyindeki ölü hücreler ve yüzdeki ince tüyler özel bir aletle nazikçe alınır.\nBu, cilt yüzeyini daha pürüzsüz hale getirebilir ve makyajın da daha eşit görünmesini sağlar.",
                "Daha pürüzsüz ve daha berrak bir cilt yüzeyi isteyenler",
                "Daha yumuşak, daha pürüzsüz ve daha ışıltılı bir cilt"),
            ["de"] = new("Sanfte Oberflächenpflege für ein glattes, ebenmäßiges Aussehen",
                "Beim Dermaplaning werden abgestorbene Hautschüppchen und feine Gesichtshärchen mit einem speziellen Instrument behutsam entfernt.\nDie Hautoberfläche kann dadurch glatter wirken, und auch Make-up lässt sich gleichmäßiger auftragen.",
                "Alle, die sich eine glattere und klarere Hautoberfläche wünschen",
                "Weichere, glattere und strahlendere Haut"),
            ["en"] = new("Gentle resurfacing for a smooth, even look",
                "In dermaplaning, dead surface cells and fine facial hair are gently removed with a specialised instrument.\nThis can leave the skin's surface smoother and help make-up sit more evenly.",
                "Anyone looking for a smoother, clearer skin surface",
                "Softer, smoother and more radiant skin")
        }),

        new("Enzyme Mask", "enzyme-mask", true, new()
        {
            ["fa"] = new("لایه‌برداری ملایم و شادابی پوست",
                "ماسک آنزیمی با استفاده از آنزیم‌های مناسب، به لایه‌برداری ملایم سطح پوست کمک می‌کند و می‌تواند ظاهر پوست را شفاف‌تر و شاداب‌تر نشان دهد.\nدر صورت نیاز، این درمان می‌تواند با مراقبت‌های تکمیلی و تکنیک‌های کانتورینگ صورت همراه شود.",
                "پوست‌های کدر و خسته",
                "ظاهری شفاف‌تر، نرم‌تر و شاداب‌تر"),
            ["tr"] = new("Nazik peeling ve canlı bir cilt",
                "Enzim maskesi, uygun enzimlerle cilt yüzeyinin nazikçe arındırılmasına yardımcı olur ve cildin daha berrak ve canlı görünmesini sağlayabilir.\nGerektiğinde tamamlayıcı bakımlar ve yüz konturlama teknikleriyle birlikte uygulanabilir.",
                "Donuk ve yorgun görünen ciltler",
                "Daha berrak, daha yumuşak ve daha canlı bir görünüm"),
            ["de"] = new("Sanftes Peeling für eine frische Ausstrahlung",
                "Die Enzymmaske unterstützt mit ausgewählten Enzymen ein sanftes Peeling der Hautoberfläche und kann die Haut klarer und frischer wirken lassen.\nBei Bedarf lässt sie sich mit ergänzender Pflege und Techniken der Gesichtskonturierung kombinieren.",
                "Fahle und müde wirkende Haut",
                "Ein klareres, weicheres und frischeres Erscheinungsbild"),
            ["en"] = new("Gentle exfoliation for a fresh glow",
                "The enzyme mask uses carefully selected enzymes to gently exfoliate the skin's surface and can help the complexion look clearer and fresher.\nIf needed, it can be combined with complementary care and facial contouring techniques.",
                "Dull and tired-looking skin",
                "A clearer, softer and fresher look")
        }),

        new("Microcurrent", "microcurrent", true, new()
        {
            ["fa"] = new("فرم‌دهی و تحریک ملایم عضلات صورت",
                "میکروکارنت از جریان‌های الکتریکی بسیار ضعیف برای تحریک ملایم عضلات صورت استفاده می‌کند و می‌تواند به ایجاد ظاهری سفت‌تر و فرم‌گرفته‌تر کمک کند.",
                "افرادی که به دنبال ظاهر لیفت‌شده‌تر و کانتور مشخص‌تر هستند.",
                "ظاهری فرم‌گرفته‌تر و شاداب‌تر"),
            ["tr"] = new("Yüz kaslarının nazikçe uyarılması ve şekillendirme",
                "Microcurrent, yüz kaslarını nazikçe uyarmak için çok düşük yoğunluklu elektrik akımları kullanır ve daha sıkı, daha şekilli bir görünüme katkıda bulunabilir.",
                "Daha kalkık bir görünüm ve daha belirgin yüz hatları isteyenler",
                "Daha şekilli ve daha canlı bir görünüm"),
            ["de"] = new("Sanfte Stimulation für definierte Konturen",
                "Microcurrent nutzt sehr schwache elektrische Impulse, um die Gesichtsmuskulatur sanft zu stimulieren, und kann zu einem strafferen, definierteren Aussehen beitragen.",
                "Alle, die sich ein gelifteter wirkendes Aussehen und klarere Konturen wünschen",
                "Ein definierteres und frischeres Aussehen"),
            ["en"] = new("Gentle facial muscle stimulation for definition",
                "Microcurrent uses very low-level electrical currents to gently stimulate the facial muscles and can help create a firmer, more sculpted look.",
                "Anyone looking for a more lifted look and more defined contours",
                "A more sculpted and refreshed look")
        }),

        new("Derma F", "derma-f", false, new()
        {
            ["fa"] = new("مراقبت هدفمند برای ظاهر پوست",
                "Derma F یک درمان تخصصی پوستی است که با توجه به تکنیک و محصولات مورد استفاده، برای بهبود ظاهر و کیفیت پوست انجام می‌شود.\nپروتکل درمانی بر اساس نیاز پوست انتخاب می‌شود.",
                "پوست‌هایی که نیاز به مراقبت هدفمند و شخصی‌سازی‌شده دارند.",
                "پوستی شاداب‌تر و با ظاهر سالم‌تر"),
            ["tr"] = new("Cildin görünümüne yönelik hedefli bakım",
                "Derma F, kullanılan teknik ve ürünlere bağlı olarak cildin görünümünü ve kalitesini iyileştirmeye yönelik profesyonel bir cilt bakımıdır.\nUygulama protokolü cildin ihtiyacına göre belirlenir.",
                "Hedefe yönelik ve kişiye özel bakıma ihtiyaç duyan ciltler",
                "Daha canlı ve daha sağlıklı görünen bir cilt"),
            ["de"] = new("Gezielte Pflege für das Hautbild",
                "Derma F ist eine professionelle Hautbehandlung, die – je nach eingesetzter Technik und Produkten – das Erscheinungsbild und die Qualität der Haut verfeinern soll.\nDas Behandlungsprotokoll wird individuell nach den Bedürfnissen der Haut gewählt.",
                "Haut, die gezielte und individuell abgestimmte Pflege braucht",
                "Ein frischeres und gesünder wirkendes Hautbild"),
            ["en"] = new("Targeted care for the look of your skin",
                "Derma F is a professional skin treatment that, depending on the technique and products used, aims to refine the appearance and quality of the skin.\nThe protocol is always chosen according to what your skin needs.",
                "Skin that needs targeted, personalised care",
                "Fresher, healthier-looking skin")
        }),

        new("Pore Tightening", "pore-tightening", false, new()
        {
            ["fa"] = new("مراقبت برای ظاهر منافذ و بافت پوست",
                "این درمان با هدف پاکسازی و مراقبت از پوست و کاهش ظاهر منافذ قابل مشاهده انجام می‌شود. انتخاب محصولات و تکنیک‌ها بر اساس نوع و شرایط پوست صورت می‌گیرد.",
                "پوست‌های چرب، مختلط و دارای منافذ قابل مشاهده",
                "ظاهر صاف‌تر و یکدست‌تر پوست"),
            ["tr"] = new("Gözeneklerin ve cilt dokusunun görünümü için bakım",
                "Bu bakım cildi arındırmak, bakımını yapmak ve belirgin gözeneklerin görünümünü azaltmak amacıyla uygulanır. Ürün ve teknikler cilt tipine ve cildin durumuna göre seçilir.",
                "Yağlı, karma ve gözenekleri belirgin ciltler",
                "Daha pürüzsüz ve daha eşit görünen bir cilt"),
            ["de"] = new("Pflege für ein verfeinertes Porenbild",
                "Diese Behandlung reinigt und pflegt die Haut und zielt darauf ab, sichtbare Poren optisch zu minimieren. Produkte und Techniken werden nach Hauttyp und Hautzustand ausgewählt.",
                "Fettige und Mischhaut mit sichtbaren Poren",
                "Ein glatteres und ebenmäßigeres Hautbild"),
            ["en"] = new("Care for the look of pores and skin texture",
                "This treatment cleanses and cares for the skin with the aim of minimising the appearance of visible pores. Products and techniques are selected according to your skin type and condition.",
                "Oily and combination skin with visible pores",
                "A smoother, more even-looking complexion")
        }),

        new("Facial Contouring", "facial-contouring", false, new()
        {
            ["fa"] = new("کانتورینگ و فرم‌دهی ظاهری صورت",
                "تکنیک‌های کانتورینگ صورت با هدف ایجاد ظاهری متعادل‌تر و فرم‌گرفته‌تر انجام می‌شوند. این درمان می‌تواند همراه با ماساژ تخصصی و تکنیک‌های مناسب صورت انجام شود.",
                "افرادی که به دنبال نمایان‌تر شدن فرم طبیعی صورت هستند.",
                "ظاهری شاداب‌تر و کانتور‌شده‌تر"),
            ["tr"] = new("Yüz hatlarına şekil veren konturlama",
                "Yüz konturlama teknikleri, daha dengeli ve daha şekilli bir görünüm elde etmek amacıyla uygulanır. Bakım, profesyonel masaj ve uygun yüz teknikleriyle birlikte yapılabilir.",
                "Yüzünün doğal hatlarını daha belirgin görmek isteyenler",
                "Daha canlı ve daha belirgin hatlara sahip bir görünüm"),
            ["de"] = new("Konturierung für harmonische Gesichtszüge",
                "Techniken der Gesichtskonturierung sollen ein ausgewogeneres, definierteres Erscheinungsbild schaffen. Die Behandlung kann mit einer professionellen Massage und passenden Gesichtstechniken kombiniert werden.",
                "Alle, die die natürliche Form ihres Gesichts betonen möchten",
                "Ein frischeres und konturierteres Aussehen"),
            ["en"] = new("Contouring that enhances your natural features",
                "Facial contouring techniques aim to create a more balanced, sculpted appearance. The treatment can be combined with a professional facial massage and suitable techniques.",
                "Anyone wishing to bring out the natural shape of their face",
                "A fresher, more contoured look")
        })
    ];

    /// <summary>First paragraph of the description is used as the short card text.</summary>
    internal static string ShortDescription(string description)
    {
        var first = description.Split('\n')[0].Trim();
        return first;
    }

    /// <summary>Plain text with line breaks -> simple paragraphs for the rich text field.</summary>
    internal static string ToHtml(string description)
        => string.Concat(description.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => $"<p>{System.Net.WebUtility.HtmlEncode(p.Trim())}</p>"));
}
