namespace Shared.EventStore.SubscriptionWorker;
 using System;
 using System.Collections.Generic;
 using System.Diagnostics.CodeAnalysis;
 using System.Net.Http;
 using System.Threading;
 using System.Threading.Tasks;
 using global::EventStore.Client;
 using Newtonsoft.Json;

 [ExcludeFromCodeCoverage]
 public class SubscriptionRepository : ISubscriptionRepository {
     private Int32 CacheHits;
     private Int32 FullRefreshHits;
     private Func<CancellationToken, Task<List<PersistentSubscriptionInfo>>> GetAllSubscriptions;
     private readonly Func<Boolean, PersistentSubscriptions, Boolean> RefreshRequired;
     private Int32 running;
     private PersistentSubscriptions Subscriptions;

     private SubscriptionRepository(Int32 cacheDuration = 120) {
         this.Subscriptions = new PersistentSubscriptions();
         this.RefreshRequired = (force,
                                 s) => force || s.InitialState || SubscriptionRepository.RefreshNeeded(s.LastTimeRefreshed, cacheDuration);
     }

     public EventHandler<String> Trace;

     public static SubscriptionRepository Create(String eventStoreConnectionString,
                                                 Int32 cacheDuration = 120) {
         EventStoreClientSettings settings = EventStoreClientSettings.Create(eventStoreConnectionString);
         HttpClient httpClient = SubscriptionWorkerHelper.CreateHttpClient(settings);
         return new SubscriptionRepository(cacheDuration) { GetAllSubscriptions = cancellationToken => SubscriptionRepository.GetSubscriptions(httpClient, cancellationToken) };
     }

     public static SubscriptionRepository Create(Task<List<PersistentSubscriptionInfo>> func,
                                                 Int32 cacheDuration = 120) {

         return new(cacheDuration) { GetAllSubscriptions = _ => func };

     }

     public static SubscriptionRepository Create(Func<CancellationToken, Task<List<PersistentSubscriptionInfo>>> func,
                                                 Int32 cacheDuration = 120) {
         return new(cacheDuration) { GetAllSubscriptions = func };
     }

     public static async Task<List<PersistentSubscriptionInfo>> GetSubscriptions(HttpClient httpClient,
                                                                                 CancellationToken cancellationToken) {
         try {
             HttpResponseMessage responseMessage = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "subscriptions"), cancellationToken);
             String responseBody = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

             if (responseMessage.IsSuccessStatusCode) {
                 List<PersistentSubscriptionInfo> list = JsonConvert.DeserializeObject<List<PersistentSubscriptionInfo>>(responseBody);

                 return list;
             }

             throw new ApplicationException($"Response was [{responseBody}] and status code was [{responseMessage.StatusCode}]");
         }
         catch (Exception ex) {
             throw new ApplicationException($"Unable to get persistent subscription list. [{ex}]");
         }
     }

     public async Task<PersistentSubscriptions> GetSubscriptions(Boolean forceRefresh,
                                                                 CancellationToken cancellationToken) {
         if (Interlocked.CompareExchange(ref this.running, 1, 0) != 0) {
             return this.GetSubscriptionsFromCache("no lock");
         }

         try {
             if (!this.RefreshRequired(forceRefresh, this.Subscriptions)) {
                 return this.GetSubscriptionsFromCache("refresh not required");
             }

             this.WriteTrace("Full refresh on repository");

             List<PersistentSubscriptionInfo> list = await this.GetAllSubscriptions(cancellationToken);

             this.FullRefreshHits++;

             this.Subscriptions = this.Subscriptions.Update(list);

             this.WriteTrace($"Full refresh on repository completed {this.FullRefreshHits}");

             return this.Subscriptions;
         }
         catch (Exception ex) {
             throw new ApplicationException($"Unable to get persistent subscription list. [{ex}]");
         }
         finally {
             Interlocked.Exchange(ref this.running, 0);
         }
     }

     public async Task PreWarm(CancellationToken cancellationToken) => await this.GetSubscriptions(true, cancellationToken);

     public void WriteTrace(String message) {
         if (this.Trace != null) {
             this.Trace(this, message);
         }
     }

     private PersistentSubscriptions GetSubscriptionsFromCache(String reason) {
         this.CacheHits++;
         this.WriteTrace($"Cache hit {this.CacheHits} - {reason}");
         return this.Subscriptions;
     }

     private static Boolean RefreshNeeded(DateTime lastRefreshed,
                                          Int32 cacheDuration) {
         TimeSpan elapsed = DateTime.Now - lastRefreshed;

         if (elapsed.TotalSeconds < cacheDuration) {
             return false;
         }

         return true;
     }
 }