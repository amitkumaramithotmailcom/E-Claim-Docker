using EClaim.Domain.Entities;
using EClaim.Domain.Enums;
using EClaim.Domain.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace E_Claim_Service
{
    public class CacheUtility: ICacheUtility
    {
        private readonly IAppSettingsService _appSettingsService;
        private readonly IDatabase _cache;
        private readonly IConfiguration _config;
        public CacheUtility(IAppSettingsService appSettingsService, IConnectionMultiplexer redis, IConfiguration config )
        {
            _appSettingsService = appSettingsService;
            _cache = redis.GetDatabase();
            _config = config;
        }

        public async Task<string> GetDataFromCachAsync(string key)
        {
            string jsonData=string.Empty;

            var setting = await _appSettingsService.GetAppSettings(Services.Redis.ToString());
            if (setting != null && setting.IsEnabled)
            {
                jsonData = await _cache.StringGetAsync($"user:{key}");
            }

            return jsonData;
        }

        public async Task<string> SetDataInCachAsync(string key, object obj)
        {
            string jsonData = string.Empty;

            var setting = await _appSettingsService.GetAppSettings(Services.Redis.ToString());
            if (setting != null && setting.IsEnabled)
            {
                int cachTime = 2;
                var RedisCachTimeInMin = _config["RedisCachTimeInMin"];
                bool isNumber = int.TryParse(RedisCachTimeInMin, out int number);
                if (isNumber)
                {
                    cachTime = number;
                }

                if (obj != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                        WriteIndented = true
                    };

                    if (obj is IQueryable queryable)
                    {
                        obj = queryable.Cast<object>().ToList();
                    }

                    jsonData = JsonSerializer.Serialize(obj, options);
                    await _cache.StringSetAsync($"user:{key}", jsonData, TimeSpan.FromMinutes(cachTime));
                }
            }

            return jsonData;
        }
    }
}
