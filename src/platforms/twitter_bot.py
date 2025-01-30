import tweepy
from loguru import logger
from typing import Dict, List, Optional
from .base_bot import BaseBot

class TwitterBot(BaseBot):
    def __init__(self, api_key: str, api_secret: str):
        """Initialize Twitter bot with API credentials."""
        self.auth = tweepy.OAuthHandler(api_key, api_secret)
        self.api = tweepy.API(self.auth)
        self.client = tweepy.Client(
            consumer_key=api_key,
            consumer_secret=api_secret
        )
        
    def post_tweet(self, content: str) -> bool:
        """Post a tweet with given content."""
        try:
            tweet = self.client.create_tweet(text=content)
            logger.info(f"Successfully posted tweet: {tweet.data['id']}")
            return True
        except Exception as e:
            logger.error(f"Error posting tweet: {str(e)}")
            return False
            
    def auto_reply(self):
        """Auto-reply to mentions and relevant tweets."""
        try:
            mentions = self.api.mentions_timeline()
            for mention in mentions:
                if not self._has_replied(mention.id):
                    reply = self._generate_reply(mention.text)
                    self.api.update_status(
                        status=reply,
                        in_reply_to_status_id=mention.id
                    )
        except Exception as e:
            logger.error(f"Error in auto reply: {str(e)}")
            
    def like_related_content(self):
        """Like tweets related to your niche."""
        try:
            search_results = self.api.search_tweets(
                q=self._get_search_query(),
                lang="en",
                count=10
            )
            for tweet in search_results:
                if not tweet.favorited:
                    tweet.favorite()
        except Exception as e:
            logger.error(f"Error liking content: {str(e)}")
            
    def get_metrics(self) -> Dict:
        """Get engagement metrics for recent tweets."""
        try:
            tweets = self.api.user_timeline(count=10)
            metrics = {
                'likes': sum(tweet.favorite_count for tweet in tweets),
                'retweets': sum(tweet.retweet_count for tweet in tweets)
            }
            return metrics
        except Exception as e:
            logger.error(f"Error getting metrics: {str(e)}")
            return {}
            
    def _has_replied(self, tweet_id: int) -> bool:
        """Check if we've already replied to a tweet."""
        try:
            replies = self.api.user_timeline(count=200)
            return any(
                reply.in_reply_to_status_id == tweet_id
                for reply in replies
            )
        except Exception:
            return False
            
    def _generate_reply(self, tweet_text: str) -> str:
        """Generate appropriate reply based on tweet content."""
        # Add your reply generation logic here
        return "Thanks for reaching out! We'll get back to you soon."
        
    def _get_search_query(self) -> str:
        """Generate search query for finding relevant content."""
        # Add your search query logic here
        return "#tech OR #innovation -filter:retweets"
