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

        Assert.Contains("Move to Main Bar", headers);
        Assert.Contains("Move to Ribbon", headers);
        Assert.Contains("Remove", headers);
        Assert.DoesNotContain(headers, h => h!.Contains("Edit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(headers, h => h!.Contains("Argument", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(headers, h => h!.Contains("Target", StringComparison.OrdinalIgnoreCase));
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
