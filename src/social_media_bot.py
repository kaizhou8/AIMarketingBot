import os
import yaml
import schedule
import time
from datetime import datetime
from loguru import logger
from typing import Dict, List, Optional
from .platforms import TwitterBot, FacebookBot, TikTokBot
from .utils import ContentGenerator, Analytics

class SocialMediaBot:
    def __init__(self, config_path: str):
        """Initialize the social media bot with configuration."""
        self.config = self._load_config(config_path)
        self.setup_platforms()
        self.content_generator = ContentGenerator()
        self.analytics = Analytics()
        
    def _load_config(self, config_path: str) -> Dict:
        """Load configuration from YAML file."""
        with open(config_path, 'r') as f:
            return yaml.safe_load(f)
            
    def setup_platforms(self):
        """Initialize social media platform bots."""
        self.platforms = {
            'twitter': TwitterBot(os.getenv('TWITTER_API_KEY'),
                                os.getenv('TWITTER_API_SECRET')),
            'facebook': FacebookBot(os.getenv('FACEBOOK_ACCESS_TOKEN')),
            'tiktok': TikTokBot(os.getenv('TIKTOK_ACCESS_TOKEN'))
        }
        
    def schedule_posts(self):
        """Schedule posts according to configuration."""
        for platform, settings in self.config['posting_schedule'].items():
            for time_slot in settings['best_times']:
                schedule.every().day.at(time_slot).do(
                    self.create_post, platform=platform
                )
                
    def create_post(self, platform: str):
        """Create and publish a post on specified platform."""
        try:
            content = self.content_generator.generate(
                platform,
                self.config['content_variation']['templates']
            )
            
            if platform == 'twitter':
                self.platforms['twitter'].post_tweet(content)
            elif platform == 'facebook':
                self.platforms['facebook'].create_post(content)
            elif platform == 'tiktok':
                self.platforms['tiktok'].upload_video(content)
                
            self.analytics.track_post(platform, content)
            logger.info(f"Successfully posted to {platform}")
            
        except Exception as e:
            logger.error(f"Error posting to {platform}: {str(e)}")
            
    def monitor_engagement(self):
        """Monitor post engagement and performance."""
        for platform in self.platforms:
            metrics = self.platforms[platform].get_metrics()
            self.analytics.update_metrics(platform, metrics)
            
    def auto_engage(self):
        """Automatically engage with relevant content."""
        for platform, settings in self.config['engagement'].items():
            if settings['auto_reply']:
                self.platforms[platform].auto_reply()
            if settings['like_related']:
                self.platforms[platform].like_related_content()
                
    def run(self):
        """Run the social media bot."""
        self.schedule_posts()
        
        # Schedule engagement monitoring
        schedule.every(
            self.config['monitoring']['check_interval']
        ).seconds.do(self.monitor_engagement)
        
        # Run continuously
        while True:
            schedule.run_pending()
            time.sleep(1)
            
if __name__ == "__main__":
    bot = SocialMediaBot("config.yaml")
    bot.run()
