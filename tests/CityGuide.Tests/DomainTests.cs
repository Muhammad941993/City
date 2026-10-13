using CityGuide.Web.Domain;
using CityGuide.Web.Services;

namespace CityGuide.Tests;

public class ListingRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Approve_publishes_for_30_days()
    {
        var p = new Property { Status = ListingStatus.Pending, RejectionReason = "old" };
        ListingRules.Approve(p, Now);
        Assert.Equal(ListingStatus.Published, p.Status);
        Assert.Equal(Now.AddDays(30), p.ExpiresAt);
        Assert.Null(p.RejectionReason);
        Assert.True(ListingRules.IsPubliclyVisible(p, Now));
        Assert.False(ListingRules.IsPubliclyVisible(p, Now.AddDays(30)));
    }

    [Theory]
    [InlineData(ListingStatus.Pending)]
    [InlineData(ListingStatus.Rejected)]
    [InlineData(ListingStatus.Expired)]
    public void Non_published_listings_are_hidden(ListingStatus status) =>
        Assert.False(ListingRules.IsPubliclyVisible(new Property { Status = status }, Now));

    [Fact]
    public void Closed_listing_stays_visible_for_7_days()
    {
        var p = new Property();
        ListingRules.Approve(p, Now);
        ListingRules.Close(p, Now);
        Assert.True(ListingRules.IsPubliclyVisible(p, Now.AddDays(6.9)));
        Assert.False(ListingRules.IsPubliclyVisible(p, Now.AddDays(7)));
    }

    [Fact]
    public void Reminder_window_is_last_3_days()
    {
        var p = new Property();
        ListingRules.Approve(p, Now);
        Assert.False(ListingRules.NeedsReminder(p, Now.AddDays(26)));
        Assert.True(ListingRules.NeedsReminder(p, Now.AddDays(27.5)));
        Assert.False(ListingRules.NeedsReminder(p, Now.AddDays(31)));
    }

    [Fact]
    public void ExpireDue_only_touches_ended_published_listings()
    {
        var ended = new Property { Status = ListingStatus.Published, ExpiresAt = Now.AddMinutes(-1) };
        var active = new Property { Status = ListingStatus.Published, ExpiresAt = Now.AddDays(1) };
        var closed = new Property { Status = ListingStatus.Closed, ExpiresAt = Now.AddDays(-1) };
        Assert.Equal(1, ListingRules.ExpireDue([ended, active, closed], Now));
        Assert.Equal(ListingStatus.Expired, ended.Status);
        Assert.Equal(ListingStatus.Published, active.Status);
        Assert.Equal(ListingStatus.Closed, closed.Status);
    }

    [Fact]
    public void Query_filter_matches_in_memory_rule()
    {
        var items = new List<Property>
        {
            new() { Id = 1, Status = ListingStatus.Published, ExpiresAt = Now.AddDays(1) },
            new() { Id = 2, Status = ListingStatus.Published, ExpiresAt = Now.AddDays(-1) },
            new() { Id = 3, Status = ListingStatus.Closed, ClosedAt = Now.AddDays(-2) },
            new() { Id = 4, Status = ListingStatus.Closed, ClosedAt = Now.AddDays(-8) },
            new() { Id = 5, Status = ListingStatus.Pending },
        };
        var viaQuery = items.AsQueryable().WherePubliclyVisible(Now).Select(p => p.Id).ToList();
        var viaRule = items.Where(p => ListingRules.IsPubliclyVisible(p, Now)).Select(p => p.Id).ToList();
        Assert.Equal([1, 3], viaQuery);
        Assert.Equal(viaRule, viaQuery);
    }
}

public class OpeningHoursTests
{
    // 2026-10-02 is a Friday.
    private static DateTime At(int day, int hour, int minute = 0) => new(2026, 10, day, hour, minute, 0);

    [Fact]
    public void Daytime_hours()
    {
        var p = new Place { OpensAt = new TimeOnly(9, 0), ClosesAt = new TimeOnly(23, 0) };
        Assert.Equal(OpenState.Closed, OpeningHours.StateAt(p, At(1, 8, 59)));
        Assert.Equal(OpenState.Open, OpeningHours.StateAt(p, At(1, 9)));
        Assert.Equal(OpenState.Open, OpeningHours.StateAt(p, At(1, 22, 59)));
        Assert.Equal(OpenState.Closed, OpeningHours.StateAt(p, At(1, 23)));
    }

    [Fact]
    public void Hours_past_midnight()
    {
        var p = new Place { OpensAt = new TimeOnly(18, 0), ClosesAt = new TimeOnly(2, 0) };
        Assert.Equal(OpenState.Open, OpeningHours.StateAt(p, At(1, 23)));
        Assert.Equal(OpenState.Open, OpeningHours.StateAt(p, At(2, 1, 30)));
        Assert.Equal(OpenState.Closed, OpeningHours.StateAt(p, At(2, 3)));
    }

    [Fact]
    public void Day_off_includes_its_own_night_shift_only()
    {
        var p = new Place { OpensAt = new TimeOnly(18, 0), ClosesAt = new TimeOnly(2, 0), WeeklyDayOff = DayOfWeek.Friday };
        Assert.Equal(OpenState.Open, OpeningHours.StateAt(p, At(2, 1)));   // Friday 1am = Thursday's shift
        Assert.Equal(OpenState.Closed, OpeningHours.StateAt(p, At(2, 20))); // Friday evening
        Assert.Equal(OpenState.Closed, OpeningHours.StateAt(p, At(3, 1)));  // Saturday 1am = Friday's shift
        Assert.Equal(OpenState.Open, OpeningHours.StateAt(p, At(3, 20)));
    }

    [Fact]
    public void Unknown_without_hours_and_24h_always_open()
    {
        Assert.Equal(OpenState.Unknown, OpeningHours.StateAt(new Place(), At(1, 12)));
        Assert.Equal(OpenState.Open, OpeningHours.StateAt(new Place { Is24Hours = true }, At(1, 3)));
        Assert.Equal(OpenState.Closed, OpeningHours.StateAt(new Place { Is24Hours = true, WeeklyDayOff = DayOfWeek.Friday }, At(2, 3)));
    }

    [Fact]
    public void Describe_formats_arabic()
    {
        var p = new Place { OpensAt = new TimeOnly(9, 0), ClosesAt = new TimeOnly(23, 30), WeeklyDayOff = DayOfWeek.Friday };
        Assert.Equal("من 9:00 ص إلى 11:30 م · إجازة يوم الجمعة", OpeningHours.Describe(p));
    }
}

public class PhoneTests
{
    [Theory]
    [InlineData("010 1234 5678", "01012345678")]
    [InlineData("+20 101 234 5678", "01012345678")]
    [InlineData("00201012345678", "01012345678")]
    [InlineData("٠١٠١٢٣٤٥٦٧٨", "01012345678")]
    [InlineData("123", "123")]
    public void Normalize(string input, string expected) => Assert.Equal(expected, Phone.Normalize(input));

    [Fact]
    public void WhatsApp_only_for_mobiles()
    {
        Assert.Equal("https://wa.me/201012345678", Phone.WhatsAppLink("01012345678"));
        Assert.Null(Phone.WhatsAppLink("123"));
        Assert.Null(Phone.WhatsAppLink("0402345678"));
        Assert.StartsWith("https://wa.me/201112223334?text=%D8", Phone.WhatsAppLink("01112223334", "السلام"));
    }

    [Theory]
    [InlineData("01012345678", true)]
    [InlineData("01512345678", true)]
    [InlineData("01312345678", false)]
    [InlineData("0101234567", false)]
    public void IsMobile(string n, bool expected) => Assert.Equal(expected, Phone.IsMobile(n));
}

public class FmtTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Price_formats_rent_monthly()
    {
        Assert.Equal("1,450,000 جنيه", Fmt.Price(new Property { Price = 1450000, Purpose = ListingPurpose.Sale }));
        Assert.Equal("3,500 جنيه شهرياً", Fmt.Price(new Property { Price = 3500, Purpose = ListingPurpose.Rent }));
    }

    [Theory]
    [InlineData(0.5, "الآن")]
    [InlineData(120, "منذ ساعتين")]
    [InlineData(60 * 5, "منذ 5 ساعات")]
    [InlineData(60 * 24, "منذ يوم")]
    [InlineData(60 * 24 * 15, "منذ 15 يوم")]
    public void Ago(double minutes, string expected) => Assert.Equal(expected, Fmt.Ago(Now.AddMinutes(-minutes), Now));

    [Fact]
    public void Title_includes_rooms_when_known()
    {
        Assert.Equal("شقة للبيع · 140 متر · 3 غرف", new Property { Type = PropertyType.Apartment, Size = 140, Rooms = 3 }.Title);
        Assert.Equal("أرض للإيجار · 400 متر", new Property { Type = PropertyType.Land, Purpose = ListingPurpose.Rent, Size = 400 }.Title);
    }
}
