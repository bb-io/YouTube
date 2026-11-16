using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Tests.YouTube.Base;
public sealed class FileManager : IFileManagementClient
{
    public Task<Stream> DownloadAsync(FileReference reference) => Task.FromResult<Stream>(File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestFiles", "Input", Path.GetFileName(reference.Name))));
    public async Task<FileReference> UploadAsync(Stream stream, string contentType, string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestFiles", "Output", Path.GetFileName(fileName));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = File.Create(path);
        await stream.CopyToAsync(file);
        return new FileReference
        {
            Name = fileName,
            ContentType = contentType
        };
    }
}
