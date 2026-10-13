using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages;

/// <summary>One search box across the directory and the property listings, results shown in two groups.</summary>
public class SearchModel(AppDbContext db, CityClock clock) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }

    public bool Searched { get; private set; }
    public List<Place> Places { get; private set; } = new();
    public List<Property> Properties { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var terms = Queries.Terms(Q);
        if (terms.Length == 0)
            return;
        Searched = true;

        var places = db.PublicPlaces();
        foreach (var t in terms)
        {
            var like = $"%{EscapeLike(t)}%";
            places = places.Where(p =>
                EF.Functions.ILike(p.Name, like, "\\") ||
                EF.Functions.ILike(p.Category!.Name, like, "\\") ||
                (p.Category!.Parent != null && EF.Functions.ILike(p.Category.Parent.Name, like, "\\")) ||
                EF.Functions.ILike(p.Area!.Name, like, "\\") ||
                (p.Description != null && EF.Functions.ILike(p.Description, like, "\\")) ||
                p.Phone.Contains(t));
        }
        Places = await places.OrderByDescending(p => p.IsVerified).ThenBy(p => p.Name).Take(30).ToListAsync();

        // Property types are enums, so map Arabic words like "شقة" to the type.
        var types = Enum.GetValues<PropertyType>().Where(pt => terms.Any(t => pt.Ar().Contains(t) || t.Contains(pt.Ar()))).ToList();
        var props = PropertySearch.Apply(db.PropertiesWithDetails(), new PropertyFilter(), clock.UtcNow);
        foreach (var t in terms)
        {
            var like = $"%{EscapeLike(t)}%";
            var isType = types.Any(pt => pt.Ar().Contains(t) || t.Contains(pt.Ar()));
            props = props.Where(p =>
                (isType && types.Contains(p.Type)) ||
                EF.Functions.ILike(p.Area!.Name, like, "\\") ||
                (p.Description != null && EF.Functions.ILike(p.Description, like, "\\")));
        }
        Properties = await props.Take(30).ToListAsync();
    }

    private static string EscapeLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
