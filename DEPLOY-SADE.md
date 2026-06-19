# StyleBazar — ən sadə deploy bələdçisi (texniki olmayanlar üçün)

Bu bələdçi saytı **canlıya çıxarmaq** üçün lazım olan hər addımı sadə dildə izah edir.
Bütün texniki hazırlıq artıq edilib — sənə yalnız aşağıdakı addımları izləmək qalır.

> **Dürüst qeyd — pul məsələsi:** 7/24 işləyən onlayn mağaza + verilənlər bazası
> tam **pulsuz** ola bilməz. Ən ucuz düzgün variant ayda **~5–7 AZN/€** olan bir
> serverdir (aşağıda göstərilir). Mən sənin əvəzinə server/domen ala bilmirəm
> (bunun üçün sənin hesabın və kartın lazımdır) — amma qalan hər şeyi hazırladım.

---

## Nə lazımdır (cəmi 2 şey alınır)

| Nə | Haradan | Təxmini qiymət |
|---|---|---|
| **Server** (Ubuntu, 4 GB RAM) | [Hetzner](https://www.hetzner.com/cloud) CX22 | ~€4.5/ay |
| **Domen** (məs. stylebazar.az) | namecheap / GoDaddy / .az üçün whois.az | ~10–25 AZN/il |

> Server üçün Hetzner ən ucuzdur. Alternativ: DigitalOcean, Contabo, Vultr.
> 4 GB RAM tövsiyə olunur (SQL Server üçün) — 2 GB-da işləyər, amma sıx olar.

---

## Addım-addım

### 1. Server al
- Hetzner Cloud-da qeydiyyatdan keç → yeni layihə → **Add Server**.
- Image: **Ubuntu 24.04**. Type: **CX22** (4 GB). Lokasiya: Almaniya/Finlandiya (Bakıya yaxın gecikmə).
- "Create & Buy". Bir-iki dəqiqəyə server hazır olur. Sənə bir **IP ünvanı** (məs. `49.12.x.x`) və **root parolu** verir.

### 2. Domeni serverə yönəlt
- Domen aldığın panelə gir → **DNS** bölməsi.
- Bir **A record** əlavə et:  `@`  →  serverin IP-si.
- (İstəsən) ikinci A record:  `www`  →  eyni IP.
- Yadda saxla. (Yayılması 5 dəq – bir neçə saat çəkə bilər.)

### 3. Serverə qoşul (Windows-dan)
- **PowerShell** aç və yaz (IP-ni öz serverininki ilə əvəz et):
  ```
  ssh root@49.12.x.x
  ```
- İlk dəfə "yes" yaz, sonra root parolunu daxil et.

### 4. Docker quraşdır
Serverdə bu bir sətri yapışdır (Docker-i quraşdırır):
```
curl -fsSL https://get.docker.com | sh
```

### 5. Kodu serverə gətir
```
git clone https://github.com/kkarimkkarimov/StyleBazarWeb.git
cd StyleBazarWeb
```

### 6. Secret-ləri yaz
```
cp .env.example .env
nano .env
```
Açılan redaktorda dəyərləri doldur (mən sənə hazır güclü dəyərlər verdim — söhbətə bax),
**DOMAIN-i öz domeninlə əvəz et**. Sonra: `Ctrl+O` → `Enter` → `Ctrl+X`.

### 7. İşə sal 🚀
```
docker compose -f docker-compose.prod.yml up -d --build
```
İlk dəfə build + SQL hazırlığı **3–6 dəqiqə** çəkir. Caddy avtomatik SSL (HTTPS)
sertifikatı alır — heç nə etməyə ehtiyac yoxdur.

### 8. Yoxla
- Brauzerdə **https://sənin-domenin** aç → sayt açılmalıdır (yaşıl kilid 🔒).
- Admin girişi: `admin@stylebazar.local` + `.env`-də qoyduğun **ADMIN_PASSWORD**.
- İlk girişdən sonra admin parolunu dəyiş.

---

## Gündəlik istifadə

| İş | Komanda (serverdə, layihə qovluğunda) |
|---|---|
| Logları izlə | `docker compose -f docker-compose.prod.yml logs -f mvc` |
| Yenilə (kod dəyişəndə) | `git pull && docker compose -f docker-compose.prod.yml up -d --build` |
| Dayandır | `docker compose -f docker-compose.prod.yml down` |
| Yenidən başlat | `docker compose -f docker-compose.prod.yml restart` |
| Statusa bax | `docker compose -f docker-compose.prod.yml ps` |

## Backup (vacib!)
Verilənlər bazası `sqldata`, şəkillər `mvc_uploads` adlı Docker volume-larında saxlanılır.
Həftədə bir dəfə server snapshot-u götür (Hetzner panelində "Snapshot" düyməsi) — ən asan backup.

---

## Email (sonradan, opsional)
Parol-bərpa emailləri üçün SendGrid (pulsuz başlanğıc paket) və ya başqa SMTP açarı
götür, `.env`-də `EMAIL_PASSWORD`-a yaz, `appsettings.Production.json`-da Email host/port-u
doldur, sonra `docker compose ... up -d` ilə yenidən başlat. Onsuz sayt tam işləyir,
yalnız "parolu unutdum" emaili getmir.

---

Daha ətraflı (server-administratoru səviyyəsi): bax → [DEPLOYMENT.md](DEPLOYMENT.md)
