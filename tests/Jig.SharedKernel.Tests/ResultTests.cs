using Jig.SharedKernel;
using Shouldly;
using Xunit;

namespace Jig.SharedKernel.Tests;

public class ResultTests
{
    [Fact]
    public void Success_carries_the_value()
    {
        Result<int> result = 42;
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Failure_carries_the_error_kind()
    {
        Result<int> result = Error.NotFound("nope");
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Kind.ShouldBe(ErrorKind.NotFound);
    }
}
