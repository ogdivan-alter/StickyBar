using StickyBar.Core.Configuration;
using StickyBar.Tests.TestInfrastructure;

namespace StickyBar.Tests.Configuration;

/// <summary>
/// The persistence base path must be injectable (§5.2-02) so tests never touch the real user profile.
/// </summary>
public sealed class ApplicationPathsTests
{
    [Fact]
    public void ForRoot_ResolvesTheApplicationLayoutBelowTheGivenRoot()
    {
        using var directory = new TemporaryDirectory();

        var paths = ApplicationPaths.ForRoot(directory.Path);

        Assert.Equal(Path.Combine(directory.Path, ApplicationPaths.FolderName), paths.DataDirectory);
        Assert.Equal(Path.Combine(paths.DataDirectory, ApplicationPaths.LogsFolderName), paths.LogDirectory);
        Assert.Equal(Path.Combine(paths.DataDirectory, ApplicationPaths.ConfigurationFileName), paths.ConfigurationFilePath);
    }

    [Fact]
    public void ForRoot_WhenTheRootIsEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() => ApplicationPaths.ForRoot(" "));
    }

    [Fact]
    public void Constructor_WhenADirectoryIsMissing_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ApplicationPaths(string.Empty, "logs"));
        Assert.Throws<ArgumentException>(() => new ApplicationPaths("data", " "));
    }

    [Fact]
    public void ForCurrentUser_PointsToTheRoamingApplicationDataFolder()
    {
        var paths = ApplicationPaths.ForCurrentUser();
        var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        Assert.StartsWith(applicationData, paths.DataDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(ApplicationPaths.FolderName, paths.DataDirectory, StringComparison.Ordinal);
    }
}
