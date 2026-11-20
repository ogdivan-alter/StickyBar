using System.Xml.Linq;

namespace StickyBar.Tests.Views;

/// <summary>
/// E6-CA1, E6-CA5, §5.2-01: verifies the XAML/code-behind contract for <c>App.xaml</c> and <c>SettingsWindow</c>.
/// </summary>
public sealed class SettingsWindowContractTests
{
    private const string SolutionFileName = "StickyBar.sln";

    [Fact]
    public void AppXaml_UsesExplicitShutdownMode()
    {
        var appXaml = XDocument.Load(ResolveRepoPath("src/StickyBar.App/App.xaml"));
        var shutdownMode = (string?)appXaml.Root?.Attribute("ShutdownMode");

        Assert.Equal("OnExplicitShutdown", shutdownMode);
    }

    [Fact]
    public void SettingsWindowCodeBehind_ContainsOnlyInitializeComponent()
    {
        var codeBehind = File.ReadAllText(ResolveRepoPath("src/StickyBar.App/Views/SettingsWindow.xaml.cs"));

        Assert.Contains("InitializeComponent();", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("Registry", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("IConfiguration", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("NotifyIcon", codeBehind, StringComparison.Ordinal);
    }

    private static string ResolveRepoPath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return Path.Combine(directory.FullName, relativePath);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate '{SolutionFileName}' from '{AppContext.BaseDirectory}'.");
    }
}
