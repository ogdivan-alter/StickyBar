using StickyDock.Core.Interaction;

namespace StickyDock.Tests.Interaction;

/// <summary>
/// E3-CA5 (grace period of the collapse timer, Q-03): the range is 300..500 ms, the default is 400 ms and a stored value
/// outside the range is normalized instead of rejected.
/// </summary>
public sealed class GracePeriodTests
{
    [Fact]
    public void Default_Is400()
    {
        Assert.Equal(400, GracePeriod.DefaultMilliseconds);
    }

    [Theory]
    [InlineData(300)]
    [InlineData(400)]
    [InlineData(500)]
    public void Normalize_KeepsAValueInsideTheRange(int milliseconds)
    {
        Assert.Equal(milliseconds, GracePeriod.Normalize(milliseconds));
    }

    [Theory]
    [InlineData(0, GracePeriod.MinimumMilliseconds)]
    [InlineData(299, GracePeriod.MinimumMilliseconds)]
    [InlineData(-400, GracePeriod.MinimumMilliseconds)]
    [InlineData(501, GracePeriod.MaximumMilliseconds)]
    [InlineData(10_000, GracePeriod.MaximumMilliseconds)]
    public void Normalize_ClampsAValueOutsideTheRange(int stored, int expected)
    {
        Assert.Equal(expected, GracePeriod.Normalize(stored));
    }

    [Fact]
    public void Bounds_MatchTheRangeOfTheSpecification()
    {
        Assert.Equal(300, GracePeriod.MinimumMilliseconds);
        Assert.Equal(500, GracePeriod.MaximumMilliseconds);
    }

    [Fact]
    public void InteractionTimings_TakeTheActivationAndTheSlideFromTheirConstants()
    {
        var timings = InteractionTimings.FromGrace(400);

        Assert.Equal(TimeSpan.FromMilliseconds(200), timings.Activation);
        Assert.Equal(TimeSpan.FromMilliseconds(400), timings.Grace);
        Assert.Equal(TimeSpan.FromMilliseconds(160), timings.Slide);
    }

    [Fact]
    public void InteractionTimings_NormalizeTheGraceTheyReceive()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(300), InteractionTimings.FromGrace(10).Grace);
        Assert.Equal(TimeSpan.FromMilliseconds(500), InteractionTimings.FromGrace(9_999).Grace);
    }
}
