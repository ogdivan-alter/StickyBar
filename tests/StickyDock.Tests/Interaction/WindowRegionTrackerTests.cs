using StickyDock.Core.Interaction;

namespace StickyDock.Tests.Interaction;

/// <summary>
/// Region report of the window footprint (D-E3-01): the report of the bar region is derived from the pointer position, so
/// it must be idempotent and must survive repeated observations of the same side (a pointer moving inside the footprint,
/// or a window changing size under a still pointer).
/// </summary>
public sealed class WindowRegionTrackerTests
{
    [Fact]
    public void Observe_Inside_FromTheInitialOutside_Enters()
    {
        var tracker = new WindowRegionTracker();

        Assert.Equal(RegionReport.Enter, tracker.Observe(true));
        Assert.True(tracker.IsInside);
    }

    [Fact]
    public void Observe_Outside_FromTheInitialOutside_ReportsNothing()
    {
        var tracker = new WindowRegionTracker();

        Assert.Equal(RegionReport.None, tracker.Observe(false));
        Assert.False(tracker.IsInside);
    }

    [Fact]
    public void Observe_InsideTwice_EntersOnce()
    {
        var tracker = new WindowRegionTracker();

        Assert.Equal(RegionReport.Enter, tracker.Observe(true));

        // Every pointer move over the window footprint asks the same question again; a second entry must not be reported,
        // because entering an already entered region is a no-op that would otherwise disturb the single timer slot.
        Assert.Equal(RegionReport.None, tracker.Observe(true));
        Assert.Equal(RegionReport.None, tracker.Observe(true));
    }

    [Fact]
    public void Observe_OutsideTwice_LeavesOnce()
    {
        var tracker = new WindowRegionTracker();

        tracker.Observe(true);

        Assert.Equal(RegionReport.Leave, tracker.Observe(false));
        Assert.Equal(RegionReport.None, tracker.Observe(false));
    }

    [Fact]
    public void Observe_AlternatingSides_ReportsEveryTransition()
    {
        var tracker = new WindowRegionTracker();

        Assert.Equal(RegionReport.Enter, tracker.Observe(true));
        Assert.Equal(RegionReport.Leave, tracker.Observe(false));
        Assert.Equal(RegionReport.Enter, tracker.Observe(true));
        Assert.Equal(RegionReport.Leave, tracker.Observe(false));
    }

    [Fact]
    public void Observe_AfterAStillPointerInside_KeepsTheRegionEntered()
    {
        // A geometry change under a still pointer samples the same side again: inside, inside, inside. The region must not
        // be left in between, which is the whole point of reconciling the report with the real cursor (D-E3-01).
        var tracker = new WindowRegionTracker();

        tracker.Observe(true);

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(RegionReport.None, tracker.Observe(true));
        }

        Assert.True(tracker.IsInside);
    }

    [Fact]
    public void Observe_WhenTheWindowGrowsOverAStillPointer_EntersOnceAndStays()
    {
        // Deploying grows the window under a pointer that does not move: the first observation inside is a real entry, and
        // the repeated observations of the following frames must not report anything.
        var tracker = new WindowRegionTracker();

        Assert.Equal(RegionReport.None, tracker.Observe(false));
        Assert.Equal(RegionReport.Enter, tracker.Observe(true));
        Assert.Equal(RegionReport.None, tracker.Observe(true));
    }
}
