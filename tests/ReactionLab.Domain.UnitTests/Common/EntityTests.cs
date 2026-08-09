using ReactionLab.Domain.Common;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Common;

public sealed class EntityTests
{
    [Fact]
    public void Entities_WithSameId_AreEqual()
    {
        var id = TestId.New();

        var first = new SampleEntity(id);
        var second = new SampleEntity(id);

        first.ShouldBe(second);
        (first == second).ShouldBeTrue();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Entities_WithDifferentIds_AreNotEqual()
    {
        var first = new SampleEntity(TestId.New());
        var second = new SampleEntity(TestId.New());

        first.ShouldNotBe(second);
        (first != second).ShouldBeTrue();
    }


    [Fact]
    public void Entities_OfDifferentTypesWithSameId_AreNotEqual()
    {
        var id = TestId.New();

        Entity<TestId> entity = new SampleEntity(id);
        Entity<TestId> other = new OtherEntity(id);

        entity.ShouldNotBe(other);
    }

    [Fact]
    public void Entity_IsNotEqualToNull()
    {
        var entity = new SampleEntity(TestId.New());

        entity.Equals(null).ShouldBeFalse();
        (entity == null).ShouldBeFalse();
    }

    private sealed class SampleEntity(TestId id) : Entity<TestId>(id);

    private sealed class OtherEntity(TestId id) : Entity<TestId>(id);
}
