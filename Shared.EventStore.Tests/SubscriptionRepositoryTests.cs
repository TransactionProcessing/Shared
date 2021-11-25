namespace Shared.EventStore.Tests
{
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

            Func<CancellationToken, Task<List<PersistentSubscriptionInfo>>> GetAllSubscriptions = async token => allSubscriptions;

            ISubscriptionRepository subscriptionRepository = SubscriptionRepository.Create(GetAllSubscriptions);

            PersistentSubscriptions list = await subscriptionRepository.GetSubscriptions(true, CancellationToken.None);

            list.PersistentSubscriptionInfo.Count.ShouldBe(allSubscriptions.Count);
        }

        #endregion
    }
}