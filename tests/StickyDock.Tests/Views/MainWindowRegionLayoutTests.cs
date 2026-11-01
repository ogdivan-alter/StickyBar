using System.Xml.Linq;

namespace StickyDock.Tests.Views;

/// <summary>
/// Structural guard of the pointer regions declared by the shell window (E3-CA3, E3-CA4 and E1-CA4).
/// </summary>
/// <remarks>
/// <para>
/// The interactive area is the union of the deployed bar and its ribbons (A4) and the window footprint covers exactly that
/// area in every state, so the region of the bar must be reported for the whole client area. While it lived on the panel
/// that the slide translates and clips, a pointer placed over the panel area during the animation was over no element;
/// WPF does not repeat the hit test when the panel settles under a still pointer, so no event arrived, the region never
/// reached the controller and the grace period expired with the pointer already inside the bar (defect D-E3-01 of E3).
/// </para>
/// <para>
/// The region therefore hangs from the window footprint through <c>WindowRegionReporter</c>, which reaffirms it on every
/// pointer move and reconciles it with the real cursor after a change of size or position. The per-element behavior is
/// not enough there: the root is the ancestor of the tab, so the pointer is already inside it while it hovers the tab and
/// no entry event announces the move into the panel.
/// </para>
/// <para>
/// The window is not created here: instantiating <c>MainWindow.xaml</c> needs a WPF application, a dispatcher and real
/// resources. What is verified is the shape of the declaration, which is where the rule lives and where a later refactor
/// would break it again in silence.
/// </para>
/// </remarks>
public sealed class MainWindowRegionLayoutTests
{
    private const string XamlRelativePath = "src/StickyDock.App/Views/MainWindow.xaml";
    private const string SolutionFileName = "StickyDock.sln";

    /// <summary>Local name of the attached property of the window reporter, matched without its XML prefix.</summary>
    private const string ReporterRegionAttribute = "WindowRegionReporter.Region";

    /// <summary>Local name of the attached property of the per-element behavior, matched without its XML prefix.</summary>
    private const string ElementRegionAttribute = "PointerRegionBehavior.Region";

    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void BarRegion_IsReportedByTheWindowReporter_OnTheRootContainer()
    {
        // E3-CA3 through the whole slide: the region must not depend on a hit-test transition, because the pointer never
        // leaves the root when it moves from the tab into the panel.
        var root = ElementCarrying(ReporterRegionAttribute, "DockRegion.Bar");

        Assert.Equal("RootPanel", NameOf(root));
        Assert.False(
            string.IsNullOrWhiteSpace((string?)root.Attribute("Background")),
            "The element that owns the bar region must paint a background, otherwise a panel without one is not hit-testable in the empty area of the client.");
        AssertCarries(root, "WindowRegionReporter.EnterCommand");
        AssertCarries(root, "WindowRegionReporter.LeaveCommand");
    }

    [Fact]
    public void BarRegion_IsNotDeclaredWithThePerElementBehavior()
    {
        // The per-element behavior reports an entry and an exit of one element, which is exactly the transition the bar
        // region cannot rely on (D-E3-01): mixing both would also report an exit the moment the hit-test leaf changes.
        var root = Named("RootPanel");

        Assert.DoesNotContain(root.Attributes(), attribute => attribute.Name.LocalName == ElementRegionAttribute);
    }

    [Fact]
    public void BarRegion_IsNotDeclaredByTheSlidingPanel()
    {
        // The panel is translated by the slide and clipped by ClipPanel: a region attached to it misses the pointer while
        // the animation runs, which is exactly the defect this guard prevents from coming back.
        var panel = Named("ContentPanel");

        Assert.DoesNotContain(panel.Attributes(), attribute => attribute.Name.LocalName == ReporterRegionAttribute);
        Assert.DoesNotContain(panel.Attributes(), attribute => attribute.Name.LocalName == ElementRegionAttribute);
        Assert.Equal("{StaticResource ContentPanelStyle}", (string?)panel.Attribute("Style"));
    }

    [Fact]
    public void IdleTab_KeepsItsRegionAndItsDragHandle()
    {
        // E1-CA4 and ADR-003 D3: the tab keeps its own region and stays the only drag handle in both states.
        var tab = ElementCarrying(ElementRegionAttribute, "DockRegion.Tab");

        Assert.Equal("TabBorder", NameOf(tab));
        Assert.Contains(tab.Attributes(), attribute => attribute.Name.LocalName == "PressDragBehavior.Command");
    }

    /// <summary>
    /// Asserts that an element declares an attribute whose local name is <paramref name="attributeName"/>; the XML prefix
    /// of the namespace is not part of the lookup and must not be assumed by the test.
    /// </summary>
    private static void AssertCarries(XElement element, string attributeName)
        => Assert.Contains(element.Attributes(), attribute => attribute.Name.LocalName == attributeName);

    private static XElement ElementCarrying(string attributeName, string region)
    {
        var value = $"{{x:Static interaction:{region}}}";

        return Assert.Single(
            Descendants(),
            element => element.Attributes().Any(attribute => attribute.Name.LocalName == attributeName && attribute.Value == value));
    }

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
