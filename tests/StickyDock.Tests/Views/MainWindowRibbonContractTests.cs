using System.Xml.Linq;

namespace StickyDock.Tests.Views;

/// <summary>
/// E4-CA1, E3-CA3, ADR-004 D1/D4: verifies the XAML contract of the ribbon panel, its interactive region and its
/// perpendicular slide animations.
/// </summary>
public sealed class MainWindowRibbonContractTests
{
    private const string XamlRelativePath = "src/StickyDock.App/Views/MainWindow.xaml";
    private const string SolutionFileName = "StickyDock.sln";
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void RibbonPanel_DeclaresRibbonRegionAndPointerCommands()
    {
        var document = LoadMainWindowXaml();
        var ribbonPanel = document
            .Descendants()
            .Single(e => (string?)e.Attribute(XamlNamespace + "Name") == "RibbonPanel");

        Assert.Equal(
            "{x:Static interaction:DockRegion.Ribbon}",
            (string?)ribbonPanel.Attributes().Single(a => a.Name.LocalName == "PointerRegionBehavior.Region").Value);
        Assert.Contains(ribbonPanel.Attributes(), a => a.Name.LocalName == "PointerRegionBehavior.EnterCommand");
        Assert.Contains(ribbonPanel.Attributes(), a => a.Name.LocalName == "PointerRegionBehavior.LeaveCommand");
    }

    [Fact]
    public void RibbonPanelStyle_AnimatesPerpendicularlyWithFillBehaviorStop()
    {
        var document = LoadMainWindowXaml();
        var style = document
            .Descendants()
            .Where(e => e.Name.LocalName == "Style")
            .Single(e => (string?)e.Attribute(XamlNamespace + "Key") == "RibbonPanelStyle");

        var storyboards = style.Descendants().Where(e => e.Name.LocalName == "Storyboard").ToList();
        Assert.Equal(5, storyboards.Count);
        Assert.All(storyboards, sb => Assert.Equal("Stop", (string?)sb.Attribute("FillBehavior")));

        var animatedProperties = style
            .Descendants()
            .Where(e => e.Name.LocalName == "DoubleAnimation")
            .Select(a => (string?)a.Attribute("Storyboard.TargetProperty"))
            .Where(p => p is not null)
            .ToList();

        Assert.Contains("(UIElement.RenderTransform).(TranslateTransform.Y)", animatedProperties);
        Assert.Contains("(UIElement.RenderTransform).(TranslateTransform.X)", animatedProperties);
    }

    private static XDocument LoadMainWindowXaml()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return XDocument.Load(Path.Combine(dir.FullName, XamlRelativePath));
    }
}
