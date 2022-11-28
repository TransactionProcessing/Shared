namespace Shared.EventStore.Tests
{
    using System.Collections.Generic;
    using SubscriptionWorker;

    public class TestData
    {
        #region Methods

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