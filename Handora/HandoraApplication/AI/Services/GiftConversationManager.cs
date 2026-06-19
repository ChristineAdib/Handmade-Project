using HandoraApplication.AI.DTOs;
using HandoraApplication.AI.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Services
{
    public class GiftConversationManager(IDistributedCache cache) : IGiftConversationManager
    {
        private readonly IDistributedCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        private static readonly DistributedCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
        };

        public async Task<GiftRequestState> GetStateAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return new GiftRequestState();

            var json = await _cache.GetStringAsync(GetStateKey(sessionId));
            if (string.IsNullOrEmpty(json))
                return new GiftRequestState();

            try
            {
                return JsonSerializer.Deserialize<GiftRequestState>(json) ?? new GiftRequestState();
            }
            catch
            {
                return new GiftRequestState();
            }
        }

        public async Task SaveStateAsync(string sessionId, GiftRequestState state)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || state == null)
                return;

            var json = JsonSerializer.Serialize(state);
            await _cache.SetStringAsync(GetStateKey(sessionId), json, CacheOptions);
        }

        public async Task ClearStateAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            await _cache.RemoveAsync(GetStateKey(sessionId));
            await _cache.RemoveAsync(GetHistoryKey(sessionId));
        }

        public async Task<List<ChatHistoryEntry>> GetHistoryAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return new List<ChatHistoryEntry>();

            var json = await _cache.GetStringAsync(GetHistoryKey(sessionId));
            if (string.IsNullOrEmpty(json))
                return new List<ChatHistoryEntry>();

            try
            {
                return JsonSerializer.Deserialize<List<ChatHistoryEntry>>(json) ?? new List<ChatHistoryEntry>();
            }
            catch
            {
                return new List<ChatHistoryEntry>();
            }
        }

        public async Task SaveHistoryAsync(string sessionId, List<ChatHistoryEntry> history)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || history == null)
                return;

            // Keep only the last 20 exchanges to avoid large cache entries
            if (history.Count > 40)
                history = history.GetRange(history.Count - 40, 40);

            var json = JsonSerializer.Serialize(history);
            await _cache.SetStringAsync(GetHistoryKey(sessionId), json, CacheOptions);
        }

        private static string GetStateKey(string sessionId) => $"GiftAssistant_Session_{sessionId}";
        private static string GetHistoryKey(string sessionId) => $"GiftAssistant_History_{sessionId}";
    }
}
