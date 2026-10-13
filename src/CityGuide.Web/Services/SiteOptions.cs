using System.Globalization;

namespace CityGuide.Web.Services;

/// <summary>Site-wide settings (bound from the "Site" section of appsettings).</summary>
public class SiteOptions
{
    /// <summary>City name, e.g. "[اسم المدينة]" until it is decided.</summary>
    public string CityName { get; set; } = "[اسم المدينة]";

    /// <summary>Public base URL used in Open Graph tags and the sitemap, e.g. https://example.com.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>The site's own WhatsApp number for people who prefer sending listings by chat.</summary>
    public string? WhatsApp { get; set; }

    public string TimeZone { get; set; } = "Africa/Cairo";

    public string SiteName => $"دليل {CityName}";
}

/// <summary>Current time in UTC and in the city's time zone.</summary>
public class CityClock(Microsoft.Extensions.Options.IOptions<SiteOptions> options)
{
    private readonly TimeZoneInfo _tz = FindZone(options.Value.TimeZone);

    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);
    public DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _tz);

    private static TimeZoneInfo FindZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
    }
}

public static class Fmt
{
    private static readonly CultureInfo Num = CultureInfo.InvariantCulture;

    public static string Price(decimal price) => price.ToString("#,0", Num);

    public static string Price(CityGuide.Web.Domain.Property p) =>
        p.Purpose == CityGuide.Web.Domain.ListingPurpose.Rent
            ? $"{Price(p.Price)} جنيه شهرياً"
            : $"{Price(p.Price)} جنيه";

    /// <summary>"منذ ساعتين", "منذ 3 أيام"…</summary>
    public static string Ago(DateTime utc, DateTime nowUtc)
    {
        var d = nowUtc - utc;
        if (d.TotalMinutes < 1) return "الآن";
        if (d.TotalMinutes < 60) return Plural((int)d.TotalMinutes, "دقيقة", "دقيقتين", "دقائق");
        if (d.TotalHours < 24) return Plural((int)d.TotalHours, "ساعة", "ساعتين", "ساعات");
        if (d.TotalDays < 30) return Plural((int)d.TotalDays, "يوم", "يومين", "أيام");
        if (d.TotalDays < 365) return Plural((int)(d.TotalDays / 30), "شهر", "شهرين", "شهور");
        return Plural((int)(d.TotalDays / 365), "سنة", "سنتين", "سنين");
    }

    private static string Plural(int n, string one, string two, string few) => n switch
    {
        1 => $"منذ {one}",
        2 => $"منذ {two}",
        <= 10 => $"منذ {n} {few}",
        _ => $"منذ {n} {one}",
    };

    public static string Date(DateTime local) => local.ToString("d/M/yyyy", Num);
}
