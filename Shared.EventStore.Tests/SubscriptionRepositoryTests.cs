using Shared.EventStore.Tests.TestObjects;
using SimpleResults;

namespace Shared.EventStore.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using SubscriptionWorker;
using Xunit;

public class SubscriptionRepositoryTests
{
    #region Methods

    [Fact]
    public async Task SubscriptionRepository_GetSubscriptions_ReturnsSubscriptions()
    {
        List<PersistentSubscriptionInfo> allSubscriptions = (TestData.GetPersistentSubscriptions_DemoEstate());

        Func<CancellationToken, Task<Result<List<PersistentSubscriptionInfo>>>> GetAllSubscriptions = async token => allSubscriptions;

        ISubscriptionRepository subscriptionRepository = SubscriptionRepository.Create(GetAllSubscriptions);

        PersistentSubscriptions list = await subscriptionRepository.GetSubscriptions(true, CancellationToken.None);

        list.PersistentSubscriptionInfo.Count.ShouldBe(allSubscriptions.Count);
    }

    [Fact]
    public async Task SubscriptionRepository_GetSubscriptions_FiltersInvalidSubscriptionsBeforeCaching()
    {
        List<PersistentSubscriptionInfo> allSubscriptions = new()
        {
            new()
            {
                StreamName = "Stream-A",
                GroupName = "Group-A"
            },
            new()
            {
                StreamName = null,
                GroupName = "Group-B"
            },
            new()
            {
                StreamName = "Stream-C",
                GroupName = "   "
            }
        };

        Func<CancellationToken, Task<Result<List<PersistentSubscriptionInfo>>>> getAllSubscriptions =
            _ => Task.FromResult(Result.Success(allSubscriptions));

        ISubscriptionRepository subscriptionRepository = SubscriptionRepository.Create(getAllSubscriptions);

        PersistentSubscriptions first = await subscriptionRepository.GetSubscriptions(true, CancellationToken.None);
        PersistentSubscriptions second = await subscriptionRepository.GetSubscriptions(false, CancellationToken.None);

        first.PersistentSubscriptionInfo.Count.ShouldBe(1);
        first.PersistentSubscriptionInfo[0].StreamName.ShouldBe("Stream-A");
        first.PersistentSubscriptionInfo[0].GroupName.ShouldBe("Group-A");
        second.PersistentSubscriptionInfo.Count.ShouldBe(1);
        second.PersistentSubscriptionInfo[0].StreamName.ShouldBe("Stream-A");
        second.PersistentSubscriptionInfo[0].GroupName.ShouldBe("Group-A");
    }

    #endregion
}
