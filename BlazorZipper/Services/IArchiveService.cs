namespace BlazorZipper.Services;

public interface IArchiveService
{
    public Task<byte[]> CreateArchiveAsync(IEnumerable<string> urls);
}