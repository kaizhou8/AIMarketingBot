using SocialMediaBot.Models;

namespace SocialMediaBot.Platforms
{
    public interface IMediaPlatform
    {
        Task<bool> PostContentAsync(string content, string? mediaPath = null);
        Task<bool> AutoReplyAsync();
        Task<bool> LikeRelatedContentAsync();
        Task<Dictionary<string, int>> GetMetricsAsync();
        Task<bool> CheckRateLimitAsync();
    }
}
