# دليل المدينة — City Guide

A local Arabic (RTL) website for one city, built for mobile first. It has two sections:

- **Directory (أرقام وخدمات):** shops, services and emergency numbers by category, with Call and WhatsApp buttons, opening hours and an "open now" status.
- **Real estate (عقارات):** listings for sale and rent, each marked owner (مالك) or broker (سمسار), with filters, a photo gallery and a ready-made WhatsApp message.

Visitors submit listings without an account. Nothing is shown until the admin approves it from `/admin`.

## Stack

| Part | Choice |
|---|---|
| Web | ASP.NET Core 8 Razor Pages (pages are rendered on the server, which helps SEO) |
| Database | PostgreSQL with EF Core (Npgsql) |
| Images | SkiaSharp: re-encoded to WebP (1280px plus a 480px thumbnail), EXIF data removed |
| Admin login | ASP.NET Identity (only the admin has an account) |
| Spam protection | Cloudflare Turnstile (optional; turned off when no keys are set) |
| UI | Hand-written CSS, no framework, matching the design canvas. Plain JS (about 5 KB) that only adds enhancements |

## Project layout

```
src/CityGuide.Web/
  Domain/      entities, enums, listing rules (30-day publish, 7-day "sold" badge), opening hours, phone helpers
  Data/        AppDbContext, migrations, seeder (8 categories, placeholder districts, emergency numbers, admin user)
  Services/    search/filtering, image store, moderation, Turnstile, hourly expiry job, form models
  Pages/       public pages + Admin/ (review queue, properties, places, categories & districts)
tests/CityGuide.Tests/   xUnit tests for rules, search, moderation, images
deploy/      systemd unit, nginx config, environment file example
```

## Run locally

Requires the .NET 8 SDK and PostgreSQL.

```bash
# Postgres on localhost with user/password postgres (see appsettings.json)
dotnet run --project src/CityGuide.Web
# → http://localhost:5172   admin: /admin  (admin@local.test / ChangeMe-2026 from appsettings.Development.json)
dotnet test
```

The database is created and migrated automatically on startup. To add a migration after changing the model:

```bash
dotnet tool restore
dotnet ef migrations add <Name> -p src/CityGuide.Web -o Data/Migrations
```

## Deploy (small Linux VPS)

1. Install the .NET 8 runtime, PostgreSQL and nginx. Create a `cityguide` database and user.
2. `dotnet publish src/CityGuide.Web -c Release -o /opt/cityguide/app`
3. Copy `deploy/cityguide.env.example` to `/opt/cityguide/cityguide.env` and fill it in.
4. Install `deploy/cityguide.service` (systemd) and `deploy/nginx.conf`, then run `certbot --nginx` for HTTPS.
5. Set `Site__BaseUrl` to the real domain so share previews (Open Graph) and `sitemap.xml` use it.

Back up the database and `/opt/cityguide/uploads`.

## Still to decide (open questions in the spec)

- Site name and domain: set with `Site:CityName` and `Site:BaseUrl`.
- The official list of districts: the seeder adds placeholders, which the admin edits at `/admin/taxonomy`.
- The site's WhatsApp number (`Site:WhatsApp`), used for people who prefer sending listings by chat.

## Main URLs

| URL | Page |
|---|---|
| `/` | Home: search, category icons, newest listings, emergency numbers |
| `/directory`, `/directory/{slug}` | Categories, then one category with sub-category and district filters |
| `/places/{id}`, `/places/add` | Place page (with a "report an error" form), add a place or suggest an edit |
| `/properties`, `/properties/{id}`, `/properties/add` | Listing list with filters, listing page, 3-step add form |
| `/search?q=` | Results split into directory and real estate |
| `/admin` | Review queue, listings expiring within 3 days (WhatsApp reminder), visitor reports, stats |
