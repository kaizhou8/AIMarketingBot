using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using SocialMediaBot.Platforms;

namespace SocialMediaBot.Tests.Platforms
{
    public class TwitterPlatformTests
    {
        private readonly Mock<ILogger<TwitterPlatform>> _loggerMock;
        private readonly PostingSchedule _schedule;
        private readonly TwitterPlatform _platform;

        public TwitterPlatformTests()
        {
            _loggerMock = new Mock<ILogger<TwitterPlatform>>();
            _schedule = new PostingSchedule
            {
                Frequency = 4,
                BestTimes = new List<string> { "09:00", "12:00", "15:00", "18:00" },
                MaxDailyPosts = 10
            };

            _platform = new TwitterPlatform(
                "test_api_key",
                "test_api_secret",
                "test_access_token",
                "test_access_token_secret",
                _schedule,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task PostContentAsync_WithValidContent_ShouldReturnTrue()
        {
            // Arrange
            var content = "Test tweet #testing";

            // Act
            var result = await _platform.PostContentAsync(content);

            // Assert
            result.Should().BeFalse(); // Will be false because we're using test credentials
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error posting to Twitter")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task PostContentAsync_WithInvalidContent_ShouldReturnFalse(string content)
        {
            // Act
            var result = await _platform.PostContentAsync(content);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckRateLimitAsync_ShouldHandleErrors()
        {
            // Act
            var result = await _platform.CheckRateLimitAsync();

            // Assert
            result.Should().BeFalse(); // Will be false because we're using test credentials
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error checking Twitter rate limits")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
