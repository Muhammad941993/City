using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Services;

/// <summary>Admin actions that change what visitors see.</summary>
public class Moderation(AppDbContext db, ImageStore images, CityClock clock)
{
    public async Task<bool> ApprovePropertyAsync(int id)
    {
        var p = await db.Properties.FindAsync(id);
        if (p is null) return false;
        ListingRules.Approve(p, clock.UtcNow);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectPropertyAsync(int id, string? reason)
    {
        var p = await db.Properties.FindAsync(id);
        if (p is null) return false;
        ListingRules.Reject(p, reason, clock.UtcNow);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ClosePropertyAsync(int id)
    {
        var p = await db.Properties.FindAsync(id);
        if (p is null) return false;
        ListingRules.Close(p, clock.UtcNow);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task DeletePropertyAsync(int id)
    {
        var p = await db.Properties.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return;
        foreach (var img in p.Images) images.Delete(img.FileName);
        db.Properties.Remove(p);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Publishes a new place, or — for a suggested correction — copies the suggestion onto the
    /// original place (new photos replace the old ones when any were sent) and removes the suggestion.
    /// </summary>
    public async Task<int?> ApprovePlaceAsync(int id)
    {
        var p = await db.Places.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return null;
        var now = clock.UtcNow;

        if (p.EditOfPlaceId is not { } originalId)
        {
            p.Status = ListingStatus.Published;
            p.RejectionReason = null;
            p.UpdatedAt = now;
            await db.SaveChangesAsync();
            return p.Id;
        }

        var original = await db.Places.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == originalId);
        if (original is null)
        {
            // The original was deleted meanwhile: publish the suggestion as a new place.
            p.EditOfPlaceId = null;
            p.Status = ListingStatus.Published;
            p.UpdatedAt = now;
            await db.SaveChangesAsync();
            return p.Id;
        }

        // The suggester's contact belongs to the suggestion, not the place.
        var (submitterName, submitterPhone) = (original.SubmitterName, original.SubmitterPhone);
        PlaceInput.From(p).ApplyTo(original);
        (original.SubmitterName, original.SubmitterPhone) = (submitterName, submitterPhone);
        original.UpdatedAt = now;

        if (p.Images.Count > 0)
        {
            foreach (var img in original.Images) images.Delete(img.FileName);
            original.Images.Clear();
            foreach (var img in p.Images.OrderBy(i => i.SortOrder))
                original.Images.Add(new PlaceImage { FileName = img.FileName, SortOrder = img.SortOrder });
            p.Images.Clear();
        }

        db.Places.Remove(p);
        await db.SaveChangesAsync();
        return original.Id;
    }

    public async Task<bool> RejectPlaceAsync(int id, string? reason)
    {
        var p = await db.Places.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return false;

        if (p.EditOfPlaceId is not null)
        {
            // A rejected correction has nothing to keep.
            foreach (var img in p.Images) images.Delete(img.FileName);
            db.Places.Remove(p);
        }
        else
        {
            p.Status = ListingStatus.Rejected;
            p.RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            p.UpdatedAt = clock.UtcNow;
        }
        await db.SaveChangesAsync();
        return true;
    }

    public async Task DeletePlaceAsync(int id)
    {
        var p = await db.Places.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return;
        var suggestions = await db.Places.Include(x => x.Images).Where(x => x.EditOfPlaceId == id).ToListAsync();
        foreach (var x in suggestions.Append(p))
        {
            foreach (var img in x.Images) images.Delete(img.FileName);
            db.Places.Remove(x);
        }
        await db.SaveChangesAsync();
    }
}
