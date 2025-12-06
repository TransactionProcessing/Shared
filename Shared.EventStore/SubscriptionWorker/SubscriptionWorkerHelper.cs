using KurrentDB.Client;

namespace Shared.EventStore.SubscriptionWorker;

using EventHandling;
using global::EventStore.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

[ExcludeFromCodeCoverage]
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

    public static HttpClient CreateHttpClient(KurrentDBClientSettings settings)
    {
        HttpClientHandler httpClientHandler = new();

        httpClientHandler.ServerCertificateCustomValidationCallback = (message,
                                                                       cert,
                                                                       chain,
                                                                       errors) => true;

        HttpClient client = new(httpClientHandler);

        client.BaseAddress = new Uri(settings.ConnectivitySettings.Address.AbsoluteUri);
        client.Timeout = TimeSpan.FromSeconds(5);

        if (settings.DefaultCredentials != null)
        {
            String authenticationString = $"{settings.DefaultCredentials.Username}:{settings.DefaultCredentials.Password}";
            String base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
        }

        return client;
    }
        
    public static List<PersistentSubscriptionInfo> GetNewSubscriptions(List<PersistentSubscriptionInfo> all,
                                                                       List<PersistentSubscriptionInfo> currentSubscriptions,
                                                                       String groupsToInclude = null,
                                                                       String groupsToIgnore = null,
                                                                       String streamsToInclude = null,
                                                                       String streamsToIgnore = null)
    {
        List<PersistentSubscriptionInfo> result = all
            .Where(p => currentSubscriptions.All(p2 => $"{p2.StreamName}-{p2.GroupName}" != $"{p.StreamName}-{p.GroupName}"))
            .Where(p => SubscriptionWorkerHelper.IgnoreSubscriptionGroup(p, groupsToIgnore))
            .Where(p => SubscriptionWorkerHelper.IncludeSubscriptionGroup(p, groupsToInclude))
            .Where(p => SubscriptionWorkerHelper.IgnoreSubscriptionStream(p, streamsToIgnore))
            .Where(p => SubscriptionWorkerHelper.IncludeSubscriptionStream(p, streamsToInclude))
            .ToList();
            
        return result;
    }

    public static SubscriptionWorker SetIgnoreGroups(this SubscriptionWorker subscriptionWorker, String ignoreGroups)
    {
        subscriptionWorker.IgnoreGroups = ignoreGroups;

        return subscriptionWorker;
    }

    public static SubscriptionWorker SetIncludeGroups(this SubscriptionWorker subscriptionWorker, String includeGroups)
    {
        subscriptionWorker.IncludeGroups = includeGroups;

        return subscriptionWorker;
    }

    public static SubscriptionWorker SetIgnoreStreams(this SubscriptionWorker subscriptionWorker, String ignoreStreams)
    {
        subscriptionWorker.IgnoreStreams = ignoreStreams;

        return subscriptionWorker;
    }

    public static SubscriptionWorker SetIncludeStreams(this SubscriptionWorker subscriptionWorker, String includeStreams)
    {
        subscriptionWorker.IncludeStreams = includeStreams;

        return subscriptionWorker;
    }

    private static Boolean IncludeSubscriptionGroup(PersistentSubscriptionInfo subscription, String include)
    {
        if (!String.IsNullOrEmpty(include))
        {
            List<String> checkList = include.Split(',').Select(s => s.Trim()).ToList();

            foreach (String chk in checkList) {
                if (subscription.GroupName.Contains(chk, StringComparison.CurrentCultureIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }
        return true;
    }

    private static Boolean IgnoreSubscriptionGroup(PersistentSubscriptionInfo subscription,
                                                   String ignore) {
        if (!String.IsNullOrEmpty(ignore)) {

            List<String> checkList = ignore.Split(',').Select(s => s.Trim()).ToList();

            foreach (String chk in checkList) {
                if (subscription.GroupName.Contains(chk, StringComparison.CurrentCultureIgnoreCase))
                    return false;
            }
        }

        return true;
    }

    private static Boolean IncludeSubscriptionStream(PersistentSubscriptionInfo subscription, String include)
    {
        if (!String.IsNullOrEmpty(include))
        {
            List<String> checkList = include.Split(',').Select(s => s.Trim()).ToList();

            foreach (String chk in checkList)
            {
                if (subscription.StreamName.Contains(chk, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        return true;
    }

    private static Boolean IgnoreSubscriptionStream(PersistentSubscriptionInfo subscription,
                                                    String ignore) {
        if (!String.IsNullOrEmpty(ignore)) {
            List<String> checkList = ignore.Split(',').Select(s => s.Trim()).ToList();

            foreach (String chk in checkList) {
                if (subscription.StreamName.Contains(chk, StringComparison.CurrentCultureIgnoreCase))
                    return false;
            }
        }

        return true;
    }

    public static SubscriptionWorker UseInMemory(this SubscriptionWorker subscriptionWorker)
    {
        //This really should be an immutable object.
        subscriptionWorker.InMemory = true;

        return subscriptionWorker;
    }

    public static void ConfigureTracing(this SubscriptionWorker worker, string type, Action<TraceEventType, string, string> traceHandler)
    {
        worker.Trace += (_, args) => traceHandler(TraceEventType.Information, type, args.Message);
        worker.Warning += (_, args) => traceHandler(TraceEventType.Warning, type, args.Message);
        worker.Error += (_, args) => traceHandler(TraceEventType.Error, type, args.Message);
    }

    #endregion
}