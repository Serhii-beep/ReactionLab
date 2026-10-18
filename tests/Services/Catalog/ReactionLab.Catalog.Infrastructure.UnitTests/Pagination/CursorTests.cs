using ReactionLab.Catalog.Application.Common.Pagination;
using Shouldly;
using Xunit;

namespace ReactionLab.Catalog.Infrastructure.UnitTests.Pagination;

public sealed class CursorTests
{
    [Fact]
    public void Decode_RoundTripsAnOffset() =>
        Cursor.Decode(Cursor.Skipping(60).Encode()).Value.Skip.ShouldBe(60);

    [Fact]
    public void Decode_RejectsANegativeOffset() =>
        Cursor.Decode(Cursor.Skipping(-30).Encode()).Error.ShouldBe(Cursor.Malformed);
}
