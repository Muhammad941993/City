using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Admin.Properties;

public class IndexModel(AppDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public ListingStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }

    public List<Property> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var q = db.PropertiesWithDetails();
        if (Status is { } s) q = q.Where(p => p.Status == s);
        foreach (var t in Queries.Terms(Q))
        {
            var digits = Phone.Normalize(t);
            q = q.Where(p => p.AdvertiserName.Contains(t) || p.Area!.Name.Contains(t) ||
                             (p.Description != null && p.Description.Contains(t)) ||
                             (digits.Length >= 4 && p.AdvertiserPhone.Contains(digits)) ||
                             p.Id.ToString() == t);
        }
        Items = await q.OrderByDescending(p => p.UpdatedAt).Take(200).ToListAsync();
    }
}
