using System.IO;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Tests;

public class ExecutablePathSpecs
{
    private const string AppDirPath = @"C:\Program Files\Afterglow\";

    private static readonly string AppExecutablePath = Path.Combine(AppDirPath, "Afterglow.exe");

    [Fact]
    public void I_get_the_app_executable_when_published_normally_or_as_a_single_file()
    {
        // Act
        var path = Program.ResolveExecutableFilePath(
            AppDirPath,
            "Afterglow",
            AppExecutablePath,
            p => p == AppExecutablePath
        );

        // Assert
        path.Should().Be(AppExecutablePath);
    }

    [Fact]
    public void I_get_the_app_executable_rather_than_the_dotnet_host_when_launched_through_it()
    {
        // Act
        var path = Program.ResolveExecutableFilePath(
            AppDirPath,
            "Afterglow",
            @"C:\Program Files\dotnet\dotnet.exe",
            p => p == AppExecutablePath
        );

        // Assert
        path.Should().Be(AppExecutablePath);
    }

    [Fact]
    public void I_get_the_process_executable_when_the_app_executable_has_a_different_name()
    {
        // Arrange
        const string processPath = @"D:\Tools\Afterglow-Portable.exe";

        // Act
        var path = Program.ResolveExecutableFilePath(
            @"D:\Tools\",
            "Afterglow",
            processPath,
            _ => false
        );

        // Assert
        path.Should().Be(processPath);
    }

    [Fact]
    public void I_never_get_the_dotnet_host_as_the_executable()
    {
        // Act
        var path = Program.ResolveExecutableFilePath(
            AppDirPath,
            "Afterglow",
            @"C:\Program Files\dotnet\dotnet.exe",
            _ => false
        );

        // Assert
        path.Should().Be(AppExecutablePath);
    }
}
