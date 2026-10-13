using System.ComponentModel.DataAnnotations;
using CityGuide.Web.Domain;

namespace CityGuide.Web.Services;

/// <summary>Form fields for a place, shared by the public form and the admin editor.</summary>
public class PlaceInput : IValidatableObject
{
    [Required(ErrorMessage = "اكتب اسم المكان")]
    [StringLength(150)]
    [Display(Name = "اسم المكان")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "اختار التصنيف")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "اختار المنطقة")]
    public int? AreaId { get; set; }

    [Required(ErrorMessage = "اكتب رقم التليفون")]
    [StringLength(20)]
    public string Phone { get; set; } = "";

    [StringLength(20)]
    public string? WhatsApp { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "اللينك مش صحيح")]
    public string? MapUrl { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool Is24Hours { get; set; }
    public TimeOnly? OpensAt { get; set; }
    public TimeOnly? ClosesAt { get; set; }
    public DayOfWeek? WeeklyDayOff { get; set; }

    [StringLength(100)]
    public string? SubmitterName { get; set; }

    [StringLength(20)]
    public string? SubmitterPhone { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (!Domain.Phone.IsValid(Domain.Phone.Normalize(Phone)))
            yield return new ValidationResult("رقم التليفون مش صحيح", [nameof(Phone)]);
        if (!string.IsNullOrWhiteSpace(WhatsApp) && !Domain.Phone.IsMobile(Domain.Phone.Normalize(WhatsApp)))
            yield return new ValidationResult("رقم الواتساب لازم يكون موبايل (01xxxxxxxxx)", [nameof(WhatsApp)]);
        if (!Is24Hours && (OpensAt is null) != (ClosesAt is null))
            yield return new ValidationResult("اكتب ميعاد الفتح والقفل الاتنين", [nameof(OpensAt)]);
        if (!string.IsNullOrWhiteSpace(MapUrl) && !MapUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult("لينك الخريطة لازم يبدأ بـ https://", [nameof(MapUrl)]);
    }

    public static PlaceInput From(Place p) => new()
    {
        Name = p.Name,
        CategoryId = p.CategoryId,
        AreaId = p.AreaId,
        Phone = p.Phone,
        WhatsApp = p.WhatsApp,
        Address = p.Address,
        MapUrl = p.MapUrl,
        Description = p.Description,
        Is24Hours = p.Is24Hours,
        OpensAt = p.OpensAt,
        ClosesAt = p.ClosesAt,
        WeeklyDayOff = p.WeeklyDayOff,
        SubmitterName = p.SubmitterName,
        SubmitterPhone = p.SubmitterPhone,
    };

    public void ApplyTo(Place p)
    {
        p.Name = Name.Trim();
        p.CategoryId = CategoryId!.Value;
        p.AreaId = AreaId!.Value;
        p.Phone = Domain.Phone.Normalize(Phone);
        p.WhatsApp = string.IsNullOrWhiteSpace(WhatsApp) ? null : Domain.Phone.Normalize(WhatsApp);
        p.Address = Clean(Address);
        p.MapUrl = Clean(MapUrl);
        p.Description = Clean(Description);
        p.Is24Hours = Is24Hours;
        p.OpensAt = Is24Hours ? null : OpensAt;
        p.ClosesAt = Is24Hours ? null : ClosesAt;
        p.WeeklyDayOff = WeeklyDayOff;
        p.SubmitterName = Clean(SubmitterName);
        p.SubmitterPhone = string.IsNullOrWhiteSpace(SubmitterPhone) ? null : Domain.Phone.Normalize(SubmitterPhone);
    }

    internal static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

/// <summary>Form fields for a property listing.</summary>
public class PropertyInput : IValidatableObject
{
    [Required(ErrorMessage = "اختار بيع ولا إيجار")]
    public ListingPurpose? Purpose { get; set; }

    [Required(ErrorMessage = "اختار نوع العقار")]
    public PropertyType? Type { get; set; }

    [Required(ErrorMessage = "اكتب السعر")]
    [Range(1, 1_000_000_000, ErrorMessage = "السعر مش منطقي")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "اكتب المساحة")]
    [Range(1, 1_000_000, ErrorMessage = "المساحة مش منطقية")]
    public int? Size { get; set; }

    [Required(ErrorMessage = "اختار المنطقة")]
    public int? AreaId { get; set; }

    [Range(0, 50)] public int? Rooms { get; set; }
    [Range(0, 20)] public int? Bathrooms { get; set; }
    [Range(-2, 100)] public int? Floor { get; set; }
    public FinishingType? Finishing { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "اللينك مش صحيح")]
    public string? MapUrl { get; set; }

    [Required(ErrorMessage = "اكتب اسمك")]
    [StringLength(100)]
    public string AdvertiserName { get; set; } = "";

    [Required(ErrorMessage = "اكتب رقم موبايلك")]
    [StringLength(20)]
    public string AdvertiserPhone { get; set; } = "";

    [Required(ErrorMessage = "قولنا إنت مالك ولا سمسار")]
    public AdvertiserType? AdvertiserType { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (!Domain.Phone.IsMobile(Domain.Phone.Normalize(AdvertiserPhone)))
            yield return new ValidationResult("رقم الموبايل لازم يكون 11 رقم ويبدأ بـ 01", [nameof(AdvertiserPhone)]);
        if (!string.IsNullOrWhiteSpace(MapUrl) && !MapUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult("لينك الخريطة لازم يبدأ بـ https://", [nameof(MapUrl)]);
    }

    public static PropertyInput From(Property p) => new()
    {
        Purpose = p.Purpose,
        Type = p.Type,
        Price = p.Price,
        Size = p.Size,
        AreaId = p.AreaId,
        Rooms = p.Rooms,
        Bathrooms = p.Bathrooms,
        Floor = p.Floor,
        Finishing = p.Finishing,
        Description = p.Description,
        MapUrl = p.MapUrl,
        AdvertiserName = p.AdvertiserName,
        AdvertiserPhone = p.AdvertiserPhone,
        AdvertiserType = p.AdvertiserType,
    };

    public void ApplyTo(Property p)
    {
        p.Purpose = Purpose!.Value;
        p.Type = Type!.Value;
        p.Price = Price!.Value;
        p.Size = Size!.Value;
        p.AreaId = AreaId!.Value;
        // Rooms and bathrooms only make sense for homes.
        var home = p.Type is PropertyType.Apartment or PropertyType.House;
        p.Rooms = home ? Rooms : null;
        p.Bathrooms = home ? Bathrooms : null;
        p.Floor = p.Type == PropertyType.Land ? null : Floor;
        p.Finishing = p.Type == PropertyType.Land ? null : Finishing;
        p.Description = PlaceInput.Clean(Description);
        p.MapUrl = PlaceInput.Clean(MapUrl);
        p.AdvertiserName = AdvertiserName.Trim();
        p.AdvertiserPhone = Domain.Phone.Normalize(AdvertiserPhone);
        p.AdvertiserType = AdvertiserType!.Value;
    }
}

public static class Uploads
{
    public const int MaxPropertyImages = 10;
    public const int MaxPlaceImages = 5;

    /// <summary>Saves uploaded images and returns their file names; errors go to the model state key.</summary>
    public static async Task<List<string>> SaveAllAsync(
        ImageStore store, IEnumerable<IFormFile>? files, int max,
        Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, string key)
    {
        var saved = new List<string>();
        var list = (files ?? []).Where(f => f.Length > 0).ToList();
        if (list.Count > max)
        {
            modelState.AddModelError(key, $"أقصى عدد {max} صور.");
            return saved;
        }

        foreach (var f in list)
        {
            var (name, error) = await store.SaveAsync(f);
            if (name is not null) saved.Add(name);
            else modelState.AddModelError(key, $"{f.FileName}: {error}");
        }

        if (!modelState.IsValid)
        {
            foreach (var n in saved) store.Delete(n);
            saved.Clear();
        }
        return saved;
    }
}

public class PlaceFormVm
{
    public PlaceInput Input { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Area> Areas { get; set; } = new();
    public bool ShowSubmitter { get; set; } = true;
}

public class PropertyFormVm
{
    public PropertyInput Input { get; set; } = new();
    public List<Area> Areas { get; set; } = new();
}
