using Jig.SharedKernel;
using Shouldly;
using Xunit;

namespace Jig.SharedKernel.Tests;

public class PseudoKeyTests
{
    [Fact]
    public void New_generates_a_non_empty_distinct_key()
    {
        var a = PseudoKey.New();
        var b = PseudoKey.New();
        a.Value.ShouldNotBe(Guid.Empty);
        a.ShouldNotBe(b);
    }
}
