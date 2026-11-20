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

    [Fact]
    public void ContentPanel_IncludesAppIconResourceAndDirectionalRibbonIndicators()
    {
        var document = LoadMainWindowXaml();

        var images = document.Descendants().Where(e => e.Name.LocalName == "Image").ToList();
        Assert.Contains(
            images,
            img => string.Equals(
                (string?)img.Attribute("Source"),
                "pack://application:,,,/Icons/sticky-64x64.png",
                StringComparison.Ordinal));

        var arrowStyle = document
            .Descendants()
            .Where(e => e.Name.LocalName == "Style")
            .Single(e => (string?)e.Attribute(XamlNamespace + "Key") == "RibbonArrowPathStyle");
        var triggers = arrowStyle.Descendants().Where(e => e.Name.LocalName == "DataTrigger").ToList();
        Assert.Equal(3, triggers.Count);

        var ribbonPanel = document
            .Descendants()
            .Single(e => (string?)e.Attribute(XamlNamespace + "Name") == "RibbonPanel");
        var buttons = ribbonPanel.Descendants().Where(e => e.Name.LocalName == "Button").ToList();
        Assert.Contains(buttons, b => (string?)b.Attribute("Command") == "{Binding ToggleActionsCommand}");
    }

    [Fact]
    public void TabBorder_HidesStickyTextWhenDocked_AndShowsIconPlusTextWhenFloating()
    {
        var document = LoadMainWindowXaml();

        var floatingStyle = StyleWithKey(document, "FloatingTabContentStyle");
        Assert.Contains(
            floatingStyle.Elements(),
            e => e.Name.LocalName == "Setter"
                && (string?)e.Attribute("Property") == "Visibility"
                && (string?)e.Attribute("Value") == "Collapsed");
        Assert.Contains(
            floatingStyle.Descendants(),
            e => e.Name.LocalName == "DataTrigger"
                && (string?)e.Attribute("Value") == "{x:Null}"
                && e.Elements().Any(s => (string?)s.Attribute("Property") == "Visibility" && (string?)s.Attribute("Value") == "Visible"));

        var floatingContent = document
            .Descendants()
            .Single(e => (string?)e.Attribute(XamlNamespace + "Name") == "FloatingTabContent");
        Assert.Contains(
            floatingContent.Descendants().Where(e => e.Name.LocalName == "Image"),
            img => string.Equals((string?)img.Attribute("Source"), "pack://application:,,,/Icons/sticky-64x64.png", StringComparison.Ordinal));
        Assert.Contains(
            floatingContent.Descendants().Where(e => e.Name.LocalName == "TextBlock"),
            tb => string.Equals((string?)tb.Attribute("Text"), "Sticky", StringComparison.Ordinal));

        var chevronStyle = StyleWithKey(document, "DockedTabChevronStyle");
        Assert.Contains(
            chevronStyle.Elements(),
            e => e.Name.LocalName == "Setter"
                && (string?)e.Attribute("Property") == "Fill"
                && (string?)e.Attribute("Value") == "Transparent");
        Assert.Contains(
            chevronStyle.Descendants(),
            e => e.Name.LocalName == "DataTrigger"
                && (string?)e.Attribute("Value") == "{x:Null}"
                && e.Elements().Any(s => (string?)s.Attribute("Property") == "Visibility" && (string?)s.Attribute("Value") == "Collapsed"));
    }

    [Fact]
    public void ContentPanel_UsesFixedThickness_DynamicDockEdge_AndIconAtStart()
    {
        var document = LoadMainWindowXaml();

        var barSlotStyle = StyleWithKey(document, "BarSlotStyle");
        Assert.Contains(
            barSlotStyle.Elements(),
            e => e.Name.LocalName == "Setter"
                && (string?)e.Attribute("Property") == "Height"
                && (string?)e.Attribute("Value") == "52");
        Assert.Contains(
            barSlotStyle.Elements(),
            e => e.Name.LocalName == "Setter"
                && (string?)e.Attribute("Property") == "DockPanel.Dock"
                && (string?)e.Attribute("Value") == "Top");

        Assert.DoesNotContain(
            barSlotStyle.Descendants(),
            e => e.Name.LocalName == "DataTrigger"
                && ((string?)e.Attribute("Binding") ?? string.Empty).Contains("HasOpenRibbon", StringComparison.Ordinal));

        var triggers = barSlotStyle.Descendants().Where(e => e.Name.LocalName == "DataTrigger").ToList();
        Assert.Contains(triggers, t => ((string?)t.Attribute("Value") ?? string.Empty).Contains("Bottom", StringComparison.Ordinal)
            && t.Elements().Any(s => (string?)s.Attribute("Property") == "Height" && (string?)s.Attribute("Value") == "52"));
        Assert.Contains(triggers, t => ((string?)t.Attribute("Value") ?? string.Empty).Contains("Left", StringComparison.Ordinal)
            && t.Elements().Any(s => (string?)s.Attribute("Property") == "Width" && (string?)s.Attribute("Value") == "52"));
        Assert.Contains(triggers, t => ((string?)t.Attribute("Value") ?? string.Empty).Contains("Right", StringComparison.Ordinal)
            && t.Elements().Any(s => (string?)s.Attribute("Property") == "Width" && (string?)s.Attribute("Value") == "52"));

        var contentPanel = document
            .Descendants()
            .Single(e => (string?)e.Attribute(XamlNamespace + "Name") == "ContentPanel");
        var mainStack = contentPanel.Elements().First(e => e.Name.LocalName == "StackPanel");
        var children = mainStack.Elements().ToList();

        var iconIndex = children.FindIndex(e => e.Name.LocalName == "Border"
            && e.Descendants().Any(img => img.Name.LocalName == "Image"
                && (string?)img.Attribute("Source") == "pack://application:,,,/Icons/sticky-64x64.png"));
        var barItemsIndex = children.FindIndex(e => e.Name.LocalName == "ItemsControl"
            && (string?)e.Attribute("ItemsSource") == "{Binding BarItems}");
        var ribbonsIndex = children.FindIndex(e => e.Name.LocalName == "ItemsControl"
            && (string?)e.Attribute("ItemsSource") == "{Binding Ribbons}");

        Assert.True(iconIndex > 0, "App icon should appear after the grip.");
        Assert.True(iconIndex < barItemsIndex, "App icon should appear before BarItems.");
        Assert.True(barItemsIndex < ribbonsIndex, "BarItems should appear before Ribbons.");
    }

    [Fact]
    public void RibbonPanel_IsNarrowStrip_WithOpenChevronArrows_AndNoGreenColors()
    {
        var document = LoadMainWindowXaml();

        var ribbonPanelStyle = StyleWithKey(document, "RibbonPanelStyle");
        Assert.Contains(
            ribbonPanelStyle.Elements(),
            e => e.Name.LocalName == "Setter"
                && (string?)e.Attribute("Property") == "Width"
                && (string?)e.Attribute("Value") == "54");
        Assert.Contains(
            ribbonPanelStyle.Descendants(),
            e => e.Name.LocalName == "DataTrigger"
                && ((string?)e.Attribute("Value") ?? string.Empty).Contains("Left", StringComparison.Ordinal)
                && e.Elements().Any(s => (string?)s.Attribute("Property") == "Height" && (string?)s.Attribute("Value") == "54"));

        var ribbonPanel = document
            .Descendants()
            .Single(e => (string?)e.Attribute(XamlNamespace + "Name") == "RibbonPanel");
        Assert.Contains(
            ribbonPanel.Descendants(),
            e => e.Name.LocalName == "MultiBinding"
                && ((string?)e.Attribute("Converter") ?? string.Empty).Contains("RibbonOffsetToMarginConverter", StringComparison.Ordinal));

        var arrowStyle = StyleWithKey(document, "RibbonArrowPathStyle");
        Assert.Contains(
            arrowStyle.Elements(),
            e => e.Name.LocalName == "Setter"
                && (string?)e.Attribute("Property") == "Fill"
                && (string?)e.Attribute("Value") == "Transparent");
        var arrowDataValues = arrowStyle
            .Descendants()
            .Where(e => e.Name.LocalName == "Setter" && (string?)e.Attribute("Property") == "Data")
            .Select(e => (string?)e.Attribute("Value") ?? string.Empty)
            .ToList();
        Assert.Equal(4, arrowDataValues.Count);
        Assert.All(arrowDataValues, data => Assert.DoesNotContain("M 6,2", data, StringComparison.Ordinal));

        var tileStyle = StyleWithKey(document, "RibbonTileButtonStyle");
        var serialized = tileStyle.ToString() + ribbonPanelStyle.ToString();
        Assert.DoesNotContain("43A047", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("66BB6A", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("81C784", serialized, StringComparison.OrdinalIgnoreCase);

        var tabBorderStyle = StyleWithKey(document, "TabBorderStyle");
        Assert.Contains(
            tabBorderStyle.Descendants(),
            e => e.Name.LocalName == "DataTrigger"
                && ((string?)e.Attribute("Value") ?? string.Empty).Contains("DockState.Expanded", StringComparison.Ordinal)
                && e.Elements().Any(s => (string?)s.Attribute("Property") == "Visibility" && (string?)s.Attribute("Value") == "Collapsed"));

        var contentControl = ribbonPanel.Elements().FirstOrDefault(e => e.Name.LocalName == "ContentControl");
        Assert.NotNull(contentControl);
        Assert.Equal("{Binding OpenRibbon}", (string?)contentControl.Attribute("Content"));
    }

    private static XElement StyleWithKey(XDocument document, string key)
        => document
            .Descendants()
            .Where(e => e.Name.LocalName == "Style")
            .Single(e => (string?)e.Attribute(XamlNamespace + "Key") == key);

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
