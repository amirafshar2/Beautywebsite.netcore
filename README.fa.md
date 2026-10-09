<div align="center" dir="rtl">

# ✦ Beauty by Negin ✦

**سایت لوکس چندزبانه و پنل مدیریت ساده برای یک استودیوی فیشیال و مراقبت پوست**

[English](README.md) · فارسی · [راهنمای نصب (ترکی)](docs/deployment-tr.md) · [راهنمای پنل](docs/panel-rehberi-fa.md)

<img src="docs/screenshots/home-fa.jpg" alt="صفحه‌ی اول سایت" width="100%">

</div>

<div dir="rtl">

## ✨ ویژگی‌های اصلی

- 🌍 **۵ زبان:** فارسی و عربی (راست‌چین)، ترکی، آلمانی و انگلیسی، با آدرس جدا برای هر زبان
- 🧑‍💼 **پنل مدیریت برای کسی که با تکنولوژی آشنا نیست:** بالای هر صفحه نوشته شده مشتری در سایت با آن بخش چه می‌کند و چرا مهم است
- 🤖 **ترجمه‌ی خودکار با یک دکمه:** متن را فارسی بنویسید؛ هوش مصنوعی Gemini بقیه‌ی زبان‌ها را پر می‌کند
- 💎 **طراحی نرم و لوکس:** سیستم طراحی «Soft UI Evolution» و «Nature Distilled» (با اسکیل ui-ux-pro-max): سایه‌های نرم، کارت شیشه‌ای روی عکس اصلی، بافت ملایم کاغذ، کنتراست استاندارد و احترام به تنظیم «کاهش حرکت»
- 🎨 **تغییر رنگ کل سایت از پنل:** ۲۰ ترکیب سه‌رنگ آماده یا تنظیم جداگانه‌ی ۸ گروه رنگ، با پیش‌نمایش زنده
- 🎬 **ویدئو برای هر خدمت:** ویدئوی گوشی را آپلود کنید؛ سرور آن را با کیفیت خوب فشرده می‌کند و فقط اگر ویدئو باشد در صفحه‌ی خدمت نشان می‌دهد (روی سرور ffmpeg لازم است؛ در Docker نصب است)
- 📅 **نوبت، چت و حساب کاربری مشتری:** درخواست نوبت، چت آنلاین، ورود بدون رمز با کد ایمیلی
- 🔎 **سئو:** نقشه‌ی سایت چندزبانه، داده‌ی ساختاریافته و راهنمای گوگل سرچ کنسول داخل پنل
- 🛡️ **امنیت و حریم خصوصی:** بدون CDN و ردیاب، رمزنگاری اطلاعات حساس، محافظت در برابر اسپم
- 📦 **قابل انتقال:** همه‌ی اطلاعات در دو پوشه، پشتیبان‌گیری و برگرداندن با یک دکمه، آماده برای Docker و Render

## 📸 تصاویر

| | |
|---|---|
| <img src="docs/screenshots/home-en.jpg" alt="انگلیسی"> | <img src="docs/screenshots/home-ar.jpg" alt="عربی"> |
| صفحه‌ی اول · انگلیسی | صفحه‌ی اول · عربی |
| <img src="docs/screenshots/services-de.jpg" alt="خدمات"> | <img src="docs/screenshots/gallery-fa.jpg" alt="گالری"> |
| خدمات · آلمانی | گالری |
| <img src="docs/screenshots/admin-dashboard.jpg" alt="پنل"> | <img src="docs/screenshots/admin-service-edit.jpg" alt="ویرایش خدمت"> |
| خانه‌ی پنل | ویرایش خدمت و دکمه‌ی ترجمه |
| <img src="docs/screenshots/admin-colors.jpg" alt="رنگ‌ها"> | <img src="docs/screenshots/admin-google.jpg" alt="گوگل"> |
| رنگ‌های سایت | راهنمای گوگل سرچ کنسول |
| <img src="docs/screenshots/theme-burgundy.jpg" alt="زرشکی"> | <img src="docs/screenshots/theme-forest.jpg" alt="سبز"> |
| ترکیب «زرشکی و شامپاینی» | ترکیب «سبز جنگلی و مریم‌گلی» |

<p align="center"><img src="docs/screenshots/mobile.jpg" alt="موبایل" width="85%"></p>

## 🛠️ تکنولوژی

- ‎.NET 10، ASP.NET Core MVC + Razor، سه لایه (DataAccess / Business / Web)
- EF Core + SQLite، ASP.NET Core Identity، Serilog
- بدون درخواست خارجی: فونت‌ها و کتابخانه‌ها روی خود سرور

## 🚀 اجرا

```bash
dotnet run --project src/BeautyByNegin.Web
```

آدرس `/admin` را باز کنید؛ صفحه‌ی «راه‌اندازی اولیه» اولین مدیر را می‌سازد.

## ☁️ راه‌اندازی روی Render

۱. مخزن را روی GitHub بگذارید.
۲. در Render: **New → Blueprint** و انتخاب همین مخزن (فایل `render.yaml`).
۳. برای `Site__InitialAdmin__Password` یک رمز بنویسید؛ مدیر با نام کاربری `admin` خودکار ساخته می‌شود.
۴. بعد از Deploy، آدرس `https://<نام-سرویس>.onrender.com/admin` را باز کنید.

> **پلن رایگان فقط برای نمایش است:** در پلن رایگان Render فایل‌ها موقتی‌اند و سرویس بعد از ۱۵ دقیقه بی‌استفاده می‌خوابد؛ دیتابیس و عکس‌ها بعد از هر Deploy، ری‌استارت یا خواب از اول ساخته می‌شوند. برای سایت واقعی پلن پولی و دیسک (`disk` در `render.yaml`) لازم است.

<p align="center">طراحی و برنامه‌نویسی: <b>امیررضا افشار</b></p>

</div>
