using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Admin.Places;

/// <summary>Create (no id) or manage a directory place. Places the admin adds are published directly.</summary>
[RequestSizeLimit(60 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 60 * 1024 * 1024)]
public class EditModel(AppDbContext db, ImageStore imageStore, Moderation moderation, CityClock clock) : PageModel
{
    [BindProperty] public PlaceInput Input { get; set; } = new();

    public Place? Existing { get; private set; }
    public PlaceFormVm Form { get; private set; } = new();
    [TempData] public string? Flash { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is not null)
        {
            Existing = await LoadExistingAsync(id.Value);
            if (Existing is null) return NotFound();
            Input = PlaceInput.From(Existing);
        }
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id, List<IFormFile>? images)
    {
        Place? p = null;
        if (id is not null)
        {
            p = await db.Places.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound();
        }
        if (Input.CategoryId is { } c && !await db.Categories.AnyAsync(x => x.Id == c))
            ModelState.AddModelError("Input.CategoryId", "اختار التصنيف");
        if (Input.AreaId is { } a && !await db.Areas.AnyAsync(x => x.Id == a))
            ModelState.AddModelError("Input.AreaId", "اختار المنطقة");

        if (ModelState.IsValid)
        {
            var max = Uploads.MaxPlaceImages - (p?.Images.Count ?? 0);
            var saved = await Uploads.SaveAllAsync(imageStore, images, Math.Max(0, max), ModelState, "images");
            if (ModelState.IsValid)
            {
                var now = clock.UtcNow;
                if (p is null)
                {
                    p = new Place { CreatedAt = now, Status = ListingStatus.Published };
                    db.Places.Add(p);
                }
                Input.ApplyTo(p);
                p.UpdatedAt = now;
                var order = p.Images.Count == 0 ? 0 : p.Images.Max(i => i.SortOrder) + 1;
                foreach (var name in saved)
                    p.Images.Add(new PlaceImage { FileName = name, SortOrder = order++ });
                await db.SaveChangesAsync();
                Flash = "تم الحفظ.";
                return RedirectToPage(new { id = p.Id });
            }
        }

        Existing = id is null ? null : await LoadExistingAsync(id.Value);
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var resultId = await moderation.ApprovePlaceAsync(id);
        if (resultId is null) return NotFound();
        Flash = "تم النشر.";
        return RedirectToPage(new { id = resultId });
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string? reason)
    {
        var editOf = await db.Places.Where(p => p.Id == id).Select(p => p.EditOfPlaceId).FirstOrDefaultAsync();
        if (!await moderation.RejectPlaceAsync(id, reason)) return NotFound();
        Flash = "تم الرفض.";
        return editOf is null ? RedirectToPage(new { id }) : RedirectToPage("/Admin/Index");
    }

    public async Task<IActionResult> OnPostToggleVerifiedAsync(int id)
    {
        var p = await db.Places.FindAsync(id);
        if (p is null) return NotFound();
        p.IsVerified = !p.IsVerified;
        await db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteImageAsync(int id, int imageId)
    {
        var img = await db.PlaceImages.FirstOrDefaultAsync(i => i.Id == imageId && i.PlaceId == id);
        if (img is not null)
        {
            imageStore.Delete(img.FileName);
            db.PlaceImages.Remove(img);
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await moderation.DeletePlaceAsync(id);
        Flash = "تم الحذف.";
        return RedirectToPage("Index");
    }

    private Task<Place?> LoadExistingAsync(int id) =>
        db.Places.AsNoTracking().Include(p => p.Images).Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

    private async Task LoadAsync() => Form = new PlaceFormVm
    {
        Input = Input,
        Categories = await db.Categories.AsNoTracking().ToListAsync(),
        Areas = await db.AreasAsync(),
    };
}
