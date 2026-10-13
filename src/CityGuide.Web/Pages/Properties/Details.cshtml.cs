using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Properties;

public class DetailsModel(AppDbContext db, CityClock clock, Turnstile captcha) : PageModel
{
    public Property? Property { get; private set; }
    public List<Property> Similar { get; private set; } = new();
    public string? Message { get; private set; }
    public bool ShowReport { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, bool reported = false)
    {
        var now = clock.UtcNow;
        Property = await db.PropertiesWithDetails().WherePubliclyVisible(now).FirstOrDefaultAsync(p => p.Id == id);
        if (Property is null)
            return NotFound();

        Similar = await PropertySearch.Similar(db.PropertiesWithDetails(), Property, now).Take(3).ToListAsync();
        if (reported)
            Message = "شكراً! وصلنا البلاغ وهنراجع الإعلان.";
        return Page();
    }

    public async Task<IActionResult> OnPostReportAsync(int id, string? message)
    {
        if (!await db.Properties.WherePubliclyVisible(clock.UtcNow).AnyAsync(p => p.Id == id))
            return NotFound();

        if (string.IsNullOrWhiteSpace(message) || !await captcha.VerifyAsync(HttpContext))
        {
            ShowReport = true;
            return await OnGetAsync(id);
        }

        var text = message.Trim();
        db.Reports.Add(new Report { PropertyId = id, Message = text[..Math.Min(500, text.Length)], CreatedAt = clock.UtcNow });
        await db.SaveChangesAsync();
        return RedirectToPage(new { id, reported = true });
    }
}
