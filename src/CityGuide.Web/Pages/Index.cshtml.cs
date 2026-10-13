using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages;

public class IndexModel(AppDbContext db, CityClock clock) : PageModel
{
    public List<Category> Categories { get; private set; } = new();
    public List<Property> Latest { get; private set; } = new();
    public List<Place> Emergency { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await db.TopCategoriesAsync();
        Latest = await PropertySearch.Apply(db.PropertiesWithDetails(), new PropertyFilter(), clock.UtcNow)
            .Take(6).ToListAsync();
        Emergency = await db.EmergencyAsync();
    }
}
