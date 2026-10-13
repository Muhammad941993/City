using System.ComponentModel.DataAnnotations;

namespace CityGuide.Web.Domain;

/// <summary>A district / neighbourhood used for filtering.</summary>
public class Area
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = "";

    public int SortOrder { get; set; }
}

/// <summary>
/// Directory category. Top-level categories (ParentId null) are shown as icons;
/// their children are the filter chips on the category page.
/// </summary>
public class Category
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = "";

    /// <summary>Latin slug used in the URL, e.g. /directory/health.</summary>
    [MaxLength(60)]
    public string Slug { get; set; } = "";

    /// <summary>Icon key rendered by the _Icon partial.</summary>
    [MaxLength(30)]
    public string Icon { get; set; } = "store";

    public int SortOrder { get; set; }

    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = new();
}

/// <summary>A shop, service, clinic, government office… in the city directory.</summary>
public class Place
{
    public int Id { get; set; }

    [MaxLength(150)]
    public string Name { get; set; } = "";

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int AreaId { get; set; }
    public Area? Area { get; set; }

    [MaxLength(20)]
    public string Phone { get; set; } = "";

    /// <summary>Optional separate WhatsApp number; falls back to Phone when it is a mobile.</summary>
    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(500)]
    public string? MapUrl { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool Is24Hours { get; set; }
    public TimeOnly? OpensAt { get; set; }
    public TimeOnly? ClosesAt { get; set; }
    public DayOfWeek? WeeklyDayOff { get; set; }

    public bool IsVerified { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Pending;

    [MaxLength(300)]
    public string? RejectionReason { get; set; }

    /// <summary>When set, this row is a visitor's suggested correction to another place.</summary>
    public int? EditOfPlaceId { get; set; }

    [MaxLength(100)]
    public string? SubmitterName { get; set; }

    [MaxLength(20)]
    public string? SubmitterPhone { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<PlaceImage> Images { get; set; } = new();
}

public class Property
{
    public int Id { get; set; }

    public ListingPurpose Purpose { get; set; }
    public PropertyType Type { get; set; }

    /// <summary>Price in EGP. For rent it is the monthly rent.</summary>
    public decimal Price { get; set; }

    /// <summary>Size in square metres.</summary>
    public int Size { get; set; }

    public int AreaId { get; set; }
    public Area? Area { get; set; }

    public int? Rooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? Floor { get; set; }
    public FinishingType? Finishing { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? MapUrl { get; set; }

    [MaxLength(100)]
    public string AdvertiserName { get; set; } = "";

    [MaxLength(20)]
    public string AdvertiserPhone { get; set; } = "";

    public AdvertiserType AdvertiserType { get; set; }

    public bool IsVerified { get; set; }
    public bool IsFeatured { get; set; }

    public ListingStatus Status { get; set; } = ListingStatus.Pending;

    [MaxLength(300)]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public List<PropertyImage> Images { get; set; } = new();

    /// <summary>e.g. "شقة للبيع · 140 متر · 3 غرف".</summary>
    public string Title
    {
        get
        {
            var t = $"{Type.Ar()} {Purpose.Ar()} · {Size} متر";
            return Rooms is > 0 ? $"{t} · {Rooms} غرف" : t;
        }
    }
}

public class PropertyImage
{
    public int Id { get; set; }
    public int PropertyId { get; set; }

    /// <summary>Base file name; the stored files are {FileName}.webp and {FileName}_t.webp.</summary>
    [MaxLength(80)]
    public string FileName { get; set; } = "";

    public int SortOrder { get; set; }
}

public class PlaceImage
{
    public int Id { get; set; }
    public int PlaceId { get; set; }

    [MaxLength(80)]
    public string FileName { get; set; } = "";

    public int SortOrder { get; set; }
}

/// <summary>A visitor report: wrong number, fake listing, etc.</summary>
public class Report
{
    public int Id { get; set; }
    public int? PlaceId { get; set; }
    public Place? Place { get; set; }
    public int? PropertyId { get; set; }
    public Property? Property { get; set; }

    [MaxLength(500)]
    public string Message { get; set; } = "";

    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
}
