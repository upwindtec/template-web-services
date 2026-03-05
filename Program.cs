//
// Template Web Services Application for Upwindtec Cloud.
// Can be freely adapted and distributed without resitrictions.
// For more information, visit https://www.upwindtec.pt
//
using expo_sample_web_services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using UpwindtecCloudStorageUtils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLogging(o => { });

builder.WebHost.UseKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Any, 7005);

    // HTTPS will fail in development environment, only HTTP calls will work. In production, the certificate will be provided by the hosting environment and HTTPS will work.
    try
    {
        serverOptions.Listen(IPAddress.Any, 7006,
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
builder.Services.AddDbContext<exposampleContext>(opt =>
        opt.UseNpgsql(connectionString)
        );

// Set the JSON serializer options to keep capitalization as in the Database
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

var app = builder.Build();
app.UseHttpLogging();

app.MapGet("/{collection}/{Id?}", GetRecords);
app.MapPost("/{collection}", AddRecord);
app.MapPatch("/{collection}/{Id}", UpdateRecord);
app.MapPut("/{collection}/{Id}", UpdateRecord);
app.MapDelete("/{collection}/{Id}", DeleteRecord);

app.Run();

/// <summary>
/// Returns a list of records;
/// If the record Id is provided, returns only that record.
/// </summary>
async Task<IResult> GetRecords(HttpRequest request,
                            [FromRoute] string collection, 
                            [FromRoute] string? Id,
                            exposampleContext db)
{
    try
    {
        if (!exposampleContext.entityTypes.TryGetValue(collection, out Type? entityType))
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
                                exposampleContext db)
{
    try
    {
        if (!exposampleContext.entityTypes.TryGetValue(collection, out Type? entityType))
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
                                exposampleContext db)
{
    try
    {
        if (!exposampleContext.entityTypes.TryGetValue(collection, out Type? entityType))
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
                                exposampleContext db)
{
    try
    {
        if (!exposampleContext.entityTypes.TryGetValue(collection, out Type? entityType))
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

