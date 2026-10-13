using CityGuide.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<PlaceImage> PlaceImages => Set<PlaceImage>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Area>().HasIndex(a => a.Name).IsUnique();

        b.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
            e.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Place>(e =>
        {
            e.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Area).WithMany().HasForeignKey(p => p.AreaId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(p => p.Images).WithOne().HasForeignKey(i => i.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => new { p.Status, p.CategoryId });
        });

        b.Entity<Property>(e =>
        {
            e.Property(p => p.Price).HasPrecision(14, 2);
            e.HasOne(p => p.Area).WithMany().HasForeignKey(p => p.AreaId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(p => p.Images).WithOne().HasForeignKey(i => i.PropertyId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(p => p.Title);
            e.HasIndex(p => new { p.Status, p.Purpose, p.PublishedAt });
        });

        b.Entity<Report>(e =>
        {
            e.HasOne(r => r.Place).WithMany().HasForeignKey(r => r.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Property).WithMany().HasForeignKey(r => r.PropertyId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
