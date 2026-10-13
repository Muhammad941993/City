using System.Text;
using System.Text.RegularExpressions;

namespace CityGuide.Web.Domain;

/// <summary>Egyptian phone number helpers.</summary>
public static partial class Phone
{
    [GeneratedRegex(@"^01[0125]\d{8}$")]
    private static partial Regex MobileRegex();

    /// <summary>
    /// Strips spaces/dashes, converts Arabic-Indic digits, and turns +20/0020 into a leading 0.
    /// Returns the input digits unchanged when it is a landline or short code.
    /// </summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (ch is >= '0' and <= '9') sb.Append(ch);
            else if (ch is >= '٠' and <= '٩') sb.Append((char)('0' + (ch - '٠'))); // ٠-٩
            else if (ch is >= '۰' and <= '۹') sb.Append((char)('0' + (ch - '۰'))); // ۰-۹
        }

        var digits = sb.ToString();
        if (digits.StartsWith("0020")) digits = "0" + digits[4..];
        else if (digits.StartsWith("20") && digits.Length == 12) digits = "0" + digits[2..];
        return digits;
    }

    public static bool IsMobile(string? normalized) => normalized is not null && MobileRegex().IsMatch(normalized);

    /// <summary>A short code (123) or landline (0xx…) or mobile.</summary>
    public static bool IsValid(string? normalized) =>
        normalized is { Length: >= 3 and <= 11 } && normalized.All(char.IsAsciiDigit);

    /// <summary>wa.me link, or null when the number is not a mobile.</summary>
    public static string? WhatsAppLink(string? phone, string? message = null)
    {
        var n = Normalize(phone);
        if (!IsMobile(n))
            return null;
        var url = "https://wa.me/2" + n;
        return string.IsNullOrEmpty(message) ? url : url + "?text=" + Uri.EscapeDataString(message);
    }

    public static string TelLink(string? phone) => "tel:" + Normalize(phone);

    /// <summary>"010 1234 5678" for display.</summary>
    public static string Display(string? phone)
    {
        var n = Normalize(phone);
        return n.Length == 11 ? $"{n[..3]} {n[3..7]} {n[7..]}" : n;
    }
}
