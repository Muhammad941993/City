using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Places;

/// <summary>Public form: suggest a new place, or (with editOf) a correction to an existing one.</summary>
public class AddModel(AppDbContext db, ImageStore imageStore, Turnstile captcha) : PageModel
{
    [BindProperty] public PlaceInput Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public int? EditOf { get; set; }

    public PlaceFormVm Form { get; private set; } = new();
    public string? ImageError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (EditOf is { } id)
        {
            var existing = await db.PublicPlaces().FirstOrDefaultAsync(p => p.Id == id);
            if (existing is null)
                return NotFound();
            Input = PlaceInput.From(existing);
            Input.SubmitterName = null;
            Input.SubmitterPhone = null;
        }
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(List<IFormFile>? images)
    {
        if (EditOf is { } id && !await db.PublicPlaces().AnyAsync(p => p.Id == id))
            return NotFound();
        await ValidateReferencesAsync();

        if (!await captcha.VerifyAsync(HttpContext))
            ModelState.AddModelError("", "اتأكد إنك مش روبوت وجرب تاني.");

        if (ModelState.IsValid)
        {
            var saved = await Uploads.SaveAllAsync(imageStore, images, Uploads.MaxPlaceImages, ModelState, "images");
            if (ModelState.IsValid)
            {
                var now = DateTime.UtcNow;
                var place = new Place { Status = ListingStatus.Pending, EditOfPlaceId = EditOf, CreatedAt = now, UpdatedAt = now };
                Input.ApplyTo(place);
                for (var i = 0; i < saved.Count; i++)
                    place.Images.Add(new PlaceImage { FileName = saved[i], SortOrder = i });
                db.Places.Add(place);
                await db.SaveChangesAsync();
                return RedirectToPage("/Submitted", new { kind = "place" });
            }
            ImageError = string.Join(" ", ModelState["images"]?.Errors.Select(e => e.ErrorMessage) ?? []);
        }

        await LoadAsync();
        return Page();
    }

    private async Task ValidateReferencesAsync()
    {
        if (Input.CategoryId is { } c && !await db.Categories.AnyAsync(x => x.Id == c))
            ModelState.AddModelError("Input.CategoryId", "اختار التصنيف");
        if (Input.AreaId is { } a && !await db.Areas.AnyAsync(x => x.Id == a))
            ModelState.AddModelError("Input.AreaId", "اختار المنطقة");
    }

    private async Task LoadAsync()
    {
        Form = new PlaceFormVm
        {
            Input = Input,
            Categories = await db.Categories.AsNoTracking().ToListAsync(),
            Areas = await db.AreasAsync(),
        };
    }
}
