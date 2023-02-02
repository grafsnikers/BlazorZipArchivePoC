namespace BlazorZipper.Services;

public interface IArchiveService
{
    public Task<byte[]> CreateArchiveAsync(List<UrlRow> urls, int maxDegreeOfParallelism);
}