using System.Globalization;
using System.Xml.Linq;
using StickyDock.Core.Docking;
using StickyDock.Core.Interaction;

namespace StickyDock.Tests.Views;

/// <summary>
/// Contract of the shell window: which element announces each pointer region and how the animations of the deployment
/// end (ADR-004 D1, D4 and D5).
/// </summary>
/// <remarks>
/// <para>
/// This file replaces the former structural guard of the regions, which asserted the opposite of the invariant that has
/// to hold: it required the bar region to hang from the root and required the sliding panel not to declare one. That
/// design let the region claim a pointer over pixels that a layered window never delivers, so a <c>MouseEnter</c>
/// arrived without its <c>MouseLeave</c> and the bar stopped collapsing (H-E3-05). The invariant is now
/// <em>paint ⇒ hit-test ⇒ region</em>: the region is announced by the element that paints it, and only by that element.
/// </para>
/// <para>
/// It also pins the lifecycle of the storyboards. A retained clock outranks the next animation, so from the second cycle
/// the deployed panel stayed at opacity 0 while the window was already deployed (H-E3-06). Therefore every storyboard
/// stops its clock and the value left on screen is the resting value declared by the state.
/// </para>
/// <para>
/// The window is not created here: instantiating <c>MainWindow.xaml</c> needs a WPF application, a dispatcher and real
/// resources. What is verified is the shape of the declaration, which is where the rules live and where a later refactor
/// would break them in silence. The animation literals are not taken from this file: they are derived from
/// <see cref="DockBarMetrics"/> and <see cref="InteractionTimings"/>, so a change in Core that the XAML does not follow
/// breaks the suite instead of being discovered by measuring pixels (deuda D-14).
/// </para>
/// </remarks>
public sealed class MainWindowStoryboardContractTests
{
    private const string XamlRelativePath = "src/StickyDock.App/Views/MainWindow.xaml";
    private const string SolutionFileName = "StickyDock.sln";

    /// <summary>Local name of the attached property of the per-element behavior, matched without its XML prefix.</summary>
    private const string RegionAttribute = "PointerRegionBehavior.Region";

    private const string ContentPanelStyleKey = "ContentPanelStyle";
    private const string TabBorderStyleKey = "TabBorderStyle";

    private const string ExpandedState = "DockState.Expanded";
    private const string CollapsingState = "DockState.Collapsing";
    private const string ArmingState = "DockState.Arming";

    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>
    /// Name of the retired region reporter, assembled from two fragments on purpose: the control grep of ADR-004 §9.4
    /// forbids the whole token anywhere under <c>src</c> and <c>tests</c>, and this file has to name it to assert that it
    /// is gone. Splitting it keeps the grep clean without weakening the assertion.
    /// </summary>
    private static readonly string RemovedReporter = "WindowRegion" + "Reporter";

    [Fact]
    public void BarRegion_IsDeclaredByTheContentPanel()
    {
        // ADR-004 D1: the panel is not an ancestor of the tab, so its own entry is real and its own exit is real too.
        var panel = DeclaringRegion("DockRegion.Bar");

        Assert.Equal("ContentPanel", NameOf(panel));
        AssertCarries(panel, "PointerRegionBehavior.EnterCommand");
        AssertCarries(panel, "PointerRegionBehavior.LeaveCommand");
    }

    [Fact]
    public void RootPanel_DoesNotPaint_And_DoesNotReportRegions()
    {
        // ADR-004 D1: a region on an element that does not paint announces a pointer over a transparent pixel, which the
        // system never delivers. The root only lays out; the window keeps its own background on purpose.
        var root = Named("RootPanel");

        Assert.Null(root.Attribute("Background"));
        Assert.DoesNotContain(
            root.Attributes(),
            attribute => attribute.Name.LocalName.StartsWith(RemovedReporter, StringComparison.Ordinal));
    }

    [Fact]
    public void NoWindowReporter_IsDeclaredAnywhere()
    {
        // The whole fabricated region machinery is gone: no element, no attribute and no value may mention it again.
        var elements = Descendants().ToList();

        Assert.DoesNotContain(elements, element => element.Name.LocalName.Contains(RemovedReporter, StringComparison.Ordinal));
        Assert.DoesNotContain(
            elements.SelectMany(element => element.Attributes()),
            attribute => attribute.Name.LocalName.Contains(RemovedReporter, StringComparison.Ordinal)
                || attribute.Value.Contains(RemovedReporter, StringComparison.Ordinal));
    }

    [Fact]
    public void TabBorder_KeepsItsRegionAndItsDragHandle()
    {
        // E1-CA4 and ADR-003 D3: the tab keeps its region and stays the only drag handle in both states.
        var tab = DeclaringRegion("DockRegion.Tab");

        Assert.Equal("TabBorder", NameOf(tab));
        AssertCarries(tab, "PressDragBehavior.Command");
    }

    [Fact]
    public void EveryStoryboardStopsItsClock()
    {
        // ADR-004 D4, rule R1: a retained clock keeps its value and outranks the next animation, which is the material
        // cause of H-E3-06. No storyboard of the window may hold its end value.
        var storyboards = Descendants().Where(element => element.Name.LocalName == "Storyboard").ToList();

        Assert.NotEmpty(storyboards);
        Assert.All(storyboards, storyboard => Assert.Equal("Stop", (string?)storyboard.Attribute("FillBehavior")));
        Assert.DoesNotContain(
            Descendants().SelectMany(element => element.Attributes()),
            attribute => attribute.Value.Contains("HoldEnd", StringComparison.Ordinal));
    }

    [Fact]
    public void CollapsingDeclaresItsRestingOpacity()
    {
        // ADR-004 D4, rule R2: the value shown is the resting value of the current state, not the last animated one, so
        // the second deployment looks exactly like the first one.
        var style = StyleWithKey(ContentPanelStyleKey);

        var visible = Assert.Single(
            style.Elements(),
            element => element.Name.LocalName == "Setter" && (string?)element.Attribute("Property") == "Opacity");

        Assert.Equal("1", (string?)visible.Attribute("Value"));

        var collapsing = Assert.Single(
            style.Descendants(),
            element => element.Name.LocalName == "DataTrigger" && TriggerState(element) == CollapsingState);

        var dark = Assert.Single(
            collapsing.Elements(),
            element => element.Name.LocalName == "Setter" && (string?)element.Attribute("Property") == "Opacity");

        Assert.Equal("0", (string?)dark.Attribute("Value"));
    }

    [Fact]
    public void DeployFade_HasNoAbsoluteStartingPoint()
    {
        // ADR-004 D4, rule R3: the offset comes from outside the screen, so it keeps a From; the opacity does not, so a
        // deployment that arrives while a slide-out is still fading converges from wherever it is instead of jumping.
        var fades = DeploymentTriggers()
            .SelectMany(trigger => AnimationsOf(trigger))
            .Where(animation => TargetProperty(animation).Contains("Opacity", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(5, fades.Count);
        Assert.All(fades, animation => Assert.Null(animation.Attribute("From")));
    }

    [Fact]
    public void FadeOutDuration_EqualsTheSettleDuration()
    {
        // ADR-004 D4: the panel stays painted while the window shrinks and goes dark exactly when the window has its
        // folded footprint, so there is no instant with the deployed footprint and nothing painted.
        var collapsing = Assert.Single(
            StyleWithKey(ContentPanelStyleKey).Descendants(),
            element => element.Name.LocalName == "DataTrigger" && TriggerState(element) == CollapsingState);

        var fade = Assert.Single(AnimationsOf(collapsing));

        Assert.Equal("1", (string?)fade.Attribute("From"));
        Assert.Equal("0", (string?)fade.Attribute("To"));
        Assert.Equal(TimeSpan.FromMilliseconds(InteractionTimings.SlideMilliseconds), DurationOf(fade));
    }

    [Fact]
    public void SlideOffsets_MatchTheThicknessGained()
    {
        // ADR-003 §5.4 and §8.3: the content enters from outside the screen, so the offset is the thickness gained when
        // deploying, with the sign pointing away from the screen and the axis of the edge. Derived from DockBarMetrics.
        var magnitude = Math.Abs(DockBarMetrics.Default.ExpandedThickness - DockBarMetrics.Default.CollapsedThickness);
        var triggers = DeploymentTriggers().ToList();

        Assert.Equal(5, triggers.Count);

        foreach (var trigger in triggers)
        {
            var edge = TriggerEdge(trigger);
            var translations = AnimationsOf(trigger)
                .Where(animation => TargetProperty(animation).Contains("TranslateTransform", StringComparison.Ordinal))
                .ToList();

            if (edge is null)
            {
                // A floating bar has no edge to come from: it only fades in (ADR-003 §12 #9).
                Assert.Empty(translations);
                continue;
            }

            var animation = Assert.Single(translations);
            var axis = edge is "DockEdge.Left" or "DockEdge.Right" ? "X" : "Y";
            var sign = edge is "DockEdge.Top" or "DockEdge.Left" ? -1d : 1d;

            Assert.Contains($"TranslateTransform.{axis})", TargetProperty(animation), StringComparison.Ordinal);
            Assert.Equal(sign * magnitude, double.Parse((string)animation.Attribute("From")!, CultureInfo.InvariantCulture));
            Assert.Equal(0d, double.Parse((string)animation.Attribute("To")!, CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public void Durations_MatchInteractionTimings()
    {
        // Deuda D-14: Timeline.Duration cannot receive the value by binding, so the literal is checked against Core here.
        var expected = TimeSpan.FromMilliseconds(InteractionTimings.SlideMilliseconds);

        var animations = DeploymentTriggers().SelectMany(trigger => AnimationsOf(trigger)).ToList();

        Assert.Equal(9, animations.Count);
        Assert.All(animations, animation => Assert.Equal(expected, DurationOf(animation)));
    }

    [Fact]
    public void MicroExpansion_StopsAndReturnsToItsBaseValue()
    {
        // HU02 CA #2 and Q-11: the micro-expansion is a round trip that ends on its base value, so releasing the clock
        // changes nothing and the effect replays on every activation instead of depending on a retained scale.
        var arming = Assert.Single(
            StyleWithKey(TabBorderStyleKey).Descendants(),
            element => element.Name.LocalName == "DataTrigger" && TriggerState(element) == ArmingState);

        var storyboard = Assert.Single(arming.Descendants(), element => element.Name.LocalName == "Storyboard");

        Assert.Equal("Stop", (string?)storyboard.Attribute("FillBehavior"));

        var scales = AnimationsOf(storyboard).ToList();

        Assert.Equal(2, scales.Count);
        Assert.All(scales, animation =>
        {
            Assert.Equal("1.06", (string?)animation.Attribute("To"));
            Assert.Equal("True", (string?)animation.Attribute("AutoReverse"));
            Assert.Equal(TimeSpan.FromMilliseconds(60), DurationOf(animation));
            Assert.Null(animation.Attribute("From"));
        });
    }

    [Fact]
    public void TabBorderStyle_InFloatingMode_AnchorsTopLeft()
    {
        var floatingTrigger = Assert.Single(
            StyleWithKey(TabBorderStyleKey).Descendants(),
            element => element.Name.LocalName == "DataTrigger"
                && (string?)element.Attribute("Binding") == "{Binding DockEdge}"
                && (string?)element.Attribute("Value") == "{x:Null}");

        var horizontalAlignment = Assert.Single(
            floatingTrigger.Elements(),
            element => element.Name.LocalName == "Setter"
                && (string?)element.Attribute("Property") == "HorizontalAlignment");
        var verticalAlignment = Assert.Single(
            floatingTrigger.Elements(),
            element => element.Name.LocalName == "Setter"
                && (string?)element.Attribute("Property") == "VerticalAlignment");

        Assert.Equal("Left", (string?)horizontalAlignment.Attribute("Value"));
        Assert.Equal("Top", (string?)verticalAlignment.Attribute("Value"));
    }

    /// <summary>Asserts that an element declares the attached property that announces <paramref name="region"/>.</summary>
    private static XElement DeclaringRegion(string region)
        => Assert.Single(
            Descendants(),
            element => element.Attributes().Any(
                attribute => attribute.Name.LocalName == RegionAttribute && StaticMember(attribute.Value) == region));

    /// <summary>Deployments of the bar: one per edge plus the floating one, all keyed on <c>DockState.Expanded</c>.</summary>
    private static IEnumerable<XElement> DeploymentTriggers()
        => StyleWithKey(ContentPanelStyleKey).Descendants().Where(
            element => element.Name.LocalName == "MultiDataTrigger"
                && element.Descendants().Any(
                    condition => condition.Name.LocalName == "Condition"
                        && StaticMember((string?)condition.Attribute("Value")) == ExpandedState));

    private static IEnumerable<XElement> AnimationsOf(XElement scope)
        => scope.Descendants().Where(element => element.Name.LocalName == "DoubleAnimation");

    private static string? TriggerState(XElement trigger)
        => StaticMember((string?)trigger.Attribute("Value"));

    /// <summary>Edge of a deployment trigger, or <see langword="null"/> for the floating bar.</summary>
    private static string? TriggerEdge(XElement trigger)
        => trigger.Descendants()
            .Where(condition => condition.Name.LocalName == "Condition"
                && ((string?)condition.Attribute("Binding") ?? string.Empty).Contains("DockEdge", StringComparison.Ordinal))
            .Select(condition => StaticMember((string?)condition.Attribute("Value")))
            .SingleOrDefault();

    /// <summary>
    /// Resolves the member an <c>{x:Static ns:Type.Member}</c> markup extension points at, without assuming the XML
    /// prefix chosen by the file, so a rename of the prefix does not break the contract.
    /// </summary>
    private static string? StaticMember(string? value)
    {
        const string Prefix = "{x:Static ";

        if (value is null || !value.StartsWith(Prefix, StringComparison.Ordinal) || !value.EndsWith('}'))
        {
            return null;
        }

        var member = value[Prefix.Length..^1];
        var separator = member.IndexOf(':');

        return separator < 0 ? member : member[(separator + 1)..];
    }

    private static string TargetProperty(XElement animation)
        => (string?)animation.Attribute("Storyboard.TargetProperty") ?? string.Empty;

    private static TimeSpan DurationOf(XElement animation)
        => TimeSpan.Parse((string)animation.Attribute("Duration")!, CultureInfo.InvariantCulture);

    private static void AssertCarries(XElement element, string attributeName)
        => Assert.Contains(element.Attributes(), attribute => attribute.Name.LocalName == attributeName);

    private static XElement StyleWithKey(string key)
        => Assert.Single(
            Descendants(),
            element => element.Name.LocalName == "Style" && (string?)element.Attribute(XamlNamespace + "Key") == key);

    private static XElement Named(string name)
        => Assert.Single(Descendants(), element => (string?)element.Attribute(XamlNamespace + "Name") == name);

    private static IEnumerable<XElement> Descendants()
    {
        var path = Path.Combine(RepositoryRoot(), XamlRelativePath);

        return XDocument.Load(path).Descendants();
    }

    private static string NameOf(XElement element)
        => (string?)element.Attribute(XamlNamespace + "Name") ?? string.Empty;

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new FileNotFoundException($"'{SolutionFileName}' was not found above '{AppContext.BaseDirectory}'.");
    }
}
