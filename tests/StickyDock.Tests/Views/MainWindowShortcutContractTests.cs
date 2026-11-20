using System.Xml.Linq;

namespace StickyDock.Tests.Views;

/// <summary>
/// E5-CA3, E5-CA4, E5-CA5, Q-06: verifies the XAML contract of shortcut buttons, drag &amp; drop behaviors, and the
/// context menu (move/remove only, never edit target/arguments).
/// </summary>
public sealed class MainWindowShortcutContractTests
{
    private const string XamlRelativePath = "src/StickyDock.App/Views/MainWindow.xaml";
    private const string SolutionFileName = "StickyDock.sln";
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void ShortcutItemTemplate_ContextMenu_OffersMoveAndRemoveWithoutEdit()
    {
        var document = LoadMainWindowXaml();
        var template = document
            .Descendants()
            .Where(e => e.Name.LocalName == "DataTemplate")
            .Single(e => (string?)e.Attribute(XamlNamespace + "Key") == "ShortcutItemTemplate");

        var headers = template
            .Descendants()
            .Where(e => e.Name.LocalName == "MenuItem")
            .Select(m => (string?)m.Attribute("Header"))
            .Where(h => h is not null)
            .ToList();

        Assert.Contains(headers, h => h!.Contains("Strings.MoveToMainBar", StringComparison.Ordinal));
        Assert.Contains(headers, h => h!.Contains("Strings.MoveToRibbon", StringComparison.Ordinal));
        Assert.Contains(headers, h => h!.Contains("Strings.Remove", StringComparison.Ordinal));
        Assert.DoesNotContain(headers, h => h!.Contains("Edit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(headers, h => h!.Contains("Argument", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            headers,
            h => h!.Replace("PlacementTarget", string.Empty, StringComparison.Ordinal)
                .Contains("Target", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(headers, h => h!.Contains("Properties", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ContentPanelAndRibbonPanel_DeclareExternalDropCommand()
    {
        var document = LoadMainWindowXaml();

        var contentPanel = document
            .Descendants()
            .Single(e => (string?)e.Attribute(XamlNamespace + "Name") == "ContentPanel");
        var ribbonPanel = document
            .Descendants()
            .Single(e => (string?)e.Attribute(XamlNamespace + "Name") == "RibbonPanel");

        Assert.Contains(
            contentPanel.Attributes(),
            a => a.Name.LocalName == "ShortcutDragDropBehavior.ExternalDropCommand");
        Assert.Contains(
            ribbonPanel.Attributes(),
            a => a.Name.LocalName == "ShortcutDragDropBehavior.ExternalDropCommand");
    }

    [Fact]
    public void TabBorder_DeclaresExternalDropCommandBoundToDropFilesOnBarCommand()
    {
        var document = LoadMainWindowXaml();

        var tabBorder = document
            .Descendants()
            .Single(e => (string?)e.Attribute(XamlNamespace + "Name") == "TabBorder");

        var externalDropAttribute = tabBorder
            .Attributes()
            .SingleOrDefault(a => a.Name.LocalName == "ShortcutDragDropBehavior.ExternalDropCommand");

        Assert.NotNull(externalDropAttribute);
        Assert.Equal("{Binding DropFilesOnBarCommand}", externalDropAttribute.Value);
    }

    [Fact]
    public void GripDragHandle_DeclaresPressDragCommandAndSizeAllCursor()
    {
        var document = LoadMainWindowXaml();

        var grip = document
            .Descendants()
            .Single(e => (string?)e.Attribute(XamlNamespace + "Name") == "GripDragHandle");

        Assert.Equal("Transparent", (string?)grip.Attribute("Background"));
        Assert.Equal("SizeAll", (string?)grip.Attribute("Cursor"));
        Assert.Equal(
            "{Binding DragCommand}",
            (string?)grip.Attributes().Single(a => a.Name.LocalName == "PressDragBehavior.Command").Value);
    }

    [Fact]
    public void PinBarButton_AndShortcutDragSuspensionCommands_AreBound()
    {
        var document = LoadMainWindowXaml();

        var pinButton = document
            .Descendants()
            .Single(e => (string?)e.Attribute(XamlNamespace + "Name") == "PinBarButton");
        Assert.Equal("{Binding TogglePinCommand}", (string?)pinButton.Attribute("Command"));

        var shortcutStyle = document
            .Descendants()
            .Where(e => e.Name.LocalName == "Style")
            .Single(e => (string?)e.Attribute(XamlNamespace + "Key") == "ShortcutButtonStyle");
        var setterProperties = shortcutStyle
            .Elements()
            .Where(e => e.Name.LocalName == "Setter")
            .Select(e => (string?)e.Attribute("Property"))
            .ToList();

        Assert.Contains("input:ShortcutDragDropBehavior.DragStartedCommand", setterProperties);
        Assert.Contains("input:ShortcutDragDropBehavior.DragEndedCommand", setterProperties);
    }

    [Fact]
    public void RibbonIndicatorsAndRibbonPanel_AcceptInternalShortcutDrop()
    {
        var document = LoadMainWindowXaml();

        var ribbonTileButton = document
            .Descendants()
            .Where(e => e.Name.LocalName == "Button")
            .Single(b => (string?)b.Attribute("Style") == "{StaticResource RibbonTileButtonStyle}");

        Assert.Equal(
            "{Binding}",
            (string?)ribbonTileButton.Attributes().Single(a => a.Name.LocalName == "ShortcutDragDropBehavior.TargetRibbon").Value);
        Assert.Equal(
            "{Binding AcceptInternalShortcutCommand}",
            (string?)ribbonTileButton.Attributes().Single(a => a.Name.LocalName == "ShortcutDragDropBehavior.MoveToRibbonCommand").Value);

        var ribbonPanel = document
            .Descendants()
            .Single(e => (string?)e.Attribute(XamlNamespace + "Name") == "RibbonPanel");

        Assert.Equal(
            "{Binding OpenRibbon}",
            (string?)ribbonPanel.Attributes().Single(a => a.Name.LocalName == "ShortcutDragDropBehavior.TargetRibbon").Value);
        Assert.Equal(
            "{Binding OpenRibbon.AcceptInternalShortcutCommand}",
            (string?)ribbonPanel.Attributes().Single(a => a.Name.LocalName == "ShortcutDragDropBehavior.MoveToRibbonCommand").Value);
    }

    [Fact]
    public void ContextMenus_BindContextMenuHoldBehaviorOpenedAndClosedCommands()
    {
        var document = LoadMainWindowXaml();

        var contextMenus = document
            .Descendants()
            .Where(e => e.Name.LocalName == "ContextMenu")
            .ToList();

        Assert.Equal(2, contextMenus.Count);

        foreach (var menu in contextMenus)
        {
            var opened = menu.Attributes()
                .SingleOrDefault(a => a.Name.LocalName == "ContextMenuHoldBehavior.OpenedCommand");
            var closed = menu.Attributes()
                .SingleOrDefault(a => a.Name.LocalName == "ContextMenuHoldBehavior.ClosedCommand");

            Assert.NotNull(opened);
            Assert.NotNull(closed);
            Assert.Contains("BeginContextMenuCommand", opened.Value, StringComparison.Ordinal);
            Assert.Contains("EndContextMenuCommand", closed.Value, StringComparison.Ordinal);
        }
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
