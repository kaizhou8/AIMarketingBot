using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;
using SocialMediaBot.Platforms;
using SocialMediaBot.Services;

namespace SocialMediaBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var service = host.Services.GetRequiredService<SocialMediaService>();
            await service.StartAsync();
            
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Add configuration
                    services.AddSingleton<IConfiguration>(sp =>
                    {
                        return new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json")
                            .Build();
                    });

                    // Add logging
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .WriteTo.File("logs/social-media-bot.log", rollingInterval: RollingInterval.Day)
                        .CreateLogger();

                    services.AddLogging(builder =>
                    {
                        builder.AddSerilog(dispose: true);
                    });

                    // Add Quartz
                    services.AddQuartz(q =>
                    {
                        q.UseMicrosoftDependencyInjectionJobFactory();
                    });
                    services.AddQuartzHostedService(opt =>
                    {
                        opt.WaitForJobsToComplete = true;
                    });

                    // Add platforms
                    var config = hostContext.Configuration.GetSection("SocialMediaBot");
                    
                    services.AddSingleton<TwitterPlatform>(sp =>
                    {
                        var twitterConfig = config.GetSection("Twitter");
                        return new TwitterPlatform(
                            twitterConfig["ApiKey"],
                            twitterConfig["ApiKeySecret"],
                            twitterConfig["AccessToken"],
                            twitterConfig["AccessTokenSecret"],
                            twitterConfig.Get<PostingSchedule>(),
                            sp.GetRequiredService<ILogger<TwitterPlatform>>()
                        );
                    });

                    services.AddSingleton<FacebookPlatform>(sp =>
                    {
                        var facebookConfig = config.GetSection("Facebook");
                        return new FacebookPlatform(
                            facebookConfig["AccessToken"],
                            facebookConfig["PageId"],
                            facebookConfig.Get<PostingSchedule>(),
                            sp.GetRequiredService<ILogger<FacebookPlatform>>()
                        );
                    });

                    services.AddSingleton<TikTokPlatform>(sp =>
                    {
                        var tiktokConfig = config.GetSection("TikTok");
                        return new TikTokPlatform(
                            tiktokConfig["AccessToken"],
                            tiktokConfig.Get<PostingSchedule>(),
                            sp.GetRequiredService<ILogger<TikTokPlatform>>()
                        );
                    });

                    // Add main service
                    services.AddSingleton<SocialMediaService>();
                });
    }
}
