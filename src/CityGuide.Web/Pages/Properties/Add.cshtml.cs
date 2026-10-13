using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Properties;

// Allow a full set of 10 large phone photos.
[RequestSizeLimit(100 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
public class AddModel(AppDbContext db, ImageStore imageStore, Turnstile captcha) : PageModel
{
    [BindProperty] public PropertyInput Input { get; set; } = new();

    public PropertyFormVm Form { get; private set; } = new();
    public string? ImageError { get; private set; }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync(List<IFormFile>? images)
    {
        if (Input.AreaId is { } a && !await db.Areas.AnyAsync(x => x.Id == a))
            ModelState.AddModelError("Input.AreaId", "اختار المنطقة");
        if (images is null || images.All(f => f.Length == 0))
            ModelState.AddModelError("images", "ضيف صورة واحدة على الأقل.");
        if (!await captcha.VerifyAsync(HttpContext))
            ModelState.AddModelError("", "اتأكد إنك مش روبوت وجرب تاني.");

        if (ModelState.IsValid)
        {
            var saved = await Uploads.SaveAllAsync(imageStore, images, Uploads.MaxPropertyImages, ModelState, "images");
            if (ModelState.IsValid)
            {
                var now = DateTime.UtcNow;
                var property = new Property { Status = ListingStatus.Pending, CreatedAt = now, UpdatedAt = now };
                Input.ApplyTo(property);
                for (var i = 0; i < saved.Count; i++)
                    property.Images.Add(new PropertyImage { FileName = saved[i], SortOrder = i });
                db.Properties.Add(property);
                await db.SaveChangesAsync();
                return RedirectToPage("/Submitted", new { kind = "property" });
            }
        }

        ImageError = string.Join(" ", ModelState["images"]?.Errors.Select(e => e.ErrorMessage) ?? []);
        if (ImageError.Length == 0 && !ModelState.IsValid)
            ImageError = "اختار الصور تاني بعد ما تصلّح البيانات.";
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync() => Form = new PropertyFormVm { Input = Input, Areas = await db.AreasAsync() };
}
