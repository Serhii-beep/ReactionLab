using ReactionLab.Domain.Common;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Common;

public sealed class ResultTests
{
    private static readonly Error SampleError = Error.Validation("Test.Failed", "Failed.");

    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_CarriesTheError()
    {
        var result = Result.Failure(SampleError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void SuccessWithValue_ExposesTheValue()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void ReadingValueOfFailure_Throws()
    {
        Result<int> result = SampleError;

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversionFromNull_ProducesFailure()
    {
        Result<string> result = (string?)null;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NullValue);
    }

    [Fact]
    public void Match_SelectsTheCorrectBranch()
    {
        Result<int> ok = 7;
        Result<int> bad = SampleError;

        ok.Match(v => $"ok:{v}", e => $"err:{e.Code}").ShouldBe("ok:7");
        bad.Match(v => $"ok:{v}", e => $"err:{e.Code}").ShouldBe("err:Test.Failed");
    }

    [Fact]
    public void ConstructingContradictoryResult_Throws()
    {
        Should.Throw<ArgumentException>(() => Result.Failure(Error.None));
    }
}
