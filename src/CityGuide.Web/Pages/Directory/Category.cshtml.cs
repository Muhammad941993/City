using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Directory;

public class CategoryModel(AppDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public int? Sub { get; set; }
    [BindProperty(SupportsGet = true)] public int? Area { get; set; }

    public Category? Category { get; private set; }
    public List<Area> Areas { get; private set; } = new();
    public List<Place> Places { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        Category = await db.Categories.AsNoTracking()
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Slug == slug && c.ParentId == null);
        if (Category is null)
            return NotFound();

        Areas = await db.AreasAsync();

        var ids = Category.Children.Select(c => c.Id).Append(Category.Id).ToList();
        var q = db.PublicPlaces();
        q = Sub is { } sub && ids.Contains(sub)
            ? q.Where(p => p.CategoryId == sub)
            : q.Where(p => ids.Contains(p.CategoryId));
        if (Area is { } area)
            q = q.Where(p => p.AreaId == area);

        Places = await q.OrderByDescending(p => p.IsVerified).ThenBy(p => p.Name).Take(200).ToListAsync();
        return Page();
    }
}
