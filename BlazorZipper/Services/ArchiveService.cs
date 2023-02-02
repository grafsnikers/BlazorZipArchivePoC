using System.IO.Compression;
using System.Text.RegularExpressions;

namespace BlazorZipper.Services;

public class ArchiveService : IArchiveService
{
    private static readonly Regex _removeInvalidChars = new Regex(
        $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]",
        RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly HttpClient _httpClient;

    public ArchiveService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<byte[]> CreateArchiveAsync(List<UrlRow> urls, int maxDegreeOfParallelism)
    {
        byte[] archiveBytes;

        await using (var memoryStream = new MemoryStream())
        {
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var tasks =
                    urls
                        .Select(async urlRow =>
                        {
                            string fileName = GetFileName(urlRow.Url);

                            Console.WriteLine($"Started Downloading {fileName}");
                            urlRow.Progress.Report("Downloading...");

                            await using var fileStream = await GetStream(urlRow.Url);

                            urlRow.Progress.Report("Compressing...");

                            await AddFileToArchive(fileName, fileStream, archive);

                            Console.WriteLine($"Finished processing {fileName}");
                            urlRow.Progress.Report("Done!");
                        });

                var parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = maxDegreeOfParallelism};
                await Parallel.ForEachAsync(tasks, parallelOptions, async (task, token) => await task);
            }

            archiveBytes = memoryStream.ToArray();
        }

        Console.WriteLine("Archive is ready!");

        return archiveBytes;
    }

    private async Task AddFileToArchive(string fileName,
        Stream fileContentStream,
        ZipArchive archive)
    {
        var entry = archive.CreateEntry(fileName);
        await using var entryStream = entry.Open();

        await fileContentStream.CopyToAsync(entryStream);
    }

    private async Task<Stream> GetStream(string url)
    {
        return await _httpClient.GetStreamAsync(new Uri(url));
    }

    private string GetFileName(string url)
    {
        var fileName = Path.GetFileName(url);

        fileName = string.IsNullOrWhiteSpace(fileName)
            ? Random.Shared.Next().ToString() + ".jpg"
            : fileName;

        fileName = SanitizedFileName(fileName);

        // Quick fix
        if (url.Contains("unsplash"))
        {
            fileName = Random.Shared.Next().ToString() + ".jpg";
        }

        return fileName;
    }

    private string SanitizedFileName(string fileName, string replacement = "_")
    {
        return _removeInvalidChars.Replace(fileName, replacement);
    }
}

public class UrlRow
{
    public string Url { get; set; }
    public string Status { get; set; }
    public IProgress<string> Progress { get; set; }
}