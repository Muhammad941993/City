using Microsoft.Extensions.Options;
using SkiaSharp;

namespace CityGuide.Web.Services;

public class ImageOptions
{
    /// <summary>Folder for uploaded images. Relative paths are resolved under wwwroot.</summary>
    public string UploadPath { get; set; } = "uploads";
    public long MaxBytes { get; set; } = 8 * 1024 * 1024;
    public int FullWidth { get; set; } = 1280;
    public int ThumbWidth { get; set; } = 480;
    public int Quality { get; set; } = 75;
}

/// <summary>
/// Decodes uploads (rejecting anything that is not an image), fixes phone-camera rotation,
/// and stores a compressed WebP plus a thumbnail so pages stay light on mobile data.
/// Re-encoding also drops EXIF metadata such as GPS location.
/// </summary>
public class ImageStore
{
    private const long MaxPixels = 50_000_000;

    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    private readonly ImageOptions _opt;
    private readonly string _root;

    public ImageStore(IOptions<ImageOptions> opt, IWebHostEnvironment env)
    {
        _opt = opt.Value;
        _root = Path.IsPathRooted(_opt.UploadPath) ? _opt.UploadPath : Path.Combine(env.WebRootPath, _opt.UploadPath);
        Directory.CreateDirectory(_root);
    }

    public static string Url(string fileName) => $"/uploads/{fileName}.webp";
    public static string ThumbUrl(string fileName) => $"/uploads/{fileName}_t.webp";

    /// <summary>Saves the image and returns its base file name, or an Arabic error message.</summary>
    public async Task<(string? FileName, string? Error)> SaveAsync(IFormFile file, CancellationToken ct = default)
    {
        if (file.Length == 0)
            return (null, "الملف فاضي.");
        if (file.Length > _opt.MaxBytes)
            return (null, $"الصورة أكبر من {_opt.MaxBytes / 1024 / 1024} ميجا.");

        using var buffer = new MemoryStream((int)file.Length);
        await using (var s = file.OpenReadStream())
            await s.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        var name = Guid.NewGuid().ToString("N");
        var error = Process(buffer, name);
        return error is null ? (name, null) : (null, error);
    }

    private string? Process(Stream input, string name)
    {
        using var codec = SKCodec.Create(input);
        if (codec is null)
            return "الملف ده مش صورة.";
        if ((long)codec.Info.Width * codec.Info.Height > MaxPixels)
            return "أبعاد الصورة كبيرة جداً.";

        using var decoded = SKBitmap.Decode(codec);
        if (decoded is null)
            return "مقدرناش نقرا الصورة.";

        using var oriented = Orient(decoded, codec.EncodedOrigin);
        var source = oriented ?? decoded;

        if (!WriteWebp(source, _opt.FullWidth, Path.Combine(_root, name + ".webp")) ||
            !WriteWebp(source, _opt.ThumbWidth, Path.Combine(_root, name + "_t.webp")))
        {
            Delete(name);
            return "مقدرناش نحفظ الصورة.";
        }
        return null;
    }

    private bool WriteWebp(SKBitmap source, int maxWidth, string path)
    {
        SKBitmap? resized = null;
        try
        {
            var bmp = source;
            if (source.Width > maxWidth)
            {
                var h = Math.Max(1, (int)Math.Round(source.Height * (maxWidth / (double)source.Width)));
                resized = source.Resize(new SKImageInfo(maxWidth, h), Sampling);
                if (resized is null) return false;
                bmp = resized;
            }

            using var image = SKImage.FromBitmap(bmp);
            using var data = image.Encode(SKEncodedImageFormat.Webp, _opt.Quality);
            if (data is null) return false;
            using var fs = File.Create(path);
            data.SaveTo(fs);
            return true;
        }
        finally
        {
            resized?.Dispose();
        }
    }

    /// <summary>Applies the EXIF orientation; returns null when nothing needs to change.</summary>
    private static SKBitmap? Orient(SKBitmap src, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
            return null;

        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        int w = src.Width, h = src.Height;
        var dst = new SKBitmap(swap ? h : w, swap ? w : h);
        using var canvas = new SKCanvas(dst);

        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                canvas.Scale(-1, 1, w / 2f, 0);
                break;
            case SKEncodedOrigin.BottomRight:
                canvas.RotateDegrees(180, w / 2f, h / 2f);
                break;
            case SKEncodedOrigin.BottomLeft:
                canvas.Scale(1, -1, 0, h / 2f);
                break;
            case SKEncodedOrigin.LeftTop:
                canvas.Scale(-1, 1, h / 2f, 0);
                canvas.Translate(h, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightTop:
                canvas.Translate(h, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom:
                canvas.Scale(-1, 1, h / 2f, 0);
                canvas.Translate(0, w);
                canvas.RotateDegrees(270);
                break;
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, w);
                canvas.RotateDegrees(270);
                break;
        }

        canvas.DrawBitmap(src, 0, 0);
        return dst;
    }

    public void Delete(string fileName)
    {
        // File names are generated GUIDs; never trust anything else as a path.
        if (!Guid.TryParseExact(fileName, "N", out _))
            return;
        foreach (var f in new[] { fileName + ".webp", fileName + "_t.webp" })
        {
            var path = Path.Combine(_root, f);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
