namespace BeautyByNegin.DataAccess.Seed;

/// <summary>Arabic default values of the site texts (keys of <see cref="TextSeed"/>).</summary>
internal static class TextSeedAr
{
    /// <summary>Arabic text for a key; falls back to English if a key has no Arabic default yet.</summary>
    internal static string Get(string key, string english) => Values.TryGetValue(key, out var v) ? v : english;

    private static readonly Dictionary<string, string> Values = new()
    {
        // Menu
        ["nav.home"] = "الرئيسية",
        ["nav.about"] = "من نحن",
        ["nav.services"] = "الخدمات",
        ["nav.gallery"] = "المعرض",
        ["nav.reviews"] = "آراء العملاء",
        ["nav.contact"] = "تواصل معنا",
        ["nav.booking"] = "احجز موعداً",
        ["nav.menu"] = "القائمة",
        ["nav.close"] = "إغلاق",
        ["nav.skip"] = "الانتقال إلى المحتوى",

        // Buttons
        ["btn.book"] = "احجز موعداً",
        ["btn.contact"] = "تواصل معنا",
        ["btn.details"] = "التفاصيل",
        ["btn.ask"] = "استفسر عن هذه الخدمة",
        ["btn.allServices"] = "جميع الخدمات",
        ["btn.allGallery"] = "عرض المعرض",
        ["btn.allReviews"] = "جميع الآراء",
        ["btn.about"] = "المزيد عنا",
        ["btn.send"] = "إرسال",
        ["btn.call"] = "اتصل بنا",
        ["btn.whatsapp"] = "واتساب",
        ["btn.email"] = "البريد الإلكتروني",
        ["btn.instagram"] = "إنستغرام",
        ["btn.telegram"] = "تيليجرام",
        ["btn.bookForm"] = "نموذج الحجز",
        ["btn.openMap"] = "افتح في الخريطة",
        ["btn.backHome"] = "العودة إلى الرئيسية",
        ["btn.subscribe"] = "اشترك",

        // Mobile bar
        ["bar.call"] = "اتصال",
        ["bar.whatsapp"] = "واتساب",
        ["bar.email"] = "بريد",
        ["bar.book"] = "حجز",

        // Home
        ["home.hero.eyebrow"] = "العناية بالوجه والبشرة",
        ["home.hero.title"] = "Beauty by Negin",
        ["home.hero.slogan"] = "بشرتك. جمالك. إشراقتك.",
        ["home.intro.eyebrow"] = "من نحن",
        ["home.intro.title"] = "عناية متخصصة تناسب بشرتك",
        ["home.intro.text"] = "لكل بشرة احتياجات مختلفة. في Beauty by Negin تُختار الخدمات وفقاً لحالة بشرتك واحتياجاتها، لتحظي بتجربة شخصية واحترافية.",
        ["home.services.eyebrow"] = "خدماتنا",
        ["home.services.title"] = "العناية بالوجه والبشرة",
        ["home.gallery.eyebrow"] = "المعرض",
        ["home.gallery.title"] = "لحظات من Beauty by Negin",
        ["home.reviews.eyebrow"] = "آراء العملاء",
        ["home.reviews.title"] = "تجارب عميلاتنا",
        ["home.instagram.title"] = "تابعينا على إنستغرام",
        ["home.newsletter.title"] = "ابقي على اطلاع",
        ["home.newsletter.text"] = "أدخلي بريدك الإلكتروني لتصلك نصائح العناية بالبشرة وأخبار Beauty by Negin.",
        ["home.contact.title"] = "يسعدنا الاعتناء ببشرتك",
        ["home.contact.text"] = "للاستشارة أو لحجز موعد، اتصلي بنا أو أرسلي رسالة.",

        // Consultation
        ["consult.eyebrow"] = "استشارة شخصية للبشرة",
        ["consult.title"] = "كل بشرة مختلفة.",
        ["consult.text"] = "قبل اختيار العلاج تُفحص حالة بشرتك، ليُختار الأنسب لها وفقاً لاحتياجاتها الحقيقية.",
        ["consult.question"] = "هل تودّين معرفة العلاج المناسب لك؟",

        // About
        ["about.eyebrow"] = "من نحن",
        ["about.title"] = "نيجين – خبيرة العناية بالبشرة",
        ["about.intro"] = "بخبرة سنوات في العناية المتخصصة بالبشرة، أسّست نيجين Beauty by Negin لهدف بسيط: عناية دقيقة وهادئة وشخصية لكل بشرة.\nتبدأ كل جلسة بالتعرّف على بشرتك، ثم تُنفَّذ العناية بدقة وصبر وفقاً لاحتياجاتها في ذلك اليوم.",
        ["about.expertise.title"] = "مجالات الخبرة",
        ["about.philosophy.title"] = "فلسفتنا",
        ["about.philosophy.text"] = "تُختار الخدمات لكل شخص وفقاً لاحتياجات بشرته وحالتها. الهدف ليس المظهر فحسب، بل العناية الصحيحة والمتخصصة بالبشرة.",
        ["about.certificates.title"] = "الشهادات والدورات",

        // Services
        ["services.eyebrow"] = "العناية بالوجه والبشرة",
        ["services.title"] = "خدماتنا",
        ["services.intro"] = "لكل بشرة احتياجات مختلفة. في Beauty by Negin تُختار الخدمات وفقاً لحالة بشرتك واحتياجاتها، لتحظي بتجربة شخصية واحترافية.",
        ["services.suitableFor"] = "مناسب لـ",
        ["services.expectedResult"] = "النتيجة المتوقعة",
        ["services.duration"] = "المدة التقريبية",
        ["services.price"] = "السعر",
        ["services.related"] = "خدمات أخرى",
        ["services.note"] = "قد يختلف اختيار العلاج بحسب حالة البشرة واحتياجات كل شخص.",
        ["services.empty"] = "سنعرض خدماتنا قريباً.",
        ["ask.title"] = "كيف تفضّلين التواصل معنا؟",

        // Gallery
        ["gallery.eyebrow"] = "المعرض",
        ["gallery.title"] = "المعرض",
        ["gallery.intro"] = "نظرة على المكان والعلاجات والنتائج في Beauty by Negin.",
        ["gallery.all"] = "الكل",
        ["gallery.before"] = "قبل",
        ["gallery.after"] = "بعد",
        ["gallery.empty"] = "ستُضاف الصور قريباً.",
        ["gallery.prev"] = "السابق",
        ["gallery.next"] = "التالي",

        // Reviews
        ["reviews.eyebrow"] = "آراء العملاء",
        ["reviews.title"] = "آراء العميلات",
        ["reviews.empty"] = "ستظهر آراء العميلات هنا قريباً.",
        ["reviews.form.title"] = "اكتبي رأيك",
        ["reviews.form.name"] = "اسمك",
        ["reviews.form.initials"] = "إظهار الأحرف الأولى من اسمي فقط",
        ["reviews.form.rating"] = "التقييم",
        ["reviews.form.text"] = "رأيك",
        ["reviews.form.service"] = "الخدمة التي حصلتِ عليها (اختياري)",
        ["reviews.form.success"] = "شكراً لك! سيُنشر رأيك بعد المراجعة.",

        // Contact
        ["contact.eyebrow"] = "تواصل معنا",
        ["contact.title"] = "تواصلي معنا",
        ["contact.intro"] = "للاستشارة أو الاستفسار أو حجز موعد، تواصلي معنا بالطريقة التي تناسبك.",
        ["contact.address"] = "",
        ["contact.addressTitle"] = "العنوان",
        ["contact.hoursTitle"] = "ساعات العمل",
        ["contact.closed"] = "مغلق",
        ["contact.byAppointment"] = "بموعد مسبق فقط",
        ["contact.form.title"] = "أرسلي رسالة",
        ["contact.form.name"] = "الاسم",
        ["contact.form.contactInfo"] = "رقم الهاتف أو البريد الإلكتروني",
        ["contact.form.message"] = "رسالتك",
        ["contact.form.success"] = "وصلتنا رسالتك، وسنتواصل معك قريباً.",

        // Booking
        ["booking.eyebrow"] = "حجز موعد",
        ["booking.title"] = "طلب موعد",
        ["booking.intro"] = "املئي النموذج أدناه وسنتواصل معك لتحديد الموعد بدقة. للحصول على رد أسرع يمكنك الاتصال بنا أو مراسلتنا.",
        ["booking.quick"] = "طرق أسرع",
        ["booking.name"] = "الاسم الكامل",
        ["booking.phone"] = "رقم الهاتف",
        ["booking.phoneHint"] = "مع رمز الدولة، مثل ‎+971 5…",
        ["booking.email"] = "البريد الإلكتروني (اختياري)",
        ["booking.service"] = "الخدمة المطلوبة",
        ["booking.service.choose"] = "اختاري…",
        ["booking.service.notSure"] = "لست متأكدة، أرغب في استشارة",
        ["booking.date"] = "التاريخ المفضّل",
        ["booking.time"] = "الوقت المفضّل",
        ["booking.message"] = "ملاحظات (اختياري)",
        ["booking.submit"] = "إرسال الطلب",
        ["booking.success.title"] = "تم استلام طلبك",
        ["booking.success.text"] = "سنتواصل معك قريباً لتحديد موعدك.",
        ["booking.success.whatsapp"] = "راسلينا على واتساب لرد أسرع",

        // Forms
        ["form.required"] = "مطلوب",
        ["form.privacy"] = "أوافق على استخدام بياناتي فقط للرد على طلبي.",
        ["form.error.required"] = "يرجى ملء هذا الحقل.",
        ["form.error.email"] = "يرجى إدخال بريد إلكتروني صحيح.",
        ["form.error.phone"] = "يرجى إدخال رقم هاتف صحيح.",
        ["form.error.date"] = "يرجى اختيار تاريخ من اليوم فصاعداً.",
        ["form.error.privacy"] = "يرجى الموافقة على سياسة الخصوصية.",
        ["form.error.generic"] = "عذراً، لم يتم الإرسال. يرجى المحاولة مرة أخرى أو الاتصال بنا.",
        ["form.error.rateLimit"] = "محاولات كثيرة، يرجى المحاولة بعد بضع دقائق.",
        ["form.sending"] = "جارٍ الإرسال…",

        // Newsletter
        ["newsletter.email"] = "بريدك الإلكتروني",
        ["newsletter.success"] = "تم اشتراكك، شكراً لك!",

        // Chat
        ["chat.open"] = "تحدّثي معنا",
        ["chat.title"] = "محادثة مع Beauty by Negin",
        ["chat.intro"] = "لبدء المحادثة، أدخلي اسمك وبريدك الإلكتروني وسنرسل لك رمز تحقق.",
        ["chat.name"] = "اسمك",
        ["chat.email"] = "بريدك الإلكتروني",
        ["chat.register"] = "أرسلي لي الرمز",
        ["chat.codeSent"] = "أرسلنا رمزاً من 6 أرقام إلى بريدك الإلكتروني. تحقّقي أيضاً من مجلد الرسائل غير المرغوب فيها.",
        ["chat.code"] = "رمز التحقق",
        ["chat.verify"] = "تأكيد",
        ["chat.resend"] = "إعادة إرسال الرمز",
        ["chat.changeEmail"] = "تغيير البريد الإلكتروني",
        ["chat.codeInvalid"] = "الرمز غير صحيح، حاولي مرة أخرى.",
        ["chat.codeExpired"] = "انتهت صلاحية الرمز، اطلبي رمزاً جديداً.",
        ["chat.placeholder"] = "اكتبي رسالتك…",
        ["chat.send"] = "إرسال",
        ["chat.welcome"] = "مرحباً! يسعدنا وجودك هنا. اكتبي رسالتك وسنرد في أقرب وقت.",
        ["chat.you"] = "أنتِ",
        ["chat.email.subject"] = "رمز التحقق للمحادثة مع Beauty by Negin",
        ["chat.email.body"] = "مرحباً {name}،\nرمز التحقق الخاص بك: {code}\nالرمز صالح لمدة 15 دقيقة.",

        // Footer
        ["footer.tagline"] = "عناية متخصصة وشخصية بالبشرة.",
        ["footer.contact"] = "التواصل",
        ["footer.follow"] = "تابعينا",
        ["footer.rights"] = "جميع الحقوق محفوظة.",

        // Templates
        ["wa.service"] = "مرحباً، أودّ الاستفسار عن {service}.",
        ["wa.general"] = "مرحباً، أودّ الاستفسار عن خدمات Beauty by Negin.",
        ["wa.booking"] = "مرحباً، أرسلت للتو طلب موعد عبر الموقع.",
        ["wa.reply"] = "مرحباً {name}، أكتب لك من Beauty by Negin بخصوص طلب موعدك.",
        ["mail.subject.reply"] = "طلب موعدك لدى Beauty by Negin",
        ["mail.subject.service"] = "استفسار عن {service}",
        ["mail.subject.general"] = "استفسار إلى Beauty by Negin",

        // Errors
        ["error.404.title"] = "الصفحة غير موجودة",
        ["error.404.text"] = "ربما تغيّر العنوان. يمكنك المتابعة من الصفحة الرئيسية.",
        ["error.500.title"] = "عذراً، حدث خطأ ما",
        ["maintenance.title"] = "سنعود قريباً",
        ["maintenance.text"] = "يجري تحديث موقعنا حالياً. حتى ذلك الحين يمكنك التواصل معنا عبر:",

        // SEO
        ["seo.home.title"] = "Beauty by Negin | العناية بالوجه والبشرة",
        ["seo.home.description"] = "عناية شخصية بالوجه والبشرة في Beauty by Negin، تُختار فيها العلاجات وفقاً لاحتياجات بشرتك الحقيقية."
    };
}
