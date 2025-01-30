using Microsoft.Extensions.Logging;
using Quartz;
using SocialMediaBot.Platforms;

namespace SocialMediaBot.Services
{
    public class SocialMediaService
    {
        private readonly IMediaPlatform[] _platforms;
        private readonly ILogger<SocialMediaService> _logger;
        private readonly IScheduler _scheduler;

        public SocialMediaService(
            TwitterPlatform twitterPlatform,
            FacebookPlatform facebookPlatform,
            TikTokPlatform tiktokPlatform,
            IScheduler scheduler,
            ILogger<SocialMediaService> logger)
        {
            _platforms = new IMediaPlatform[] 
            { 
                twitterPlatform, 
                facebookPlatform, 
                tiktokPlatform 
            };
            _scheduler = scheduler;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            try
            {
                // Schedule content posting
                var postingJob = JobBuilder.Create<ContentPostingJob>()
                    .WithIdentity("postingJob", "social")
                    .Build();

                var postingTrigger = TriggerBuilder.Create()
                    .WithIdentity("postingTrigger", "social")
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(9, 0))
                    .Build();

                await _scheduler.ScheduleJob(postingJob, postingTrigger);

                // Schedule engagement monitoring
                var monitoringJob = JobBuilder.Create<EngagementMonitoringJob>()
                    .WithIdentity("monitoringJob", "social")
                    .Build();

                var monitoringTrigger = TriggerBuilder.Create()
                    .WithIdentity("monitoringTrigger", "social")
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(30)
                        .RepeatForever())
                    .Build();

                await _scheduler.ScheduleJob(monitoringJob, monitoringTrigger);

                await _scheduler.Start();
                _logger.LogInformation("Social media service started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting social media service");
                throw;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                await _scheduler.Shutdown();
                _logger.LogInformation("Social media service stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping social media service");
                throw;
            }
        }

        public class ContentPostingJob : IJob
        {
            private readonly IMediaPlatform[] _platforms;
            private readonly ILogger<ContentPostingJob> _logger;

            public ContentPostingJob(IMediaPlatform[] platforms, ILogger<ContentPostingJob> logger)
            {
                _platforms = platforms;
                _logger = logger;
            }

            public async Task Execute(IJobExecutionContext context)
            {
                foreach (var platform in _platforms)
                {
                    try
                    {
                        if (await platform.CheckRateLimitAsync())
                        {
                            var content = GenerateContent();
                            await platform.PostContentAsync(content);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error executing posting job for {platform.GetType().Name}");
                    }
                }
            }

            private string GenerateContent()
            {
                // Add your content generation logic here
                return $"Check out our latest updates! #trending {DateTime.Now:yyyy-MM-dd}";
            }
        }

        public class EngagementMonitoringJob : IJob
        {
            private readonly IMediaPlatform[] _platforms;
            private readonly ILogger<EngagementMonitoringJob> _logger;

            public EngagementMonitoringJob(IMediaPlatform[] platforms, ILogger<EngagementMonitoringJob> logger)
            {
                _platforms = platforms;
                _logger = logger;
            }

            public async Task Execute(IJobExecutionContext context)
            {
                foreach (var platform in _platforms)
                {
                    try
                    {
                        if (await platform.CheckRateLimitAsync())
                        {
                            await platform.AutoReplyAsync();
                            await platform.LikeRelatedContentAsync();
                            var metrics = await platform.GetMetricsAsync();
                            LogMetrics(platform.GetType().Name, metrics);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error executing monitoring job for {platform.GetType().Name}");
                    }
                }
            }

            private void LogMetrics(string platform, Dictionary<string, int> metrics)
            {
                foreach (var (metric, value) in metrics)
                {
                    _logger.LogInformation($"{platform} - {metric}: {value}");
                }
            }
        }
    }
}
