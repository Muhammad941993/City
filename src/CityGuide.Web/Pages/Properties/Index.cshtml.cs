using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Properties;

public class IndexModel(AppDbContext db, CityClock clock) : PageModel
{
    public const int PageSize = 20;

    [BindProperty(SupportsGet = true)] public ListingPurpose? Purpose { get; set; }
    [BindProperty(SupportsGet = true)] public PropertyType? Type { get; set; }
    [BindProperty(SupportsGet = true)] public int? Area { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MinPrice { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MaxPrice { get; set; }
    [BindProperty(SupportsGet = true)] public int? MinSize { get; set; }
    [BindProperty(SupportsGet = true)] public int? MaxSize { get; set; }
    [BindProperty(SupportsGet = true)] public AdvertiserType? Advertiser { get; set; }
    [BindProperty(SupportsGet = true)] public PropertySort Sort { get; set; }
    [BindProperty(SupportsGet = true, Name = "page")] public int PageNumber { get; set; } = 1;

    public List<Property> Results { get; private set; } = new();
    public List<Area> Areas { get; private set; } = new();
    public int Total { get; private set; }
    public int Pages => (Total + PageSize - 1) / PageSize;
    public PropertyFilter Filter { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Purpose ??= ListingPurpose.Sale;
        PageNumber = Math.Max(1, PageNumber);
        Filter = new PropertyFilter
        {
            Purpose = Purpose,
            Type = Type,
            AreaId = Area,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            MinSize = MinSize,
            MaxSize = MaxSize,
            Advertiser = Advertiser,
            Sort = Sort,
        };

        var q = PropertySearch.Apply(db.PropertiesWithDetails(), Filter, clock.UtcNow);
        Total = await q.CountAsync();
        Results = await q.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync();
        Areas = await db.AreasAsync();
    }

    /// <summary>Current query string with some values replaced (null removes the key).</summary>
    public string Link(params (string Key, object? Value)[] changes)
    {
        var dict = Request.Query.ToDictionary(kv => kv.Key, kv => (string?)kv.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        dict.Remove("page");
        foreach (var (k, v) in changes)
            dict[k] = v?.ToString();
        var parts = dict.Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");
        var qs = string.Join("&", parts);
        return "/properties" + (qs.Length > 0 ? "?" + qs : "");
    }
}
