using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Services;

/// <summary>Shared read queries for the public pages.</summary>
public static class Queries
{
    public static IQueryable<Place> PublicPlaces(this AppDbContext db) =>
        db.Places.AsNoTracking()
            .Where(p => p.Status == ListingStatus.Published && p.EditOfPlaceId == null)
            .Include(p => p.Category).ThenInclude(c => c!.Parent)
            .Include(p => p.Area)
            .Include(p => p.Images);

    public static IQueryable<Property> PropertiesWithDetails(this AppDbContext db) =>
        db.Properties.AsNoTracking()
            .Include(p => p.Area)
            .Include(p => p.Images);

    public static Task<List<Place>> EmergencyAsync(this AppDbContext db) =>
        db.PublicPlaces()
            .Where(p => p.Category!.Slug == "emergency" || p.Category!.Parent!.Slug == "emergency")
            .OrderBy(p => p.Phone.Length).ThenBy(p => p.Name)
            .Take(6)
            .ToListAsync();

    public static Task<List<Area>> AreasAsync(this AppDbContext db) =>
        db.Areas.AsNoTracking().OrderBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync();

    public static Task<List<Category>> TopCategoriesAsync(this AppDbContext db) =>
        db.Categories.AsNoTracking().Where(c => c.ParentId == null).OrderBy(c => c.SortOrder).ToListAsync();

    /// <summary>Splits a search string into words, ignoring very short ones.</summary>
    public static string[] Terms(string? q) =>
        (q ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2).Take(5).ToArray();
}
