namespace CityGuide.Web.Domain;

/// <summary>
/// Moderation rules from the spec: a property is published for 30 days after
/// approval, the advertiser is reminded 3 days before it ends, and a sold/rented
/// listing stays visible with a badge for 7 days.
/// </summary>
public static class ListingRules
{
    public static readonly TimeSpan PublishDuration = TimeSpan.FromDays(30);
    public static readonly TimeSpan ReminderBefore = TimeSpan.FromDays(3);
    public static readonly TimeSpan ClosedVisibleFor = TimeSpan.FromDays(7);

    public static void Approve(Property p, DateTime nowUtc)
    {
        p.Status = ListingStatus.Published;
        p.RejectionReason = null;
        p.PublishedAt = nowUtc;
        p.ExpiresAt = nowUtc + PublishDuration;
        p.ClosedAt = null;
        p.UpdatedAt = nowUtc;
    }

    /// <summary>Extends a published or expired listing for another full period.</summary>
    public static void Renew(Property p, DateTime nowUtc) => Approve(p, nowUtc);

    public static void Reject(Property p, string? reason, DateTime nowUtc)
    {
        p.Status = ListingStatus.Rejected;
        p.RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        p.UpdatedAt = nowUtc;
    }

    public static void Close(Property p, DateTime nowUtc)
    {
        p.Status = ListingStatus.Closed;
        p.ClosedAt = nowUtc;
        p.UpdatedAt = nowUtc;
    }

    public static bool IsPubliclyVisible(Property p, DateTime nowUtc) => p.Status switch
    {
        ListingStatus.Published => p.ExpiresAt is null || p.ExpiresAt > nowUtc,
        ListingStatus.Closed => p.ClosedAt is not null && p.ClosedAt.Value + ClosedVisibleFor > nowUtc,
        _ => false,
    };

    /// <summary>EF-translatable version of <see cref="IsPubliclyVisible"/>.</summary>
    public static IQueryable<Property> WherePubliclyVisible(this IQueryable<Property> q, DateTime nowUtc)
    {
        var closedSince = nowUtc - ClosedVisibleFor;
        return q.Where(p =>
            (p.Status == ListingStatus.Published && (p.ExpiresAt == null || p.ExpiresAt > nowUtc)) ||
            (p.Status == ListingStatus.Closed && p.ClosedAt != null && p.ClosedAt > closedSince));
    }

    public static bool NeedsReminder(Property p, DateTime nowUtc) =>
        p.Status == ListingStatus.Published &&
        p.ExpiresAt is not null &&
        p.ExpiresAt > nowUtc &&
        p.ExpiresAt <= nowUtc + ReminderBefore;

    /// <summary>Marks published listings whose period has ended as expired. Returns how many changed.</summary>
    public static int ExpireDue(IEnumerable<Property> published, DateTime nowUtc)
    {
        var n = 0;
        foreach (var p in published)
        {
            if (p.Status == ListingStatus.Published && p.ExpiresAt is not null && p.ExpiresAt <= nowUtc)
            {
                p.Status = ListingStatus.Expired;
                p.UpdatedAt = nowUtc;
                n++;
            }
        }
        return n;
    }
}
