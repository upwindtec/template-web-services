//
// Template Web Services Application for Upwindtec Cloud.
// Can be freely adapted and distributed without resitrictions.
// For more information, visit https://www.upwindtec.pt
//
using expo_sample_web_services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using UpwindtecCloudStorageUtils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLogging(o => { });

builder.WebHost.UseKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Any, 8080);

    // HTTPS will fail in development environment, only HTTP calls will work. In production, the certificate will be provided by the hosting environment and HTTPS will work.
    try
    {
        serverOptions.Listen(IPAddress.Any, 8089,
            listenOptions =>
            {
                listenOptions.UseHttps(new X509Certificate2(@"cert/santaluzia1.pfx"));
            }
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to configure HTTPS: {ex.Message}");
    }
});

// in production, the connection string is provided as an environment variable, in development it is provided by the MT connection string
// do not log the connection string for security reasons as it will contain the password to the database
string? connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
if (connectionString == null)
{
    connectionString = builder.Configuration.GetConnectionString("expo-sample");
}

// use PostgreSQL as database provider
builder.Services.AddDbContext<ExpoSampleContext>(opt =>
        opt.UseNpgsql(connectionString)
        );

// Set the JSON serializer options to keep capitalization as in the Database
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

// Create another DataSource to receive notifications from the Database
builder.Services.AddSingleton<NpgsqlDataSource>((sp) =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    return dataSourceBuilder.Build();
});

// Register the Database Notification Handler
builder.Services.AddSingleton<PostgresNotificationHandler>(ExpoSampleContext.notificationHandler);

// Add the Background Service processing the Notifications
builder.Services.AddHostedService<PostgresNotificationService>();

var app = builder.Build();
app.UseHttpLogging();

app.MapGet("/{collection}/{Id?}", GetRecords);
app.MapPost("/{collection}", AddRecord);
app.MapPatch("/{collection}/{Id}", UpdateRecord);
app.MapPut("/{collection}/{Id}", UpdateRecord);
app.MapDelete("/{collection}/{Id}", DeleteRecord);

// Custom endpoint to monitor the completion states of items using Postgres NOTIFY / LISTEN
app.MapGet("/done", GetItemCompletionState);


app.Run();

/// <summary>
/// Returns a list of records;
/// If the record Id is provided, returns only that record.
/// </summary>
async Task<IResult> GetRecords(HttpRequest request,
                            [FromRoute] string collection, 
                            [FromRoute] string? Id,
                            ExpoSampleContext db)
{
    try
    {
        if (!ExpoSampleContext.entityTypes.TryGetValue(collection, out Type? entityType))
        {
            return TypedResults.NotFound();
        }

        // dynamically invoke the generic method based on the entity type
        var method = typeof(EFCoreUtils).GetMethod(nameof(EFCoreUtils.GetOneOrMultipleRecords),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var genericMethod = method!.MakeGenericMethod(entityType);
        var task = (Task<IResult>)genericMethod.Invoke(null, [request, Id, db, null, null])!;
        return await task;
    }
    catch (Exception ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
}

/// <summary>
/// Routes the post request to the appropriate entity-specific post helper based on the collection name.
/// The data for updating the record can be either received as a parameter or retrieved from the request body
/// </summary>
async Task<IResult> AddRecord(HttpRequest request,
                                [FromRoute] string collection,
                                Dictionary<string, System.Text.Json.JsonElement>? data,
                                ExpoSampleContext db)
{
    try
    {
        if (!ExpoSampleContext.entityTypes.TryGetValue(collection, out Type? entityType))
        {
            return TypedResults.NotFound();
        }
        // dynamically invoke the generic method based on the entity type
        var method = typeof(EFCoreUtils).GetMethod(nameof(EFCoreUtils.AddRecord),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var genericMethod = method!.MakeGenericMethod(entityType);
        var task = (Task<IResult>)genericMethod.Invoke(null, [request, data, db])!;
        return await task;
    }
    catch (Exception ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
}

/// <summary>
/// Routes the put or patch request to the appropriate entity-specific helper based on the collection name.
/// The data for updating the record can be either received as a parameter or retrieved from the request body
/// </summary>
async Task<IResult> UpdateRecord(HttpRequest request,
                                [FromRoute] string collection,
                                [FromRoute] string Id,
                                Dictionary<string, System.Text.Json.JsonElement>? data,
                                ExpoSampleContext db)
{
    try
    {
        if (!ExpoSampleContext.entityTypes.TryGetValue(collection, out Type? entityType))
        {
            return TypedResults.NotFound();
        }
        // dynamically invoke the generic method based on the entity type
        var method = typeof(EFCoreUtils).GetMethod(nameof(EFCoreUtils.UpdateRecord),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var genericMethod = method!.MakeGenericMethod(entityType);
        var task = (Task<IResult>)genericMethod.Invoke(null, [request, Id, data, db])!;
        return await task;
    }
    catch (Exception ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
}

/// <summary>
/// Routes the delete request to the appropriate entity-specific delete helper based on the collection name.
/// </summary>
async Task<IResult> DeleteRecord(HttpRequest request,
                                [FromRoute] string collection,
                                [FromRoute] string Id,
                                bool? DeleteRelatedItems,
                                ExpoSampleContext db)
{
    try
    {
        if (!ExpoSampleContext.entityTypes.TryGetValue(collection, out Type? entityType))
        {
            return TypedResults.NotFound();
        }
        // dynamically invoke the generic method based on the entity type
        var method = typeof(EFCoreUtils).GetMethod(nameof(EFCoreUtils.DeleteRecord),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var genericMethod = method!.MakeGenericMethod(entityType);
        var task = (Task<IResult>)genericMethod.Invoke(null, [request, Id, DeleteRelatedItems, db])!;
        return await task;
    }
    catch (Exception ex)
    {
        return TypedResults.BadRequest(ex.Message);
    }
}

/// <summary>
/// Retrieves the completion state of an Item using SSE.
/// </summary>
/// <param name="Accept">The Accept header value.</param>
/// <param name="request">The HTTP request.</param>
/// <param name="response">The HTTP response.</param>
/// <returns>Streams or returns the Item state.</returns>
static async Task<IResult> GetItemCompletionState([FromHeader] string? Accept,
                    HttpContext context,
                    HttpResponse response
                    )
{
    if (Accept is not null && Accept == "text/event-stream")
    {
        //
        // case of SSE streaming
        //
        PostgresNotificationListener listener = new PostgresNotificationListener();
        ExpoSampleContext.notificationHandler.AddListener(listener);

        response.ContentType = "text/event-stream";

        // This call has the effect of sending the response without chunking.
        // This is to be compatible with the Firebase client libraries
        response.Headers.TransferEncoding = "";

        // loop until the request is cancelled by the client closing the connection or by a timeout.
        while (context.RequestAborted.IsCancellationRequested == false)
        {
            SSEResponse resp = new SSEResponse { path = "/", data = (listener.eventPayload) };
            // return the initial state
            var json = JsonSerializer.Serialize(resp);
            await response.WriteAsync($"event: put\ndata: {json}\n\n");
            await response.Body.FlushAsync();

            // monitor state changes
            listener.ewh.WaitOne();
        }
        ExpoSampleContext.notificationHandler.RemoveListener(listener);
        return TypedResults.Ok();
    }
    else
    {
        return TypedResults.InternalServerError();
    }
}
