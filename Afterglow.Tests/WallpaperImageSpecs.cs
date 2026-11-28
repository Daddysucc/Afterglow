using Afterglow.Core;
using Afterglow.Services;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Tests;

public class WallpaperImageSpecs
{
    [Fact]
    public void I_keep_the_clear_skies_drawn_before_there_was_weather()
    {
        // Act
        var fileName = WallpaperService.GetImageFileName((1920, 1080), WallpaperSky.Clear(7));

        // Assert
        fileName.Should().Be("Sky 4 1920x1080 07.png");
        WallpaperService.TryParseImageFileName(fileName).Should().Be(WallpaperSky.Clear(7));
    }

    [Fact]
    public void I_get_a_sky_drawn_for_the_weather_known_again_from_its_file_name()
    {
        // Arrange
        var sky = WallpaperSky.Drawn(
            DynamicWallpaper.GetPalette(-3.3, new SkyConditions(LowClouds: 0.6, Haze: 0.3))
        );

        // Act
        var fileName = WallpaperService.GetImageFileName((3840, 2160), sky);

        // Assert
        fileName.Should().MatchRegex("^Sky 4 3840x2160 [0-9a-f]{26}\\.png$");
        WallpaperService.TryParseImageFileName(fileName).Should().Be(sky);
    }

    [Fact]
    public void I_get_the_clear_skies_drawn_for_my_altitude_known_again_from_their_file_names()
    {
        // Act
        var fileName = WallpaperService.GetImageFileName((1920, 1080), WallpaperSky.Clear(30, 3));

        // Assert
        fileName.Should().Be("Sky 4 1920x1080 h3 30.png");
        WallpaperService.TryParseImageFileName(fileName).Should().Be(WallpaperSky.Clear(30, 3));
        WallpaperSky.Clear(30, 3).Palette.Should().NotBe(WallpaperSky.Clear(30).Palette);
    }

    [Theory]
    [InlineData("Sky 4 1920x1080 h0 30.png")]
    [InlineData("Sky 4 1920x1080 h9 30.png")]
    [InlineData("Sky 3 1920x1080 07.png")]
    [InlineData("Sky 4 1920x1080 99.png")]
    [InlineData("Sky 4 1920x1080 overcast 07.png")]
    [InlineData("Sky 4 1920x1080 0123456789ABCDEF0123456789.png")]
    [InlineData("Sky 4 1920x1080 07.png.tmp")]
    [InlineData("Original wallpapers.txt")]
    public void I_never_get_other_files_taken_for_skies(string fileName)
    {
        // Act & Assert
        WallpaperService.TryParseImageFileName(fileName).Should().BeNull();
    }
}
