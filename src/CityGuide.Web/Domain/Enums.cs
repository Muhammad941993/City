namespace CityGuide.Web.Domain;

/// <summary>Lifecycle of anything a visitor submits (properties and places).</summary>
public enum ListingStatus
{
    Pending = 0,
    Published = 1,
    Rejected = 2,
    /// <summary>Sold or rented. Stays visible with a badge for a few days.</summary>
    Closed = 3,
    Expired = 4,
}

public enum ListingPurpose
{
    Sale = 0,
    Rent = 1,
}

public enum PropertyType
{
    Apartment = 0,
    House = 1,
    Land = 2,
    Shop = 3,
    Warehouse = 4,
}

public enum AdvertiserType
{
    Owner = 0,
    Broker = 1,
}

public enum FinishingType
{
    Full = 0,
    Half = 1,
    None = 2,
}

public enum PropertySort
{
    Newest = 0,
    PriceAsc = 1,
    PriceDesc = 2,
}

public static class EnumText
{
    public static string Ar(this ListingStatus s) => s switch
    {
        ListingStatus.Pending => "قيد المراجعة",
        ListingStatus.Published => "منشور",
        ListingStatus.Rejected => "مرفوض",
        ListingStatus.Closed => "تم البيع / التأجير",
        ListingStatus.Expired => "منتهي",
        _ => s.ToString(),
    };

    public static string Ar(this ListingPurpose p) => p == ListingPurpose.Sale ? "للبيع" : "للإيجار";

    public static string Ar(this PropertyType t) => t switch
    {
        PropertyType.Apartment => "شقة",
        PropertyType.House => "بيت",
        PropertyType.Land => "أرض",
        PropertyType.Shop => "محل",
        PropertyType.Warehouse => "مخزن",
        _ => t.ToString(),
    };

    public static string Ar(this AdvertiserType a) => a == AdvertiserType.Owner ? "مالك" : "سمسار";

    public static string Ar(this FinishingType f) => f switch
    {
        FinishingType.Full => "كامل",
        FinishingType.Half => "نصف تشطيب",
        FinishingType.None => "بدون تشطيب",
        _ => f.ToString(),
    };

    public static string Ar(this PropertySort s) => s switch
    {
        PropertySort.Newest => "الأحدث",
        PropertySort.PriceAsc => "الأقل سعراً",
        PropertySort.PriceDesc => "الأعلى سعراً",
        _ => s.ToString(),
    };

    /// <summary>"تم البيع" or "تم التأجير" depending on purpose.</summary>
    public static string ClosedText(this ListingPurpose p) => p == ListingPurpose.Sale ? "تم البيع" : "تم التأجير";
}
