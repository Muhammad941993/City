using System.Text;
using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection("Site"));
builder.Services.Configure<ImageOptions>(builder.Configuration.GetSection("Images"));
builder.Services.Configure<TurnstileOptions>(builder.Configuration.GetSection("Turnstile"));

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(o =>
    {
        o.Password.RequiredLength = 10;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>();

// Persist login-cookie keys across restarts/redeploys when a folder is configured (production).
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("CityGuide");
}

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/admin/login";
    o.AccessDeniedPath = "/admin/login";
    o.Cookie.Name = "cg.admin";
    o.ExpireTimeSpan = TimeSpan.FromDays(14);
    o.SlidingExpiration = true;
});

builder.Services.AddRazorPages(o =>
{
    // Only the site admin has an account; visitors never log in.
    o.Conventions.AuthorizeFolder("/Admin");
    o.Conventions.AllowAnonymousToPage("/Admin/Login");
});

// Emit Arabic as-is instead of &#x...; entities (smaller, readable pages).
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));

builder.Services.AddSingleton<CityClock>();
builder.Services.AddSingleton<ImageStore>();
builder.Services.AddScoped<Moderation>();
builder.Services.AddHttpClient<Turnstile>();
builder.Services.AddHostedService<ExpiryService>();

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    // Behind nginx on the VPS.
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/NotFound", "?code={0}");

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Uploaded images never change (new upload = new file name), so cache them for a long time.
        if (ctx.Context.Request.Path.StartsWithSegments("/uploads"))
            ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
    },
});

// An absolute Images:UploadPath (recommended in production) lives outside wwwroot.
var imageOptions = app.Services.GetRequiredService<IOptions<ImageOptions>>().Value;
if (Path.IsPathRooted(imageOptions.UploadPath))
{
    Directory.CreateDirectory(imageOptions.UploadPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(imageOptions.UploadPath),
        RequestPath = "/uploads",
        OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable",
    });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/robots.txt", (IOptions<SiteOptions> site) =>
{
    var sb = new StringBuilder("User-agent: *\nDisallow: /admin\n");
    if (site.Value.BaseUrl is { Length: > 0 } baseUrl)
        sb.Append($"Sitemap: {baseUrl.TrimEnd('/')}/sitemap.xml\n");
    return Results.Text(sb.ToString(), "text/plain");
});

app.MapGet("/sitemap.xml", async (AppDbContext db, HttpContext ctx, IOptions<SiteOptions> site) =>
{
    var baseUrl = site.Value.BaseUrl?.TrimEnd('/') ?? $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    var now = DateTime.UtcNow;
    var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");
    void Add(string path, DateTime? modified = null)
    {
        sb.Append("<url><loc>").Append(System.Security.SecurityElement.Escape(baseUrl + path)).Append("</loc>");
        if (modified is { } m) sb.Append("<lastmod>").Append(m.ToString("yyyy-MM-dd")).Append("</lastmod>");
        sb.Append("</url>\n");
    }

    Add("/");
    Add("/directory");
    Add("/properties");
    Add("/properties?purpose=Rent");
    foreach (var slug in await db.Categories.Where(c => c.ParentId == null).Select(c => c.Slug).ToListAsync())
        Add($"/directory/{slug}");
    foreach (var p in await db.Places.Where(p => p.Status == ListingStatus.Published && p.EditOfPlaceId == null)
                 .Select(p => new { p.Id, p.UpdatedAt }).ToListAsync())
        Add($"/places/{p.Id}", p.UpdatedAt);
    foreach (var p in await db.Properties.WherePubliclyVisible(now).Select(p => new { p.Id, p.UpdatedAt }).ToListAsync())
        Add($"/properties/{p.Id}", p.UpdatedAt);

    sb.Append("</urlset>");
    return Results.Text(sb.ToString(), "application/xml");
});

await Seeder.RunAsync(app.Services, app.Configuration, app.Logger);

app.Run();

public partial class Program;
