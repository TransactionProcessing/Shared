namespace Shared.EventStore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using EventHandling;
    using Moq;
    using Shouldly;
    using SubscriptionWorker;
    using Xunit;

    public class SubscriptionWorkerHelperTests
    {
        #region Fields

        private List<IDomainEventHandler> eventHandlers;

        private readonly List<IDomainEventHandler> projectionEventHandlers;

        #endregion

        #region Constructors

        public SubscriptionWorkerHelperTests()
        {
            Mock<IDomainEventHandler> eventhandler = new();

            this.eventHandlers = new List<IDomainEventHandler>
                                 {
                                     eventhandler.Object
                                 };

            this.projectionEventHandlers = new List<IDomainEventHandler>
                                           {
                                               eventhandler.Object
                                           };
        }

        #endregion

        #region Methods

        [Fact]
        public void SubscriptionWorker_DefaultValues_WorkerCreated()
        {
            Task<List<PersistentSubscriptionInfo>> getSubscriptions = new(TestData.GetPersistentSubscriptions_DemoEstate);
            String eventStoreConnectionString = "esdb://admin:changeit@192.168.1.133:2113?tls=false&tlsVerifyCert=false";
            ISubscriptionRepository subscriptionService = SubscriptionRepository.Create(getSubscriptions);

            SubscriptionWorker concurrentSubscriptions =
                SubscriptionWorker.CreateConcurrentSubscriptionWorker(eventStoreConnectionString, this.projectionEventHandlers, subscriptionService);

            concurrentSubscriptions.FilterSubscriptions.ShouldBeNull();
            concurrentSubscriptions.IgnoreSubscriptions.ShouldBe("local-");
            concurrentSubscriptions.StreamNameFilter.ShouldBeNull();
            concurrentSubscriptions.InflightMessages.ShouldBe(200);
            concurrentSubscriptions.IsOrdered.ShouldBeFalse();
        }

        [Fact]
        public void SubscriptionWorkerHelper_ConcurrentStreamsWithSpecificStreamNameSpecified_SubscriptionsReturned()
        {
            List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate();
            List<PersistentSubscriptionInfo> current = new List<PersistentSubscriptionInfo>();

            List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, false, null, null, "$et-EstateCreatedEvent");

            actual.Count.ShouldBe(1);
        }

        [Fact]
        public void SubscriptionWorkerHelper_ConcurrentSubscriptionsConcurrentFilteredOnly_SubscriptionsReturned()
        {
            List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate();
            List<PersistentSubscriptionInfo> current = new List<PersistentSubscriptionInfo>();

            List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, false, "local-", "Reporting");

            actual.Count.ShouldBe(1);
        }

        [Theory]
        [InlineData("Reporting,Migrations")]
        [InlineData(" Reporting,Migrations")]
        [InlineData("Reporting ,Migrations")]
        [InlineData("Reporting, Migrations")]
        [InlineData("Reporting,Migrations ")]
        public void SubscriptionWorkerHelper_ConcurrentSubscriptionsConcurrentFilteredOnly_MultipleFilters_SubscriptionsReturned(String filter)
        {
            List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate();
            List<PersistentSubscriptionInfo> current = new List<PersistentSubscriptionInfo>();

            List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, false, "local-", filter);

            actual.Count.ShouldBe(2);
        }

        [Fact]
        public void SubscriptionWorkerHelper_ConcurrentSubscriptionsIgnoringLocal_SubscriptionsReturned()
        {
            List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate();
            List<PersistentSubscriptionInfo> current = new List<PersistentSubscriptionInfo>();

            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "Test Stream #1",
                        GroupName = "local-1"
                    });

            List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, false, "local-");

            actual.Count.ShouldBe(4);
        }

        [Fact]
        public void SubscriptionWorkerHelper_ConcurrentSubscriptionsSelected_SubscriptionsReturned()
        {
            List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate();
            List<PersistentSubscriptionInfo> current = new List<PersistentSubscriptionInfo>();

            List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, false, "local-");

            actual.Count.ShouldBe(4);
        }

        [Theory]
        [InlineData(true, null, null, null, 3)]
        [InlineData(false, null, null, null, 5)]
        [InlineData(true, "OrderedX", null, null, 2)]
        [InlineData(false, "Migrations", null, null, 3)]
        [InlineData(true, "Migrations", null, null, 3)]
        [InlineData(false, "local-2", "local-", null, 1)]
        [InlineData(true, "OrderedX", "Ordered", null, 2)]
        [InlineData(false, null, "Migrations", null, 2)]
        [InlineData(false, null, null, "EstateCreatedEvent", 2)]
        [InlineData(false, null, null, "Estate", 5)]
        [InlineData(true, null, null, "Estate", 0)]
        [InlineData(true, null, null, "$projections_ExternalProjections_result_1", 1)]
        [InlineData(true, null, null, "$projections_ExternalProjections_result_", 3)]
        [InlineData(false, null, "Migrations", "$et-EstateCreated", 1)]
        public void SubscriptionWorkerHelper_GetNew_SubscriptionsFiltersAsExpected__SubscriptionsReturned(Boolean isOrdered,
                                                                                                          String ignoreSubscriptions,
                                                                                                          String filter,
                                                                                                          String streamName,
                                                                                                          Int32 expected)
        {
            List<PersistentSubscriptionInfo> all = new();
            List<PersistentSubscriptionInfo> current = new();

            //NOTE: The combinations are ridiculously over complicated, but at the moment I can't think of a better way of achieving this.

            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "$et-EstateCreatedEvent",
                        GroupName = "local-1"
                    });
            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "DemoEstate",
                        GroupName = "Reporting"
                    });
            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "$et-EstateCreatedEvent",
                        GroupName = "Migrations"
                    });
            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "$et-EstateNameUpdated",
                        GroupName = "Migrations"
                    });
            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "$et-EstateCreated",
                        GroupName = "local-2"
                    });

            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "$projections_ExternalProjections_result_1",
                        GroupName = "Ordered"
                    });
            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "$projections_ExternalProjections_result_2",
                        GroupName = "OrderedX"
                    });
            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "$projections_ExternalProjections_result_3",
                        GroupName = "Ordered"
                    });

            var actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, isOrdered, ignoreSubscriptions, filter, streamName);

            actual.Count.ShouldBe(expected);
        }

        [Fact]
        public void SubscriptionWorkerHelper_GetSubscriptionsContainingStreamName_SubscriptionsReturned()
        {
            List<PersistentSubscriptionInfo> all = new();
            var current = new List<PersistentSubscriptionInfo>();

            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "$et-EstateCreatedEvent",
                        GroupName = "local-1"
                    });

            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "$et-EstateNameUpdatedEvent",
                        GroupName = "local-1"
                    });

            var actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, false, null, null, "$et-Estate");

            actual.Count.ShouldBe(2);
        }

        [Fact]
        public void SubscriptionWorkerHelper_GetSubscriptionsMatchingStreamName_SubscriptionsReturned()
        {
            List<PersistentSubscriptionInfo> all = new();
            var current = new List<PersistentSubscriptionInfo>();

            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "$et-EstateCreatedEvent",
                        GroupName = "local-1"
                    });

            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "$et-SomeEvent",
                        GroupName = "local-1"
                    });

            var actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, false, null, null, "$et-EstateCreatedEvent");

            actual.Count.ShouldBe(1);
        }

        [Theory]
        [InlineData(true, null, "local-", 2)]
        [InlineData(true, "Group4", "local-", 1)]
        [InlineData(true, null, null, 5)]
        [InlineData(false, null, "local-", 3)]
        [InlineData(false, "Group4", "local-", 3)]
        [InlineData(false, null, null, 7)]
        public void SubscriptionWorkerHelper_LocalSubscriptionsLocalConcurrentSelected_WorkerCreated(Boolean isOrdered,
                                                                                                     String ignoreSubscriptions,
                                                                                                     String filter,
                                                                                                     Int32 expected)
        {
            var all = TestData.GetPersistentSubscriptions_DemoEstate();
            var current = new List<PersistentSubscriptionInfo>();

            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "Test Stream #1",
                        GroupName = "local-Group1"
                    });
            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "Test Stream #2",
                        GroupName = "local-Group2"
                    });
            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "Test Stream #3",
                        GroupName = "local-Group3"
                    });
            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "Test Stream #4",
                        GroupName = "local-Ordered-Group4"
                    });
            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "Test Stream #5",
                        GroupName = "Ordered-local-Group5"
                    });

            var actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, isOrdered, ignoreSubscriptions, filter);

            actual.Count.ShouldBe(expected);
        }

        [Fact]
        public void SubscriptionWorkerHelper_OrderedSubscriptionsIgnoringLocal_SubscriptionsReturned()
        {
            var all = TestData.GetPersistentSubscriptions_DemoEstate();
            var current = new List<PersistentSubscriptionInfo>();

            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "Test Stream #1",
                        GroupName = "local-1"
                    });

            var actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, true, "local-");

            actual.Count.ShouldBe(3);
        }

        [Fact]
        public void SubscriptionWorkerHelper_OrderedSubscriptionsIgnoringLocalOrdered_SubscriptionsReturned()
        {
            var all = TestData.GetPersistentSubscriptions_DemoEstate();
            var current = new List<PersistentSubscriptionInfo>();

            all.Add(new PersistentSubscriptionInfo
                    {
                        StreamName = "Test Stream #1",
                        GroupName = "local-Ordered"
                    });

            var actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, true, "local-");

            actual.Count.ShouldBe(3);
        }

        [Fact]
        public void SubscriptionWorkerHelper_OrderedSubscriptionsOrderedFilteredOnly_SubscriptionsReturned()
        {
            var all = TestData.GetPersistentSubscriptions_DemoEstate();
            var current = new List<PersistentSubscriptionInfo>();

            var actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, true, "local-", "local-");

            actual.Count.ShouldBe(0);
        }

        [Fact]
        public void SubscriptionWorkerHelper_OrderedSubscriptionsSelected_SubscriptionsReturned()
        {
            var all = TestData.GetPersistentSubscriptions_DemoEstate();
            var current = new List<PersistentSubscriptionInfo>();

            var actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, true, "local-");

            actual.Count.ShouldBe(3);
        }

        #endregion
    }
}