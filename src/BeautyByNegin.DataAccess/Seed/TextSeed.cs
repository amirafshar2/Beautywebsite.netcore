using BeautyByNegin.DataAccess.Entities;

namespace BeautyByNegin.DataAccess.Seed;

/// <summary>
/// Default values of every editable site text in fa / tr / de / en.
/// The seeder only inserts keys/languages that are missing, so new keys can be added in later
/// versions without touching texts the admin already edited.
/// Placeholders in curly braces ({service}, {brand}) are replaced at runtime.
/// </summary>
internal static class TextSeed
{
    internal sealed record Entry(string Key, string Group, TextKind Kind, string Hint, string Fa, string Tr, string De, string En);

    private static Entry L(string key, string group, string hint, string fa, string tr, string de, string en)
        => new(key, group, TextKind.Line, hint, fa, tr, de, en);

    private static Entry M(string key, string group, string hint, string fa, string tr, string de, string en)
        => new(key, group, TextKind.Multiline, hint, fa, tr, de, en);

    internal static readonly Entry[] All =
    [
        // ---------------- Menu
        L("nav.home", "nav", "منوی بالای سایت", "خانه", "Ana Sayfa", "Startseite", "Home"),
        L("nav.about", "nav", "منوی بالای سایت", "درباره ما", "Hakkımızda", "Über uns", "About"),
        L("nav.services", "nav", "منوی بالای سایت", "خدمات", "Hizmetler", "Behandlungen", "Treatments"),
        L("nav.gallery", "nav", "منوی بالای سایت", "گالری", "Galeri", "Galerie", "Gallery"),
        L("nav.reviews", "nav", "منوی بالای سایت", "نظرات", "Yorumlar", "Bewertungen", "Reviews"),
        L("nav.contact", "nav", "منوی بالای سایت", "تماس", "İletişim", "Kontakt", "Contact"),
        L("nav.booking", "nav", "دکمه‌ی نوبت در منوی بالا", "رزرو نوبت", "Randevu Al", "Termin buchen", "Book an Appointment"),
        L("nav.menu", "nav", "دکمه‌ی منو در موبایل", "منو", "Menü", "Menü", "Menu"),
        L("nav.close", "nav", "بستن منو / پنجره", "بستن", "Kapat", "Schließen", "Close"),
        L("nav.skip", "nav", "لینک «رفتن به محتوا» برای کاربران صفحه‌خوان", "رفتن به محتوای اصلی", "İçeriğe geç", "Zum Inhalt springen", "Skip to content"),

        // ---------------- Buttons
        L("btn.book", "buttons", "دکمه‌ی اصلی رزرو", "رزرو نوبت", "Randevu Al", "Termin buchen", "Book an Appointment"),
        L("btn.contact", "buttons", "دکمه‌ی تماس در بالای صفحه‌ی اصلی", "تماس با ما", "Bize Ulaşın", "Kontakt aufnehmen", "Contact Us"),
        L("btn.details", "buttons", "دکمه‌ی روی کارت خدمات", "جزئیات", "Detaylar", "Details", "Details"),
        L("btn.ask", "buttons", "دکمه‌ی پرسش درباره‌ی یک خدمت", "درباره‌ی این خدمت بپرسید", "Bu Bakım Hakkında Sorun", "Zu dieser Behandlung anfragen", "Ask About This Treatment"),
        L("btn.allServices", "buttons", "لینک زیر خدمات صفحه‌ی اصلی", "همه‌ی خدمات", "Tüm Hizmetler", "Alle Behandlungen", "All Treatments"),
        L("btn.allGallery", "buttons", "لینک زیر گالری صفحه‌ی اصلی", "مشاهده‌ی گالری", "Tüm Galeri", "Zur Galerie", "View Gallery"),
        L("btn.allReviews", "buttons", "لینک زیر نظرات صفحه‌ی اصلی", "همه‌ی نظرات", "Tüm Yorumlar", "Alle Bewertungen", "All Reviews"),
        L("btn.about", "buttons", "لینک معرفی کوتاه صفحه‌ی اصلی", "بیشتر درباره‌ی ما", "Hakkımızda Daha Fazla", "Mehr über uns", "More About Us"),
        L("btn.send", "buttons", "دکمه‌ی ارسال فرم‌ها", "ارسال", "Gönder", "Senden", "Send"),
        L("btn.call", "buttons", "دکمه‌ی تماس تلفنی", "تماس تلفنی", "Ara", "Anrufen", "Call Us"),
        L("btn.whatsapp", "buttons", "دکمه‌ی واتس‌اپ", "واتس‌اپ", "WhatsApp", "WhatsApp", "WhatsApp"),
        L("btn.email", "buttons", "دکمه‌ی ایمیل", "ایمیل", "E-posta", "E-Mail", "Email Us"),
        L("btn.instagram", "buttons", "دکمه‌ی اینستاگرام", "اینستاگرام", "Instagram", "Instagram", "Instagram"),
        L("btn.telegram", "buttons", "دکمه‌ی تلگرام", "تلگرام", "Telegram", "Telegram", "Telegram"),
        L("btn.bookForm", "buttons", "گزینه‌ی فرم نوبت در پنجره‌ی «درباره‌ی این خدمت بپرسید»", "فرم رزرو نوبت", "Randevu Formu", "Terminanfrage", "Booking Form"),
        L("btn.openMap", "buttons", "لینک نقشه در صفحه‌ی تماس", "نمایش روی نقشه", "Haritada Aç", "In Karte öffnen", "Open in Maps"),
        L("btn.backHome", "buttons", "دکمه‌ی بازگشت در صفحه‌ی خطا", "بازگشت به صفحه‌ی اصلی", "Ana Sayfaya Dön", "Zur Startseite", "Back to Home"),
        L("btn.subscribe", "buttons", "دکمه‌ی عضویت در خبرنامه", "عضویت", "Abone Ol", "Anmelden", "Subscribe"),

        // ---------------- Mobile bottom bar
        L("bar.call", "bar", "نوار ثابت پایین صفحه در موبایل", "تماس", "Ara", "Anrufen", "Call"),
        L("bar.whatsapp", "bar", "نوار ثابت پایین صفحه در موبایل", "واتس‌اپ", "WhatsApp", "WhatsApp", "WhatsApp"),
        L("bar.email", "bar", "نوار ثابت پایین صفحه در موبایل", "ایمیل", "E-posta", "E-Mail", "Email"),
        L("bar.book", "bar", "نوار ثابت پایین صفحه در موبایل", "نوبت", "Randevu", "Termin", "Book"),

        // ---------------- Home page
        L("home.hero.eyebrow", "home", "نوشته‌ی کوچک بالای عنوان بزرگ صفحه‌ی اصلی", "فیشیال و مراقبت تخصصی پوست", "FACIAL & CİLT BAKIMI", "FACIAL & HAUTPFLEGE", "FACIAL & SKINCARE TREATMENTS"),
        L("home.hero.title", "home", "عنوان بزرگ روی عکس اصلی", "Beauty by Negin", "Beauty by Negin", "Beauty by Negin", "Beauty by Negin"),
        L("home.hero.slogan", "home", "شعار زیر عنوان بزرگ روی عکس اصلی", "پوست شما. زیبایی شما. درخشش شما.", "Cildiniz. Güzelliğiniz. Işıltınız.", "Ihre Haut. Ihre Schönheit. Ihr Strahlen.", "Your Skin. Your Beauty. Your Glow."),
        L("home.intro.eyebrow", "home", "نوشته‌ی کوچک بالای معرفی کوتاه", "درباره‌ی ما", "HAKKIMIZDA", "ÜBER UNS", "ABOUT"),
        L("home.intro.title", "home", "عنوان معرفی کوتاه صفحه‌ی اصلی", "مراقبت تخصصی، متناسب با پوست شما", "Cildinize özel, uzman bakım", "Fachkundige Pflege, abgestimmt auf Ihre Haut", "Expert care, tailored to your skin"),
        M("home.intro.text", "home", "دو سه جمله معرفی در صفحه‌ی اصلی",
            "هر پوست نیاز متفاوتی دارد. در Beauty by Negin، خدمات پوستی بر اساس وضعیت و نیاز پوست شما انتخاب می‌شوند تا تجربه‌ای شخصی‌سازی‌شده و حرفه‌ای داشته باشید.",
            "Her cildin ihtiyacı farklıdır. Beauty by Negin'de bakımlar, cildinizin durumuna ve gerçek ihtiyaçlarına göre seçilir; böylece size özel ve profesyonel bir deneyim yaşarsınız.",
            "Jede Haut hat andere Bedürfnisse. Bei Beauty by Negin werden die Behandlungen nach Zustand und Bedürfnissen Ihrer Haut ausgewählt – für ein persönliches und professionelles Erlebnis.",
            "Every skin has different needs. At Beauty by Negin, treatments are chosen according to the condition and needs of your skin, for a personalised and professional experience."),
        L("home.services.eyebrow", "home", "نوشته‌ی کوچک بالای بخش خدمات", "خدمات ما", "BAKIMLARIMIZ", "UNSERE BEHANDLUNGEN", "OUR TREATMENTS"),
        L("home.services.title", "home", "عنوان بخش خدمات صفحه‌ی اصلی", "فیشیال و مراقبت پوست", "Facial ve Cilt Bakımı", "Facials & Hautpflege", "Facial & Skincare Treatments"),
        L("home.gallery.eyebrow", "home", "نوشته‌ی کوچک بالای گالری صفحه‌ی اصلی", "گالری", "GALERİ", "GALERIE", "GALLERY"),
        L("home.gallery.title", "home", "عنوان گالری صفحه‌ی اصلی", "لحظه‌هایی از Beauty by Negin", "Beauty by Negin'den kareler", "Einblicke in Beauty by Negin", "Moments at Beauty by Negin"),
        L("home.reviews.eyebrow", "home", "نوشته‌ی کوچک بالای نظرات", "نظرات مشتریان", "MÜŞTERİ YORUMLARI", "KUNDENSTIMMEN", "CLIENT REVIEWS"),
        L("home.reviews.title", "home", "عنوان نظرات صفحه‌ی اصلی", "تجربه‌ی مراجعین ما", "Danışanlarımızın deneyimleri", "Was unsere Kundinnen sagen", "What our clients say"),
        L("home.instagram.title", "home", "عنوان بخش اینستاگرام", "ما را در اینستاگرام دنبال کنید", "Bizi Instagram'da takip edin", "Folgen Sie uns auf Instagram", "Follow us on Instagram"),
        L("home.newsletter.title", "home", "عنوان بخش خبرنامه", "از تازه‌ها باخبر شوید", "Yeniliklerden haberdar olun", "Bleiben Sie informiert", "Stay in touch"),
        M("home.newsletter.text", "home", "متن کوتاه بخش خبرنامه",
            "برای دریافت نکات مراقبت از پوست و خبرهای Beauty by Negin، ایمیل خود را وارد کنید.",
            "Cilt bakımı önerileri ve Beauty by Negin haberleri için e-posta adresinizi bırakın.",
            "Tragen Sie Ihre E-Mail-Adresse ein und erhalten Sie Pflegetipps und Neuigkeiten von Beauty by Negin.",
            "Leave your email to receive skincare tips and news from Beauty by Negin."),
        L("home.contact.title", "home", "عنوان بخش تماس پایین صفحه‌ی اصلی", "آماده‌ی مراقبت از پوستتان هستیم", "Cildiniz için buradayız", "Wir freuen uns auf Sie", "We look forward to caring for your skin"),
        M("home.contact.text", "home", "متن بخش تماس پایین صفحه‌ی اصلی",
            "برای مشاوره یا رزرو نوبت، تماس بگیرید یا پیام بدهید.",
            "Danışmanlık veya randevu için bizi arayın ya da mesaj gönderin.",
            "Für eine Beratung oder einen Termin rufen Sie uns an oder schreiben Sie uns.",
            "Call or message us for a consultation or an appointment."),

        // ---------------- Consultation block (home + services)
        L("consult.eyebrow", "consultation", "نوشته‌ی کوچک بالای بخش مشاوره", "مشاوره‌ی شخصی پوست", "KİŞİYE ÖZEL CİLT DANIŞMANLIĞI", "PERSÖNLICHE HAUTBERATUNG", "PERSONALIZED SKIN CONSULTATION"),
        L("consult.title", "consultation", "عنوان بخش مشاوره (بخش تیره)", "هر پوست متفاوت است.", "Her cilt farklıdır.", "Jede Haut ist anders.", "Every skin is different."),
        M("consult.text", "consultation", "متن بخش مشاوره",
            "قبل از انتخاب درمان، وضعیت پوست شما بررسی می‌شود تا مناسب‌ترین سرویس بر اساس نیازهای واقعی پوست انتخاب شود.",
            "Bakım seçilmeden önce cildiniz değerlendirilir; böylece cildinizin gerçek ihtiyaçlarına en uygun hizmet belirlenir.",
            "Vor jeder Behandlung wird Ihre Haut genau betrachtet, damit die passende Behandlung nach ihren tatsächlichen Bedürfnissen gewählt werden kann.",
            "Before choosing a treatment, your skin is assessed so that the most suitable service can be selected based on its real needs."),
        L("consult.question", "consultation", "سؤال بالای دکمه‌های تماس در بخش مشاوره", "می‌خواهید بدانید کدام درمان برای شما مناسب است؟", "Hangi bakımın size uygun olduğunu merak mı ediyorsunuz?", "Möchten Sie wissen, welche Behandlung zu Ihnen passt?", "Want to know which treatment is right for you?"),

        // ---------------- About
        L("about.eyebrow", "about", "نوشته‌ی کوچک بالای صفحه‌ی درباره‌ی ما", "درباره‌ی ما", "HAKKIMIZDA", "ÜBER UNS", "ABOUT"),
        L("about.title", "about", "عنوان صفحه‌ی درباره‌ی ما", "نگین؛ متخصص مراقبت از پوست", "Negin – cilt bakım uzmanı", "Negin – Expertin für Hautpflege", "Negin – skincare specialist"),
        M("about.intro", "about", "معرفی Negin در صفحه‌ی درباره‌ی ما",
            "نگین با سال‌ها تجربه در مراقبت تخصصی از پوست، Beauty by Negin را با یک هدف ساده بنا کرد: مراقبتی دقیق، آرام و شخصی برای هر پوست.\nهر جلسه با شناخت پوست شما آغاز می‌شود و مراقبت‌ها با دقت و حوصله، متناسب با نیاز همان روز پوست انجام می‌شوند.",
            "Negin, cilt bakımındaki uzun yıllara dayanan deneyimiyle Beauty by Negin'i basit bir amaçla kurdu: her cilde özenli, huzurlu ve kişiye özel bir bakım.\nHer seans cildinizi tanımakla başlar; bakımlar, cildinizin o günkü ihtiyacına göre titizlikle uygulanır.",
            "Mit langjähriger Erfahrung in der professionellen Hautpflege hat Negin Beauty by Negin mit einem klaren Ziel gegründet: sorgfältige, ruhige und persönliche Pflege für jede Haut.\nJede Sitzung beginnt damit, Ihre Haut kennenzulernen – die Pflege wird dann mit Ruhe und Präzision auf ihre aktuellen Bedürfnisse abgestimmt.",
            "With years of experience in professional skincare, Negin founded Beauty by Negin with one simple aim: careful, calm and personal care for every skin.\nEvery session begins by getting to know your skin, and each treatment is carried out with precision and patience, tailored to what your skin needs that day."),
        L("about.expertise.title", "about", "عنوان فهرست تخصص‌ها", "زمینه‌های تخصص", "Uzmanlık Alanları", "Schwerpunkte", "Areas of Expertise"),
        L("about.philosophy.title", "about", "عنوان بخش فلسفه‌ی کاری", "فلسفه‌ی کاری ما", "Çalışma Felsefemiz", "Unsere Philosophie", "Our Philosophy"),
        M("about.philosophy.text", "about", "متن فلسفه‌ی کاری",
            "خدمات برای هر فرد، بر اساس نیاز و وضعیت پوست او انتخاب می‌شوند. هدف فقط زیباتر دیده شدن نیست؛ هدف، مراقبت درست و تخصصی از پوست است.",
            "Bakımlar her kişi için cildinin ihtiyacına ve durumuna göre seçilir. Amaç yalnızca daha güzel görünmek değil; cildin doğru ve uzmanca bakımıdır.",
            "Die Behandlungen werden für jede Person nach Bedarf und Zustand ihrer Haut ausgewählt. Es geht nicht nur um das Aussehen – sondern um die richtige, fachkundige Pflege der Haut.",
            "Treatments are chosen for each person according to the needs and condition of their skin. The goal is not only how the skin looks, but caring for it correctly and expertly."),
        L("about.certificates.title", "about", "عنوان فهرست مدارک و دوره‌ها", "مدارک و دوره‌ها", "Sertifikalar ve Eğitimler", "Zertifikate & Weiterbildungen", "Certificates & Training"),

        // ---------------- Services
        L("services.eyebrow", "services", "نوشته‌ی کوچک بالای صفحه‌ی خدمات", "فیشیال و مراقبت پوست", "FACIAL & CİLT BAKIMI", "FACIAL & HAUTPFLEGE", "FACIAL & SKINCARE TREATMENTS"),
        L("services.title", "services", "عنوان صفحه‌ی خدمات", "خدمات ما", "Hizmetlerimiz", "Unsere Behandlungen", "Our Treatments"),
        M("services.intro", "services", "متن معرفی بالای صفحه‌ی خدمات",
            "هر پوست نیاز متفاوتی دارد. در Beauty by Negin، خدمات پوستی بر اساس وضعیت و نیاز پوست شما انتخاب می‌شوند تا تجربه‌ای شخصی‌سازی‌شده و حرفه‌ای داشته باشید.",
            "Her cildin ihtiyacı farklıdır. Beauty by Negin'de bakımlar, cildinizin durumuna ve ihtiyacına göre seçilir; böylece kişiye özel ve profesyonel bir deneyim yaşarsınız.",
            "Jede Haut hat andere Bedürfnisse. Bei Beauty by Negin werden die Behandlungen nach Zustand und Bedürfnissen Ihrer Haut ausgewählt – für ein persönliches und professionelles Erlebnis.",
            "Every skin has different needs. At Beauty by Negin, treatments are selected according to your skin's condition and needs, for a personalised and professional experience."),
        L("services.suitableFor", "services", "عنوان «مناسب برای» در صفحه‌ی هر خدمت", "مناسب برای", "Uygun olduğu ciltler", "Geeignet für", "Suitable for"),
        L("services.expectedResult", "services", "عنوان «نتیجه‌ی مورد انتظار»", "نتیجه‌ی مورد انتظار", "Beklenen sonuç", "Erwartetes Ergebnis", "Expected result"),
        L("services.duration", "services", "عنوان «مدت زمان»", "مدت زمان تقریبی", "Tahmini süre", "Ungefähre Dauer", "Approx. duration"),
        L("services.price", "services", "عنوان قیمت (فقط وقتی نمایش قیمت روشن است)", "قیمت", "Fiyat", "Preis", "Price"),
        L("services.related", "services", "عنوان «خدمات دیگر» پایین صفحه‌ی هر خدمت", "خدمات دیگر", "Diğer Bakımlar", "Weitere Behandlungen", "Other Treatments"),
        L("services.note", "services", "یادداشت کوچک پایین صفحه‌ی هر خدمت",
            "انتخاب درمان ممکن است با توجه به وضعیت و نیاز پوست هر فرد متفاوت باشد.",
            "Bakım seçimi, kişinin cilt durumuna ve ihtiyaçlarına göre değişebilir.",
            "Die Auswahl der Behandlung kann je nach individuellem Hautzustand und Bedürfnissen variieren.",
            "Treatment selection may vary depending on individual skin condition and needs."),
        L("services.empty", "services", "وقتی هنوز خدمتی منتشر نشده", "به‌زودی خدمات معرفی می‌شوند.", "Hizmetler yakında burada olacak.", "Die Behandlungen werden in Kürze vorgestellt.", "Treatments will be presented soon."),
        L("ask.title", "services", "عنوان پنجره‌ی «درباره‌ی این خدمت بپرسید»", "چطور با شما در ارتباط باشیم؟", "Size nasıl ulaşalım?", "Wie möchten Sie uns kontaktieren?", "How would you like to reach us?"),

        // ---------------- Gallery
        L("gallery.eyebrow", "gallery", "نوشته‌ی کوچک بالای صفحه‌ی گالری", "گالری", "GALERİ", "GALERIE", "GALLERY"),
        L("gallery.title", "gallery", "عنوان صفحه‌ی گالری", "گالری", "Galeri", "Galerie", "Gallery"),
        M("gallery.intro", "gallery", "متن کوتاه بالای گالری",
            "نگاهی به فضای کار، مراقبت‌ها و نتایج Beauty by Negin.",
            "Beauty by Negin'in çalışma ortamına, bakımlarına ve sonuçlarına bir bakış.",
            "Ein Einblick in Atmosphäre, Behandlungen und Ergebnisse bei Beauty by Negin.",
            "A glimpse into the space, the treatments and the results at Beauty by Negin."),
        L("gallery.all", "gallery", "دکمه‌ی «همه» در فیلتر گالری", "همه", "Tümü", "Alle", "All"),
        L("gallery.before", "gallery", "برچسب عکس قبل", "قبل", "Önce", "Vorher", "Before"),
        L("gallery.after", "gallery", "برچسب عکس بعد", "بعد", "Sonra", "Nachher", "After"),
        L("gallery.empty", "gallery", "وقتی گالری خالی است", "به‌زودی تصاویر اضافه می‌شوند.", "Görseller yakında eklenecek.", "Bilder folgen in Kürze.", "Images coming soon."),
        L("gallery.prev", "gallery", "دکمه‌ی عکس قبلی در نمایش بزرگ", "قبلی", "Önceki", "Zurück", "Previous"),
        L("gallery.next", "gallery", "دکمه‌ی عکس بعدی در نمایش بزرگ", "بعدی", "Sonraki", "Weiter", "Next"),

        // ---------------- Reviews
        L("reviews.eyebrow", "reviews", "نوشته‌ی کوچک بالای صفحه‌ی نظرات", "نظرات مشتریان", "MÜŞTERİ YORUMLARI", "KUNDENSTIMMEN", "CLIENT REVIEWS"),
        L("reviews.title", "reviews", "عنوان صفحه‌ی نظرات", "نظرات مراجعین", "Danışan Yorumları", "Bewertungen", "Client Reviews"),
        L("reviews.empty", "reviews", "وقتی هنوز نظری ثبت نشده", "به‌زودی نظرات مراجعین اینجا نمایش داده می‌شود.", "Danışan yorumları yakında burada.", "Bewertungen folgen in Kürze.", "Reviews coming soon."),
        L("reviews.form.title", "reviews", "عنوان فرم ثبت نظر", "نظر خود را بنویسید", "Yorumunuzu yazın", "Ihre Bewertung", "Write a review"),
        L("reviews.form.name", "reviews", "فرم ثبت نظر", "نام شما", "Adınız", "Ihr Name", "Your name"),
        L("reviews.form.initials", "reviews", "فرم ثبت نظر", "فقط حروف اول نامم نمایش داده شود", "Sadece adımın baş harfleri görünsün", "Nur meine Initialen anzeigen", "Show only my initials"),
        L("reviews.form.rating", "reviews", "فرم ثبت نظر", "امتیاز", "Puan", "Bewertung", "Rating"),
        L("reviews.form.text", "reviews", "فرم ثبت نظر", "نظر شما", "Yorumunuz", "Ihre Erfahrung", "Your review"),
        L("reviews.form.service", "reviews", "فرم ثبت نظر", "خدمت دریافت‌شده (اختیاری)", "Aldığınız bakım (isteğe bağlı)", "Erhaltene Behandlung (optional)", "Treatment received (optional)"),
        L("reviews.form.success", "reviews", "پیام بعد از ثبت نظر", "سپاس از شما! نظرتان پس از بررسی منتشر می‌شود.", "Teşekkür ederiz! Yorumunuz incelendikten sonra yayınlanacak.", "Vielen Dank! Ihre Bewertung wird nach Prüfung veröffentlicht.", "Thank you! Your review will be published after approval."),

        // ---------------- Contact
        L("contact.eyebrow", "contact", "نوشته‌ی کوچک بالای صفحه‌ی تماس", "تماس", "İLETİŞİM", "KONTAKT", "CONTACT"),
        L("contact.title", "contact", "عنوان صفحه‌ی تماس", "با ما در تماس باشید", "Bize ulaşın", "Kontaktieren Sie uns", "Get in touch"),
        M("contact.intro", "contact", "متن کوتاه بالای صفحه‌ی تماس",
            "برای مشاوره، سؤال یا رزرو نوبت، از هر راهی که راحت‌ترید با ما در ارتباط باشید.",
            "Danışmanlık, soru veya randevu için size en uygun yoldan bize ulaşabilirsiniz.",
            "Für Beratung, Fragen oder Termine erreichen Sie uns auf dem Weg, der Ihnen am liebsten ist.",
            "For a consultation, a question or an appointment, reach us whichever way suits you best."),
        M("contact.address", "contact", "آدرس سالن (در صفحه‌ی تماس و پایین همه‌ی صفحه‌ها)", "", "", "", ""),
        L("contact.addressTitle", "contact", "عنوان آدرس", "آدرس", "Adres", "Adresse", "Address"),
        L("contact.hoursTitle", "contact", "عنوان ساعات کاری", "ساعات کاری", "Çalışma Saatleri", "Öffnungszeiten", "Opening Hours"),
        L("contact.closed", "contact", "برای روزهای تعطیل", "تعطیل", "Kapalı", "Geschlossen", "Closed"),
        L("contact.byAppointment", "contact", "یادداشت زیر ساعات کاری", "فقط با رزرو قبلی", "Sadece randevu ile", "Nur nach Terminvereinbarung", "By appointment only"),
        L("contact.form.title", "contact", "عنوان فرم کوتاه تماس", "ارسال پیام", "Mesaj Gönderin", "Nachricht senden", "Send a Message"),
        L("contact.form.name", "contact", "فرم تماس", "نام", "Adınız", "Name", "Name"),
        L("contact.form.contactInfo", "contact", "فرم تماس", "شماره تلفن یا ایمیل", "Telefon veya e-posta", "Telefon oder E-Mail", "Phone or email"),
        L("contact.form.message", "contact", "فرم تماس", "پیام شما", "Mesajınız", "Ihre Nachricht", "Your message"),
        L("contact.form.success", "contact", "پیام بعد از ارسال فرم تماس", "پیام شما رسید. به‌زودی با شما تماس می‌گیریم.", "Mesajınız alındı. En kısa sürede size dönüş yapacağız.", "Ihre Nachricht ist angekommen. Wir melden uns in Kürze.", "Your message has been received. We will get back to you soon."),

        // ---------------- Booking
        L("booking.eyebrow", "booking", "نوشته‌ی کوچک بالای صفحه‌ی رزرو", "رزرو نوبت", "RANDEVU", "TERMIN", "APPOINTMENT"),
        L("booking.title", "booking", "عنوان صفحه‌ی رزرو", "درخواست نوبت", "Randevu Talebi", "Terminanfrage", "Request an Appointment"),
        M("booking.intro", "booking", "متن کوتاه بالای صفحه‌ی رزرو",
            "فرم زیر را پر کنید؛ برای هماهنگی زمان دقیق با شما تماس می‌گیریم. برای پاسخ سریع‌تر می‌توانید تماس بگیرید یا پیام بدهید.",
            "Aşağıdaki formu doldurun; kesin saati ayarlamak için sizinle iletişime geçeceğiz. Daha hızlı yanıt için bizi arayabilir veya mesaj gönderebilirsiniz.",
            "Füllen Sie das Formular aus – wir melden uns, um den genauen Termin abzustimmen. Für eine schnellere Antwort können Sie uns auch anrufen oder schreiben.",
            "Fill in the form below and we will contact you to arrange the exact time. For a faster reply, feel free to call or message us."),
        L("booking.quick", "booking", "عنوان سه دکمه‌ی سریع بالای فرم", "راه سریع‌تر", "Daha hızlı ulaşın", "Schneller Kontakt", "Quicker options"),
        L("booking.name", "booking", "فرم رزرو", "نام و نام خانوادگی", "Ad Soyad", "Vor- und Nachname", "Full name"),
        L("booking.phone", "booking", "فرم رزرو", "شماره تلفن", "Telefon numarası", "Telefonnummer", "Phone number"),
        L("booking.phoneHint", "booking", "توضیح زیر فیلد تلفن", "همراه با کد کشور، مثلاً ‎+98 912 …", "Ülke koduyla, örn. +90 5…", "Mit Ländervorwahl, z. B. +49 …", "Including country code, e.g. +44 …"),
        L("booking.email", "booking", "فرم رزرو", "ایمیل (اختیاری)", "E-posta (isteğe bağlı)", "E-Mail (optional)", "Email (optional)"),
        L("booking.service", "booking", "فرم رزرو", "خدمت مورد نظر", "İstediğiniz bakım", "Gewünschte Behandlung", "Treatment"),
        L("booking.service.choose", "booking", "گزینه‌ی پیش‌فرض فهرست خدمات", "انتخاب کنید…", "Seçiniz…", "Bitte wählen…", "Please choose…"),
        L("booking.service.notSure", "booking", "گزینه‌ی «مطمئن نیستم» در فهرست خدمات", "مطمئن نیستم، مشاوره می‌خواهم", "Emin değilim, danışmak istiyorum", "Ich bin unsicher und wünsche eine Beratung", "Not sure – I'd like a consultation"),
        L("booking.date", "booking", "فرم رزرو", "تاریخ دلخواه", "Tercih edilen tarih", "Wunschdatum", "Preferred date"),
        L("booking.time", "booking", "فرم رزرو", "زمان دلخواه", "Tercih edilen saat", "Bevorzugte Uhrzeit", "Preferred time"),
        L("booking.message", "booking", "فرم رزرو", "توضیحات (اختیاری)", "Mesajınız (isteğe bağlı)", "Nachricht (optional)", "Message (optional)"),
        L("booking.submit", "booking", "دکمه‌ی ارسال فرم رزرو", "ارسال درخواست", "Talebi Gönder", "Anfrage senden", "Send Request"),
        L("booking.success.title", "booking", "عنوان صفحه‌ی تشکر بعد از رزرو", "درخواست شما ثبت شد", "Talebiniz alındı", "Vielen Dank für Ihre Anfrage", "Your request has been received"),
        M("booking.success.text", "booking", "متن صفحه‌ی تشکر بعد از رزرو",
            "به‌زودی برای هماهنگی زمان نوبت با شما تماس می‌گیریم.",
            "Randevu saatini ayarlamak için en kısa sürede sizinle iletişime geçeceğiz.",
            "Wir melden uns in Kürze, um Ihren Termin abzustimmen.",
            "We will contact you shortly to arrange your appointment."),
        L("booking.success.whatsapp", "booking", "دکمه‌ی واتس‌اپ در صفحه‌ی تشکر", "برای پاسخ سریع‌تر در واتس‌اپ پیام بدهید", "Daha hızlı yanıt için WhatsApp'tan yazın", "Für eine schnellere Antwort per WhatsApp schreiben", "Message us on WhatsApp for a faster reply"),

        // ---------------- Shared form texts
        L("form.required", "forms", "ستاره‌ی فیلدهای الزامی", "الزامی", "zorunlu", "Pflichtfeld", "required"),
        M("form.privacy", "forms", "متن تیک رضایت حریم خصوصی زیر فرم‌ها",
            "موافقم اطلاعاتم فقط برای پاسخ به درخواستم استفاده شود.",
            "Bilgilerimin yalnızca talebime yanıt vermek için kullanılmasını kabul ediyorum.",
            "Ich bin einverstanden, dass meine Angaben ausschließlich zur Bearbeitung meiner Anfrage verwendet werden.",
            "I agree that my details are used only to respond to my request."),
        L("form.error.required", "forms", "پیام خطای فیلد خالی", "لطفاً این قسمت را پر کنید.", "Lütfen bu alanı doldurun.", "Bitte füllen Sie dieses Feld aus.", "Please fill in this field."),
        L("form.error.email", "forms", "پیام خطای ایمیل نادرست", "لطفاً یک ایمیل درست وارد کنید.", "Lütfen geçerli bir e-posta adresi girin.", "Bitte geben Sie eine gültige E-Mail-Adresse ein.", "Please enter a valid email address."),
        L("form.error.phone", "forms", "پیام خطای تلفن نادرست", "لطفاً شماره تلفن را درست وارد کنید.", "Lütfen geçerli bir telefon numarası girin.", "Bitte geben Sie eine gültige Telefonnummer ein.", "Please enter a valid phone number."),
        L("form.error.date", "forms", "پیام خطای تاریخ گذشته", "لطفاً تاریخی از امروز به بعد انتخاب کنید.", "Lütfen bugünden sonraki bir tarih seçin.", "Bitte wählen Sie ein Datum ab heute.", "Please choose a date from today onwards."),
        L("form.error.privacy", "forms", "پیام خطای نزدن تیک رضایت", "لطفاً با سیاست حریم خصوصی موافقت کنید.", "Lütfen gizlilik politikasını onaylayın.", "Bitte stimmen Sie der Datenschutzerklärung zu.", "Please accept the privacy policy."),
        L("form.error.generic", "forms", "پیام خطای عمومی", "متأسفیم، ارسال انجام نشد. لطفاً دوباره تلاش کنید یا تماس بگیرید.", "Üzgünüz, gönderilemedi. Lütfen tekrar deneyin veya bizi arayın.", "Leider hat das nicht geklappt. Bitte versuchen Sie es erneut oder rufen Sie uns an.", "Sorry, that didn't work. Please try again or give us a call."),
        L("form.error.rateLimit", "forms", "وقتی کسی پشت سر هم فرم می‌فرستد", "درخواست‌های زیادی فرستاده شده؛ لطفاً چند دقیقه بعد دوباره تلاش کنید.", "Çok fazla deneme yapıldı; lütfen birkaç dakika sonra tekrar deneyin.", "Zu viele Anfragen – bitte versuchen Sie es in einigen Minuten erneut.", "Too many attempts – please try again in a few minutes."),
        L("form.sending", "forms", "در حال ارسال فرم", "در حال ارسال…", "Gönderiliyor…", "Wird gesendet…", "Sending…"),

        // ---------------- Newsletter
        L("newsletter.email", "newsletter", "فیلد ایمیل خبرنامه", "ایمیل شما", "E-posta adresiniz", "Ihre E-Mail-Adresse", "Your email"),
        L("newsletter.success", "newsletter", "پیام بعد از عضویت در خبرنامه", "عضویت شما انجام شد. سپاس!", "Aboneliğiniz tamamlandı. Teşekkürler!", "Vielen Dank für Ihre Anmeldung!", "You're subscribed – thank you!"),

        // ---------------- Chat (bottom-left)
        L("chat.open", "chat", "متن دکمه‌ی چت پایین سمت چپ (برای صفحه‌خوان)", "گفتگو با ما", "Bize yazın", "Chatten Sie mit uns", "Chat with us"),
        L("chat.title", "chat", "عنوان پنجره‌ی چت", "گفتگو با Beauty by Negin", "Beauty by Negin ile sohbet", "Chat mit Beauty by Negin", "Chat with Beauty by Negin"),
        M("chat.intro", "chat", "متن شروع چت (قبل از ثبت‌نام)",
            "برای شروع گفتگو، نام و ایمیل خود را وارد کنید. یک کد تأیید برایتان می‌فرستیم.",
            "Sohbete başlamak için adınızı ve e-posta adresinizi girin. Size bir doğrulama kodu göndereceğiz.",
            "Geben Sie Ihren Namen und Ihre E-Mail-Adresse ein, um den Chat zu starten. Wir senden Ihnen einen Bestätigungscode.",
            "Enter your name and email to start chatting. We'll send you a verification code."),
        L("chat.name", "chat", "فیلد نام در چت", "نام شما", "Adınız", "Ihr Name", "Your name"),
        L("chat.email", "chat", "فیلد ایمیل در چت", "ایمیل شما", "E-posta adresiniz", "Ihre E-Mail-Adresse", "Your email"),
        L("chat.register", "chat", "دکمه‌ی دریافت کد", "دریافت کد تأیید", "Doğrulama kodu gönder", "Code anfordern", "Send me a code"),
        L("chat.codeSent", "chat", "پیام بعد از ارسال کد", "کد ۶ رقمی به ایمیل شما فرستاده شد. پوشه‌ی اسپم را هم ببینید.", "6 haneli kod e-posta adresinize gönderildi. Spam klasörünü de kontrol edin.", "Wir haben Ihnen einen 6-stelligen Code per E-Mail gesendet. Bitte prüfen Sie auch den Spam-Ordner.", "We've emailed you a 6-digit code. Please also check your spam folder."),
        L("chat.code", "chat", "فیلد کد تأیید", "کد تأیید", "Doğrulama kodu", "Bestätigungscode", "Verification code"),
        L("chat.verify", "chat", "دکمه‌ی تأیید کد", "تأیید", "Doğrula", "Bestätigen", "Verify"),
        L("chat.resend", "chat", "لینک ارسال دوباره‌ی کد", "ارسال دوباره‌ی کد", "Kodu tekrar gönder", "Code erneut senden", "Resend code"),
        L("chat.changeEmail", "chat", "لینک تغییر ایمیل", "تغییر ایمیل", "E-postayı değiştir", "E-Mail ändern", "Change email"),
        L("chat.codeInvalid", "chat", "خطای کد نادرست", "کد درست نیست. دوباره امتحان کنید.", "Kod hatalı. Lütfen tekrar deneyin.", "Der Code ist nicht korrekt. Bitte erneut versuchen.", "That code isn't right. Please try again."),
        L("chat.codeExpired", "chat", "خطای کد منقضی", "این کد منقضی شده؛ کد جدید بگیرید.", "Kodun süresi doldu; lütfen yeni kod isteyin.", "Der Code ist abgelaufen – bitte fordern Sie einen neuen an.", "This code has expired – please request a new one."),
        L("chat.placeholder", "chat", "متن داخل کادر نوشتن پیام", "پیام خود را بنویسید…", "Mesajınızı yazın…", "Ihre Nachricht…", "Type your message…"),
        L("chat.send", "chat", "دکمه‌ی ارسال پیام چت", "ارسال", "Gönder", "Senden", "Send"),
        M("chat.welcome", "chat", "اولین پیام خوش‌آمد در چت (بعد از تأیید ایمیل)",
            "سلام! خوشحالیم که اینجا هستید. پیامتان را بنویسید؛ در اولین فرصت پاسخ می‌دهیم.",
            "Merhaba! Burada olmanıza sevindik. Mesajınızı yazın; en kısa sürede yanıt vereceğiz.",
            "Hallo! Schön, dass Sie da sind. Schreiben Sie uns – wir antworten so bald wie möglich.",
            "Hello! We're glad you're here. Write your message and we'll reply as soon as we can."),
        L("chat.you", "chat", "برچسب پیام‌های بازدیدکننده", "شما", "Siz", "Sie", "You"),
        L("chat.email.subject", "chat", "موضوع ایمیل کد تأیید", "کد تأیید گفتگو با Beauty by Negin", "Beauty by Negin sohbet doğrulama kodu", "Ihr Bestätigungscode für den Chat mit Beauty by Negin", "Your Beauty by Negin chat verification code"),
        M("chat.email.body", "chat", "متن ایمیل کد تأیید ({code} جای کد قرار می‌گیرد)",
            "سلام {name}،\nکد تأیید شما: {code}\nاین کد تا ۱۵ دقیقه معتبر است.",
            "Merhaba {name},\nDoğrulama kodunuz: {code}\nBu kod 15 dakika geçerlidir.",
            "Hallo {name},\nIhr Bestätigungscode lautet: {code}\nDer Code ist 15 Minuten gültig.",
            "Hello {name},\nYour verification code is: {code}\nThis code is valid for 15 minutes."),

        // ---------------- Footer
        M("footer.tagline", "footer", "جمله‌ی کوتاه پایین سایت",
            "مراقبت تخصصی و شخصی از پوست.",
            "Uzman ve kişiye özel cilt bakımı.",
            "Fachkundige, persönliche Hautpflege.",
            "Expert, personalised skincare."),
        L("footer.contact", "footer", "عنوان ستون تماس در پایین سایت", "تماس", "İletişim", "Kontakt", "Contact"),
        L("footer.follow", "footer", "عنوان شبکه‌های اجتماعی در پایین سایت", "شبکه‌های اجتماعی", "Bizi takip edin", "Folgen Sie uns", "Follow us"),
        L("footer.rights", "footer", "متن حق نشر", "همه‌ی حقوق محفوظ است.", "Tüm hakları saklıdır.", "Alle Rechte vorbehalten.", "All rights reserved."),

        // ---------------- WhatsApp / e-mail templates
        L("wa.service", "templates", "پیام آماده‌ی واتس‌اپ برای پرسش درباره‌ی یک خدمت؛ {service} نام خدمت است", "سلام، می‌خواستم درباره‌ی {service} اطلاعات بگیرم.", "Merhaba, {service} hakkında bilgi almak istiyorum.", "Hallo, ich hätte gerne Informationen zu {service}.", "Hello, I'd like some information about {service}."),
        L("wa.general", "templates", "پیام آماده‌ی واتس‌اپ برای دکمه‌های عمومی", "سلام، می‌خواستم درباره‌ی خدمات Beauty by Negin اطلاعات بگیرم.", "Merhaba, Beauty by Negin hizmetleri hakkında bilgi almak istiyorum.", "Hallo, ich hätte gerne Informationen zu den Behandlungen von Beauty by Negin.", "Hello, I'd like some information about Beauty by Negin's treatments."),
        L("wa.booking", "templates", "پیام آماده‌ی واتس‌اپ در صفحه‌ی تشکر بعد از رزرو", "سلام، همین الان درخواست نوبت در سایت ثبت کردم.", "Merhaba, az önce web sitesinden randevu talebi gönderdim.", "Hallo, ich habe gerade eine Terminanfrage über die Website gesendet.", "Hello, I've just sent an appointment request through the website."),
        L("mail.subject.service", "templates", "موضوع آماده‌ی ایمیل برای پرسش درباره‌ی یک خدمت", "پرسش درباره‌ی {service}", "{service} hakkında bilgi", "Anfrage zu {service}", "Enquiry about {service}"),
        L("mail.subject.general", "templates", "موضوع آماده‌ی ایمیل عمومی", "پرسش از Beauty by Negin", "Beauty by Negin'e soru", "Anfrage an Beauty by Negin", "Enquiry to Beauty by Negin"),

        // ---------------- Errors & maintenance
        L("error.404.title", "errors", "عنوان صفحه‌ی «پیدا نشد»", "این صفحه پیدا نشد", "Sayfa bulunamadı", "Seite nicht gefunden", "Page not found"),
        M("error.404.text", "errors", "متن صفحه‌ی «پیدا نشد»", "شاید آدرس تغییر کرده باشد. از صفحه‌ی اصلی ادامه دهید.", "Adres değişmiş olabilir. Ana sayfadan devam edebilirsiniz.", "Vielleicht hat sich die Adresse geändert. Machen Sie gerne auf der Startseite weiter.", "The address may have changed. Feel free to continue from the home page."),
        L("error.500.title", "errors", "عنوان صفحه‌ی خطا", "متأسفیم، مشکلی پیش آمد", "Üzgünüz, bir sorun oluştu", "Entschuldigung, etwas ist schiefgelaufen", "Sorry, something went wrong"),
        L("maintenance.title", "errors", "عنوان صفحه‌ی «به‌زودی» (حالت تعمیر)", "به‌زودی برمی‌گردیم", "Çok yakında", "Wir sind bald zurück", "Coming soon"),
        M("maintenance.text", "errors", "متن صفحه‌ی «به‌زودی»", "سایت در حال به‌روزرسانی است. تا آن زمان می‌توانید از راه‌های زیر با ما در تماس باشید.", "Sitemiz güncelleniyor. Bu sürede bize aşağıdaki yollarla ulaşabilirsiniz.", "Unsere Website wird gerade aktualisiert. Bis dahin erreichen Sie uns gerne hier:", "Our website is being updated. In the meantime you can reach us here:"),

        // ---------------- Search engines (Google)
        L("seo.home.title", "seo", "عنوانی که گوگل برای صفحه‌ی اصلی نشان می‌دهد", "Beauty by Negin | فیشیال و مراقبت تخصصی پوست", "Beauty by Negin | Facial ve Cilt Bakımı", "Beauty by Negin | Facials & Hautpflege", "Beauty by Negin | Facial & Skincare Treatments"),
        M("seo.home.description", "seo", "توضیحی که گوگل زیر عنوان صفحه‌ی اصلی نشان می‌دهد",
            "فیشیال و مراقبت شخصی پوست در Beauty by Negin؛ خدمات بر اساس نیاز واقعی پوست شما انتخاب می‌شوند.",
            "Beauty by Negin'de kişiye özel facial ve cilt bakımı; bakımlar cildinizin gerçek ihtiyaçlarına göre seçilir.",
            "Persönliche Facials und Hautpflege bei Beauty by Negin – Behandlungen, abgestimmt auf die tatsächlichen Bedürfnisse Ihrer Haut.",
            "Personalised facials and skincare at Beauty by Negin – treatments chosen for your skin's real needs.")
    ];
}
