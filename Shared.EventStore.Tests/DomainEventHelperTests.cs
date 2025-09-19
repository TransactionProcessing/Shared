using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Tests
{
    using DomainDrivenDesign.EventSourcing;
    using EventStore.Tests;
    using Shared.EventStore.Tests.TestObjects;
    using Shouldly;
    using Xunit;

    public class DomainEventHelperTests
    {
        [Fact]
        public void DomainEventHelper_GetProperty_CorrectCase_PropertyNotFound()
        {

            AggregateNameSetEvent domainEvent = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            String propertyValue = DomainEventHelper.GetProperty<String>(domainEvent, "AggregateName1");
            propertyValue.ShouldBe(default);
        }

        [Theory]
        [InlineData("AggregateName1")]
        [InlineData("aggregatename1")]
        [InlineData("AGGREGATENAME1")]
        public void DomainEventHelper_GetProperty_IgnoreCase_PropertyNotFound(String propertyName)
        {

            AggregateNameSetEvent domainEvent = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            String propertyValue = DomainEventHelper.GetProperty<String>(domainEvent, propertyName, true);
            propertyValue.ShouldBe(default);
        }

        [Fact]
        public void DomainEventHelper_GetProperty_CorrectCase_PropertyFound(){

            AggregateNameSetEvent domainEvent = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            String propertyValue = DomainEventHelper.GetProperty<String>(domainEvent, "AggregateName");
            propertyValue.ShouldBe(TestData.EstateName);
        }

        [Theory]
        [InlineData("AggregateName")]
        [InlineData("aggregatename")]
        [InlineData("AGGREGATENAME")]
        public void DomainEventHelper_GetProperty_IgnoreCase_PropertyFound(String propertyName)
        {

            AggregateNameSetEvent domainEvent = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            String propertyValue = DomainEventHelper.GetProperty<String>(domainEvent, propertyName, true);
            propertyValue.ShouldBe(TestData.EstateName);
        }

        [Theory]
        [InlineData("AggregateName", true)]
        [InlineData("aggregatename", false)]
        [InlineData("AGGREGATENAME", false)]
        [InlineData("InvalidPropery", false)]
        public void DomainEventHelper_HasProperty_ExpectedResultReturned(String propertyName, Boolean expectedResult)
        {

            AggregateNameSetEvent domainEvent = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            var result = DomainEventHelper.HasProperty(domainEvent, propertyName);
            result.ShouldBe(expectedResult);
        }

    }
}
