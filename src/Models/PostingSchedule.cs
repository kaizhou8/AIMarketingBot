namespace SocialMediaBot.Models
{
    public class PostingSchedule
    {
        public int Frequency { get; set; }
        public List<string> BestTimes { get; set; } = new();
        public int MaxDailyPosts { get; set; }
    }
}
