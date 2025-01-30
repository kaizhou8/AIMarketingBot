using Microsoft.Extensions.Logging;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace SocialMediaBot.Platforms
{
    public class TwitterPlatform : IMediaPlatform
    {
        private readonly TwitterClient _client;
        private readonly ILogger<TwitterPlatform> _logger;
        private readonly PostingSchedule _schedule;

        public TwitterPlatform(string apiKey, string apiKeySecret, string accessToken, string accessTokenSecret, 
            PostingSchedule schedule, ILogger<TwitterPlatform> logger)
        {
            _client = new TwitterClient(apiKey, apiKeySecret, accessToken, accessTokenSecret);
            _schedule = schedule;
            _logger = logger;
        }

        public async Task<bool> PostContentAsync(string content, string? mediaPath = null)
        {
            try
            {
                if (mediaPath != null)
                {
                    var media = await _client.Upload.UploadTweetImageAsync(mediaPath);
                    await _client.Tweets.PublishTweetAsync(new PublishTweetParameters(content)
                    {
                        MediaIds = { media.Id }
                    });
                }
                else
                {
                    await _client.Tweets.PublishTweetAsync(content);
                }
                _logger.LogInformation($"Successfully posted to Twitter: {content}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting to Twitter");
                return false;
            }
        }

        public async Task<bool> AutoReplyAsync()
        {
            try
            {
                var mentions = await _client.Tweets.GetMentionsTimelineAsync();
                foreach (var mention in mentions)
                {
                    if (!await HasRepliedAsync(mention.Id))
                    {
                        var reply = GenerateReply(mention.Text);
                        await _client.Tweets.PublishTweetAsync(
                            new PublishTweetParameters(reply)
                            {
                                InReplyToTweetId = mention.Id
                            });
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-replying on Twitter");
                return false;
            }
        }

        public async Task<bool> LikeRelatedContentAsync()
        {
            try
            {
                var searchParameters = new SearchTweetsParameters("#tech OR #innovation -filter:retweets")
                {
                    Lang = LanguageFilter.English,
                    SearchType = SearchResultType.Recent
                };

                var tweets = await _client.Search.SearchTweetsAsync(searchParameters);
                foreach (var tweet in tweets)
                {
                    if (!tweet.Favorited)
                    {
                        await _client.Tweets.FavoriteTweetAsync(tweet.Id);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking related content on Twitter");
                return false;
            }
        }

        public async Task<Dictionary<string, int>> GetMetricsAsync()
        {
            try
            {
                var user = await _client.Users.GetAuthenticatedUserAsync();
                var metrics = new Dictionary<string, int>
                {
                    ["followers"] = user.FollowersCount,
                    ["following"] = user.FriendsCount,
                    ["tweets"] = user.StatusesCount
                };
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Twitter metrics");
                return new Dictionary<string, int>();
            }
        }

        public async Task<bool> CheckRateLimitAsync()
        {
            try
            {
                var limits = await _client.RateLimits.GetRateLimitsAsync();
                return limits.TweetsLimit.Remaining > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Twitter rate limits");
                return false;
            }
        }

        private async Task<bool> HasRepliedAsync(long tweetId)
        {
            var userTweets = await _client.Tweets.GetUserTimelineAsync();
            return userTweets.Any(t => t.InReplyToStatusId == tweetId);
        }

        private string GenerateReply(string originalTweet)
        {
            // Add your reply generation logic here
            return "Thanks for reaching out! We'll get back to you soon. 🙂";
        }
    }
}
