using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using POC_tus;
using POC_tus.Endpoint;
using System.Net;
using tusdotnet;
using tusdotnet.Helpers;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Concatenation;
using tusdotnet.Models.Configuration;
using tusdotnet.Models.Expiration;
using tusdotnet.Stores;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
//builder.Services.AddSingleton<TusDiskStorageOptionHelper>();
//builder.Services.AddSingleton(services => CreateTusConfigurationForCleanupService(services));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors(builder=>builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders(CorsHelper.GetExposedHeaders()));

app.UseTus(httpContext => new DefaultTusConfiguration
{
    Store = new TusDiskStore(@"D:\tusfiles\"),
    UrlPath = "/files",
    Events = new Events
    {
        OnFileCompleteAsync = async eventContext =>
        {
            // eventContext.FileId is the id of the file that was uploaded.
            // eventContext.Store is the data store that was used (in this case an instance of the TusDiskStore)

            // A normal use case here would be to read the file and do some processing on it.
            ITusFile file = await eventContext.GetFileAsync();
            var result = await DoSomeProcessing(file, eventContext.CancellationToken).ConfigureAwait(false);

            if (!result)
            {
                //throw new Exception("Something went wrong during processing");
            }
        }
    }
});

static Task<bool> DoSomeProcessing(ITusFile file, CancellationToken cancellationToken)
{
    return Task.FromResult(true);
}

app.UseRouting();

//// Handle downloads (must be set before MapTus)
//app.MapGet("/files/{fileId}", DownloadFileEndpoint.HandleRoute);

//// Setup tusdotnet for the /files/ path.
//app.MapTus("/files/", TusConfigurationFactory);


app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html"); ;

app.Run();

//static DefaultTusConfiguration CreateTusConfigurationForCleanupService(IServiceProvider services)
//{
//    var path = services.GetRequiredService<TusDiskStorageOptionHelper>().StorageDiskPath;
//    Console.WriteLine(path);
//    // Simplified configuration just for the ExpiredFilesCleanupService to show load order of configs.
//    return new DefaultTusConfiguration
//    {
//        Store = new TusDiskStore(path),
//        Expiration = new AbsoluteExpiration(TimeSpan.FromMinutes(5))
//    };
//}

//static Task<DefaultTusConfiguration> TusConfigurationFactory(HttpContext httpContext)
//{
//    var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

//    // Change the value of EnableOnAuthorize in appsettings.json to enable or disable
//    // the new authorization event.
//    var enableAuthorize = httpContext.RequestServices.GetRequiredService<IOptions<OnAuthorizeOption>>().Value.EnableOnAuthorize;

//    var diskStorePath = httpContext.RequestServices.GetRequiredService<TusDiskStorageOptionHelper>().StorageDiskPath;

//    var config = new DefaultTusConfiguration
//    {
//        Store = new TusDiskStore(diskStorePath),
//        MetadataParsingStrategy = MetadataParsingStrategy.AllowEmptyValues,
//        UsePipelinesIfAvailable = true,
//        Events = new Events
//        {
//            OnAuthorizeAsync = ctx =>
//            {
//                // Note: This event is called even if RequireAuthorization is called on the endpoint.
//                // In that case this event is not required but can be used as fine-grained authorization control.
//                // This event can also be used as a "on request started" event to prefetch data or similar.

//                if (!enableAuthorize)
//                    return Task.CompletedTask;

//                if (ctx.HttpContext.User.Identity?.IsAuthenticated != true)
//                {
//                    ctx.HttpContext.Response.Headers.Add("WWW-Authenticate", new StringValues("Basic realm=tusdotnet-test-net6.0"));
//                    ctx.FailRequest(HttpStatusCode.Unauthorized);
//                    return Task.CompletedTask;
//                }

//                if (ctx.HttpContext.User.Identity.Name != "test")
//                {
//                    ctx.FailRequest(HttpStatusCode.Forbidden, "'test' is the only allowed user");
//                    return Task.CompletedTask;
//                }

//                // Do other verification on the user; claims, roles, etc.

//                // Verify different things depending on the intent of the request.
//                // E.g.:
//                //   Does the file about to be written belong to this user?
//                //   Is the current user allowed to create new files or have they reached their quota?
//                //   etc etc
//                switch (ctx.Intent)
//                {
//                    case IntentType.CreateFile:
//                        break;
//                    case IntentType.ConcatenateFiles:
//                        break;
//                    case IntentType.WriteFile:
//                        break;
//                    case IntentType.DeleteFile:
//                        break;
//                    case IntentType.GetFileInfo:
//                        break;
//                    case IntentType.GetOptions:
//                        break;
//                    default:
//                        break;
//                }

//                return Task.CompletedTask;
//            },

//            OnBeforeCreateAsync = ctx =>
//            {
//                // Partial files are not complete so we do not need to validate
//                // the metadata in our example.
//                if (ctx.FileConcatenation is FileConcatPartial)
//                {
//                    return Task.CompletedTask;
//                }

//                //if (!ctx.Metadata.ContainsKey("name") || ctx.Metadata["name"].HasEmptyValue)
//                //{
//                //    ctx.FailRequest("name metadata must be specified. ");
//                //}

//                //if (!ctx.Metadata.ContainsKey("contentType") || ctx.Metadata["contentType"].HasEmptyValue)
//                //{
//                //    ctx.FailRequest("contentType metadata must be specified. ");
//                //}

//                return Task.CompletedTask;
//            },
//            OnCreateCompleteAsync = ctx =>
//            {
//                logger.LogInformation($"Created file {ctx.FileId} using {ctx.Store.GetType().FullName}");
//                return Task.CompletedTask;
//            },
//            OnBeforeDeleteAsync = ctx =>
//            {
//                // Can the file be deleted? If not call ctx.FailRequest(<message>);
//                return Task.CompletedTask;
//            },
//            OnDeleteCompleteAsync = ctx =>
//            {
//                logger.LogInformation($"Deleted file {ctx.FileId} using {ctx.Store.GetType().FullName}");
//                return Task.CompletedTask;
//            },
//            OnFileCompleteAsync = ctx =>
//            {
//                logger.LogInformation($"Upload of {ctx.FileId} completed using {ctx.Store.GetType().FullName}");
//                // If the store implements ITusReadableStore one could access the completed file here.
//                // The default TusDiskStore implements this interface:
//                //var file = await ctx.GetFileAsync();
//                return Task.CompletedTask;
//            }
//        },
//        // Set an expiration time where incomplete files can no longer be updated.
//        // This value can either be absolute or sliding.
//        // Absolute expiration will be saved per file on create
//        // Sliding expiration will be saved per file on create and updated on each patch/update.
//        Expiration = new AbsoluteExpiration(TimeSpan.FromMinutes(5))
//    };

//    return Task.FromResult(config);
//}
