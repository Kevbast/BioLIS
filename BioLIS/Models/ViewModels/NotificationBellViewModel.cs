namespace BioLIS.Models.ViewModels
{
    public class NotificationBellViewModel
    {
        public int UnreadCount { get; set; }
        public List<Notification> LatestUnread { get; set; } = new();
    }
}
