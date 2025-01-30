using Facebook;
using Microsoft.Extensions.Logging;

namespace SocialMediaBot.Platforms
{
    public class FacebookPlatform : IMediaPlatform
    {
        private readonly FacebookClient _client;
        private readonly string _pageId;
        private readonly ILogger<FacebookPlatform> _logger;
        private readonly PostingSchedule _schedule;

        public FacebookPlatform(string accessToken, string pageId, PostingSchedule schedule, 
            ILogger<FacebookPlatform> logger)
        {
            _client = new FacebookClient(accessToken);
            _pageId = pageId;
            _schedule = schedule;
            _logger = logger;
        }

        public async Task<bool> PostContentAsync(string content, string? mediaPath = null)
        {
            try
            {
                if (mediaPath != null)
                {
                    // Upload photo with content
                    var mediaData = await File.ReadAllBytesAsync(mediaPath);
                    await _client.PostTaskAsync($"{_pageId}/photos", new
                    {
                        message = content,
                        source = mediaData
                    });
                }
                else
                {
                    // Post text only
                    await _client.PostTaskAsync($"{_pageId}/feed", new
                    {
                        message = content
                    });
                }
                _logger.LogInformation($"Successfully posted to Facebook: {content}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting to Facebook");
                return false;
            }
        }

        public async Task<bool> AutoReplyAsync()
        {
            try
            {
                var result = await _client.GetTaskAsync($"{_pageId}/conversations");
                var conversations = (result as dynamic).data;

                foreach (var conversation in conversations)
                {
                    if (!await HasRepliedAsync(conversation.id.ToString()))
                    {
                        var reply = GenerateReply(conversation.message);
                        await _client.PostTaskAsync($"{conversation.id}/messages", new
                        {
                            message = reply
                        });
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-replying on Facebook");
                return false;
            }
        }

        public async Task<bool> LikeRelatedContentAsync()
        {
            try
            {
                var result = await _client.GetTaskAsync("search", new
                {
                    q = "tech innovation",
                    type = "page"
                });

                var pages = (result as dynamic).data;
                foreach (var page in pages)
                {
                    var posts = await _client.GetTaskAsync($"{page.id}/posts");
                    foreach (var post in (posts as dynamic).data)
                    {
                        await _client.PostTaskAsync($"{post.id}/likes");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking related content on Facebook");
                return false;
            }
        }

        public async Task<Dictionary<string, int>> GetMetricsAsync()
        {
            try
            {
                var result = await _client.GetTaskAsync($"{_pageId}/insights", new
                {
                    metric = "page_fans,page_impressions,page_engaged_users"
                });

                var metrics = new Dictionary<string, int>();
                var data = (result as dynamic).data;
                foreach (var metric in data)
                {
                    metrics[metric.name] = metric.values[0].value;
                }
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Facebook metrics");
                return new Dictionary<string, int>();
            }
        }

        public async Task<bool> CheckRateLimitAsync()
        {
            try
            {
                // Facebook doesn't provide direct rate limit API
                // Implement your own rate limiting logic
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Facebook rate limits");
                return false;
            }
        }

        private async Task<bool> HasRepliedAsync(string conversationId)
        {
            try
            {
                var result = await _client.GetTaskAsync($"{conversationId}/messages");
                var messages = (result as dynamic).data;
                return messages.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateReply(string originalMessage)
        {
            // Add your reply generation logic here
            return "Thank you for your message! We'll get back to you soon. 😊";
        }
    }
}
