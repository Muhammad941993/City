using CityGuide.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Data;

/// <summary>Applies migrations and inserts the starting categories, areas and the admin account.</summary>
public static class Seeder
{
    // Top-level categories from the spec, with their sub-categories (shown as filter chips).
    private static readonly (string Name, string Slug, string Icon, string[] Children)[] Categories =
    [
        ("صحة", "health", "health", ["صيدليات", "دكاترة", "معامل", "مستشفيات"]),
        ("حرفيون", "crafts", "tools", ["سباك", "كهربائي", "نجار", "نقاش"]),
        ("مطاعم وكافيهات", "food", "food", ["مطاعم", "أفران", "حلواني", "كافيهات"]),
        ("تعليم", "education", "school", ["مدارس", "حضانات", "سنترات"]),
        ("تسوق", "shopping", "cart", ["سوبر ماركت", "ملابس", "أدوات منزلية"]),
        ("خدمات حكومية", "government", "gov", ["سجل مدني", "بريد", "شهر عقاري"]),
        ("مواصلات", "transport", "bus", ["مواقف", "سيارات أجرة"]),
        ("طوارئ", "emergency", "siren", ["إسعاف", "مطافئ", "شرطة"]),
    ];

    // Placeholders until the official list of districts is decided (open question in the spec).
    private static readonly string[] Areas = ["وسط البلد", "الحي الأول", "الحي الثاني", "الحي الثالث"];

    public static async Task RunAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (db.Database.IsRelational())
            await db.Database.MigrateAsync();

        await SeedTaxonomyAsync(db);
        await SeedAdminAsync(scope.ServiceProvider, config, logger);
    }

    public static async Task SeedTaxonomyAsync(AppDbContext db)
    {
        if (!await db.Categories.AnyAsync())
        {
            var order = 0;
            foreach (var (name, slug, icon, children) in Categories)
            {
                var parent = new Category { Name = name, Slug = slug, Icon = icon, SortOrder = order++ };
                var childOrder = 0;
                foreach (var child in children)
                {
                    parent.Children.Add(new Category
                    {
                        Name = child,
                        Slug = $"{slug}-{childOrder + 1}",
                        Icon = icon,
                        SortOrder = childOrder++,
                    });
                }
                db.Categories.Add(parent);
            }
        }

        if (!await db.Areas.AnyAsync())
        {
            for (var i = 0; i < Areas.Length; i++)
                db.Areas.Add(new Area { Name = Areas[i], SortOrder = i });
        }

        await db.SaveChangesAsync();

        // Emergency numbers are needed from day one, so they are seeded as published places.
        if (!await db.Places.AnyAsync())
        {
            var area = await db.Areas.OrderBy(a => a.SortOrder).FirstAsync();
            var emergency = await db.Categories.Include(c => c.Children).FirstAsync(c => c.Slug == "emergency");
            var now = DateTime.UtcNow;
            foreach (var (name, phone) in new[] { ("إسعاف", "123"), ("مطافئ", "180"), ("شرطة النجدة", "122") })
            {
                var sub = emergency.Children.FirstOrDefault(c => name.StartsWith(c.Name)) ?? emergency;
                db.Places.Add(new Place
                {
                    Name = name,
                    CategoryId = sub.Id,
                    AreaId = area.Id,
                    Phone = phone,
                    Is24Hours = true,
                    IsVerified = true,
                    Status = ListingStatus.Published,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedAdminAsync(IServiceProvider sp, IConfiguration config, ILogger logger)
    {
        var email = config["Admin:Email"];
        var password = config["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin:Email / Admin:Password not configured; no admin account was created.");
            return;
        }

        var users = sp.GetRequiredService<UserManager<IdentityUser>>();
        if (await users.FindByEmailAsync(email) is not null)
            return;

        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            logger.LogError("Could not create admin: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
