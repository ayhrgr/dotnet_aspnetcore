// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.OutputCaching.Policies;

long requests = 0;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOutputCaching(options =>
{
    // options.RequestPolicies.Add(new VaryByQueryPolicy("culture").Map("/query"));
});

var app = builder.Build();

app.UseOutputCaching();

// Cached because default policy
app.MapGet("/", () => "Hello " + DateTime.UtcNow.ToString("o"));

// Cached because default policy
app.MapGet("/slownolock", async (context) =>
{
    var logger = context.RequestServices.GetService<ILogger<OutputCachingMiddleware>>();
    logger.LogWarning("Slowing ... {requests}", requests++);
    await Task.Delay(1000);
    await context.Response.WriteAsync("Slow " + DateTime.UtcNow.ToString("o"));
}).WithOutputCachingPolicy(new ExpirationPolicy(TimeSpan.FromSeconds(1)), new LockingPolicy(false));

// Cached because default policy
app.MapGet("/slow", async (context) =>
{
    var logger = context.RequestServices.GetService<ILogger<OutputCachingMiddleware>>();
    logger.LogWarning("Slowing ... {requests}", requests++);
    await Task.Delay(1000);
    await context.Response.WriteAsync("Slow " + DateTime.UtcNow.ToString("o"));
}).WithOutputCachingPolicy(new ExpirationPolicy(TimeSpan.FromSeconds(1)), new LockingPolicy(true));

// Cached because default policy
app.MapGet("/nocache", async context =>
{
    //context.Features.Get<IOutputCachingFeature>().ResponsePolicies.Add(new NoCachingPolicy());
    await context.Response.WriteAsync("Not cached " + DateTime.UtcNow.ToString("o"));
}).WithOutputCachingPolicy(
        new NoCachingPolicy()
    );

// Cached because Response Caching policy and contains "Cache-Control: public"
app.MapGet("/headers", async context =>
{
    //context.Features.Get<IOutputCachingFeature>().ResponsePolicies.Add(new ResponseCachingPolicy());
    context.Response.Headers.CacheControl = CacheControlHeaderValue.PublicString;
    await context.Response.WriteAsync("Headers " + DateTime.UtcNow.ToString("o"));
}).WithOutputCachingPolicy(
        new ResponseCachingPolicy()
    );

app.MapGet("/query", async context =>
{
    await context.Response.WriteAsync($"Culture: {context.Request.Query["culture"]} {DateTime.UtcNow.ToString("o")}");
}).OutputCacheVaryByQuery("culture");

await app.RunAsync();
