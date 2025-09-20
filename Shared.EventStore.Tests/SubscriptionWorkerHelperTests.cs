using Shared.EventStore.Tests.TestObjects;

namespace Shared.EventStore.Tests;

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

    private Mock<IDomainEventHandlerResolver> domainEventHandlerResolver;

    #endregion

    #region Constructors

    public SubscriptionWorkerHelperTests()
    {
        domainEventHandlerResolver = new();
    }

    #endregion

    #region Methods

    [Fact]
    public void SubscriptionWorkerHelper_GetNewSubscriptions_SubscriptionsReturned()
    {
        List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate();
        List<PersistentSubscriptionInfo> current = new List<PersistentSubscriptionInfo>();

        List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current);

        actual.Count.ShouldBe(5);
    }

    [Fact]
    public void SubscriptionWorkerHelper_GetNewSubscriptions_NewSubscriptionsOnSecondGet_SubscriptionsReturned()
    {
        List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate_Updated();
        List<PersistentSubscriptionInfo> current = TestData.GetPersistentSubscriptions_DemoEstate();

        List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current);

        actual.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("Transaction Processor", 1)]
    [InlineData("Transaction Processor, File Processor", 2)]
    public void SubscriptionWorkerHelper_GetNewSubscriptions_GroupsToInclude_SubscriptionsReturned(String groupsToIncludeFilter, Int32 expectedCount)
    {
        List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate();
        List<PersistentSubscriptionInfo> current = new List<PersistentSubscriptionInfo>();

        List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current, 
            groupsToInclude:groupsToIncludeFilter);

        actual.Count.ShouldBe(expectedCount);
    }

    [Theory]
    [InlineData("Transaction Processor", 4)]
    [InlineData("Transaction Processor, File Processor", 3)]
    public void SubscriptionWorkerHelper_GetNewSubscriptions_GroupsToIgnore_SubscriptionsReturned(String groupsToIgnoreFilter, Int32 expectedCount)
    {
        List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate();
        List<PersistentSubscriptionInfo> current = new List<PersistentSubscriptionInfo>();

        List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current,
            groupsToIgnore: groupsToIgnoreFilter);

        actual.Count.ShouldBe(expectedCount);
    }

    [Fact]
    public void SubscriptionWorkerHelper_GetNewSubscriptions_BothGroupsToIncludeAndIgnore_SubscriptionsReturned()
    {
        List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate();
        List<PersistentSubscriptionInfo> current = new List<PersistentSubscriptionInfo>();

        String groupsToIncludeFilter = "File Processor";
        String groupsToIgnoreFilter = "Transaction Processor";

        List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current,
            groupsToInclude: groupsToIncludeFilter,
            groupsToIgnore: groupsToIgnoreFilter);

        actual.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("DemoEstate", 3)]
    [InlineData("DemoEstate,$et-EstateCreatedEvent", 4)]
    public void SubscriptionWorkerHelper_GetNewSubscriptions_StreamsToInclude_SubscriptionsReturned(String streamsToIncludeFilter, Int32 expectedCount)
    {
        List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate();
        List<PersistentSubscriptionInfo> current = new List<PersistentSubscriptionInfo>();

        List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current,
            streamsToInclude:streamsToIncludeFilter);

        actual.Count.ShouldBe(expectedCount);
    }

    [Theory]
    [InlineData("DemoEstate", 2)]
    public void SubscriptionWorkerHelper_GetNewSubscriptions_StreamsToIgnore_SubscriptionsReturned(String streamsToIgnoreFilter, Int32 expectedCount)
    {
        List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate();
        List<PersistentSubscriptionInfo> current = new List<PersistentSubscriptionInfo>();
            
        List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current,
            streamsToIgnore: streamsToIgnoreFilter);

        actual.Count.ShouldBe(expectedCount);
    }

    [Fact]
    public void SubscriptionWorkerHelper_GetNewSubscriptions_StreamsToIncludeAndIgnore_SubscriptionsReturned()
    {
        List<PersistentSubscriptionInfo> all = TestData.GetPersistentSubscriptions_DemoEstate();
        List<PersistentSubscriptionInfo> current = new List<PersistentSubscriptionInfo>();

        String streamsToIncludeFilter = "Merchant";
        String streamsToIgnoreFilter = "Estate";

        List<PersistentSubscriptionInfo> actual = SubscriptionWorkerHelper.GetNewSubscriptions(all, current,
            streamsToInclude: streamsToIncludeFilter,
            streamsToIgnore:streamsToIgnoreFilter);

        actual.Count.ShouldBe(1);
    }

    #endregion
}