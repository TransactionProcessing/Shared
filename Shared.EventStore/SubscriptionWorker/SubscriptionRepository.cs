\n﻿namespace Shared.EventStore.SubscriptionWorker;\n\nusing System;\nusing System.Collections.Generic;\nusing System.Diagnostics.CodeAnalysis;\nusing System.Net.Http;\nusing System.Threading;\nusing System.Threading.Tasks;\nusing global::EventStore.Client;\nusing Newtonsoft.Json;\n\n[ExcludeFromCodeCoverage]\npublic class SubscriptionRepository : ISubscriptionRepository\n{\n    #region Fields\n\n    private readonly Int32 CacheHits;\n\n    private readonly Int32 FullRefreshHits;\n\n    private Func<CancellationToken, Task<List<PersistentSubscriptionInfo>>> GetAllSubscriptions;\n\n    private readonly Func<Boolean, PersistentSubscriptions, Boolean> RefreshRequired;\n\n    private Int32 running;\n\n    private PersistentSubscriptions Subscriptions;\n\n    #endregion\n\n    #region Constructors\n\n    private SubscriptionRepository(Int32 cacheDuration = 120)\n    {\n        this.Subscriptions = new PersistentSubscriptions();\n\n        this.RefreshRequired = (force, s) => force || s.InitialState || SubscriptionRepository.RefreshNeeded(s.LastTimeRefreshed, cacheDuration);\n    }\n\n    #endregion\n\n    #region Events\n\n    public EventHandler<String> Trace;\n\n    #endregion\n\n    #region Methods\n\n    public static SubscriptionRepository Create(String eventStoreConnectionString,Int32 cacheDuration = 120)\n    {\n        EventStoreClientSettings settings = EventStoreClientSettings.Create(eventStoreConnectionString);\n        HttpClient httpClient = SubscriptionWorkerHelper.CreateHttpClient(settings);\n\n        return new SubscriptionRepository(cacheDuration)\n        {\n            GetAllSubscriptions = cancellationToken => SubscriptionRepository.GetSubscriptions(httpClient, cancellationToken)\n        };\n    }\n\n    public static SubscriptionRepository Create(Task<List<PersistentSubscriptionInfo>> func,Int32 cacheDuration = 120)\n    {\n        return new(cacheDuration)\n        {\n            GetAllSubscriptions = _ => func\n        };\n    }\n\n    public static SubscriptionRepository Create(Func<CancellationToken, Task<List<PersistentSubscriptionInfo>>> func,Int32 cacheDuration = 120)
    {
        return new(cacheDuration)
        {
            GetAllSubscriptions = func
        };
    }

    public static async Task<List<PersistentSubscriptionInfo>> GetSubscriptions(HttpClient httpClient, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage responseMessage = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "subscriptions"), cancellationToken);
            String responseBody = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

            if (responseMessage.IsSuccessStatusCode)
            {
                List<PersistentSubscriptionInfo> list = JsonConvert.DeserializeObject<List<PersistentSubscriptionInfo>>(responseBody);

                return list;
            }

            throw new Exception($"Response was [{responseBody}] and status code was [{responseMessage.StatusCode}]");
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to get persistent subscription list. [{ex}]");
        }
    }

    public async Task<PersistentSubscriptions> GetSubscriptions(Boolean forceRefresh, CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref this.running, 1, 0) != 0)
        {
            return this.GetSubscriptionsFromCache("no lock");
        }

        try
        {
            if (!this.RefreshRequired(forceRefresh, this.Subscriptions))
            {
                return this.GetSubscriptionsFromCache("refresh not required");
            }

            this.WriteTrace("Full refresh on repository");

            List<PersistentSubscriptionInfo> list = await this.GetAllSubscriptions(cancellationToken);

            this.FullRefreshHits++;

            this.Subscriptions = this.Subscriptions.Update(list);

            this.WriteTrace($"Full refresh on repository completed {this.FullRefreshHits}");

            return this.Subscriptions;
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to get persistent subscription list. [{ex}]");
        }
        finally
        {
            Interlocked.Exchange(ref this.running, 0);
        }
    }

    public async Task PreWarm(CancellationToken cancellationToken) => await this.GetSubscriptions(true, cancellationToken);

    public void WriteTrace(String message)
    {
        if (this.Trace != null)
        {
            this.Trace(this, message);
        }
    }

    private PersistentSubscriptions GetSubscriptionsFromCache(String reason)
    {
        this.CacheHits++;
        this.WriteTrace($"Cache hit {this.CacheHits} - {reason}");
        return this.Subscriptions;
    }

    private static Boolean RefreshNeeded(DateTime lastRefreshed, Int32 cacheDuration)
    {
        TimeSpan elapsed = DateTime.Now - lastRefreshed;

        if (elapsed.TotalSeconds < cacheDuration)
        {
            return false;
        }

        return true;
    }

    #endregion
}