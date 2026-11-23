using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Core.Tests;

public class HueSpecs
{
    private static readonly DateTimeOffset Start = new(2026, 11, 22, 20, 0, 0, TimeSpan.Zero);

    private static readonly ColorConfiguration Warm = new(4500, 0.6);

    // A white ambiance light, at a cool white
    private static HueLight Light(
        string id = "light-1",
        bool isOn = true,
        int? mirek = 233,
        double? brightness = 80,
        double? x = null,
        double? y = null
    ) => new(id, "device-" + id, isOn, brightness, mirek, 153, 454, x, y);

    // ---- Reading the bridge's responses ----

    [Fact]
    public void I_can_have_lights_read_from_the_bridge()
    {
        // Arrange
        const string json = """
            {"errors":[],"data":[
              {"id":"a","owner":{"rid":"dev-a","rtype":"device"},"on":{"on":true},
               "dimming":{"brightness":56.3},
               "color_temperature":{"mirek":366,"mirek_valid":true,"mirek_schema":{"mirek_minimum":153,"mirek_maximum":500}},
               "color":{"xy":{"x":0.4573,"y":0.41}}},
              {"id":"b","owner":{"rid":"dev-b","rtype":"device"},"on":{"on":false},
               "dimming":{"brightness":100},
               "color_temperature":{"mirek":null,"mirek_valid":false,"mirek_schema":{"mirek_minimum":153,"mirek_maximum":500}},
               "color":{"xy":{"x":0.2,"y":0.3}}},
              {"id":"plug","owner":{"rid":"dev-c","rtype":"device"},"on":{"on":true}},
              {"owner":{"rid":"no-id"}}
            ]}
            """;

        // Act
        var lights = HueJson.ParseLights(json);

        // Assert
        lights.Should().HaveCount(3);

        lights[0].Should().Be(new HueLight("a", "dev-a", true, 56.3, 366, 153, 500, 0.4573, 0.41));

        // Showing a color, rather than a white
        lights[1].IsOn.Should().BeFalse();
        lights[1].Mirek.Should().BeNull();
        lights[1].SupportsColorTemperature.Should().BeTrue();

        // Only on and off
        lights[2].SupportsBrightness.Should().BeFalse();
        lights[2].SupportsColorTemperature.Should().BeFalse();
    }

    [Fact]
    public void I_can_have_rooms_and_zones_read_from_the_bridge()
    {
        // Arrange
        const string json = """
            {"errors":[],"data":[
              {"id":"r2","metadata":{"name":"Office"},"children":[{"rid":"dev-a","rtype":"device"}]},
              {"id":"r1","metadata":{"name":"Bedroom"},"children":[{"rid":"light-z","rtype":"light"},{"rid":"dev-b","rtype":"device"}]},
              {"id":"r3","children":[]}
            ]}
            """;

        // Act
        var rooms = HueJson.ParseRooms(json);

        // Assert
        rooms.Select(r => r.Name).Should().Equal("Bedroom", "Office", "r3");
        rooms[0].DeviceIds.Should().Equal("dev-b");
        rooms[0].LightIds.Should().Equal("light-z");

        rooms[1].Contains(Light("a") with { DeviceId = "dev-a" }).Should().BeTrue();
        rooms[0].Contains(Light("light-z") with { DeviceId = null }).Should().BeTrue();
        rooms[0].Contains(Light("a") with { DeviceId = "dev-a" }).Should().BeFalse();
    }

    [Fact]
    public void I_can_have_bridges_found_on_my_network()
    {
        // Act
        var bridges = HueJson.ParseDiscovery(
            """[{"id":"001788fffe123456","internalipaddress":"192.168.1.20","port":443},{"id":"x"}]"""
        );

        // Assert
        bridges.Should().Equal(new HueBridge("001788fffe123456", "192.168.1.20"));
        HueJson.ParseDiscovery("{}").Should().BeEmpty();
    }

    [Fact]
    public void I_can_connect_once_the_button_on_the_bridge_is_pressed()
    {
        // Act & assert
        HueJson
            .ParsePairing(
                """[{"error":{"type":101,"address":"","description":"link button not pressed"}}]"""
            )
            .Should()
            .Be(((string?)null, (int?)101));

        HueJson
            .ParsePairing("""[{"success":{"username":"abc123"}}]""")
            .Should()
            .Be(("abc123", (int?)null));
    }

    [Fact]
    public void I_can_have_lights_set_with_requests_the_bridge_understands()
    {
        // Act & assert
        HueJson
            .ToRequestBody(new HueCommand("a", 370, 60, null, null, 4000))
            .Should()
            .Be(
                """{"color_temperature":{"mirek":370},"dimming":{"brightness":60},"dynamics":{"duration":4000}}"""
            );

        HueJson
            .ToRequestBody(new HueCommand("a", null, null, 0.4573, 0.41, 1000))
            .Should()
            .Be("""{"color":{"xy":{"x":0.4573,"y":0.41}},"dynamics":{"duration":1000}}""");
    }

    // ---- Following ----

    [Fact]
    public void I_get_lights_that_are_on_set_to_the_cycle()
    {
        // Arrange
        var controller = new HueLightController();

        // Act
        var commands = controller.Update([Light()], Warm, true, true, Start);

        // Assert
        commands
            .Should()
            .Equal(
                new HueCommand(
                    "light-1",
                    387,
                    60,
                    null,
                    null,
                    HueLightController.InitialTransitionMilliseconds
                )
            );
        controller.FollowedLightCount.Should().Be(1);
    }

    [Fact]
    public void I_never_get_lights_turned_on()
    {
        // Arrange
        var controller = new HueLightController();

        // Act
        var commands = controller.Update([Light(isOn: false)], Warm, true, true, Start);

        // Assert
        commands.Should().BeEmpty();
        controller.FollowedLightCount.Should().Be(0);
    }

    [Fact]
    public void I_only_get_what_i_chose_to_follow_changed()
    {
        // Arrange
        var controller = new HueLightController();

        // Act
        var temperatureOnly = controller.Update([Light("a")], Warm, true, false, Start);
        var brightnessOnly = controller.Update([Light("b")], Warm, false, true, Start);

        // Assert
        temperatureOnly.Should().ContainSingle().Which.Brightness.Should().BeNull();
        temperatureOnly[0].Mirek.Should().Be(387);
        brightnessOnly.Should().ContainSingle().Which.Mirek.Should().BeNull();
        brightnessOnly[0].Brightness.Should().Be(60);
    }

    [Fact]
    public void I_get_the_temperature_kept_within_what_each_light_supports()
    {
        // Arrange
        var controller = new HueLightController();

        // Act
        var commands = controller.Update(
            [Light()],
            new ColorConfiguration(1200, 1),
            true,
            false,
            Start
        );

        // Assert
        commands.Should().ContainSingle().Which.Mirek.Should().Be(454);
    }

    [Fact]
    public void I_get_lights_adjusted_gently_as_the_cycle_moves_on_but_not_for_tiny_changes()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light()], Warm, true, true, Start);
        var settled = Light(mirek: 387, brightness: 60);

        // Act
        var tiny = controller.Update(
            [settled],
            new ColorConfiguration(4490, 0.603),
            true,
            true,
            Start.AddSeconds(10)
        );
        var larger = controller.Update(
            [settled],
            new ColorConfiguration(4300, 0.58),
            true,
            true,
            Start.AddSeconds(20)
        );

        // Assert
        tiny.Should().BeEmpty();
        larger
            .Should()
            .Equal(
                new HueCommand(
                    "light-1",
                    407,
                    58,
                    null,
                    null,
                    HueLightController.FollowTransitionMilliseconds
                )
            );
    }

    [Fact]
    public void I_get_a_light_left_alone_after_changing_it_myself_until_it_is_turned_off_and_on()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light()], Warm, true, true, Start);

        // Act: changed in the Hue app, once the light had time to settle
        var afterChange = controller.Update(
            [Light(mirek: 250, brightness: 100)],
            new ColorConfiguration(2400, 0.5),
            true,
            true,
            Start.AddSeconds(10)
        );
        var countAfterChange = controller.FollowedLightCount;

        controller.Update([Light(isOn: false)], Warm, true, true, Start.AddSeconds(20));
        var afterTurningOn = controller.Update(
            [Light(mirek: 250, brightness: 100)],
            Warm,
            true,
            true,
            Start.AddSeconds(30)
        );

        // Assert
        afterChange.Should().BeEmpty();
        countAfterChange.Should().Be(0);
        afterTurningOn.Should().ContainSingle().Which.Mirek.Should().Be(387);
    }

    [Fact]
    public void I_do_not_get_a_light_taken_for_changed_while_it_is_still_getting_there()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light()], Warm, true, true, Start);

        // Act: still at its old color, a moment after being set
        var commands = controller.Update([Light()], Warm, true, true, Start.AddSeconds(2));

        // Assert
        commands.Should().BeEmpty();
        controller.FollowedLightCount.Should().Be(1);
    }

    [Fact]
    public void I_get_a_light_set_again_when_setting_it_failed()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light()], Warm, true, true, Start);

        // Act
        controller.MarkFailed("light-1");
        var commands = controller.Update([Light()], Warm, true, true, Start.AddSeconds(10));

        // Assert
        commands.Should().ContainSingle().Which.Mirek.Should().Be(387);
        controller.FollowedLightCount.Should().Be(1);
    }

    [Fact]
    public void I_get_lights_put_back_as_they_were_when_following_stops()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update(
            [
                Light("white"),
                Light("color", mirek: null, x: 0.2, y: 0.3),
                Light("off"),
                Light("mine"),
            ],
            Warm,
            true,
            true,
            Start
        );

        // Act
        var commands = controller.Restore(
            [
                Light("white", mirek: 387, brightness: 60),
                Light("color", mirek: 387, brightness: 60),
                Light("off", isOn: false),
                // Changed in the Hue app since (not noticed yet)
                Light("mine", mirek: 300, brightness: 60),
            ],
            Start.AddSeconds(10)
        );

        // Assert
        commands
            .Should()
            .BeEquivalentTo([
                new HueCommand(
                    "white",
                    233,
                    80,
                    null,
                    null,
                    HueLightController.RestoreTransitionMilliseconds
                ),
                new HueCommand(
                    "color",
                    null,
                    80,
                    0.2,
                    0.3,
                    HueLightController.RestoreTransitionMilliseconds
                ),
            ]);
        controller.FollowedLightCount.Should().Be(0);
    }

    [Fact]
    public void I_do_not_get_a_light_i_changed_myself_put_back()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light()], Warm, true, true, Start);
        controller.Update([Light(mirek: 300)], Warm, true, true, Start.AddSeconds(10));

        // Act
        var commands = controller.Restore([Light(mirek: 300)], Start.AddSeconds(20));

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void I_get_only_the_brightness_put_back_when_i_stop_following_it()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light()], Warm, true, true, Start);

        // Act
        var commands = controller.Update(
            [Light(mirek: 387, brightness: 60)],
            Warm,
            true,
            false,
            Start.AddSeconds(10)
        );

        // Assert
        commands
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(
                new HueCommand(
                    "light-1",
                    null,
                    80,
                    null,
                    null,
                    HueLightController.InitialTransitionMilliseconds
                )
            );
        controller.FollowedLightCount.Should().Be(1);
    }

    [Fact]
    public void I_get_lights_put_back_when_their_room_is_no_longer_chosen()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light("a"), Light("b")], Warm, true, false, Start);

        // Act
        var commands = controller.Restore(
            [Light("a", mirek: 387), Light("b", mirek: 387)],
            Start.AddSeconds(10),
            id => id == "b"
        );

        // Assert
        commands.Should().ContainSingle().Which.LightId.Should().Be("b");
        controller.FollowedLightCount.Should().Be(1);
    }

    // A light that shows warm whites and colors
    private static HueLight ColorLight(
        string id = "light-1",
        int? mirek = 233,
        double x = 0.31,
        double y = 0.33
    ) => new(id, "device-" + id, true, 80, mirek, 153, 447, x, y);

    [Theory]
    // The screen's own white is a neutral white light
    [InlineData(6500, 2, 250)]
    // Warmer tints get lights that are warmer still
    [InlineData(4500, 2, 387)]
    [InlineData(4500, 1, 318)]
    [InlineData(3100, 2, 500)]
    // Cooler tints go up to a daylight white
    [InlineData(20_000, 2, 154)]
    public void I_get_white_lights_set_to_about_as_warm_as_the_screen_looks(
        double screenKelvin,
        double warmth,
        int expected
    )
    {
        // Arrange
        var light = new HueLight("a", null, true, 80, 233, 153, 500, null, null);

        // Act
        var (mirek, x, y) = HueLightController.ToLightColor(screenKelvin, warmth, light);

        // Assert
        mirek.Should().Be(expected);
        x.Should().BeNull();
        y.Should().BeNull();
    }

    [Fact]
    public void I_get_color_lights_turned_amber_once_they_run_out_of_warm_whites()
    {
        // Act
        var white = HueLightController.ToLightColor(4500, 2, ColorLight());
        var amber = HueLightController.ToLightColor(3100, 2, ColorLight());
        var deepest = HueLightController.ToLightColor(1200, 4, ColorLight());

        // Assert
        white.Should().Be(((int?)387, (double?)null, (double?)null));

        amber.Mirek.Should().BeNull();
        amber.X.Should().BeInRange(0.53, 0.57);
        amber.Y.Should().BeInRange(0.38, 0.42);

        // No further than a deep orange, which every color light can show
        deepest.X.Should().BeInRange(0.59, 0.62);
        deepest.Y.Should().BeInRange(0.34, 0.37);
    }

    [Fact]
    public void I_get_the_same_white_on_lights_as_on_screens()
    {
        // Act
        var neutral = WhitePoint.GetChromaticity(6500);
        var warm = WhitePoint.GetChromaticity(3000);

        // Assert (close to daylight, and to a 3000 K light)
        neutral.X.Should().BeApproximately(0.313, 0.01);
        neutral.Y.Should().BeApproximately(0.329, 0.01);
        warm.X.Should().BeApproximately(0.437, 0.01);
        warm.Y.Should().BeApproximately(0.404, 0.01);
    }

    [Fact]
    public void I_get_color_lights_followed_and_left_alone_once_i_change_them()
    {
        // Arrange
        var controller = new HueLightController();
        var evening = new ColorConfiguration(3100, 0.9);

        // Act
        var first = controller.Update([ColorLight()], evening, true, false, Start);
        var command = first.Should().ContainSingle().Subject;
        var shown = ColorLight(mirek: null, x: command.ColorX!.Value, y: command.ColorY!.Value);

        var settled = controller.Update([shown], evening, true, false, Start.AddSeconds(10));
        var changed = controller.Update(
            [shown with { ColorX = 0.2, ColorY = 0.2 }],
            new ColorConfiguration(2500, 0.9),
            true,
            false,
            Start.AddSeconds(20)
        );

        // Assert
        command.Mirek.Should().BeNull();
        settled.Should().BeEmpty();
        changed.Should().BeEmpty();
        controller.FollowedLightCount.Should().Be(0);

        // Put back to the white it had
        var restored = new HueLightController();
        restored.Update([ColorLight()], evening, true, false, Start);
        restored
            .Restore([shown], Start.AddSeconds(1))
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(new HueCommand("light-1", 233, null, null, null, 1000));
    }

    [Fact]
    public void I_get_a_light_set_again_when_it_did_not_take_what_it_was_set_to()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light("a"), Light("b")], Warm, true, false, Start);

        // Act (the bridge lost what was sent to "a", so it's still as it was once its quick
        // transition is over, and it's sent again quickly)
        var commands = controller.Update(
            [Light("a"), Light("b", mirek: 387)],
            Warm,
            true,
            false,
            Start.AddSeconds(3.5)
        );

        // Assert
        commands
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(
                new HueCommand(
                    "a",
                    387,
                    null,
                    null,
                    null,
                    HueLightController.InitialTransitionMilliseconds
                )
            );
        controller.FollowedLightCount.Should().Be(2);
    }

    [Fact]
    public void I_get_a_light_that_is_out_of_reach_set_again_less_and_less_often_until_it_gets_there()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light()], Warm, true, false, Start);

        // Act (checked every 10 seconds, while it stays as it was)
        var resentAt = new List<int>();
        for (var i = 1; i <= 10; i++)
        {
            if (controller.Update([Light()], Warm, true, false, Start.AddSeconds(10 * i)).Count > 0)
                resentAt.Add(10 * i);
        }

        // Assert
        resentAt.Should().Equal(10, 20, 40, 80);
        controller.FollowedLightCount.Should().Be(1, "it's never given up on");
        controller.UnresponsiveLightCount.Should().Be(1);

        // And once it gets there, it's fine again
        controller.Update([Light()], Warm, true, false, Start.AddSeconds(120));
        controller.Update([Light(mirek: 387)], Warm, true, false, Start.AddSeconds(130));
        controller.UnresponsiveLightCount.Should().Be(0);
        controller.FollowedLightCount.Should().Be(1);
    }

    [Fact]
    public void I_get_lights_tried_again_less_and_less_often_while_they_are_out_of_reach()
    {
        // Act & assert
        HueLightController.GetRetryDelay(1).Should().Be(TimeSpan.Zero);
        HueLightController.GetRetryDelay(2).Should().Be(TimeSpan.FromSeconds(10));
        HueLightController.GetRetryDelay(3).Should().Be(TimeSpan.FromSeconds(20));
        HueLightController.GetRetryDelay(10).Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void I_get_a_light_put_back_even_if_it_did_not_take_what_it_was_last_set_to()
    {
        // Arrange (it follows, then misses the latest change)
        var controller = new HueLightController();
        controller.Update([Light()], Warm, true, false, Start);
        controller.Update(
            [Light(mirek: 387)],
            new ColorConfiguration(5000, 0.6),
            true,
            false,
            Start.AddSeconds(10)
        );

        // Act (still at the white from before the last change)
        var commands = controller.Restore([Light(mirek: 387)], Start.AddSeconds(30));

        // Assert (back to how it was before it was first set)
        commands.Should().ContainSingle().Which.Mirek.Should().Be(233);
    }

    [Fact]
    public void I_get_a_light_left_alone_when_i_set_it_back_to_how_it_was_before_afterglow()
    {
        // Arrange (it was seen following)
        var controller = new HueLightController();
        controller.Update([Light()], Warm, true, false, Start);
        controller.Update([Light(mirek: 387)], Warm, true, false, Start.AddSeconds(10));

        // Act (set back to its old white, e.g. with a voice assistant)
        var commands = controller.Update(
            [Light(mirek: 233)],
            Warm,
            true,
            false,
            Start.AddSeconds(20)
        );

        // Assert
        commands.Should().BeEmpty();
        controller.GetState("light-1").Should().Be(HueLightState.LeftAlone);
    }

    [Fact]
    public void I_get_errors_reported_by_the_bridge_read()
    {
        // Act & assert
        HueJson
            .ParseErrors("""{"errors":[{"description":"too many requests"}],"data":[]}""")
            .Should()
            .Equal("too many requests");
        HueJson.ParseErrors("""{"errors":[],"data":[]}""").Should().BeEmpty();
        HueJson.ParseErrors("not json").Should().BeEmpty();
    }

    [Theory]
    // Lights match the screen's brightness by default
    [InlineData(0.85, 1, 85)]
    // Higher values dim them more, lower ones less, and full brightness always stays full
    [InlineData(0.8, 2, 64)]
    [InlineData(0.85, 0.5, 92.2)]
    [InlineData(1, 4, 100)]
    // Never quite off
    [InlineData(0, 1, 1)]
    public void I_get_lights_dimmed_as_strongly_as_i_choose(
        double screenBrightness,
        double dimming,
        double expected
    )
    {
        // Act & assert
        HueLightController.ToLightBrightness(screenBrightness, dimming).Should().Be(expected);
    }

    [Fact]
    public void I_get_lights_dimmed_more_strongly_when_i_choose_to()
    {
        // Arrange
        var controller = new HueLightController { Dimming = 2 };

        // Act
        var commands = controller.Update([Light()], Warm, false, true, Start);

        // Assert
        commands.Should().ContainSingle().Which.Brightness.Should().Be(36);
    }

    [Fact]
    public void I_get_a_light_set_again_when_it_shows_something_it_was_set_to_earlier()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light()], new ColorConfiguration(6500, 0.6), true, false, Start);
        controller.Update(
            [Light(mirek: 250)],
            new ColorConfiguration(4500, 0.6),
            true,
            false,
            Start.AddSeconds(1)
        );
        controller.Update(
            [Light(mirek: 250)],
            new ColorConfiguration(3500, 0.6),
            true,
            false,
            Start.AddSeconds(2)
        );

        // Act (the last two didn't get there, so it's still at the first)
        var commands = controller.Update(
            [Light(mirek: 250)],
            new ColorConfiguration(3500, 0.6),
            true,
            false,
            Start.AddSeconds(20)
        );

        // Assert
        commands.Should().ContainSingle().Which.Mirek.Should().Be(454);
        controller.GetState("light-1").Should().Be(HueLightState.NotResponding);

        // While a white outside of what it was set to is a change made by something else
        controller
            .Update(
                [Light(mirek: 160)],
                new ColorConfiguration(3500, 0.6),
                true,
                false,
                Start.AddSeconds(40)
            )
            .Should()
            .BeEmpty();
        controller.GetState("light-1").Should().Be(HueLightState.LeftAlone);
    }

    [Fact]
    public void I_get_a_light_checked_only_once_its_slow_transition_is_over()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light()], Warm, true, false, Start);
        controller.Update(
            [Light(mirek: 387)],
            new ColorConfiguration(5000, 0.6),
            true,
            false,
            Start.AddSeconds(10)
        );

        // Act (still on its way, partway through its 4 s transition)
        var during = controller.Update(
            [Light(mirek: 370)],
            new ColorConfiguration(5000, 0.6),
            true,
            false,
            Start.AddSeconds(13)
        );

        // Assert
        during.Should().BeEmpty();
        controller.GetState("light-1").Should().Be(HueLightState.Following);
        HueLightController
            .GetSettleTime(HueLightController.FollowTransitionMilliseconds)
            .Should()
            .Be(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void I_get_a_light_set_again_when_it_is_caught_between_what_it_was_set_to()
    {
        // Arrange (dimmed a lot, quickly)
        var controller = new HueLightController();
        controller.Update(
            [Light(brightness: 90)],
            new ColorConfiguration(4500, 0.9),
            false,
            true,
            Start
        );
        controller.Update(
            [Light(brightness: 90)],
            new ColorConfiguration(4500, 0.1),
            false,
            true,
            Start.AddSeconds(1)
        );
        controller.Update(
            [Light(brightness: 60)],
            new ColorConfiguration(4500, 0.9),
            false,
            true,
            Start.AddSeconds(2)
        );

        // Act (it reports a brightness partway, which it was never set to)
        var commands = controller.Update(
            [Light(brightness: 77)],
            new ColorConfiguration(4500, 0.9),
            false,
            true,
            Start.AddSeconds(20)
        );

        // Assert
        commands.Should().ContainSingle().Which.Brightness.Should().Be(90);
        controller.GetState("light-1").Should().Be(HueLightState.NotResponding);
    }

    [Fact]
    public void I_get_a_light_caught_up_rather_than_left_alone_after_moving_a_slider_quickly()
    {
        // Arrange (a slider moved quickly, so the light is set several times in a row)
        var controller = new HueLightController();
        for (var i = 0; i < 4; i++)
        {
            controller.Update(
                [Light()],
                new ColorConfiguration(6500 - 500 * i, 0.6),
                true,
                false,
                Start.AddSeconds(i),
                isUserChange: true
            );
        }

        // Act (it shows something odd on the way, which it was never set to)
        var commands = controller.Update(
            [Light(mirek: 160)],
            new ColorConfiguration(5000, 0.6),
            true,
            false,
            Start.AddSeconds(15)
        );

        // Assert
        commands.Should().ContainSingle();
        controller.GetState("light-1").Should().NotBe(HueLightState.LeftAlone);

        // While once things have been calm for a while, a change is taken for one made by
        // something else
        controller.Update(
            [Light(mirek: 160)],
            new ColorConfiguration(5000, 0.6),
            true,
            false,
            Start.AddSeconds(60)
        );
        controller.GetState("light-1").Should().Be(HueLightState.LeftAlone);
    }

    [Fact]
    public void I_get_a_light_caught_up_rather_than_left_alone_after_a_change_i_made_however_many_lights_there_are()
    {
        // Arrange (a change made by the user, sent long after the one before, as there are many
        // lights)
        var controller = new HueLightController();
        controller.Update([Light()], new ColorConfiguration(6500, 0.6), true, false, Start);
        controller.Update(
            [Light(mirek: 250)],
            new ColorConfiguration(3500, 0.6),
            true,
            false,
            Start.AddSeconds(10),
            isUserChange: true
        );

        // Act (it shows something odd on the way)
        var commands = controller.Update(
            [Light(mirek: 160)],
            new ColorConfiguration(3500, 0.6),
            true,
            false,
            Start.AddSeconds(20)
        );

        // Assert
        commands.Should().ContainSingle();
        controller.GetState("light-1").Should().NotBe(HueLightState.LeftAlone);
    }

    [Fact]
    public void I_get_a_light_left_alone_when_i_change_it_while_the_cycle_moves_on_gently()
    {
        // Arrange (the cycle moves on in small steps, a while apart)
        var controller = new HueLightController();
        controller.Update([Light()], new ColorConfiguration(4500, 0.6), true, false, Start);
        controller.Update(
            [Light(mirek: 387)],
            new ColorConfiguration(4450, 0.6),
            true,
            false,
            Start.AddSeconds(60)
        );

        // Act
        controller.Update(
            [Light(mirek: 160)],
            new ColorConfiguration(4450, 0.6),
            true,
            false,
            Start.AddSeconds(70)
        );

        // Assert
        controller.GetState("light-1").Should().Be(HueLightState.LeftAlone);
    }

    [Fact]
    public void I_can_get_a_light_left_alone_followed_again()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light()], Warm, true, false, Start);
        controller.Update([Light(mirek: 160)], Warm, true, false, Start.AddSeconds(10));
        controller.GetState("light-1").Should().Be(HueLightState.LeftAlone);

        // Act
        controller.FollowAgain("light-1");
        var commands = controller.Update(
            [Light(mirek: 160)],
            Warm,
            true,
            false,
            Start.AddSeconds(11)
        );

        // Assert
        commands.Should().ContainSingle().Which.Mirek.Should().Be(387);
        controller.GetState("light-1").Should().Be(HueLightState.Following);

        // And it still goes back to how it was before Afterglow first set it
        controller
            .Restore([Light(mirek: 387)], Start.AddSeconds(12))
            .Should()
            .ContainSingle()
            .Which.Mirek.Should()
            .Be(233);
    }

    [Theory]
    // At full brightness, small changes can't be seen
    [InlineData(1.0, 0.98, false)]
    [InlineData(1.0, 0.96, true)]
    // While when dim, much smaller ones can be (as long as the light can show them)
    [InlineData(0.1, 0.096, false)]
    [InlineData(0.1, 0.094, true)]
    public void I_only_get_lights_dimmed_by_amounts_that_can_be_seen(
        double from,
        double to,
        bool isSent
    )
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update(
            [Light(brightness: 50)],
            new ColorConfiguration(4500, from),
            false,
            true,
            Start
        );

        // Act
        var commands = controller.Update(
            [Light(brightness: from * 100)],
            new ColorConfiguration(4500, to),
            false,
            true,
            Start.AddMinutes(1)
        );

        // Assert
        commands.Should().HaveCount(isSent ? 1 : 0);
    }

    [Theory]
    [InlineData(4470, false)]
    [InlineData(4400, true)]
    public void I_only_get_lights_set_to_another_white_when_it_can_be_seen(
        double kelvin,
        bool isSent
    )
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light()], new ColorConfiguration(4500, 0.6), true, false, Start);

        // Act
        var commands = controller.Update(
            [Light(mirek: 387)],
            new ColorConfiguration(kelvin, 0.6),
            true,
            false,
            Start.AddMinutes(1)
        );

        // Assert
        commands.Should().HaveCount(isSent ? 1 : 0);
    }

    [Fact]
    public void I_get_lights_set_to_exactly_what_i_choose_however_small_the_change()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update(
            [Light(brightness: 50)],
            new ColorConfiguration(4500, 1),
            true,
            true,
            Start
        );

        // Act (moved a slider a little, which the cycle on its own wouldn't send)
        var commands = controller.Update(
            [Light(mirek: 387, brightness: 100)],
            new ColorConfiguration(4470, 0.99),
            true,
            true,
            Start.AddMinutes(1),
            isUserChange: true
        );

        // Assert
        commands
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new { Mirek = 390, Brightness = 99.0 });
    }

    [Fact]
    public void I_get_a_light_that_already_matched_set_again_when_it_misses_what_comes_next()
    {
        // Arrange (it already shows the cycle, so nothing is sent at first, and it's seen
        // following)
        var controller = new HueLightController();
        controller.Update([Light(mirek: 387)], Warm, true, false, Start).Should().BeEmpty();
        controller.Update([Light(mirek: 387)], Warm, true, false, Start.AddSeconds(10));
        controller.Update(
            [Light(mirek: 387)],
            new ColorConfiguration(5000, 0.6),
            true,
            false,
            Start.AddSeconds(20)
        );

        // Act (what was sent next didn't get there)
        var commands = controller.Update(
            [Light(mirek: 387)],
            new ColorConfiguration(5000, 0.6),
            true,
            false,
            Start.AddSeconds(40)
        );

        // Assert
        commands.Should().ContainSingle().Which.Mirek.Should().Be(342);
        controller.GetState("light-1").Should().Be(HueLightState.NotResponding);
    }

    [Fact]
    public void I_only_get_lights_turned_on_when_i_ask_for_it()
    {
        // Arrange
        var controller = new HueLightController();

        // Act
        var notAsked = controller.Update([Light(isOn: false)], Warm, true, true, Start);
        var asked = controller.Update(
            [Light(isOn: false)],
            Warm,
            true,
            true,
            Start.AddSeconds(1),
            isTurningOn: true
        );

        // Assert (straight to the cycle)
        notAsked.Should().BeEmpty();
        asked
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(new HueCommand("light-1", 387, 60, null, null, 1000, true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void I_get_lights_afterglow_turned_on_turned_off_again_or_left_neutral_while_paused(
        bool isPause
    )
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light(isOn: false)], Warm, true, true, Start, isTurningOn: true);

        // Act (just after, while it still reads as off)
        var commands = controller.Restore([Light(isOn: false)], Start.AddSeconds(1), null, isPause);

        // Assert
        commands
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(
                isPause
                    ? new HueCommand(
                        "light-1",
                        HueLightController.NeutralMirek,
                        100,
                        null,
                        null,
                        1000
                    )
                    : new HueCommand("light-1", null, null, null, null, 1000, false)
            );
    }

    [Fact]
    public void I_get_a_light_afterglow_turned_on_left_on_once_i_turned_it_off_and_on_myself()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light(isOn: false)], Warm, true, false, Start, isTurningOn: true);
        controller.Update([Light(isOn: false)], Warm, true, false, Start.AddSeconds(10));
        controller.Update([Light(mirek: 387)], Warm, true, false, Start.AddSeconds(20));

        // Act
        var commands = controller.Restore([Light(mirek: 387)], Start.AddSeconds(30));

        // Assert (put back to the white it had when turned on, rather than off)
        commands.Should().ContainSingle().Which.IsOn.Should().BeNull();
    }

    [Fact]
    public void I_get_lights_left_as_they_are_when_the_pc_sleeps_and_followed_again_afterwards()
    {
        // Arrange (one light was on already, and one was turned on by Afterglow)
        var controller = new HueLightController();
        controller.Update(
            [Light("a"), Light("b", isOn: false)],
            Warm,
            true,
            false,
            Start,
            isTurningOn: true
        );
        var lights = new[] { Light("a", mirek: 387), Light("b", mirek: 387) };

        // Act
        var commands = controller.TurnOff(lights, Start.AddSeconds(10), true);

        // Assert (only the one Afterglow turned on is turned off)
        commands
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(new HueCommand("b", null, null, null, null, 1000, false));

        // And the other still goes back to how it was before Afterglow, once it lets go of it
        controller
            .Restore(lights, Start.AddMinutes(10))
            .Should()
            .ContainSingle()
            .Which.Mirek.Should()
            .Be(233);
    }

    [Fact]
    public void I_can_get_lights_turned_off_when_the_pc_shuts_down()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light("a"), Light("b")], Warm, true, false, Start);

        // Act (one of them was changed by something else meanwhile)
        var commands = controller.TurnOff(
            [Light("a", mirek: 387), Light("b", mirek: 160)],
            Start.AddMinutes(1),
            false
        );

        // Assert
        commands.Should().ContainSingle().Which.LightId.Should().Be("a");
        commands[0].IsOn.Should().BeFalse();
        controller.FollowedLightCount.Should().Be(0);
    }

    [Fact]
    public void I_get_a_light_changed_elsewhere_followed_again_after_a_while()
    {
        // Arrange
        var controller = new HueLightController { ReclaimDelay = TimeSpan.FromHours(1) };
        controller.Update([Light()], Warm, true, false, Start);
        controller.Update([Light(mirek: 160)], Warm, true, false, Start.AddMinutes(1));
        controller.GetState("light-1").Should().Be(HueLightState.LeftAlone);
        controller.GetReclaimTime("light-1").Should().Be(Start.AddMinutes(61));

        // Act (changed again meanwhile, which counts from then)
        controller.Update([Light(mirek: 170)], Warm, true, false, Start.AddMinutes(30));
        var tooSoon = controller.Update(
            [Light(mirek: 170)],
            Warm,
            true,
            false,
            Start.AddMinutes(80)
        );
        var later = controller.Update([Light(mirek: 170)], Warm, true, false, Start.AddMinutes(91));

        // Assert (gently back to the cycle)
        tooSoon.Should().BeEmpty();
        later
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(
                new HueCommand(
                    "light-1",
                    387,
                    null,
                    null,
                    null,
                    HueLightController.FollowTransitionMilliseconds
                )
            );
        controller.GetState("light-1").Should().Be(HueLightState.Following);

        // And it still goes back to how it was before Afterglow
        controller
            .Restore([Light(mirek: 387)], Start.AddMinutes(92))
            .Should()
            .ContainSingle()
            .Which.Mirek.Should()
            .Be(233);
    }

    [Fact]
    public void I_get_lights_changed_quickly_when_asked_to()
    {
        // Arrange
        var controller = new HueLightController();
        controller.Update([Light()], Warm, true, false, Start);

        // Act
        var commands = controller.Update(
            [Light(mirek: 387)],
            new ColorConfiguration(5000, 1),
            true,
            false,
            Start.AddSeconds(1),
            500
        );

        // Assert
        commands.Should().ContainSingle().Which.TransitionMilliseconds.Should().Be(500);
    }
}
