using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using ZiggyCreatures.Caching.Fusion;

namespace Projectr.OleksiiKraievyi.CachedApi
{
    public static class SaveRequestPayloadCommand
    {
        private const string CollectionName = "data";
        private const string Key = "cache_key";

        public static async Task<CachedData> ExecuteAsync(
            IMongoDatabase mongoDatabase,
            IFusionCache cache,
            (string key, string value) command)
        {
            var (key, value) = command;
            var collection = mongoDatabase.GetCollection<BsonDocument>(CollectionName);

            var document = new BsonDocument { { "key", key }, { "value", value } };
            await collection.InsertOneAsync(document);

            var cachedValue = await cache.TryGetAsync<CachedData>(Key);

            return await cache.GetOrSetAsync<CachedData>(
                Key,
                async _ =>
                {
                    var values = await collection.AsQueryable().Take(5).ToListAsync();
                    var cachedDataValue = string.Join("<br />", values.Select(v => v.ToString()));

                    return new CachedData(cachedDataValue);
                },
                opt => opt.SetDuration(TimeSpan.FromSeconds(5)));
        }
    }

    public record CachedData(string data);
}

