namespace CityGuide.Web.Domain;

public enum OpenState
{
    Unknown,
    Open,
    Closed,
}

public static class OpeningHours
{
    /// <summary>Whether a place is open at the given local (city) time. Handles hours past midnight.</summary>
    public static OpenState StateAt(Place p, DateTime local)
    {
        if (p.Is24Hours)
            return p.WeeklyDayOff == local.DayOfWeek ? OpenState.Closed : OpenState.Open;

        if (p.OpensAt is not { } opens || p.ClosesAt is not { } closes)
            return p.WeeklyDayOff == local.DayOfWeek ? OpenState.Closed : OpenState.Unknown;

        var now = TimeOnly.FromDateTime(local);
        var overnight = closes <= opens; // e.g. 18:00 → 02:00

        // Hours after midnight belong to the shift that started the day before.
        var afterMidnight = overnight && now < closes;
        var shiftDay = afterMidnight ? local.AddDays(-1).DayOfWeek : local.DayOfWeek;
        if (p.WeeklyDayOff == shiftDay)
            return OpenState.Closed;

        var open = overnight ? now >= opens || now < closes : now >= opens && now < closes;
        return open ? OpenState.Open : OpenState.Closed;
    }

    /// <summary>Human text such as "مفتوح 24 ساعة" or "من 9:00 ص إلى 11:00 م".</summary>
    public static string? Describe(Place p)
    {
        string? hours = null;
        if (p.Is24Hours)
            hours = "مفتوح 24 ساعة";
        else if (p.OpensAt is { } o && p.ClosesAt is { } c)
            hours = $"من {Format(o)} إلى {Format(c)}";

        if (p.WeeklyDayOff is { } off)
            hours = hours is null ? $"إجازة يوم {DayName(off)}" : $"{hours} · إجازة يوم {DayName(off)}";

        return hours;
    }

    public static string Format(TimeOnly t)
    {
        var h = t.Hour % 12 == 0 ? 12 : t.Hour % 12;
        var suffix = t.Hour < 12 ? "ص" : "م";
        return $"{h}:{t.Minute:00} {suffix}";
    }

    public static string DayName(DayOfWeek d) => d switch
    {
        DayOfWeek.Saturday => "السبت",
        DayOfWeek.Sunday => "الأحد",
        DayOfWeek.Monday => "الاثنين",
        DayOfWeek.Tuesday => "الثلاثاء",
        DayOfWeek.Wednesday => "الأربعاء",
        DayOfWeek.Thursday => "الخميس",
        DayOfWeek.Friday => "الجمعة",
        _ => d.ToString(),
    };
}
