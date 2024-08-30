using System;
namespace RTLolaMo3Vis.Models
{
    public interface INotificationManager
    {
        event EventHandler NotificationReceived;
        public void Initialize();
        public void SendNotification(string title, string message, DateTime? notifyTime = null);
        public void ReceiveNotification(string title, string message);
    }

    public class NotificationEventArgs : EventArgs
    {
        public string Title { get; set; }
        public string Message { get; set; }
    }
}
