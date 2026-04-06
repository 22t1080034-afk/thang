namespace SV22T1080034.Shop.Services
{
    public interface IImageProvider
    {
        string Name { get; }
        Task<string?> FetchImageUrlAsync(string productName, int productId);
        Task<bool> IsAvailableAsync();
    }
}
