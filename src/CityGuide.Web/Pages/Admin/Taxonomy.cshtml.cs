using System.Text.RegularExpressions;
using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Pages.Admin;

/// <summary>Manage directory categories and the districts used in every filter.</summary>
public partial class TaxonomyModel(AppDbContext db) : PageModel
{
    public static readonly string[] IconKeys = ["health", "tools", "food", "school", "cart", "gov", "bus", "siren", "store", "building", "phone"];

    public List<Category> Categories { get; private set; } = new();
    public List<Area> Areas { get; private set; } = new();
    public HashSet<int> UsedCategories { get; private set; } = new();
    public HashSet<int> UsedAreas { get; private set; } = new();

    [TempData] public string? Flash { get; set; }

    [GeneratedRegex("^[a-z0-9-]{2,60}$")]
    private static partial Regex SlugRegex();

    public async Task OnGetAsync()
    {
        Categories = await db.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
        Areas = await db.Areas.AsNoTracking().OrderBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync();
        UsedCategories = (await db.Places.Select(p => p.CategoryId).Distinct().ToListAsync()).ToHashSet();
        var areaIds = await db.Places.Select(p => p.AreaId).Union(db.Properties.Select(p => p.AreaId)).Distinct().ToListAsync();
        UsedAreas = areaIds.ToHashSet();
    }

    public async Task<IActionResult> OnPostAddAreaAsync(string name)
    {
        name = name?.Trim() ?? "";
        if (name.Length is 0 or > 100) return Fail("اكتب اسم المنطقة.");
        if (await db.Areas.AnyAsync(a => a.Name == name)) return Fail("المنطقة دي موجودة.");
        var order = await db.Areas.Select(a => (int?)a.SortOrder).MaxAsync() ?? -1;
        db.Areas.Add(new Area { Name = name, SortOrder = order + 1 });
        await db.SaveChangesAsync();
        return Done("تمت إضافة المنطقة.");
    }

    public async Task<IActionResult> OnPostUpdateAreaAsync(int id, string name, int sortOrder)
    {
        var a = await db.Areas.FindAsync(id);
        name = name?.Trim() ?? "";
        if (a is null) return NotFound();
        if (name.Length is 0 or > 100) return Fail("اكتب اسم المنطقة.");
        if (await db.Areas.AnyAsync(x => x.Name == name && x.Id != id)) return Fail("الاسم ده مستخدم.");
        a.Name = name;
        a.SortOrder = sortOrder;
        await db.SaveChangesAsync();
        return Done("تم الحفظ.");
    }

    public async Task<IActionResult> OnPostDeleteAreaAsync(int id)
    {
        if (await db.Places.AnyAsync(p => p.AreaId == id) || await db.Properties.AnyAsync(p => p.AreaId == id))
            return Fail("المنطقة مستخدمة في إعلانات، مينفعش تتحذف.");
        var a = await db.Areas.FindAsync(id);
        if (a is not null) { db.Areas.Remove(a); await db.SaveChangesAsync(); }
        return Done("تم الحذف.");
    }

    public async Task<IActionResult> OnPostAddCategoryAsync(string name, int? parentId, string? slug, string? icon)
    {
        name = name?.Trim() ?? "";
        if (name.Length is 0 or > 100) return Fail("اكتب اسم التصنيف.");

        Category? parent = null;
        if (parentId is not null)
        {
            parent = await db.Categories.FirstOrDefaultAsync(c => c.Id == parentId && c.ParentId == null);
            if (parent is null) return Fail("التصنيف الرئيسي مش موجود.");
            slug = await UniqueSlugAsync(parent.Slug);
            icon = parent.Icon;
        }
        else
        {
            slug = slug?.Trim().ToLowerInvariant();
            if (slug is null || !SlugRegex().IsMatch(slug)) return Fail("الرابط لازم يكون حروف إنجليزي صغيرة وأرقام وشرطة (مثال: pets).");
            if (await db.Categories.AnyAsync(c => c.Slug == slug)) return Fail("الرابط ده مستخدم.");
            if (icon is null || !IconKeys.Contains(icon)) icon = "store";
        }

        var order = await db.Categories.Where(c => c.ParentId == parentId).Select(c => (int?)c.SortOrder).MaxAsync() ?? -1;
        db.Categories.Add(new Category { Name = name, ParentId = parent?.Id, Slug = slug, Icon = icon, SortOrder = order + 1 });
        await db.SaveChangesAsync();
        return Done("تمت إضافة التصنيف.");
    }

    public async Task<IActionResult> OnPostUpdateCategoryAsync(int id, string name, int sortOrder, string? icon)
    {
        var c = await db.Categories.FindAsync(id);
        name = name?.Trim() ?? "";
        if (c is null) return NotFound();
        if (name.Length is 0 or > 100) return Fail("اكتب اسم التصنيف.");
        c.Name = name;
        c.SortOrder = sortOrder;
        if (c.ParentId is null && icon is not null && IconKeys.Contains(icon)) c.Icon = icon;
        await db.SaveChangesAsync();
        return Done("تم الحفظ.");
    }

    public async Task<IActionResult> OnPostDeleteCategoryAsync(int id)
    {
        if (await db.Categories.AnyAsync(c => c.ParentId == id)) return Fail("احذف التصنيفات الفرعية الأول.");
        if (await db.Places.AnyAsync(p => p.CategoryId == id)) return Fail("التصنيف فيه أماكن، انقلها الأول.");
        var c = await db.Categories.FindAsync(id);
        if (c is not null) { db.Categories.Remove(c); await db.SaveChangesAsync(); }
        return Done("تم الحذف.");
    }

    private async Task<string> UniqueSlugAsync(string prefix)
    {
        for (var i = 1; ; i++)
        {
            var s = $"{prefix}-{i}";
            if (!await db.Categories.AnyAsync(c => c.Slug == s)) return s;
        }
    }

    private IActionResult Done(string msg) { Flash = msg; return RedirectToPage(); }
    private IActionResult Fail(string msg) { Flash = "⚠ " + msg; return RedirectToPage(); }
}
