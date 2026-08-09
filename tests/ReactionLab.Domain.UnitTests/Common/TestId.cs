using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.UnitTests.Common;

internal readonly record struct TestId(Guid Value) : IStronglyTypedId<TestId>
{
    public static TestId From(Guid value) => new(value);

    public static TestId New() => new(Guid.CreateVersion7());
}
