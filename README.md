# Beauty by Negin

Cilt bakımı ve facial hizmetleri için çok dilli web sitesi ve yönetim paneli.

- **Teknoloji:** .NET 10 (LTS), ASP.NET Core MVC + Razor, EF Core + SQLite, ASP.NET Core Identity
- **Diller:** Farsça (RTL), Türkçe, Almanca, İngilizce, Arapça (RTL). Her dil panelden açılıp kapatılabilir.
- **Harici istek yok:** CDN, Google Fonts, Google Maps veya analitik yok. Fontlar, kütüphaneler ve görseller sunucunun kendisinden gelir.
- **Taşınabilir:** Değişen tüm veriler iki klasörde durur: `App_Data/` ve `wwwroot/uploads/`. Taşınmak için klasörü kopyalamak ya da panelden alınan yedek zip'ini geri yüklemek yeterlidir.

---

## İçindekiler

1. [Proje yapısı](#1-proje-yapısı)
2. [Lokal geliştirme ve çalıştırma](#2-lokal-geliştirme-ve-çalıştırma)
3. [İlk kurulum sihirbazı](#3-ilk-kurulum-sihirbazı)
4. [Yayın (publish) seçenekleri](#4-yayın-publish-seçenekleri)
5. [Plesk (Windows / IIS) hostinge kurulum](#5-plesk-windows--iis-hostinge-kurulum)
6. [Linux VPS (Ubuntu + Nginx + systemd + Let's Encrypt)](#6-linux-vps-ubuntu--nginx--systemd--lets-encrypt)
7. [Docker ile çalıştırma](#7-docker-ile-çalıştırma)
8. [Domain değiştirme ve SSL](#8-domain-değiştirme-ve-ssl)
9. [Taşınma senaryosu: İran → Türkiye → Almanya](#9-taşınma-senaryosu-i̇ran--türkiye--almanya)
10. [Şifre sıfırlama (komut satırı)](#10-şifre-sıfırlama-komut-satırı)
11. [E-posta ve Telegram bildirimleri](#11-e-posta-ve-telegram-bildirimleri)
12. [Yapılandırma](#12-yapılandırma)
13. [Teknik kararlar ve nedenleri](#13-teknik-kararlar-ve-nedenleri)
14. [Lisanslar](#14-lisanslar)
15. [Sorun giderme](#15-sorun-giderme)

---

## 1. Proje yapısı

Üç katmanlı mimari:

```
BeautyByNegin.sln
├── src/
│   ├── BeautyByNegin.DataAccess/   Veri katmanı: entity'ler, AppDbContext, migration'lar, seed verisi
│   ├── BeautyByNegin.Business/     İş katmanı: içerik, ayarlar, görsel işleme, e-posta/Telegram, yedekleme, çöp kutusu
│   └── BeautyByNegin.Web/          Sunum katmanı: ziyaretçi sayfaları, Areas/Admin (yönetim paneli), middleware
│       ├── App_Data/               (çalışırken oluşur) site.db, logs/, backups/, keys/
│       └── wwwroot/uploads/        (çalışırken oluşur) yüklenen görseller (yalnızca WebP)
├── docs/panel-rehberi-fa.md        Farsça panel kullanım kılavuzu (panelde "Yardım" menüsünde de görünür)
├── Dockerfile
└── docker-compose.yml
```

- **Çok dilli içerik:** Her içerik tablosunun bir çeviri tablosu var (ör. `Service` + `ServiceTranslation`). Bir dilde metin yoksa önce varsayılan dil, sonra herhangi bir dil gösterilir.
- **URL'ler:** `/fa/...`, `/tr/...`, `/de/...`, `/en/...`, `/ar/...`. Sayfa adları dile göre çevrilir (ör. `/de/behandlungen`, `/tr/hizmetler`, `/en/treatments`). Kapalı bir dilin adresleri varsayılan dile yönlendirilir (404 değil).
- **Yönetim paneli:** `Areas/Admin` altında, varsayılan adres `/admin`. Panel dili kullanıcı başına seçilir (FA/TR/DE/EN).
- **Silme işlemi:** İçerikler önce çöp kutusuna gider (soft delete) ve 30 gün sonra otomatik silinir.
- **Müşteri hesapları:** Ziyaretçiler sitedeki "Giriş" butonuyla yalnızca e-postalarıyla kaydolur ve giriş yapar (şifre yok; e-postaya 6 haneli kod gelir, cihaz başına oturum, çerezde yalnızca rastgele token, veritabanında SHA-256 özeti). Giriş yapan müşteri sohbet edebilir, kendi randevu taleplerini ve durumlarını görebilir, (açıksa) yorum yazabilir, adını/telefonunu düzenleyebilir ve hesabını silebilir. Panele erişimleri yoktur. Sohbet yalnızca giriş yapmış müşterilere açıktır. Yetkiler panelde **Genel Ayarlar → Müşteri hesapları** bölümünden açılıp kapatılır; hesaplar **Müşteriler** ekranından yönetilir (engelleme, tüm cihazlardan çıkış, not, silme). E-posta (SMTP) ayarlanmadan hesaplar ve sohbet görünmez.
- **Örnek görseller:** İlk çalıştırmada `SampleContent/` klasöründeki marka renklerinde hazırlanmış görseller (hero, hakkımızda, danışmanlık, 10 hizmet, galeri, Instagram) ve örnek bir "bakım sonrası öneriler" sayfası **bir kez** eklenir. Admin sonradan silerse tekrar eklenmez. Gerçek fotoğraflar panelden yüklenip bunların yerine konur. İlk başlangıç bu yüzden 20–30 saniye sürebilir.

## 2. Lokal geliştirme ve çalıştırma

Gereken: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```bash
git clone <repo> && cd Negin
dotnet run --project src/BeautyByNegin.Web
```

Tarayıcıda `http://localhost:5204` adresini açın. **Visual Studio 2022/2026** kullanıyorsanız `BeautyByNegin.sln` dosyasını açıp F5'e basmanız yeterli.

İlk çalıştırmada:

- `src/BeautyByNegin.Web/App_Data/site.db` otomatik oluşur (migration'lar uygulanır).
- Başlangıç verisi yüklenir: 10 hizmet (5 dilde), site metinleri, galeri kategorileri, yasal sayfa taslakları ve yer tutucu görseller.
- `/admin` adresi kurulum sihirbazını açar.

Faydalı komutlar:

```bash
# Yeni migration (model değiştiğinde)
dotnet tool restore
dotnet ef migrations add <Ad> --project src/BeautyByNegin.DataAccess --startup-project src/BeautyByNegin.Web

# Production modunda dene (sıkıştırma, minify, önbellek açık)
ASPNETCORE_ENVIRONMENT=Production dotnet run --project src/BeautyByNegin.Web
```

> **E-postasız yerel test:** `appsettings.Development.json` içindeki `"Site": { "DevMailToFile": true }` sayesinde, Development ortamında SMTP ayarlanmamışsa e-postalar (ör. müşteri giriş kodu) gönderilmez; `App_Data/dev-mail/` klasörüne ve Visual Studio'nun **Output** penceresine yazılır. Böylece "Giriş" butonu ve sohbet bilgisayarınızda da görünür ve test edilebilir. Production'da bu ayar yoktur; orada gerçek SMTP gerekir.
>
> Site adresi `/` her zaman panelde seçilen **ana dile** (varsayılan: Farsça) yönlenir; tarayıcı dili dikkate alınmaz.

> Geliştirme ortamında (`Development`) CSS/JS dosyaları olduğu gibi gelir ve yanıt sıkıştırma kapalıdır (Visual Studio'nun tarayıcı yenileme özelliği bozulmasın diye). `Production` ortamında dosyalar küçültülür ve sıkıştırılır.

## 3. İlk kurulum sihirbazı

Veritabanında hiç kullanıcı yoksa `/admin` adresi otomatik olarak `/admin/setup` sihirbazına yönlenir. Sihirbazda şunlar girilir:

- Yönetici kullanıcı adı, görünen ad ve şifre (en az 8 karakter ve en az 1 rakam)
- Panel dili
- Sitenin varsayılan dili
- Ülke (seçince saat dilimi otomatik dolar) ve saat dilimi
- Telefon, WhatsApp, e-posta

Kurulum bittikten sonra sihirbaz bir daha açılmaz. Ek kullanıcılar panelde **Kullanıcılar** menüsünden eklenir.

## 4. Yayın (publish) seçenekleri

### a) Self-contained publish (önerilen: ucuz Plesk hostlar)

.NET runtime'ı uygulamanın içine koyar. Sunucuda .NET yüklü olmasa ya da eski bir sürüm olsa bile çalışır.

```bash
# Windows sunucu (Plesk / IIS)
dotnet publish src/BeautyByNegin.Web -c Release -r win-x64 --self-contained true -o publish/win-x64

# Linux sunucu
dotnet publish src/BeautyByNegin.Web -c Release -r linux-x64 --self-contained true -o publish/linux-x64
```

Çıktı yaklaşık 130 MB'tır ve `BeautyByNegin.Web.exe` (Windows) ya da `BeautyByNegin.Web` (Linux) dosyasını içerir.

### b) Framework-dependent publish

Sunucuda **ASP.NET Core Runtime 10** kurulu olmalıdır. Çıktı daha küçüktür (yaklaşık 55 MB) ve runtime güncellemelerini sunucu yapar.

```bash
dotnet publish src/BeautyByNegin.Web -c Release -o publish/app
# çalıştırma: dotnet BeautyByNegin.Web.dll
```

Her iki durumda da:

- Publish çıktısı `App_Data/` ve `wwwroot/uploads/` klasörlerini **içermez**. Bu sayede güncelleme yüklerken canlı veriler ezilmez.
- `web.config` (IIS için) otomatik üretilir. 2 GB'a kadar yedek dosyası yüklenebilmesi için ayarlıdır.
- `docs/panel-rehberi-fa.md` çıktıya kopyalanır, böylece panelin "Yardım" sayfası çalışır.

## 5. Plesk (Windows / IIS) hostinge kurulum

1. **Publish alın:** Bilgisayarınızda bölüm 4a'daki `win-x64` self-contained komutunu çalıştırın.
2. **Plesk'te siteyi hazırlayın:**
   - *Websites & Domains → Hosting Settings*: Belge kökü (document root) olarak ör. `httpdocs` kalabilir.
   - *ASP.NET Settings* (veya *Dedicated IIS Application Pool*): **.NET CLR version = "No Managed Code"** seçin. ASP.NET Core kendi işlemini yönetir.
   - Hosting sağlayıcısının sunucuda **ASP.NET Core Module (ANCM V2)** kurulu olmalıdır. Plesk'in Windows sunucularında genelde kuruludur. Emin değilseniz destek ekibine "ASP.NET Core Hosting Bundle kurulu mu?" diye sorun. Self-contained publish'te runtime sürümü önemli değildir, sadece modül gerekir.
3. **Dosyaları yükleyin:** `publish/win-x64` klasörünün **içeriğini** Plesk *File Manager* veya FTP ile `httpdocs` içine yükleyin. Büyük dosya sayısı için zip yükleyip Plesk'te "Extract" kullanmak daha hızlıdır.
4. **Yazma izni verin:** *File Manager*'da `httpdocs` klasörünün yanındaki kilit simgesi (*Change Permissions*) ile uygulama havuzu kullanıcısına (ör. `IIS APPPOOL\...` veya Plesk'in site kullanıcısı) **yazma/değiştirme** izni verin. En azından şu klasörler için gereklidir:
   - `App_Data` (yoksa siz oluşturun)
   - `wwwroot\uploads` (yoksa siz oluşturun)
5. **Siteyi açın:** `https://alanadiniz.com/admin` adresinde kurulum sihirbazı açılır.
6. **SSL:** Plesk'te *SSL/TLS Certificates → Let's Encrypt* ile ücretsiz sertifika alın. Sertifika çalıştıktan sonra panelde **Genel Ayarlar → Güvenlik** bölümünden "Her zaman https ile açılsın" seçeneğini açın (bkz. bölüm 8).

**Güncelleme yüklerken:** Yeni publish çıktısını aynı klasöre kopyalayın. `App_Data` ve `wwwroot\uploads` publish çıktısında olmadığı için verileriniz korunur. Dosyalar kilitliyse önce kök klasöre `app_offline.htm` adında boş bir dosya koyun (site geçici olarak durur), kopyalama bitince silin.

> **İpucu:** Plesk "500.30 / 500.31" hatası verirse `web.config` içinde `stdoutLogEnabled="true"` yapın ve `App_Data\logs\stdout*.log` dosyasına bakın. Uygulamanın kendi logları her zaman `App_Data\logs\site-*.log` içindedir.

## 6. Linux VPS (Ubuntu + Nginx + systemd + Let's Encrypt)

Aşağıdaki adımlar Ubuntu 24.04 içindir. Alan adınızın DNS **A kaydı** sunucunun IP adresini göstermelidir.

### 6.1 Uygulamayı yükleyin

Bilgisayarınızda Linux için publish alın (bölüm 4a, `linux-x64`) ve sunucuya kopyalayın:

```bash
# bilgisayarınızda
rsync -avz publish/linux-x64/ kullanici@SUNUCU_IP:/tmp/bbn/

# sunucuda
sudo useradd --system --home /var/www/beautybynegin --shell /usr/sbin/nologin bbn
sudo mkdir -p /var/www/beautybynegin
sudo rsync -a /tmp/bbn/ /var/www/beautybynegin/
sudo chown -R bbn:bbn /var/www/beautybynegin
sudo chmod +x /var/www/beautybynegin/BeautyByNegin.Web
```

> Framework-dependent publish kullanacaksanız önce runtime'ı kurun: `sudo apt install -y aspnetcore-runtime-10.0`. Ubuntu deposunda yoksa Microsoft paket deposunu ekleyin. Bu durumda aşağıdaki `ExecStart` satırı `/usr/bin/dotnet /var/www/beautybynegin/BeautyByNegin.Web.dll` olur.

Ubuntu'da ICU kütüphanesi genelde kuruludur (Farsça takvim ve Türkçe/Almanca dil verileri için gereklidir). Yoksa: `sudo apt install -y libicu74` (sürüm numarası dağıtıma göre değişir).

### 6.2 systemd servisi

`/etc/systemd/system/beautybynegin.service`:

```ini
[Unit]
Description=Beauty by Negin
After=network.target

[Service]
User=bbn
WorkingDirectory=/var/www/beautybynegin
ExecStart=/var/www/beautybynegin/BeautyByNegin.Web
Restart=always
RestartSec=5
KillSignal=SIGINT
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=DOTNET_NOLOGO=1

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now beautybynegin
sudo systemctl status beautybynegin       # "active (running)" görmelisiniz
journalctl -u beautybynegin -f            # canlı log
```

### 6.3 Nginx (reverse proxy)

```bash
sudo apt install -y nginx
```

`/etc/nginx/sites-available/beautybynegin`:

```nginx
server {
    listen 80;
    server_name beautybynegin.com www.beautybynegin.com;

    # Yedek zip'i geri yükleme ve görsel yükleme için
    client_max_body_size 2G;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   X-Forwarded-Host  $host;
        proxy_read_timeout 300s;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/beautybynegin /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx
```

Uygulama aynı sunucudaki proxy'den gelen `X-Forwarded-*` başlıklarına güvenir. Böylece https ve ziyaretçi IP'si (spam koruması için) doğru algılanır.

### 6.4 Let's Encrypt (ücretsiz SSL)

```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d beautybynegin.com -d www.beautybynegin.com
```

Certbot Nginx ayarına 443 bloğunu ekler ve sertifikayı otomatik yeniler (`systemctl list-timers | grep certbot`). Sonra panelde https yönlendirmesini açın (bölüm 8).

### 6.5 Güncelleme

```bash
sudo systemctl stop beautybynegin
sudo rsync -a --exclude App_Data --exclude wwwroot/uploads /tmp/bbn/ /var/www/beautybynegin/
sudo chown -R bbn:bbn /var/www/beautybynegin
sudo systemctl start beautybynegin
```

Veritabanı değişiklikleri (migration'lar) uygulama başlarken otomatik uygulanır.

### 6.6 Güvenlik duvarı (öneri)

```bash
sudo ufw allow OpenSSH && sudo ufw allow 'Nginx Full' && sudo ufw enable
```

## 7. Docker ile çalıştırma

Sunucuda Docker ve Docker Compose kurulu olmalıdır.

```bash
git clone <repo> && cd Negin
docker compose up -d --build
```

- Site `http://127.0.0.1:8080` adresinde çalışır. Port yalnızca sunucunun kendisine açıktır; dışarıya Nginx (bölüm 6.3, `proxy_pass http://127.0.0.1:8080;`) veya Caddy ile SSL'li olarak yayınlayın.
- Veriler iki Docker volume'ünde durur: `appdata` (`/app/App_Data`) ve `uploads` (`/app/wwwroot/uploads`). Konteyneri silmek veya yeniden build etmek verileri silmez.
- Konteyner root olmayan `app` kullanıcısı ile çalışır.
- Ortam değişkeni `Site__TrustAllProxies=true` ayarlıdır, çünkü proxy konteynerin dışından (Docker ağı üzerinden) gelir.

Faydalı komutlar:

```bash
docker compose logs -f                      # loglar
git pull && docker compose up -d --build    # güncelleme
docker compose exec web dotnet BeautyByNegin.Web.dll admin list
docker compose exec web dotnet BeautyByNegin.Web.dll admin reset-password negin YeniSifre123
```

Yedek almanın en kolay yolu yine panelden **Yedekleme → Yedeği indir**'dir.

## 8. Domain değiştirme ve SSL

1. Yeni alan adının DNS **A kaydını** sunucunun IP adresine yönlendirin.
2. Sunucu ayarına yeni adı ekleyin:
   - Plesk: *Add Domain* veya *Domain Aliases*
   - Nginx: `server_name` satırı
3. SSL sertifikası alın:
   - Plesk: *Let's Encrypt*
   - Linux: `certbot --nginx -d yenidomain.com`
4. Panelde **Genel Ayarlar → Genel → Sitenin ana adresi** alanına yeni adresi yazın, ör. `https://yenidomain.com`. Bu adres canonical URL, hreflang, sitemap.xml, Open Graph ve e-postalardaki linklerde kullanılır.
5. SSL çalışıyorsa (adres kilitli açılıyorsa) **Genel Ayarlar → Güvenlik** bölümünde:
   - **Her zaman https ile açılsın** seçeneğini açın.
   - **HSTS** seçeneğini, her şeyin https ile sorunsuz çalıştığından emin olduktan sonra açın. HSTS, tarayıcıya bir yıl boyunca yalnızca https kullanmasını söyler; geri almak zordur.
6. Eski domainden yeni domaine kalıcı (301) yönlendirme ekleyin (Plesk: *Hosting Settings → Preferred domain / Redirect*; Nginx: ayrı bir `server` bloğunda `return 301 https://yenidomain.com$request_uri;`).

> İlk kurulumda SSL yoksa site http üzerinden normal çalışır. https yönlendirmesi varsayılan olarak kapalıdır, böylece site bozulmaz.

## 9. Taşınma senaryosu: İran → Türkiye → Almanya

Yedek zip'i **her şeyi** içerir:

- Veritabanı (içerikler, çeviriler, ayarlar, randevu talepleri, mesajlar, kullanıcılar)
- Yüklenen tüm görseller
- Şifreleme anahtarları (`App_Data/keys`). Bunlar sayesinde SMTP şifresi ve Telegram token'ı yeni sunucuda da çözülebilir ve oturumlar geçerli kalır.

Adımlar:

1. **Eski sunucuda yedek alın:** Panel → **Yedekleme → Yedeği indir**. Zip dosyasını güvenli bir yere kaydedin.
2. **Yeni sunucuya kurun:** Bölüm 5, 6 veya 7'deki adımlarla siteyi boş olarak kurun.
3. **Geri yükleyin:**
   - Yeni sitede kurulum sihirbazını geçici bir kullanıcıyla tamamlayın.
   - Panel → **Yedekleme → Yedekten geri yükle**'ye gidin, zip'i seçin, onay kutusunu işaretleyin ve geri yükleyin.
   - Geri yüklemeden önce mevcut durumun otomatik bir yedeği alınır.
   - Ardından **yedekteki** kullanıcı adı ve şifreyle giriş yapın. Geçici kullanıcı kaybolur.
4. **Ayarları güncelleyin:**
   - **Genel Ayarlar:** ülke (saat dilimi otomatik değişir), şehir, sitenin ana adresi
   - **İletişim ve Sosyal Medya:** telefon, WhatsApp, adres, harita linki, çalışma saatleri
   - **Diller:** yeni varsayılan dil (Türkiye için `tr`, Almanya için `de`) ve dil sırası. Farsça kapatılmak zorunda değildir; sıralamada aşağı alınabilir.
   - **Sayfalar:** Almanya için *Impressum* ve *Datenschutz* sayfalarındaki `[ ]` alanlarını doldurun ve bir hukukçuya kontrol ettirin.
   - **Genel Ayarlar → Site bölümleri:** Gizlilik onayı zorunluluğu (DSGVO) Almanya'da açık kalmalıdır.
   - **E-posta (SMTP) ve Telegram:** Yeni sunucudan test mesajı gönderin. Telegram İran'daki sunuculardan erişilemez, Türkiye ve Almanya'da çalışır.
5. **Domain ve SSL:** Domain değişiyorsa bölüm 8'i uygulayın.
6. **Eski sunucuyu kapatmadan önce** yeni sitede bir test randevusu gönderin ve panelde göründüğünü kontrol edin.

Alternatif (paneline erişilemeyen durumlar için): Uygulama dururken `App_Data/` ve `wwwroot/uploads/` klasörlerini olduğu gibi yeni sunucudaki aynı yerlere kopyalamak da yeterlidir.

## 10. Şifre sıfırlama (komut satırı)

E-posta ayarlıysa giriş ekranındaki **"Şifremi unuttum"** linki kullanılır. E-posta yoksa sunucuda komut satırından:

```bash
# Linux / Docker (uygulama klasöründe)
./BeautyByNegin.Web admin reset-password <kullanici> <YeniSifre123>       # self-contained
dotnet BeautyByNegin.Web.dll admin reset-password <kullanici> <YeniSifre123>   # framework-dependent

# Windows
BeautyByNegin.Web.exe admin reset-password <kullanici> <YeniSifre123>

# Diğer komutlar
... admin list                                     # kullanıcıları listele
... admin create <kullanici> <Sifre123> [Admin|Editor]   # yeni kullanıcı (ilk girişte şifre değiştirilir)
```

- Komut hesabın kilidini de açar.
- Uygulama çalışırken de çalıştırılabilir.
- Uygulama, hangi klasörden çağrılırsa çağrılsın kendi klasöründeki `App_Data`'yı kullanır.

**Plesk'te komut satırı yoksa:**

1. *Websites & Domains → Scheduled Tasks → Add Task* ekranını açın.
2. *Run a command* seçin ve komut olarak `C:\...\httpdocs\BeautyByNegin.Web.exe` girin. Argüman: `admin reset-password negin YeniSifre123`.
3. **Run Now** ile bir kez çalıştırın.
4. Ardından görevi silin. Görevde şifre açık yazılıdır.

## 11. E-posta ve Telegram bildirimleri

- **Her talep önce veritabanına kaydedilir:** randevu, iletişim mesajı, sohbet, bülten aboneliği. Bildirimler arka planda gönderilir. E-posta veya Telegram çalışmasa bile hiçbir talep kaybolmaz; hepsi panelde görünür. (İran'da Telegram ve bazı yabancı SMTP sunucuları engellidir.)
- **E-posta (SMTP):** Panel → Genel Ayarlar → E-posta sunucusu bilgileri. Gerekli olduğu yerler:
  - Sohbet doğrulama kodu (sohbet butonu ancak SMTP ayarlıysa görünür)
  - Yeni talep bildirimleri
  - Şifre sıfırlama
- **Telegram botu:** @BotFather'dan token alın, panele girin, botunuza Telegram'da *Start* deyin, panelde **Chat ID bul** ve **Test mesajı gönder** butonlarına basın. Bildirim türleri ayrı ayrı seçilir (randevu, mesaj, sohbet, bülten).
- SMTP şifresi ve Telegram token'ı veritabanında **şifrelenmiş** olarak saklanır (ASP.NET Data Protection, anahtarlar `App_Data/keys`).

## 12. Yapılandırma

`appsettings.json` neredeyse hiç değiştirilmez; ayarların tamamı panelden yapılır. Değiştirilebilecekler:

| Anahtar | Varsayılan | Açıklama |
|---|---|---|
| `ConnectionStrings:Default` | `Data Source=\|DataDirectory\|/site.db` | `\|DataDirectory\|` = `App_Data` klasörü |
| `Site:AdminPath` | `/admin` | Panel adresi (ör. `/yonetim` yaparak tahmin edilmesini zorlaştırabilirsiniz) |
| `Site:TrustAllProxies` | `false` | Proxy farklı bir makinede/konteynerdeyse `true` (Docker'da açık) |

Ortam değişkeni olarak da verilebilir: `Site__AdminPath=/yonetim`.

**Veritabanı sağlayıcısını değiştirmek** (ör. ileride SQL Server veya PostgreSQL):

1. `BeautyByNegin.DataAccess` projesine ilgili EF Core paketini ekleyin.
2. `DataAccessSetup.AddDataAccess` içindeki `UseSqlite` satırını değiştirin.
3. Yeni sağlayıcı için migration'ları yeniden oluşturun.

Kodun geri kalanı sağlayıcıdan bağımsızdır.

## 13. Teknik kararlar ve nedenleri

- **SQLite:** Tek dosya, kurulum gerektirmez, yedeklemesi ve taşıması kolaydır. Bu ölçekteki bir site için fazlasıyla hızlıdır.
- **Çalışma anında küçültme (runtime minification):** CSS/JS dosyaları ilk istekte NUglify ile küçültülür, bellekte tutulur ve içerik hash'iyle (`?v=...`) versiyonlanır. Böylece Node.js, npm veya bir build adımı gerekmez; Visual Studio'dan publish almak yeterlidir ve her hostta aynı şekilde çalışır. Dosya değişirse otomatik yenilenir.
- **Görseller:** Yüklenen her görsel SixLabors.ImageSharp ile işlenir:
  - İçeriği doğrulanır (sahte uzantılı dosyalar reddedilir).
  - Döndürmesi düzeltilir ve EXIF/GPS bilgisi silinir.
  - Panelde seçilen kırpma uygulanır.
  - 480/960/1600 px genişliklerde **WebP**'ye çevrilir.
  - Dosya adları rastgeledir. `/uploads` altından yalnızca `.webp` dosyaları sunulur.
- **Harici istek yok:**
  - Fontlar (Cormorant Garamond, Jost, Vazirmatn) lokal `woff2` dosyalarıdır.
  - Kütüphaneler (Quill, Cropper.js, SortableJS) `wwwroot/lib` içindedir.
  - Google Maps yerine harita linki ve isteğe bağlı statik harita görseli kullanılır.
  - Gizlilik (DSGVO) ve hız için bu önemlidir.
- **Katı Content-Security-Policy:**
  - Satır içi (inline) script ve stil yoktur.
  - Tüm script'ler ayrı dosyalardadır.
  - Yalnızca kendi alan adından kaynak yüklenebilir.
- **Spam koruması:** Captcha yerine (Google reCAPTCHA harici istek olurdu):
  - gizli honeypot alanı
  - en az 3 saniyelik doldurma süresi
  - IP başına hız sınırı
  - antiforgery token
- **Önbellek:**
  - İçerikler bellekte önbelleklenir; panelde her kayıtta önbellek temizlenir, değişiklik anında görünür.
  - Statik dosyalar ve görseller uzun süreli tarayıcı önbelleğiyle sunulur.
- **Lighthouse (mobil, Production):**
  - Performans: 98–100
  - Erişilebilirlik: 100
  - En İyi Uygulamalar: 100
  - SEO: 100

## 14. Lisanslar

| Bileşen | Lisans | Not |
|---|---|---|
| **SixLabors.ImageSharp** 3.x | **Six Labors Split License** | Açık kaynak projelerde, başka bir paketin bağımlılığı olarak ve **yıllık brüt geliri 1 milyon ABD dolarının altındaki** şirketlerde ücretsizdir (Apache 2.0 koşulları). Bu sınırın üstündeki ticari kullanım için Six Labors'tan ücretli lisans gerekir. Tek kişilik bir güzellik salonu için ücretsiz kullanım kapsamındadır. Ayrıntı: sixlabors.com/pricing |
| MailKit | MIT | |
| HtmlSanitizer | MIT | |
| NUglify | BSD-2-Clause | |
| Serilog | Apache 2.0 | |
| EF Core, ASP.NET Core | MIT | |
| Quill | BSD-3-Clause | `wwwroot/lib/quill` |
| Cropper.js | MIT | `wwwroot/lib/cropperjs` |
| SortableJS | MIT | `wwwroot/lib/sortablejs` |
| Cormorant Garamond, Jost, Vazirmatn | SIL Open Font License 1.1 | `wwwroot/fonts` |

## 15. Sorun giderme

| Belirti | Çözüm |
|---|---|
| IIS'te **500.30 / 500.31** | `web.config` içinde `stdoutLogEnabled="true"` yapın ve `App_Data\logs\stdout*.log` dosyasına bakın. Genelde yazma izni eksiktir (bölüm 5, adım 4) ya da ANCM modülü kurulu değildir. |
| IIS'te **500.19** | Sunucuda ASP.NET Core Module yok. Hosting sağlayıcısından "ASP.NET Core Hosting Bundle" kurmasını isteyin. |
| Görsel yüklenmiyor | `wwwroot/uploads` klasörüne yazma izni verin. Linux'ta: `sudo chown -R bbn:bbn /var/www/beautybynegin`. |
| Büyük yedek geri yüklenmiyor (413) | Nginx'te `client_max_body_size 2G;` satırını ekleyin. IIS için `web.config` zaten 2 GB'a ayarlıdır. |
| Panel girişinden sonra tekrar girişe dönüyor | Tarayıcı çerezlerini temizleyin. Sunucu taşındıysa `App_Data/keys` klasörünün de taşındığından emin olun (yedek zip'i bunu otomatik yapar). |
| https altında sonsuz yönlendirme | Proxy `X-Forwarded-Proto` başlığını göndermiyor. Bölüm 6.3'teki `proxy_set_header` satırlarını ekleyin; Docker'da `Site__TrustAllProxies=true` olmalıdır. |
| Farsça tarihler veya Türkçe karakterler bozuk (Linux) | ICU kütüphanesini kurun: `sudo apt install libicu74` (sürüm dağıtıma göre değişir). |
| E-posta gitmiyor | Panelde **Test e-postası gönder**'e basın; hata mesajı gösterilir. Port 587 + SSL/TLS veya 465 deneyin. Hosting firmanız dış SMTP'yi engelliyor olabilir. |
| Telegram bildirimi gelmiyor | İran'daki sunuculardan Telegram'a erişilemez; bu normaldir, talepler panelde görünür. Başka ülkede: botunuza *Start* dediğinizden ve Chat ID'nin dolu olduğundan emin olun. |
| Şifre unutuldu | Bölüm 10. |
| Loglar | `App_Data/logs/site-YYYYMMDD.log` (günlük dosya, 30 gün saklanır) |
