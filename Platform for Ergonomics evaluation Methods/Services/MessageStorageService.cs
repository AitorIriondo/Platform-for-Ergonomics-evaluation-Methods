using Microsoft.Extensions.Caching.Memory;

namespace Platform_for_Ergonomics_evaluation_Methods.Services
{
    public class MessageStorageService
    {
        private readonly IMemoryCache _memoryCache;
        private const string CacheKey = "LatestJsonMessage";

        public MessageStorageService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        // Store the latest JSON message
        public void StoreMessage(string jsonMessage)
        {
            _memoryCache.Set(CacheKey, jsonMessage);
        }

        // Retrieve the latest JSON message
        public string GetLatestMessage()
        {
            _memoryCache.TryGetValue(CacheKey, out string jsonMessage);
            return jsonMessage ?? "No message received yet.";
        }
    }
}
