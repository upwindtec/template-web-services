//
// Template Web Services Application for Upwindtec Cloud.
// Can be freely adapted and distributed without resitrictions.
// For more information, visit https://www.upwindtec.pt
//
using System.Diagnostics;

namespace expo_sample_web_services
{
    /// <summary>
    /// Handles a Notification received from a Postgres NOTIFY / LISTEN.
    /// </summary>
    public class PostgresNotificationListener
    {
        public EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
        public string eventPayload = "";

        public ValueTask HandleNotificationAsync(string notification, CancellationToken cancellationToken)
        {
            eventPayload = notification;
            ewh.Set();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Logs all Notifications received from a Postgres Channel.
    /// </summary>
    public class PostgresNotificationHandler
    {
        private List<PostgresNotificationListener> listeners = new List<PostgresNotificationListener>();

        public void AddListener(PostgresNotificationListener listener) { listeners.Add(listener); }

        public void RemoveListener(PostgresNotificationListener listener) { listeners.Remove(listener); }

        public ValueTask HandleNotificationAsync(string notification, CancellationToken cancellationToken)
        {
            Trace.WriteLine("PostgresNotification, Payload = " + notification);

            // send the notification to all listeners
            foreach (var listener in listeners)
            {
                listener.HandleNotificationAsync(notification, cancellationToken);
            }

            return ValueTask.CompletedTask;
        }

    }
}
