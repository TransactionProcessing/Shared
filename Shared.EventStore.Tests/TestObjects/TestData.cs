namespace Shared.EventStore.Tests.TestObjects
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using global::EventStore.Client;
    using Newtonsoft.Json;
    using Shared.General;
    using System.Text;
    using DomainDrivenDesign.EventSourcing;
    using NLog.LayoutRenderers.Wrappers;
    using SubscriptionWorker;
    using PersistentSubscriptionInfo = SubscriptionWorker.PersistentSubscriptionInfo;

    public class TestData
    {
        #region Methods

        public static Guid AggregateId = Guid.Parse("103B335B-540A-4985-BB80-FD9B2BABF866");
        public static Guid EventId = Guid.Parse("C9416757-582C-4F67-A320-80FE1E937045");

        public static string EstateName = "Test Estate 1";

        public static EventRecord CreateEventRecord<T>(T domainEvent, string streamId, bool addToMap = true) where T : DomainEvent
        {
            byte[] eventData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(domainEvent));
            byte[] customEventMetaData = Encoding.UTF8.GetBytes(string.Empty);

            Dictionary<string, string> metaData = new Dictionary<string, string>();
            metaData.Add("type", domainEvent.GetType().FullName);
            metaData.Add("created", "1000000");
            metaData.Add("content-type", "application-json");

            if (addToMap)
                TypeMap.AddType(typeof(T), domainEvent.GetType().FullName);

            EventRecord r = new EventRecord(streamId,
                                            Uuid.FromGuid(domainEvent.EventId),
                                            0,
                                            new Position(0, 0),
                                            metaData,
                                            eventData,
                                            customEventMetaData);
            return r;
        }

        public static List<PersistentSubscriptionInfo> GetPersistentSubscriptions_DemoEstate()
        {
            List<PersistentSubscriptionInfo> list = new List<PersistentSubscriptionInfo>();

            list.Add(new PersistentSubscriptionInfo
            {
                StreamName = "DemoEstate",
                GroupName = "Estate Management"
            });
            list.Add(new PersistentSubscriptionInfo
            {
                StreamName = "$et-EstateCreatedEvent",
                GroupName = "Migrations"
            });
            list.Add(new PersistentSubscriptionInfo
            {
                StreamName = "FileProcessorSubscriptionStream_DemoEstate",
                GroupName = "File Processor"
            });
            list.Add(new PersistentSubscriptionInfo
            {
                StreamName = "TransactionProcessorSubscriptionStream_DemoEstate",
                GroupName = "Transaction Processor"
            });

            list.Add(new PersistentSubscriptionInfo
            {
                StreamName = "$ce-MerchantBalanceArchive",
                GroupName = "Ordered"
            });

            return list;
        }

        public static List<PersistentSubscriptionInfo> GetPersistentSubscriptions_DemoEstate_Updated()
        {
            List<PersistentSubscriptionInfo> list = new List<PersistentSubscriptionInfo>();

            list.Add(new PersistentSubscriptionInfo
            {
                StreamName = "DemoEstate",
                GroupName = "Estate Management"
            });
            list.Add(new PersistentSubscriptionInfo
            {
                StreamName = "$et-EstateCreatedEvent",
                GroupName = "Migrations"
            });
            list.Add(new PersistentSubscriptionInfo
            {
                StreamName = "FileProcessorSubscriptionStream_DemoEstate",
                GroupName = "File Processor"
            });
            list.Add(new PersistentSubscriptionInfo
            {
                StreamName = "TransactionProcessorSubscriptionStream_DemoEstate",
                GroupName = "Transaction Processor"
            });

            list.Add(new PersistentSubscriptionInfo
            {
                StreamName = "$ce-MerchantBalanceArchive",
                GroupName = "Ordered"
            });

            list.Add(new PersistentSubscriptionInfo
            {
                StreamName = "DemoEstate2",
                GroupName = "Estate Management"
            });

            return list;
        }

        #endregion
    }
}