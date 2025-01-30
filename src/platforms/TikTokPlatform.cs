using Microsoft.Extensions.Logging;
using System.Net.Http;
using Newtonsoft.Json;

namespace SocialMediaBot.Platforms
{
    public class TikTokPlatform : IMediaPlatform
    {
        private readonly HttpClient _client;
        private readonly string _accessToken;
        private readonly ILogger<TikTokPlatform> _logger;
        private readonly PostingSchedule _schedule;

        public TikTokPlatform(string accessToken, PostingSchedule schedule, ILogger<TikTokPlatform> logger)
        {
            _client = new HttpClient();
            _accessToken = accessToken;
            _schedule = schedule;
            _logger = logger;
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        }

        public async Task<bool> PostContentAsync(string content, string? mediaPath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(mediaPath))
                {
                    _logger.LogError("Video path is required for TikTok posts");
                    return false;
                }

                // First, initiate video upload
                var uploadUrl = await InitiateVideoUpload();
                
                // Upload video chunks
                await UploadVideoChunks(uploadUrl, mediaPath);
                
                // Finalize the post with description
                var response = await _client.PostAsync("https://open.tiktokapis.com/v2/post/publish/", 
                    new StringContent(JsonConvert.SerializeObject(new
                    {
                        description = content,
                        privacy_level = "PUBLIC",
                        disable_duet = false,
                        disable_comment = false,
                        disable_stitch = false
                    })));

                var success = response.IsSuccessStatusCode;
                if (success)
                {
                    _logger.LogInformation($"Successfully posted to TikTok: {content}");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting to TikTok");
                return false;
            }
        }

        public async Task<bool> AutoReplyAsync()
        {
            try
            {
                var response = await _client.GetAsync("https://open.tiktokapis.com/v2/video/comment/list/");
                if (response.IsSuccessStatusCode)
                {
                    var comments = JsonConvert.DeserializeObject<dynamic>(
                        await response.Content.ReadAsStringAsync()
                    );

                    foreach (var comment in comments.data.comments)
                    {
                        if (!await HasRepliedAsync(comment.id.ToString()))
                        {
                            var reply = GenerateReply(comment.text.ToString());
                            await _client.PostAsync("https://open.tiktokapis.com/v2/video/comment/reply/",
                                new StringContent(JsonConvert.SerializeObject(new
                                {
                                    comment_id = comment.id,
                                    text = reply
                                })));
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-replying on TikTok");
                return false;
            }
        }

        public async Task<bool> LikeRelatedContentAsync()
        {
            try
            {
                var response = await _client.GetAsync(
                    "https://open.tiktokapis.com/v2/video/search/?keywords=tech,innovation"
                );
                
                if (response.IsSuccessStatusCode)
                {
                    var videos = JsonConvert.DeserializeObject<dynamic>(
                        await response.Content.ReadAsStringAsync()
                    );

                    foreach (var video in videos.data.videos)
                    {
                        await _client.PostAsync("https://open.tiktokapis.com/v2/video/like/",
                            new StringContent(JsonConvert.SerializeObject(new
                            {
                                video_id = video.id
                            })));
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking related content on TikTok");
                return false;
            }
        }

        public async Task<Dictionary<string, int>> GetMetricsAsync()
        {
            try
            {
                var response = await _client.GetAsync("https://open.tiktokapis.com/v2/user/info/");
                var metrics = new Dictionary<string, int>();

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonConvert.DeserializeObject<dynamic>(
                        await response.Content.ReadAsStringAsync()
                    );

                    metrics["followers"] = data.data.stats.follower_count;
                    metrics["following"] = data.data.stats.following_count;
                    metrics["likes"] = data.data.stats.heart_count;
                    metrics["videos"] = data.data.stats.video_count;
                }

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TikTok metrics");
                return new Dictionary<string, int>();
            }
        }

        public async Task<bool> CheckRateLimitAsync()
        {
            try
            {
                var response = await _client.GetAsync("https://open.tiktokapis.com/v2/rate_limit/info/");
                if (response.IsSuccessStatusCode)
                {
                    var data = JsonConvert.DeserializeObject<dynamic>(
                        await response.Content.ReadAsStringAsync()
                    );
                    return data.data.remaining_requests > 0;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking TikTok rate limits");
                return false;
            }
        }

        private async Task<string> InitiateVideoUpload()
        {
            var response = await _client.PostAsync("https://open.tiktokapis.com/v2/video/upload/",
                new StringContent("{}"));
            var data = JsonConvert.DeserializeObject<dynamic>(
                await response.Content.ReadAsStringAsync()
            );
            return data.data.upload_url;
        }

        private async Task UploadVideoChunks(string uploadUrl, string videoPath)
        {
            const int chunkSize = 5 * 1024 * 1024; // 5MB chunks
            var fileBytes = await File.ReadAllBytesAsync(videoPath);
            
            for (int i = 0; i < fileBytes.Length; i += chunkSize)
            {
                var chunk = new byte[Math.Min(chunkSize, fileBytes.Length - i)];
                Array.Copy(fileBytes, i, chunk, 0, chunk.Length);
                
                var content = new ByteArrayContent(chunk);
                content.Headers.Add("Content-Range", 
                    $"bytes {i}-{i + chunk.Length - 1}/{fileBytes.Length}");
                
                await _client.PutAsync(uploadUrl, content);
            }
        }

        private async Task<bool> HasRepliedAsync(string commentId)
        {
            try
            {
                var response = await _client.GetAsync(
                    $"https://open.tiktokapis.com/v2/video/comment/list/reply/?comment_id={commentId}"
                );
                if (response.IsSuccessStatusCode)
                {
                    var replies = JsonConvert.DeserializeObject<dynamic>(
                        await response.Content.ReadAsStringAsync()
                    );
                    return replies.data.replies.Count > 0;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateReply(string originalComment)
        {
            // Add your reply generation logic here
            return "Thanks for your comment! 😊 Follow us for more content!";
        }
    }
}
