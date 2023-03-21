using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using POC_tus;
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

builder.Services.AddControllersWithViews();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html"); ;

app.Run();

