using System.Text.Json;
using StackExchange.Redis;

namespace CachingWithRedis_WebAPI.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _cacheDb;
        private readonly bool _isInit;

        public RedisCacheService(IConfiguration configuration)
        {
            var connectionString = configuration.GetSection("RedisConnectionString").Value;
            if (connectionString is not null)
            {
                var redis = ConnectionMultiplexer.Connect(connectionString);
                _cacheDb = redis.GetDatabase();
                _isInit = true;
            }
        }

        public T GetData<T>(string key)
        {
            if (_isInit && IsCorrectString(key))
            {
                var value = _cacheDb.StringGet(key);
                if (IsCorrectString(value))
                {
                    return JsonSerializer.Deserialize<T>(value);
                }
            }
            return default;
        }
        public bool SetData<T>(string key, T value, DateTimeOffset expirationTime)
        {
            if (_isInit && IsCorrectString(key))
            {
                var expiryTime = expirationTime.DateTime.Subtract(DateTime.Now);
                var serializableValue = JsonSerializer.Serialize(value);
                return _cacheDb.StringSet(key, serializableValue, expiryTime);
            }

            return false;
        }
        public bool RemoveData(string key)
        {
            if (_isInit && IsCorrectString(key)) _cacheDb.KeyDelete(key);
            return false;
        }
        private bool IsCorrectString(in string key) => !string.IsNullOrEmpty(key);
    }
}