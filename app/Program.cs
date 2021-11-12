using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using ZiggyCreatures.Caching.Fusion;
using Projectr.OleksiiKraievyi.CachedApi;
using Microsoft.AspNetCore.Http;

WebHost.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
        services
            .AddLogging()
            .AddFusionCache(o =>
            {
                o.DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromMinutes(1),
                    Priority = CacheItemPriority.Normal
                };
            })
            .AddSingleton(sp => new MongoClient(Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")))
            .AddSingleton<IMongoDatabase>(sp => sp.GetRequiredService<MongoClient>().GetDatabase("TestDb")))
    .Configure(app => app
            .UseRouting()
            .UseEndpoints(e => 
            {
                var cache = e.ServiceProvider.GetRequiredService<IFusionCache>();
                var mongoDatabase = e.ServiceProvider.GetRequiredService<IMongoDatabase>();

                e.MapGet("/", async ctx => 
                {
                    using var requestReader = new StreamReader(ctx.Request.Body);
                    var requestData = await requestReader.ReadToEndAsync();

                    var data = await SaveRequestPayloadCommand.ExecuteAsync(
                        mongoDatabase,
                        cache,
                        (ctx.TraceIdentifier, requestData));

                    var responseBody = $"<html><body>Data saved. Data get from store:<br />{data}</body></html>";
                    await ctx.Response.WriteAsync(responseBody);
                });
            }))
    .Build()
    .Run();