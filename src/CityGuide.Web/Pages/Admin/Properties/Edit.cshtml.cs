using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Admin.Properties;

/// <summary>Create (no id) or manage a property: edit fields, photos, status, badges.</summary>
[RequestSizeLimit(100 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
public class EditModel(AppDbContext db, ImageStore imageStore, Moderation moderation, CityClock clock) : PageModel
{
    [BindProperty] public PropertyInput Input { get; set; } = new();

    public Property? Existing { get; private set; }
    public PropertyFormVm Form { get; private set; } = new();
    [TempData] public string? Flash { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is not null)
        {
            Existing = await db.PropertiesWithDetails().FirstOrDefaultAsync(p => p.Id == id);
            if (Existing is null) return NotFound();
            Input = PropertyInput.From(Existing);
        }
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id, List<IFormFile>? images, bool publish = false)
    {
        Property? p = null;
        if (id is not null)
        {
            p = await db.Properties.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound();
        }
        if (Input.AreaId is { } a && !await db.Areas.AnyAsync(x => x.Id == a))
            ModelState.AddModelError("Input.AreaId", "اختار المنطقة");

        var max = Uploads.MaxPropertyImages - (p?.Images.Count ?? 0);
        if (ModelState.IsValid)
        {
            var saved = await Uploads.SaveAllAsync(imageStore, images, Math.Max(0, max), ModelState, "images");
            if (ModelState.IsValid)
            {
                var now = clock.UtcNow;
                if (p is null)
                {
                    p = new Property { CreatedAt = now, Status = ListingStatus.Pending };
                    db.Properties.Add(p);
                }
                Input.ApplyTo(p);
                p.UpdatedAt = now;
                var order = p.Images.Count == 0 ? 0 : p.Images.Max(i => i.SortOrder) + 1;
                foreach (var name in saved)
                    p.Images.Add(new PropertyImage { FileName = name, SortOrder = order++ });
                if (publish && p.Status != ListingStatus.Published)
                    ListingRules.Approve(p, now);
                await db.SaveChangesAsync();
                Flash = "تم الحفظ.";
                return RedirectToPage(new { id = p.Id });
            }
        }

        Existing = id is null ? null : await db.PropertiesWithDetails().FirstOrDefaultAsync(x => x.Id == id);
        await LoadAsync();
        return Page();
    }

    public Task<IActionResult> OnPostApproveAsync(int id) => Act(id, () => moderation.ApprovePropertyAsync(id), "تم النشر لمدة 30 يوم.");
    public Task<IActionResult> OnPostRejectAsync(int id, string? reason) => Act(id, () => moderation.RejectPropertyAsync(id, reason), "تم الرفض.");
    public Task<IActionResult> OnPostCloseAsync(int id) => Act(id, () => moderation.ClosePropertyAsync(id), "تم التعليم كمباع / مؤجر.");

    public async Task<IActionResult> OnPostToggleAsync(int id, string flag)
    {
        var p = await db.Properties.FindAsync(id);
        if (p is null) return NotFound();
        if (flag == "verified") p.IsVerified = !p.IsVerified;
        else if (flag == "featured") p.IsFeatured = !p.IsFeatured;
        p.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteImageAsync(int id, int imageId)
    {
        var img = await db.PropertyImages.FirstOrDefaultAsync(i => i.Id == imageId && i.PropertyId == id);
        if (img is not null)
        {
            imageStore.Delete(img.FileName);
            db.PropertyImages.Remove(img);
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await moderation.DeletePropertyAsync(id);
        Flash = "تم حذف الإعلان.";
        return RedirectToPage("Index");
    }

    private async Task<IActionResult> Act(int id, Func<Task<bool>> action, string message)
    {
        if (!await action()) return NotFound();
        Flash = message;
        return RedirectToPage(new { id });
    }

    private async Task LoadAsync() => Form = new PropertyFormVm { Input = Input, Areas = await db.AreasAsync() };
}
