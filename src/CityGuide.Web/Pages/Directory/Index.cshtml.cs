using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Directory;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Category> Categories { get; private set; } = new();

    /// <summary>Published places per top-level category.</summary>
    public Dictionary<int, int> Counts { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await db.Categories.AsNoTracking()
            .Where(c => c.ParentId == null)
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var perCategory = await db.PublicPlaces()
            .GroupBy(p => p.Category!.ParentId ?? p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();
        Counts = perCategory.ToDictionary(x => x.Key, x => x.Count);
    }
}
