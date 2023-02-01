using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorZipper.Services;

public class ArchiveService
{
    private readonly HttpClient _httpClient;

    public ArchiveService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<byte[]> CreateArchive(IEnumerable<string> urls)
    {
        byte[] archiveBytes;

        await using (var memoryStream = new MemoryStream())
        {
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                await Task.WhenAll(
                    urls
                        .Select(async url =>
                        {
                            var fileName = Path.GetFileName(url);
                            Console.WriteLine($"Started Downloading {fileName}");

                            await using var fileStream = await GetStream(url);

                            return AddFileToArchive(fileName, fileStream, archive);
                        }));
            }

            archiveBytes = memoryStream.ToArray();
        }

        return archiveBytes;
    }

    private async Task AddFileToArchive(string fileName,
        Stream fileContentStream,
        ZipArchive archive)
    {
        var entry = archive.CreateEntry(fileName);
        await using var entryStream = entry.Open();

        await fileContentStream.CopyToAsync(entryStream);

        Console.WriteLine($"Finished processing {fileName}");
    }

    private async Task<Stream> GetStream(string url)
    {
        return await _httpClient.GetStreamAsync(new Uri(url));
    }
}
