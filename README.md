<div align="center">

# ✦ Beauty by Negin ✦

**A luxury, multilingual website and a no-tech-skills admin panel for a facial & skincare studio**

[![.NET 10](https://img.shields.io/badge/.NET-10_LTS-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET_Core-MVC_%2B_Razor-5C2D91)](https://learn.microsoft.com/aspnet/core/mvc/overview)
[![EF Core + SQLite](https://img.shields.io/badge/EF_Core-SQLite-003B57?logo=sqlite&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![Languages](https://img.shields.io/badge/languages-FA_·_TR_·_DE_·_EN_·_AR-C2A878)](#-multilingual-by-design)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)](#-run-with-docker)
[![Render](https://img.shields.io/badge/Render-Blueprint-46E3B7?logo=render&logoColor=white)](#-deploy-on-render)
[![No CDN](https://img.shields.io/badge/external_requests-0-3A2E2B)](#-privacy--security)

[فارسی](README.fa.md) · English · [Türkçe kurulum rehberi](docs/deployment-tr.md)

<img src="docs/screenshots/home-fa.jpg" alt="Beauty by Negin – home page (Persian)" width="100%">

</div>

---

## ✨ Highlights

| | |
|---|---|
| 🌍 **5 languages, 2 directions** | Persian & Arabic (RTL), Turkish, German, English (LTR) with translated URLs and `hreflang` |
| 🧑‍💼 **Admin panel for non-technical users** | Every screen explains *what the customer does with it and why it matters* |
| 🤖 **One-click AI translation** | Write in Persian, press a button: Google Gemini fills all other languages |
| 🎨 **Site colors from the panel** | 20 three-color combinations or 8 fine-grained color groups, with live preview and contrast checks |
| 🎬 **Treatment videos** | Upload a phone video, the server compresses it (often 5–10× smaller) and shows it on the treatment page |
| 📅 **Bookings, chat & customer accounts** | Appointment requests, live chat, passwordless login with e-mail code |
| 🔎 **SEO built in** | Sitemap with all languages, structured data, Google Search Console guide in the panel |
| 🛡️ **Privacy & security** | Strict CSP, zero CDNs / trackers, encrypted secrets, rate limiting, spam protection |
| 📦 **Portable** | All data lives in two folders, one-click backup & restore, Docker and Render ready |

---

## 📸 Screenshots

### Public website

<table>
<tr>
<td width="50%"><img src="docs/screenshots/home-en.jpg" alt="Home – English"><p align="center"><sub>Home · English</sub></p></td>
<td width="50%"><img src="docs/screenshots/home-ar.jpg" alt="Home – Arabic"><p align="center"><sub>Home · Arabic (RTL)</sub></p></td>
</tr>
<tr>
<td><img src="docs/screenshots/services-de.jpg" alt="Treatments – German"><p align="center"><sub>Treatments · German</sub></p></td>
<td><img src="docs/screenshots/service-detail-en.jpg" alt="Treatment detail"><p align="center"><sub>Treatment detail</sub></p></td>
</tr>
<tr>
<td><img src="docs/screenshots/gallery-fa.jpg" alt="Gallery"><p align="center"><sub>Gallery with categories</sub></p></td>
<td><img src="docs/screenshots/booking-en.jpg" alt="Booking form"><p align="center"><sub>Appointment request</sub></p></td>
</tr>
</table>

<p align="center"><img src="docs/screenshots/mobile.jpg" alt="Mobile views" width="85%"><br><sub>Mobile first: Persian home, treatments, English contact</sub></p>

<details>
<summary><b>Full home page (click)</b></summary>
<p align="center"><img src="docs/screenshots/home-fa-full.jpg" alt="Full home page" width="70%"></p>
</details>

### Admin panel

<table>
<tr>
<td width="50%"><img src="docs/screenshots/admin-dashboard.jpg" alt="Dashboard"><p align="center"><sub>Dashboard with "what does the customer do here?" box</sub></p></td>
<td width="50%"><img src="docs/screenshots/admin-service-edit.jpg" alt="Edit treatment"><p align="center"><sub>Editing a treatment · language tabs + AI translate button</sub></p></td>
</tr>
<tr>
<td><img src="docs/screenshots/admin-colors.jpg" alt="Site colors"><p align="center"><sub>Site colors · three-color combinations & live preview</sub></p></td>
<td><img src="docs/screenshots/admin-google.jpg" alt="Google Search Console"><p align="center"><sub>Step-by-step Google Search Console setup</sub></p></td>
</tr>
</table>

### One click, a new look

<table>
<tr>
<td width="33%"><img src="docs/screenshots/home-fa.jpg" alt="Original theme"><p align="center"><sub>Original · brown & cream</sub></p></td>
<td width="33%"><img src="docs/screenshots/theme-burgundy.jpg" alt="Burgundy theme"><p align="center"><sub>Burgundy & champagne</sub></p></td>
<td width="33%"><img src="docs/screenshots/theme-forest.jpg" alt="Forest theme"><p align="center"><sub>Forest & sage</sub></p></td>
</tr>
</table>

---

## 🤖 Built with AI (vibe coding)

This project was built through **vibe coding**: I described the requirements, design and behaviour in natural language, and an AI coding assistant wrote most of the code. My part was the product and the process – defining features from the client's real needs, reviewing and testing every step, steering architecture and design decisions, and deploying the result.

It is an experiment in how far a complete, production-ready web application can be taken with AI as the main implementer, while a human stays in charge of requirements, quality and responsibility.

---

## 🧩 Features

<details open>
<summary><b>Public website</b></summary>

- Home page with hero, about, treatments, consultation block, gallery, reviews and Instagram feed
- Treatment pages with images, duration, price (optional), FAQ and "ask about this treatment"
- Optional **video per treatment**: uploaded in the panel, compressed in the background with ffmpeg (H.264 + AAC, CRF 23, max. 1920 px / 30 fps, faststart, metadata stripped) and shown on the detail page only when present
- Appointment request form, contact form, newsletter sign-up
- Gallery with categories and before/after images, customer reviews (moderated)
- **Customer accounts** without passwords (6-digit e-mail code): profile, own booking requests, chat history
- **Live chat** between visitors and the studio
- Legal pages (imprint, privacy, aftercare …) freely editable
- "Coming soon" maintenance mode, custom error pages
- Lighthouse 97–100 (performance, accessibility, best practices, SEO)

</details>

<details>
<summary><b>Admin panel</b> (Persian, Turkish, German, English)</summary>

- Dashboard, appointments with status workflow, messages, chats, customer accounts
- Treatments, gallery, reviews, home page, about, contact & social media, pages, newsletter, all site texts
- Language tabs on every multilingual field, missing-translation warnings
- **AI translation** (Google Gemini) next to every multilingual input — results are placed in the right fields, never saved automatically
- **Site colors**: 20 ready three-color palettes + own palette, 8 color groups with ~20 matching suggestions each, automatic readable text color on buttons (WCAG AA)
- **Google Search Console**: verification code field, sitemap address and an 8-step guide
- Gmail-first e-mail setup with a step-by-step guide, Telegram notifications
- Image upload with automatic WebP resizing, drag & drop ordering, trash with restore
- Backup & restore as a single zip, automatic daily backups
- Users with roles (Admin / Editor), first-run setup wizard
- Mobile friendly with bottom navigation

</details>

<details>
<summary><b>Under the hood</b></summary>

- **.NET 10**, ASP.NET Core MVC + Razor, three layers: `DataAccess` → `Business` → `Web` (Admin as MVC Area)
- **EF Core + SQLite**, migrations applied automatically, seed data in 5 languages
- **ASP.NET Core Identity** for the panel, separate session-based customer accounts
- **Semantic CSS tokens** + `/theme.css` generated from panel settings (only changed values, cache-busted)
- Strict **Content-Security-Policy** (no inline scripts/styles), HSTS option, rate limiting, honeypot spam protection
- Secrets (SMTP password, Telegram token, Gemini key) encrypted with ASP.NET Data Protection
- Serilog rolling logs, response compression, minified & versioned assets, long-term static caching
- No external requests: fonts, libraries (Quill, SortableJS) and images are served locally

</details>

---

## 🌍 Multilingual by design

| Language | Direction | Example URL |
|---|---|---|
| فارسی (default) | RTL | `/fa/services` |
| Türkçe | LTR | `/tr/hizmetler` |
| Deutsch | LTR | `/de/behandlungen` |
| English | LTR | `/en/treatments` |
| العربية | RTL | `/ar/treatments` |

Each language can be switched on or off, set as default and use native digits (۱۲۳).

---

## 🚀 Quick start

```bash
git clone <this repository>
cd Negin
dotnet run --project src/BeautyByNegin.Web
```

Open `http://localhost:5xxx/admin` — the **setup wizard** creates the first admin.
In development, e-mails (login codes) are written to `App_Data/dev-mail/` instead of being sent.

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download).

---

## 🐳 Run with Docker

```bash
docker compose up -d --build
# site: http://localhost:8080   ·   data in named volumes (App_Data, uploads)
```

Reset an admin password:

```bash
docker compose exec web dotnet BeautyByNegin.Web.dll admin reset-password <user> <new-password>
```

---

## ☁️ Deploy on Render

The repository contains a [`render.yaml`](render.yaml) Blueprint and a Render-specific [`Dockerfile.render`](Dockerfile.render).

1. Push this repository to GitHub.
2. In Render: **New → Blueprint** → select the repository.
3. Enter a password for `Site__InitialAdmin__Password` (the first admin `admin` is created automatically).
4. Deploy, then open `https://<your-service>.onrender.com/admin`.

> **Use a paid instance with a disk.** On Render's free plan the file system is temporary: the SQLite database and uploaded images are reset after every deploy, restart or 15‑minute spin-down (the setup wizard would then be open again).
> The Blueprint therefore uses the `starter` plan with a 1 GB disk mounted at `/app/App_Data` – database, keys, backups **and** images/videos are stored there (`Dockerfile.render` links `wwwroot/uploads` into it). A service created by hand needs: Dockerfile Path `./Dockerfile.render`, a paid instance type, and a disk with mount path `/app/App_Data`.

### 🎬 Video compression (ffmpeg)

Treatment videos are compressed with **ffmpeg**. The Docker images install it automatically. On other servers:

- **Linux:** `sudo apt install ffmpeg`
- **Windows / Plesk:** put `ffmpeg.exe` and `ffprobe.exe` (e.g. from the "essentials" build at gyan.dev) into `App_Data\tools\` – or set `Site:FfmpegPath`.

Without ffmpeg the panel still accepts MP4 files up to 100 MB unchanged and shows a hint.

---

## 🖥️ Hosting & deployment

Self-contained publish for Windows/Plesk, Linux VPS (systemd + Nginx + Let's Encrypt), Docker, domain/SSL changes and moving between countries are described in detail in the [deployment guide (Turkish)](docs/deployment-tr.md).
The admin user guide (Persian) is in [`docs/panel-rehberi-fa.md`](docs/panel-rehberi-fa.md) and is also shown inside the panel under **Help**.

```text
src/
├─ BeautyByNegin.DataAccess   EF Core, entities, migrations, seed data
├─ BeautyByNegin.Business     services: content, settings, e-mail, Telegram, AI translation, theme, backup
└─ BeautyByNegin.Web          MVC site, Admin area, middleware, wwwroot
docs/                          guides and screenshots
Dockerfile · docker-compose.yml · Dockerfile.render · render.yaml
```

---

## 🔒 Privacy & security

- No Google Fonts, no CDN, no analytics, no tracking cookies
- Strict CSP, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`, optional HSTS
- Uploaded files are re-encoded; only images are accepted and served
- Login lockout, rate limits on forms, chat and login codes
- All data stays in `App_Data/` and `wwwroot/uploads/` on your own server

---

<div align="center">

Designed & developed by **Amir Reza Afshar** · built with AI-assisted vibe coding

</div>
