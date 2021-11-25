namespace Shared.EventStore.SubscriptionWorker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using EventHandling;
    using global::EventStore.Client;

    public static class SubscriptionWorkerHelper
    {
        #region Methods

        public static void SafeInvokeEvent(EventHandler<TraceEventArgs> eventHandler,
                                           Object sender,
                                           String message)
        {
            if (eventHandler != null)
            {
                TraceEventArgs args = new()
                                      {
                                          Message = message
                                      };

                eventHandler.Invoke(sender, args);
            }
        }

        public static HttpClient CreateHttpClient(EventStoreClientSettings settings)
        {
            HttpClientHandler httpClientHandler = new();

            httpClientHandler.ServerCertificateCustomValidationCallback = (message,
                                                                           cert,
                                                                           chain,
                                                                           errors) => true;

            HttpClient client = new(httpClientHandler);

            client.BaseAddress = new Uri(settings.ConnectivitySettings.Address.AbsoluteUri);
            client.Timeout = TimeSpan.FromSeconds(5); //TODO: configurable? - not sure it should be tbh

            if (settings.DefaultCredentials != null)
            {
                client.DefaultRequestHeaders.Authorization =
                    new BasicAuthenticationHeaderValue(settings.DefaultCredentials.Username, settings.DefaultCredentials.Password);
            }

            return client;
        }

        public static SubscriptionWorker FilterSubscriptions(this SubscriptionWorker subscriptionWorker, String filterSubscriptions)
        {
            subscriptionWorker.FilterSubscriptions = filterSubscriptions;

            return subscriptionWorker;
        }

        public static SubscriptionWorker FilterByStreamName(this SubscriptionWorker subscriptionWorker, String streamName)
        {
            subscriptionWorker.StreamNameFilter = streamName;

            return subscriptionWorker;
        }

        public static List<PersistentSubscription> GetPersistentSubscription(this SubscriptionWorker subscriptionWorker)
        {
            return subscriptionWorker.CurrentSubscriptions;
        }
        
        public static List<PersistentSubscriptionInfo> GetNewSubscriptions(List<PersistentSubscriptionInfo> all,
                                                                           List<PersistentSubscriptionInfo> currentSubscriptions,
                                                                           Boolean isOrdered,
                                                                           String ignoreSubscriptionsWithGroupName = null,
                                                                           String filterSubscriptionsWithGroupName = null,
                                                                           String streamName = null)
        {
            var result = all
                         .Where(p => currentSubscriptions.All(p2 => $"{p2.StreamName}-{p2.GroupName}" != $"{p.StreamName}-{p.GroupName}"))
                         .Where(p => {
                                    if (isOrdered && p.GroupName.Contains("Ordered", StringComparison.CurrentCultureIgnoreCase)) return true;
                                    if (isOrdered == false && !p.GroupName.Contains("Ordered")) return true;

                                    return false;
                                })
                         .Where(p => SubscriptionWorkerHelper.IgnoreSubscription(p, ignoreSubscriptionsWithGroupName))
                         .Where(p => SubscriptionWorkerHelper.FilterSubscription(p, filterSubscriptionsWithGroupName))
                         .Where(p => SubscriptionWorkerHelper.ContainsStream(p, streamName))
                         .ToList();

            return result;
        }

        public static SubscriptionWorker IgnoreSubscriptions(this SubscriptionWorker subscriptionWorker, String ignoreSubscriptions)
        {
            subscriptionWorker.IgnoreSubscriptions = ignoreSubscriptions;

            return subscriptionWorker;
        }

        internal static Boolean FilterSubscription(PersistentSubscriptionInfo subscription, String filter)
        {
            List<string> filterList = new List<String>();
            if (!String.IsNullOrEmpty(filter))
            {
                if (filter.Contains(",") == false)
                {
                    filterList.Add(filter);
                }
                else
                {
                    filterList = filter.Split(",").ToList();
                }

                foreach (String filterItem in filterList)
                {
                    // Handle the fact that spaces could bne in the filter
                    var trimmedFilterItem = filterItem.Trim();
                    if (subscription.GroupName.Contains(trimmedFilterItem, StringComparison.CurrentCultureIgnoreCase))
                        return true;
                }

                return false;
            }

            return true;
        }

        internal static Boolean IgnoreSubscription(PersistentSubscriptionInfo subscription, String ignore)
        {
            if (!String.IsNullOrEmpty(ignore))
            {
                if (subscription.GroupName.Contains(ignore, StringComparison.CurrentCultureIgnoreCase))
                    return false;
            }

            return true;
        }

        internal static Boolean ContainsStream(PersistentSubscriptionInfo subscription, String stream)
        {
            if (String.IsNullOrEmpty(stream)) return true;

            return subscription.StreamName.Contains(stream, StringComparison.CurrentCultureIgnoreCase);
        }

        public static SubscriptionWorker UseInMemory(this SubscriptionWorker subscriptionWorker)
        {
            //This really should be an immutable object.
            subscriptionWorker.InMemory = true;

            return subscriptionWorker;
        }

        #endregion
    }
}