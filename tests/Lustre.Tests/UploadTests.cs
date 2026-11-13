using Microsoft.AspNetCore.Hosting;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using ProjetDotNet.Shared;
using Xunit;

namespace Lustre.Tests;

public sealed class UploadTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lustre-upload-tests-" + Guid.NewGuid().ToString("N"));
    private readonly FileService _files;
    public UploadTests() { Directory.CreateDirectory(_root); _files = new FileService(new TestEnvironment { WebRootPath = _root }); }
    private static IFormFile Upload(byte[] bytes, string name) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "image", name);
    [Theory] [InlineData("payload.png")] [InlineData("payload.jpg")] [InlineData("payload.svg")]
    public async Task RejectsForgedImageExtensions(string name) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => _files.SaveFile(Upload("<script>alert(1)</script>"u8.ToArray(), name), new[] { ".png", ".jpg", ".svg" }));
    [Fact] public async Task RejectsEmptyAndOversizedFiles()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _files.SaveFile(Upload(Array.Empty<byte>(), "empty.png"), new[] { ".png" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _files.SaveFile(Upload(new byte[1024 * 1024 + 1], "large.png"), new[] { ".png" }));
    }
    [Theory] [InlineData("../secret")] [InlineData("..\\secret")] [InlineData("C:\\secret")]
    public void RejectsTraversalOnDelete(string name) => Assert.Throws<InvalidOperationException>(() => _files.DeleteFile(name));
    [Fact] public void MissingImageDeletionIsIdempotent() => _files.DeleteFile("missing.png");
    [Fact] public async Task GeneratesNameAndIgnoresClientPath()
    {

        using var buffer = new MemoryStream();
        new StbImageWriteSharp.ImageWriter().WritePng(new byte[] { 255, 255, 255, 255 }, 1, 1, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, buffer);
        var bytes = buffer.ToArray();
        var name = await _files.SaveFile(Upload(bytes, "../../client.PNG"), new[] { ".png" });
        Assert.EndsWith(".png", name); Assert.DoesNotContain("client", name);
        var decoded = StbImageSharp.ImageResult.FromMemory(await File.ReadAllBytesAsync(Path.Combine(_root, "images", name)));
        Assert.Equal(1, decoded.Width);
        _files.DeleteFile(name);
    }
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
