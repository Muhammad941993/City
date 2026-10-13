using System.Text;
using CityGuide.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace CityGuide.Tests;

public class ImageStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cg-img-" + Guid.NewGuid().ToString("N"));
    private readonly ImageStore _store;

    public ImageStoreTests()
    {
        _store = new ImageStore(Options.Create(new ImageOptions { UploadPath = _dir, FullWidth = 800, ThumbWidth = 200 }), new Env());
    }

    public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    private static IFormFile File(byte[] bytes, string name = "a.png") =>
        new FormFile(new MemoryStream(bytes), 0, bytes.Length, "images", name);

    private static byte[] Png(int w, int h)
    {
        using var bmp = new SKBitmap(w, h);
        bmp.Erase(SKColors.Teal);
        using var img = SKImage.FromBitmap(bmp);
        return img.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    [Fact]
    public async Task Saves_resized_webp_and_thumbnail()
    {
        var (name, error) = await _store.SaveAsync(File(Png(1600, 1200)));
        Assert.Null(error);
        using var full = SKBitmap.Decode(Path.Combine(_dir, name + ".webp"));
        using var thumb = SKBitmap.Decode(Path.Combine(_dir, name + "_t.webp"));
        Assert.Equal((800, 600), (full.Width, full.Height));
        Assert.Equal((200, 150), (thumb.Width, thumb.Height));
    }

    [Fact]
    public async Task Small_images_are_not_upscaled()
    {
        var (name, _) = await _store.SaveAsync(File(Png(300, 100)));
        using var full = SKBitmap.Decode(Path.Combine(_dir, name + ".webp"));
        Assert.Equal(300, full.Width);
    }

    [Fact]
    public async Task Rejects_non_images()
    {
        var (name, error) = await _store.SaveAsync(File(Encoding.UTF8.GetBytes("<script>alert(1)</script>"), "x.jpg"));
        Assert.Null(name);
        Assert.NotNull(error);
        Assert.Empty(Directory.GetFiles(_dir));
    }

    [Fact]
    public void Delete_ignores_non_guid_names()
    {
        var outside = Path.Combine(_dir, "..", "keep.webp");
        System.IO.File.WriteAllText(outside, "x");
        _store.Delete("../keep");
        Assert.True(System.IO.File.Exists(outside));
        System.IO.File.Delete(outside);
    }

    private sealed class Env : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Test";
    }
}
