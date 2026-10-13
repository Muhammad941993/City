using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace CityGuide.Tests;

public class DataTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly string _uploads = Path.Combine(Path.GetTempPath(), "cg-tests-" + Guid.NewGuid().ToString("N"));
    private readonly DateTime _now = DateTime.UtcNow;

    public DataTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        Seeder.SeedTaxonomyAsync(_db).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_uploads)) Directory.Delete(_uploads, true);
    }

    private Moderation NewModeration() => new(_db,
        new ImageStore(Options.Create(new ImageOptions { UploadPath = _uploads }), new FakeEnv()),
        new CityClock(Options.Create(new SiteOptions())));

    private Property AddProperty(Action<Property> setup)
    {
        var p = new Property { AreaId = _db.Areas.First().Id, AdvertiserName = "x", AdvertiserPhone = "01012345678", CreatedAt = _now, UpdatedAt = _now };
        setup(p);
        _db.Properties.Add(p);
        _db.SaveChanges();
        return p;
    }

    [Fact]
    public void Seeder_creates_8_categories_and_emergency_numbers()
    {
        Assert.Equal(8, _db.Categories.Count(c => c.ParentId == null));
        Assert.Equal(3, _db.Places.Count(p => p.Status == ListingStatus.Published));
    }

    [Fact]
    public void Search_filters_and_sorts()
    {
        var area2 = _db.Areas.OrderBy(a => a.SortOrder).Skip(1).First().Id;
        AddProperty(p => { p.Type = PropertyType.Apartment; p.Price = 900_000; p.Size = 100; ListingRules.Approve(p, _now.AddHours(-3)); });
        AddProperty(p => { p.Type = PropertyType.Apartment; p.Price = 1_500_000; p.Size = 150; p.AreaId = area2; ListingRules.Approve(p, _now.AddHours(-2)); });
        AddProperty(p => { p.Type = PropertyType.Land; p.Price = 2_000_000; p.Size = 400; p.AdvertiserType = AdvertiserType.Broker; ListingRules.Approve(p, _now.AddHours(-1)); });
        AddProperty(p => { p.Type = PropertyType.Apartment; p.Price = 500_000; p.Purpose = ListingPurpose.Rent; ListingRules.Approve(p, _now); });
        AddProperty(p => { p.Type = PropertyType.Apartment; p.Price = 100; p.Status = ListingStatus.Pending; });

        List<decimal> Prices(PropertyFilter f) => PropertySearch.Apply(_db.Properties, f, _now).Select(p => p.Price).ToList();

        Assert.Equal([2_000_000m, 1_500_000m, 900_000m], Prices(new PropertyFilter { Purpose = ListingPurpose.Sale }));
        Assert.Equal([900_000m, 1_500_000m, 2_000_000m], Prices(new PropertyFilter { Purpose = ListingPurpose.Sale, Sort = PropertySort.PriceAsc }));
        Assert.Equal([1_500_000m, 900_000m], Prices(new PropertyFilter { Purpose = ListingPurpose.Sale, Type = PropertyType.Apartment }));
        Assert.Equal([1_500_000m], Prices(new PropertyFilter { Purpose = ListingPurpose.Sale, AreaId = area2 }));
        Assert.Equal([1_500_000m, 900_000m], Prices(new PropertyFilter { Purpose = ListingPurpose.Sale, MinPrice = 800_000, MaxPrice = 1_600_000 }));
        Assert.Equal([2_000_000m], Prices(new PropertyFilter { Purpose = ListingPurpose.Sale, MinSize = 200 }));
        Assert.Equal([2_000_000m], Prices(new PropertyFilter { Purpose = ListingPurpose.Sale, Advertiser = AdvertiserType.Broker }));
        Assert.Equal([500_000m], Prices(new PropertyFilter { Purpose = ListingPurpose.Rent }));
    }

    [Fact]
    public void Featured_first_and_sold_last()
    {
        AddProperty(p => { p.Price = 1; ListingRules.Approve(p, _now); ListingRules.Close(p, _now); });
        AddProperty(p => { p.Price = 2; ListingRules.Approve(p, _now.AddDays(-1)); p.IsFeatured = true; });
        AddProperty(p => { p.Price = 3; ListingRules.Approve(p, _now); });
        var order = PropertySearch.Apply(_db.Properties, new PropertyFilter(), _now).Select(p => p.Price).ToList();
        Assert.Equal([2m, 3m, 1m], order);
    }

    [Fact]
    public async Task Approving_an_edit_suggestion_updates_the_original_and_removes_the_suggestion()
    {
        var cat = _db.Categories.First(c => c.ParentId != null);
        var area = _db.Areas.First();
        var original = new Place { Name = "قديم", CategoryId = cat.Id, AreaId = area.Id, Phone = "01000000000", Status = ListingStatus.Published, SubmitterName = "صاحب المحل", CreatedAt = _now, UpdatedAt = _now };
        _db.Places.Add(original);
        await _db.SaveChangesAsync();
        var suggestion = new Place { Name = "جديد", CategoryId = cat.Id, AreaId = area.Id, Phone = "01111111111", Is24Hours = true, EditOfPlaceId = original.Id, SubmitterName = "زائر", Status = ListingStatus.Pending, CreatedAt = _now, UpdatedAt = _now };
        _db.Places.Add(suggestion);
        await _db.SaveChangesAsync();

        var resultId = await NewModeration().ApprovePlaceAsync(suggestion.Id);

        Assert.Equal(original.Id, resultId);
        var updated = await _db.Places.SingleAsync(p => p.Id == original.Id);
        Assert.Equal("جديد", updated.Name);
        Assert.Equal("01111111111", updated.Phone);
        Assert.True(updated.Is24Hours);
        Assert.Equal("صاحب المحل", updated.SubmitterName);
        Assert.False(await _db.Places.AnyAsync(p => p.Id == suggestion.Id));
    }

    [Fact]
    public async Task Rejecting_a_new_place_keeps_it_with_reason()
    {
        var cat = _db.Categories.First();
        var p = new Place { Name = "x", CategoryId = cat.Id, AreaId = _db.Areas.First().Id, Phone = "123", CreatedAt = _now, UpdatedAt = _now };
        _db.Places.Add(p);
        await _db.SaveChangesAsync();
        Assert.True(await NewModeration().RejectPlaceAsync(p.Id, " رقم غلط "));
        Assert.Equal(ListingStatus.Rejected, p.Status);
        Assert.Equal("رقم غلط", p.RejectionReason);
    }

    private sealed class FakeEnv : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Test";
    }
}
