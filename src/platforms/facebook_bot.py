import facebook
from loguru import logger
from typing import Dict, List, Optional
from .base_bot import BaseBot

class FacebookBot(BaseBot):
    def __init__(self, access_token: str):
        """Initialize Facebook bot with access token."""
        self.graph = facebook.GraphAPI(access_token)
        
    def create_post(self, content: str, image_url: Optional[str] = None) -> bool:
        """Create a Facebook post with optional image."""
        try:
            if image_url:
                self.graph.put_photo(
                    image=requests.get(image_url).content,
                    message=content
                )
            else:
                self.graph.put_object(
                    parent_object='me',
                    connection_name='feed',
                    message=content
                )
            logger.info("Successfully created Facebook post")
            return True
        except Exception as e:
            logger.error(f"Error creating Facebook post: {str(e)}")
            return False
            
    def auto_reply(self):
        """Auto-reply to comments on posts."""
        try:
            posts = self.graph.get_connections('me', 'posts')
            for post in posts['data']:
                comments = self.graph.get_connections(
                    post['id'],
                    'comments'
                )
                for comment in comments['data']:
                    if not self._has_replied(comment['id']):
                        reply = self._generate_reply(comment['message'])
                        self.graph.put_comment(
                            object_id=comment['id'],
                            message=reply
                        )
        except Exception as e:
            logger.error(f"Error in auto reply: {str(e)}")
            
    def like_related_content(self):
        """Like relevant posts in your niche."""
        try:
            # Search for relevant posts (requires additional permissions)
            search_results = self.graph.search(
                type='page',
                q=self._get_search_query()
            )
            for page in search_results['data']:
                posts = self.graph.get_connections(
                    page['id'],
                    'posts'
                )
                for post in posts['data']:
                    self.graph.put_like(post['id'])
        except Exception as e:
            logger.error(f"Error liking content: {str(e)}")
            
    def get_metrics(self) -> Dict:
        """Get engagement metrics for recent posts."""
        try:
            posts = self.graph.get_connections('me', 'posts')
            metrics = {
                'likes': 0,
                'comments': 0,
                'shares': 0
            }
            for post in posts['data']:
                post_data = self.graph.get_object(
                    id=post['id'],
                    fields='likes.summary(true),comments.summary(true),shares'
                )
                metrics['likes'] += post_data['likes']['summary']['total_count']
                metrics['comments'] += post_data['comments']['summary']['total_count']
                if 'shares' in post_data:
                    metrics['shares'] += post_data['shares']['count']
            return metrics
        except Exception as e:
            logger.error(f"Error getting metrics: {str(e)}")
            return {}
            
    def _has_replied(self, comment_id: str) -> bool:
        """Check if we've already replied to a comment."""
        try:
            replies = self.graph.get_connections(comment_id, 'comments')
            return len(replies['data']) > 0
        except Exception:
            return False
            
    def _generate_reply(self, comment_text: str) -> str:
        """Generate appropriate reply based on comment content."""
        # Add your reply generation logic here
        return "Thank you for your comment! We appreciate your feedback."
        
    def _get_search_query(self) -> str:
        """Generate search query for finding relevant content."""
        # Add your search query logic here
        return "technology innovation"
