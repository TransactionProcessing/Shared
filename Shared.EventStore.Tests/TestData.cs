namespace Shared.EventStore.Tests
{
    using System.Collections.Generic;
    using SubscriptionWorker;

    public class TestData
    {
        #region Methods

        public static List<PersistentSubscriptionInfo> GetPersistentSubscriptions_DemoEstate()
        {
            var list = new List<PersistentSubscriptionInfo>();

            list.Add(new PersistentSubscriptionInfo
                     {
                         StreamName = "DemoEstate",
                         GroupName = "Reporting"
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
                         StreamName = "$projections_ExternalProjections_result_TestMerchant1",
                         GroupName = "Ordered"
                     });

            list.Add(new PersistentSubscriptionInfo
                     {
                         StreamName = "$projections_ExternalProjections_result_TestMerchant2",
                         GroupName = "Ordered"
                     });

            list.Add(new PersistentSubscriptionInfo
                     {
                         StreamName = "$projections_ExternalProjections_result_TestMerchant3",
                         GroupName = "Ordered"
                     });

            return list;
        }

        #endregion
    }
}