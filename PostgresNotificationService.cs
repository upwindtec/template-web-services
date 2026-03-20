//
// Template Web Services Application for Upwindtec Cloud.
// Can be freely adapted and distributed without resitrictions.
// For more information, visit https://www.upwindtec.pt
//
using Npgsql;

namespace expo_sample_web_services;

/// <summary>
/// Response sent when monitoring Item completion state.
/// </summary>
public class SSEResponse
{
	public required string path { get; set; }
	public object? data { get; set; }
}

    /// <summary>
    /// This Service waits for Notifications received on a given Postgres Channel name.
    /// </summary>
    public class PostgresNotificationService : BackgroundService
    {
        private readonly PostgresNotificationHandler _postgresNotificationHandler;
        private readonly NpgsqlDataSource _npgsqlDataSource;

        public PostgresNotificationService(NpgsqlDataSource npgsqlDataSource, PostgresNotificationHandler postgresNotificationHandler)
        {
            _npgsqlDataSource = npgsqlDataSource;
            _postgresNotificationHandler = postgresNotificationHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // We are running both loops until either of them is stopped or runs dry ...
            await Task
                .WhenAny(SetupPostgresAsync(stoppingToken))
                .ConfigureAwait(false);

            // Initializes the Postgres Listener by issuing a LISTEN Command.
            async Task SetupPostgresAsync(CancellationToken cancellationToken)
            {
                // Open a new Connection, which can be used to issue the LISTEN Command 
                // to the Postgres Database.
                using var connection = await _npgsqlDataSource
                    .OpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false);

            // If we receive a message from Postgres, we notify all listeners.
            // In critical application where we don't want to skip any events,
            // we would need to store the notification in a Channel (System.Threading.Channels)
            // and process these asynchronously
            connection.Notification += async (sender, x) =>
                {
                    await _postgresNotificationHandler.HandleNotificationAsync(x.Payload, cancellationToken).ConfigureAwait(false);
                };

            // Register to the datachange notifications
            using (var command = new NpgsqlCommand($"LISTEN datachange", connection))
                {
                    await command
                        .ExecuteNonQueryAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

                // Put the connection into the Wait State until the Cancellation is requested
                while (!cancellationToken.IsCancellationRequested)
                {
                    await connection
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
    }
