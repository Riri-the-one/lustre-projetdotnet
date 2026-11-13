
namespace ProjetDotNet.Shared;

public interface IFileService
{
    void DeleteFile(string fileName);
    Task<string> SaveFile(IFormFile file, string[] allowedExtensions);
}

public class FileService(IWebHostEnvironment environment) : IFileService
{
    private const int MaxBytes = 1024 * 1024;
    private string DirectoryPath => Path.Combine(environment.WebRootPath, "images");

    public async Task<string> SaveFile(IFormFile file, string[] allowedExtensions)
    {
        if (file.Length is <= 0 or > MaxBytes) throw new InvalidOperationException("Image must be between 1 byte and 1 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
            || extension is not (".jpg" or ".jpeg" or ".png"))
            throw new InvalidOperationException("Only JPEG and PNG images are accepted.");
        await using var input = file.OpenReadStream();
        using var data = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await input.ReadAsync(buffer)) > 0)
        {
            if (data.Length + count > MaxBytes) throw new InvalidOperationException("Image exceeds 1 MB.");
            await data.WriteAsync(buffer.AsMemory(0, count));
        }
        var bytes = data.ToArray();
        var png = bytes.Length >= 24 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 && bytes[^2] == 255 && bytes[^1] == 217;
        if ((extension == ".png" && !png) || (extension != ".png" && !jpeg))
            throw new InvalidOperationException("Image contents do not match its extension.");
        // Identify dimensions before allocating decoded pixels, then decode and re-encode.
        try
        {
            using var encoded = new MemoryStream(bytes);
            var info = StbImageSharp.ImageInfo.FromStream(encoded)
                ?? throw new InvalidOperationException("The image header is invalid.");
            if (info.Width <= 0 || info.Height <= 0 || info.Width > 4096 || info.Height > 4096
                || (long)info.Width * info.Height > 8_000_000)
                throw new InvalidOperationException("Image dimensions are too large.");
            var image = StbImageSharp.ImageResult.FromMemory(bytes, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
            using var normalized = new MemoryStream();
            var writer = new StbImageWriteSharp.ImageWriter();
            if (extension == ".png") writer.WritePng(image.Data, image.Width, image.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, normalized);
            else writer.WriteJpg(image.Data, image.Width, image.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, normalized, 85);
            if (normalized.Length > MaxBytes) throw new InvalidOperationException("Normalized image exceeds 1 MB.");
            bytes = normalized.ToArray();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidOperationException("The uploaded image cannot be decoded.", ex);
        }
        Directory.CreateDirectory(DirectoryPath);
        var name = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(DirectoryPath, name);
        try { await File.WriteAllBytesAsync(path, bytes); }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
        return name;
    }

    public void DeleteFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName
            || fileName.Contains('\\') || fileName.Contains('/') || fileName.Contains(':'))
            throw new InvalidOperationException("Invalid image name.");
        // File.Delete is idempotent for an absent file; other IO failures propagate to the caller.
        if (Directory.Exists(DirectoryPath)) File.Delete(Path.Combine(DirectoryPath, fileName));
    }
}
