namespace Shared.EventStore.SubscriptionWorker
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using global::EventStore.Client;
    using Newtonsoft.Json;

    public class SubscriptionRepository : ISubscriptionRepository
    {
        #region Fields

        private Int32 CacheHits;

        private Int32 FullRefreshHits;

        private Func<CancellationToken, Task<List<PersistentSubscriptionInfo>>> GetAllSubscriptions;

        private readonly Func<Boolean, PersistentSubscriptions, Boolean> RefreshRequired;

        private Int32 running;

        private PersistentSubscriptions Subscriptions;

        #endregion

        #region Constructors

        private SubscriptionRepository()
        {
            this.Subscriptions = new PersistentSubscriptions();

            this.RefreshRequired = (force, s) => force || s.InitialState || SubscriptionRepository.RefreshNeeded(s.LastTimeRefreshed);
        }

        #endregion

        #region Events

        public EventHandler<String> Trace;

        #endregion

        #region Methods

        public static SubscriptionRepository Create(String eventStoreConnectionString)
        {
            EventStoreClientSettings settings = EventStoreClientSettings.Create(eventStoreConnectionString);
            HttpClient httpClient = SubscriptionWorkerHelper.CreateHttpClient(settings);

            return new SubscriptionRepository
                   {
                       GetAllSubscriptions = cancellationToken => SubscriptionRepository.GetSubscriptions(httpClient, cancellationToken)
                   };
        }

        public static SubscriptionRepository Create(Task<List<PersistentSubscriptionInfo>> func)
        {
            return new()
                   {
                       GetAllSubscriptions = _ => func
                   };
        }

        public static SubscriptionRepository Create(Func<CancellationToken, Task<List<PersistentSubscriptionInfo>>> func)
        {
            return new()
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
                if (this.RefreshRequired(forceRefresh, this.Subscriptions) == false)
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

        private static Boolean RefreshNeeded(DateTime lastRefreshed)
        {
            TimeSpan elapsed = DateTime.Now - lastRefreshed;

            //TODO: Add to configuration
            //OR this.PersistentSubscriptionPollingInSeconds x 2 ?
            //60 seconds is how often we are willing to refresh our cache.
            //I suspect this could be much higher (trade of with update the UI and wanting response, versus how often we hot the hit ES essentially getting
            //the same information each time.
            if (elapsed.TotalSeconds < 120)
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}