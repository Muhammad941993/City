using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Admin;

/// <summary>Review queue: new listings, suggested corrections, listings about to expire, and visitor reports.</summary>
public class IndexModel(AppDbContext db, Moderation moderation, CityClock clock) : PageModel
{
    public List<Property> PendingProperties { get; private set; } = new();
    public List<Place> PendingPlaces { get; private set; } = new();
    public Dictionary<int, Place> Originals { get; private set; } = new();
    public List<Property> Expiring { get; private set; } = new();
    public List<Report> Reports { get; private set; } = new();

    public int PublishedProperties { get; private set; }
    public int PublishedPlaces { get; private set; }
    public int ClosedThisMonth { get; private set; }
    public List<(string Name, int Count)> TopCategories { get; private set; } = new();

    [TempData] public string? Flash { get; set; }

    public async Task OnGetAsync()
    {
        var now = clock.UtcNow;
        PendingProperties = await db.PropertiesWithDetails()
            .Where(p => p.Status == ListingStatus.Pending).OrderBy(p => p.CreatedAt).ToListAsync();
        PendingPlaces = await db.Places.AsNoTracking()
            .Include(p => p.Category).Include(p => p.Area).Include(p => p.Images)
            .Where(p => p.Status == ListingStatus.Pending).OrderBy(p => p.CreatedAt).ToListAsync();

        var originalIds = PendingPlaces.Where(p => p.EditOfPlaceId != null).Select(p => p.EditOfPlaceId!.Value).ToList();
        Originals = await db.Places.AsNoTracking().Include(p => p.Category).Include(p => p.Area)
            .Where(p => originalIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var soon = now + ListingRules.ReminderBefore;
        Expiring = await db.PropertiesWithDetails()
            .Where(p => p.Status == ListingStatus.Published && p.ExpiresAt != null && p.ExpiresAt > now && p.ExpiresAt <= soon)
            .OrderBy(p => p.ExpiresAt).ToListAsync();

        Reports = await db.Reports.AsNoTracking().Include(r => r.Place).Include(r => r.Property)
            .Where(r => !r.IsResolved).OrderByDescending(r => r.CreatedAt).Take(50).ToListAsync();

        PublishedProperties = await db.Properties.WherePubliclyVisible(now).CountAsync(p => p.Status == ListingStatus.Published);
        PublishedPlaces = await db.Places.CountAsync(p => p.Status == ListingStatus.Published && p.EditOfPlaceId == null);
        var monthAgo = now.AddDays(-30);
        ClosedThisMonth = await db.Properties.CountAsync(p => p.Status == ListingStatus.Closed && p.ClosedAt > monthAgo);

        var top = await db.Places.Where(p => p.Status == ListingStatus.Published && p.EditOfPlaceId == null)
            .GroupBy(p => p.Category!.Parent != null ? p.Category.Parent.Name : p.Category!.Name)
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(5).ToListAsync();
        TopCategories = top.Select(x => (x.Key, x.Count)).ToList();
    }

    public async Task<IActionResult> OnPostApprovePropertyAsync(int id)
    {
        if (await moderation.ApprovePropertyAsync(id)) Flash = "تم نشر الإعلان لمدة 30 يوم.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectPropertyAsync(int id, string? reason)
    {
        if (await moderation.RejectPropertyAsync(id, reason)) Flash = "تم رفض الإعلان.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRenewPropertyAsync(int id)
    {
        if (await moderation.ApprovePropertyAsync(id)) Flash = "تم تجديد الإعلان 30 يوم.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClosePropertyAsync(int id)
    {
        if (await moderation.ClosePropertyAsync(id)) Flash = "تم تعليم الإعلان: تم البيع / التأجير.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApprovePlaceAsync(int id)
    {
        if (await moderation.ApprovePlaceAsync(id) is not null) Flash = "تم نشر البيانات.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectPlaceAsync(int id, string? reason)
    {
        if (await moderation.RejectPlaceAsync(id, reason)) Flash = "تم الرفض.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResolveReportAsync(int id)
    {
        var r = await db.Reports.FindAsync(id);
        if (r is not null)
        {
            r.IsResolved = true;
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
