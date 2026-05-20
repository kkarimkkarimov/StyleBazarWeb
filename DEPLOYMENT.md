# StyleBazar — Deploy üçün təlimatlar

Bu sənəd StyleBazar-ı production-a deploy etmək üçün lazım olan **bütün** addımları, env-var-ları və yoxlamaları əhatə edir. Hər deploy-dan əvvəl **bütün checklistlər keçirilməlidir.**

---

## 📦 1. Tələblər

| Komponent | Versiya | Qeyd |
|---|---|---|
| .NET SDK | 10.0+ | Build üçün |
| .NET Runtime | 10.0+ | Server-də |
| SQL Server | 2019+ | Azure SQL, AWS RDS, və ya self-hosted |
| SMTP relay | — | SendGrid, AWS SES, Mailgun, Postmark və ya öz mail serveri |
| Reverse proxy | — | nginx və ya IIS, HTTPS termination üçün |

---

## 🔐 2. Tələb olunan ENV var-lar

Hər iki proyekt (MVC + API) eyni dəyərləri istifadə edir. Production-da bunlar **mütləqdir** — `Program.cs:ValidateRequiredSecrets()` startup-da yoxlayır və əskik dəyər olarsa app boot olmayacaq.

```bash
# === Database ===
ConnectionStrings__DefaultConnection="Server=sqlserver.stylebazar.az,1433;Database=StyleBazar;User Id=stylebazar_app;Password=<STRONG>;TrustServerCertificate=False;Encrypt=True"

# === JWT signing key (≥32 chars, 256 bits) ===
# Generate: openssl rand -base64 48
Jwt__Key="<256-bit-base64-string>"

# === Admin bootstrap user (AdminSeeder yaradacaq, sonra dəyişə bilməz) ===
Admin__Password="<STRONG-PASSWORD>"

# === SMTP — şifrə bərpası emaili üçün ===
# appsettings.Production.json-da Email:Provider=Smtp + Host/Port/Username/FromAddress
# qalanı set edilməlidir, password yalnız env var olaraq:
Email__Password="<SMTP-API-KEY>"
```

Optional override-lar:
```bash
ASPNETCORE_ENVIRONMENT=Production    # appsettings.Production.json-u yükləyir
ASPNETCORE_URLS=http://+:5000        # reverse proxy üçün port
```

`__` (ikili underscore) konfiq səviyyəsi ayırıcısıdır — `Jwt:Key` → `Jwt__Key`.

---

## 🔑 3. Secret rotation

| Secret | Tezlik | Necə |
|---|---|---|
| `Jwt__Key` | Rüb (3 ay) | Yeni key generate et, env var dəyiş, app restart. Aktiv token-lər 60 dəq ərzində istifadədən düşür (ExpireMinutes=60). |
| `Admin__Password` | İlk login-dən sonra | İlk login + dəyiş. Bu env var yalnız ilk seed üçündür. |
| SQL Server password | Yarım il | Read-write user şifrəsini dəyiş, dərhal env var update + restart. |
| `Email__Password` | İl | SMTP/API provider-də key rotate, env update. |

---

## 🗄 4. İlk deploy

```bash
# 1. Build
dotnet publish Stylebazar.MVC -c Release -o /opt/stylebazar/mvc
dotnet publish Stylebazar.API -c Release -o /opt/stylebazar/api

# 2. DB yarat və migration tətbiq et
sqlcmd -S sqlserver.stylebazar.az -d master -Q "CREATE DATABASE StyleBazar;"
dotnet ef database update --project Stylebazar.DAL --startup-project Stylebazar.MVC

# 3. Env var-ları set et (systemd, IIS, Azure App Service-də provider-spesifik)
export ConnectionStrings__DefaultConnection="..."
export Jwt__Key="..."
export Admin__Password="..."
export Email__Password="..."

# 4. Start
cd /opt/stylebazar/mvc && dotnet Stylebazar.MVC.dll
```

İlk startup-da görəcəyiniz log lines:

```
[INF] RoleSeeder — Buyer/Seller/Admin rolları yaradıldı
[INF] AdminSeeder — admin@stylebazar.local bootstrap edildi
[INF] SubscriptionSeeder: backfilled 0 user(s) with Free subscription rows.
[INF] Now listening on: http://0.0.0.0:5000
```

---

## 🔐 5. HTTPS + reverse proxy

Production-da Kestrel-i **birbaşa internet üzərinə açmayın**. nginx termination tövsiyə olunur:

```nginx
server {
    listen 443 ssl http2;
    server_name stylebazar.az www.stylebazar.az;

    ssl_certificate     /etc/letsencrypt/live/stylebazar.az/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/stylebazar.az/privkey.pem;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}

# HTTP → HTTPS redirect
server {
    listen 80;
    server_name stylebazar.az www.stylebazar.az;
    return 301 https://$server_name$request_uri;
}
```

HSTS app tərəfindən aktivdir (`UseHsts()` — 30 gün default). HTTPS-i 30 gündən tez söndürmək lazım olsa, `UseHsts` parametrini override et.

---

## 💾 6. SQL backup

Tövsiyə edilən cədvəl (Azure SQL üçün avtomatik aktivdir; self-hosted üçün cron):

| Backup növü | Tezlik | Saxlama |
|---|---|---|
| Full | Həftəlik (Bazar günü 02:00) | 4 həftə |
| Differential | Hər gün 02:00 | 7 gün |
| Transaction log | 15 dəq | 24 saat |

**Disaster recovery hədəfi:** RPO ≤ 15 dəq, RTO ≤ 1 saat.

---

## 📊 7. Log rotation

Serilog konfiqurasiyası `appsettings.json` → `Serilog:WriteTo[1].Args`:
- `rollingInterval: "Day"` — günlük yeni fayl
- `retainedFileCountLimit: 7` — 7 gün saxla, sonra avtomatik sil

Log fayllarının ölçüsü çox böyüyürsə `fileSizeLimitBytes` parametri əlavə et (məs. 100 MB hər fayl).

Production-da log monitoring sistemi (Seq, Datadog, Splunk, ELK) tövsiyə olunur — `Serilog.Sinks.Seq` və ya `Serilog.Sinks.ElasticSearch` package-ləri var.

---

## 🩺 8. Health check

`GET /health` endpoint-i geri qaytarır:
- **200** + body "Healthy" — DB əlçatandır, app sağlamdır
- **503** + body "Unhealthy" — DB əlçatansız və ya başqa kritik problem

Load balancer-də bu endpoint-i monitor edin. Recommended interval: 30 saniyə.

---

## 🚀 9. Smoke test checklist (deploy-dan sonra)

```bash
BASE="https://stylebazar.az"

# 1. Health
curl -s "$BASE/health" | grep -q "Healthy" && echo "✅ Health" || echo "❌ Health"

# 2. Homepage
curl -s -o /dev/null -w "%{http_code}" "$BASE/" | grep -q "200" && echo "✅ Home"

# 3. Compression
curl -sI -H "Accept-Encoding: br" "$BASE/" | grep -qi "content-encoding: br" && echo "✅ Brotli"

# 4. Security headers
curl -sI "$BASE/" | grep -qi "content-security-policy" && echo "✅ CSP"

# 5. Rate limit
for i in $(seq 1 12); do curl -s -o /dev/null -w "%{http_code}\n" -X POST "$BASE/Account/Login" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  --data "Email=bad@bad.local&Password=Wrong"; done | tail -2
# Sonuncu iki dəyər 429 olmalıdır

# 6. SMTP send test (admin kimi login → forgot password)
# Manual: gerçək email gəldiyini yoxla
```

---

## 🆘 10. Rollback prosedürü

Migration geriyə qaytarmaq lazım olarsa:

```bash
# Hansı migration aktivdir?
dotnet ef migrations list --project Stylebazar.DAL --startup-project Stylebazar.MVC

# Əvvəlki migration-a qayıt
dotnet ef database update <EarlierMigrationName> --project Stylebazar.DAL --startup-project Stylebazar.MVC

# Sonra app-i restart et (DB schema mismatch-i ilə çalışmayacaq)
```

**Diqqət:** Cascade-li delete olan migration-ları geriyə qaytarmaq data itkisinə səbəb ola bilər. Kritik dəyişikliklərdən əvvəl `git tag deploy-YYYY-MM-DD` ilə revision-u qeyd edin.

---

## 📞 İletişim

Deploy zamanı problem olarsa: `admin@stylebazar.local` və ya GitHub Issues.
