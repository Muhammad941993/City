using CityGuide.Web.Domain;

namespace CityGuide.Web.Services;

/// <summary>Filters from the properties list page (all optional).</summary>
public class PropertyFilter
{
    public ListingPurpose? Purpose { get; set; }
    public PropertyType? Type { get; set; }
    public int? AreaId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinSize { get; set; }
    public int? MaxSize { get; set; }
    public AdvertiserType? Advertiser { get; set; }
    public PropertySort Sort { get; set; } = PropertySort.Newest;

    public bool HasAdvancedFilters =>
        Type is not null || AreaId is not null || MinPrice is not null || MaxPrice is not null ||
        MinSize is not null || MaxSize is not null || Advertiser is not null || Sort != PropertySort.Newest;
}

public static class PropertySearch
{
    public static IQueryable<Property> Apply(IQueryable<Property> q, PropertyFilter f, DateTime nowUtc)
    {
        q = q.WherePubliclyVisible(nowUtc);

        if (f.Purpose is { } purpose) q = q.Where(p => p.Purpose == purpose);
        if (f.Type is { } type) q = q.Where(p => p.Type == type);
        if (f.AreaId is { } area) q = q.Where(p => p.AreaId == area);
        if (f.MinPrice is { } minP) q = q.Where(p => p.Price >= minP);
        if (f.MaxPrice is { } maxP) q = q.Where(p => p.Price <= maxP);
        if (f.MinSize is { } minS) q = q.Where(p => p.Size >= minS);
        if (f.MaxSize is { } maxS) q = q.Where(p => p.Size <= maxS);
        if (f.Advertiser is { } adv) q = q.Where(p => p.AdvertiserType == adv);

        // Featured listings first, sold/rented last, then the requested order.
        var ordered = q.OrderByDescending(p => p.IsFeatured)
                       .ThenBy(p => p.Status == ListingStatus.Closed);

        return f.Sort switch
        {
            PropertySort.PriceAsc => ordered.ThenBy(p => p.Price).ThenByDescending(p => p.Id),
            PropertySort.PriceDesc => ordered.ThenByDescending(p => p.Price).ThenByDescending(p => p.Id),
            _ => ordered.ThenByDescending(p => p.PublishedAt).ThenByDescending(p => p.Id),
        };
    }

    /// <summary>Same purpose and type, preferring the same area and a similar price.</summary>
    public static IQueryable<Property> Similar(IQueryable<Property> q, Property p, DateTime nowUtc) =>
        q.WherePubliclyVisible(nowUtc)
         .Where(x => x.Id != p.Id && x.Purpose == p.Purpose && x.Type == p.Type && x.Status == ListingStatus.Published)
         .OrderByDescending(x => x.AreaId == p.AreaId)
         .ThenBy(x => x.Price > p.Price ? x.Price - p.Price : p.Price - x.Price);
}
