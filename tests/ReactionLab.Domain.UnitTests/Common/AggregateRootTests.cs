using ReactionLab.Domain.Common;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Common;

public sealed class AggregateRootTests
{
    [Fact]
    public void NewAggregate_HasNoDomainEvents()
    {
        var aggregate = new SampleAggregate(TestId.New());

        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Raise_AddsEventInOrder()
    {
        var aggregate = new SampleAggregate(TestId.New());

        aggregate.DoSomething();
        aggregate.DoSomething();

        aggregate.DomainEvents.Count.ShouldBe(2);
        aggregate.DomainEvents.ShouldAllBe(e => e is SomethingHappened);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheCollection()
    {
        var aggregate = new SampleAggregate(TestId.New());
        aggregate.DoSomething();

        aggregate.ClearDomainEvents();
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void DomainEvents_CannotBeMutatedByCallers()
    {
        var aggregate = new SampleAggregate(TestId.New());

        aggregate.DomainEvents.ShouldBeAssignableTo<IReadOnlyList<IDomainEvent>>();
        (aggregate.DomainEvents as ICollection<IDomainEvent>)?.IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    public void EachEvent_HasDistinctIdentity()
    {
        var aggregate = new SampleAggregate(TestId.New());

        aggregate.DoSomething();
        aggregate.DoSomething();

        aggregate.DomainEvents
            .Select(e => e.EventId)
            .Distinct()
            .Count()
            .ShouldBe(2);
    }

    private sealed record SomethingHappened : DomainEvent;

    private sealed class SampleAggregate(TestId id) : AggregateRoot<TestId>(id)
    {
        public void DoSomething() => Raise(new SomethingHappened());
    }
}
