using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Admin.Places;

public class IndexModel(AppDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public ListingStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public int? Category { get; set; }
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }

    public List<Place> Items { get; private set; } = new();
    public List<Category> Categories { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await db.TopCategoriesAsync();
        var q = db.Places.AsNoTracking().Include(p => p.Category).Include(p => p.Area).Where(p => p.EditOfPlaceId == null);
        if (Status is { } s) q = q.Where(p => p.Status == s);
        if (Category is { } c) q = q.Where(p => p.CategoryId == c || p.Category!.ParentId == c);
        foreach (var t in Queries.Terms(Q))
        {
            var digits = Phone.Normalize(t);
            q = q.Where(p => p.Name.Contains(t) || (digits.Length >= 3 && p.Phone.Contains(digits)));
        }
        Items = await q.OrderBy(p => p.Name).Take(300).ToListAsync();
    }
}
