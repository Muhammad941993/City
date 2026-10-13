using Microsoft.AspNetCore.Html;

namespace CityGuide.Web.Services;

/// <summary>Inline stroke icons (decorative, aria-hidden). Keeps pages free of icon fonts.</summary>
public static class Icons
{
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["pin"] = "<path d=\"M12 21s7-6.2 7-11a7 7 0 10-14 0c0 4.8 7 11 7 11z\"/><circle cx=\"12\" cy=\"10\" r=\"2.5\"/>",
        ["search"] = "<circle cx=\"11\" cy=\"11\" r=\"7\"/><path d=\"M20 20l-3.5-3.5\"/>",
        ["home"] = "<path d=\"M4 11l8-7 8 7v9a1 1 0 01-1 1h-4v-6H9v6H5a1 1 0 01-1-1z\"/>",
        ["building"] = "<path d=\"M3 20h18M5 20V9l7-5 7 5v11M9 20v-6h6v6\"/>",
        ["phone"] = "<path d=\"M5 4h4l2 5-2.5 1.5a11 11 0 005 5L15 13l5 2v4a2 2 0 01-2 2A16 16 0 013 6a2 2 0 012-2z\"/>",
        ["whatsapp"] = "<path d=\"M21 12a8 8 0 01-11.8 7L4 20l1.1-4.6A8 8 0 1121 12z\"/>",
        ["share"] = "<path d=\"M14 5l7 7-7 7M21 12H8a5 5 0 00-5 5v1\"/>",
        ["plus"] = "<path d=\"M12 5v14M5 12h14\"/>",
        ["check"] = "<path d=\"M5 12.5l4.5 4.5L19 7.5\"/>",
        ["back"] = "<path d=\"M9 5l7 7-7 7\"/>",
        ["close"] = "<path d=\"M6 6l12 12M18 6L6 18\"/>",
        ["filter"] = "<path d=\"M4 6h16M7 12h10M10 18h4\"/>",
        ["map"] = "<path d=\"M9 4L3 6v14l6-2 6 2 6-2V4l-6 2-6-2zM9 4v14M15 6v14\"/>",
        ["clock"] = "<circle cx=\"12\" cy=\"12\" r=\"8\"/><path d=\"M12 8v4l3 2\"/>",
        ["flag"] = "<path d=\"M5 21V4M5 4h11l-2 4 2 4H5\"/>",
        ["camera"] = "<path d=\"M4 8h3l2-3h6l2 3h3v11H4z\"/><circle cx=\"12\" cy=\"13\" r=\"3.5\"/>",
        ["grid"] = "<rect x=\"4\" y=\"4\" width=\"7\" height=\"7\" rx=\"1.5\"/><rect x=\"13\" y=\"4\" width=\"7\" height=\"7\" rx=\"1.5\"/><rect x=\"4\" y=\"13\" width=\"7\" height=\"7\" rx=\"1.5\"/><rect x=\"13\" y=\"13\" width=\"7\" height=\"7\" rx=\"1.5\"/>",
        ["store"] = "<path d=\"M4 9l1-5h14l1 5M4 9v11h16V9M4 9a3 3 0 006 0 3 3 0 006 0 3 3 0 004 0\"/>",
        ["health"] = "<path d=\"M12 20s-7-4.5-7-10a4 4 0 017-2.6A4 4 0 0119 10c0 5.5-7 10-7 10z\"/><path d=\"M12 9v5M9.5 11.5h5\"/>",
        ["tools"] = "<path d=\"M14.5 6.5a4 4 0 00-5.3 5.3L4 17l3 3 5.2-5.2a4 4 0 005.3-5.3l-2.5 2.5-2.5-.5-.5-2.5z\"/>",
        ["food"] = "<path d=\"M7 3v8a2 2 0 002 2v8M5 3v5M9 3v5M16 21V3c2.5 1 4 4 4 8h-4\"/>",
        ["school"] = "<path d=\"M3 9l9-5 9 5-9 5-9-5zM7 11.5V16c0 1.5 2.5 3 5 3s5-1.5 5-3v-4.5\"/>",
        ["cart"] = "<path d=\"M3 4h2l2.4 11h10.2L20 8H6.2\"/><circle cx=\"9\" cy=\"19\" r=\"1.5\"/><circle cx=\"17\" cy=\"19\" r=\"1.5\"/>",
        ["gov"] = "<path d=\"M3 21h18M5 21V10M19 21V10M9 21V10M15 21V10M3 10l9-6 9 6z\"/>",
        ["bus"] = "<rect x=\"5\" y=\"3\" width=\"14\" height=\"15\" rx=\"2\"/><path d=\"M5 11h14M8 18v3M16 18v3\"/><circle cx=\"8.5\" cy=\"14.5\" r=\".8\"/><circle cx=\"15.5\" cy=\"14.5\" r=\".8\"/>",
        ["siren"] = "<path d=\"M12 3a6 6 0 016 6v6H6V9a6 6 0 016-6zM4 19h16M12 7v4\"/>",
        ["logout"] = "<path d=\"M15 4h4v16h-4M10 8l-4 4 4 4M6 12h10\"/>",
    };

    public static IHtmlContent Svg(string name, int size = 22, double stroke = 2)
    {
        var path = Paths.GetValueOrDefault(name) ?? Paths["store"];
        return new HtmlString(
            $"<svg class=\"ico\" width=\"{size}\" height=\"{size}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" " +
            $"stroke-width=\"{stroke.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">{path}</svg>");
    }

    /// <summary>Colour pairs for category tiles: (background, foreground).</summary>
    public static (string Bg, string Fg) Tone(string icon) => icon switch
    {
        "health" => ("#E7F5EC", "#0F7A48"),
        "siren" => ("#FBE4E4", "#B03030"),
        "food" => ("#FDEBD0", "#A8620A"),
        "tools" => ("#FDEBD0", "#A8620A"),
        "school" => ("#E8EEFB", "#2F55B5"),
        "gov" => ("#EEEAF8", "#5B45A8"),
        _ => ("#E3F1F2", "#0B7A83"),
    };
}
