using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Places;

public class DetailsModel(AppDbContext db, Turnstile captcha) : PageModel
{
    public Place? Place { get; private set; }
    public string? Message { get; private set; }
    public bool ShowReport { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, bool reported = false)
    {
        Place = await db.PublicPlaces().FirstOrDefaultAsync(p => p.Id == id);
        if (Place is null)
            return NotFound();
        if (reported)
            Message = "شكراً! وصلنا البلاغ وهنراجع البيانات.";
        return Page();
    }

    public async Task<IActionResult> OnPostReportAsync(int id, string? message)
    {
        if (!await db.PublicPlaces().AnyAsync(p => p.Id == id))
            return NotFound();

        if (string.IsNullOrWhiteSpace(message) || !await captcha.VerifyAsync(HttpContext))
        {
            ShowReport = true;
            return await OnGetAsync(id);
        }

        db.Reports.Add(new Report
        {
            PlaceId = id,
            Message = message.Trim()[..Math.Min(500, message.Trim().Length)],
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return RedirectToPage(new { id, reported = true });
    }
}
