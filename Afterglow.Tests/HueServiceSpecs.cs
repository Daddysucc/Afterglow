using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Afterglow.Core;
using Afterglow.Services;
using Afterglow.Utils;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Tests;

public class HueServiceSpecs : IDisposable
{
    private const string Address = "192.168.1.20";
    private const string Certificate = "C3A1";

    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "Afterglow.Tests." + Guid.NewGuid().ToString("N")
    );

    private DateTimeOffset _now = new(2026, 11, 22, 20, 0, 0, TimeSpan.Zero);

    public HueServiceSpecs() => Directory.CreateDirectory(_directoryPath);

    // A bridge with a room ("Living room") that holds all of its lights
    private sealed class FakeBridge
    {
        public sealed record Light(bool IsOn, int? Mirek, double Brightness);

        public Dictionary<string, Light> Lights { get; } = new(StringComparer.Ordinal);

        public List<(HttpMethod Method, string Path, string? Key, string Body)> Requests { get; } =
        [];

        public HueService? Service { get; set; }

        public string PresentedCertificate { get; set; } = Certificate;

        public string ExpectedKey { get; set; } = "app-key";

        // How many connection attempts happen before the button is pressed
        public int ButtonPressedAfter { get; set; }

        public bool IsReachable { get; set; } = true;

        // How many light commands are answered with "too many requests", or accepted but lost
        public int BusyPuts { get; set; }
        public int DroppedPuts { get; set; }

        private int _pairingAttempts;

        public IEnumerable<(string Id, string Body)> Puts =>
            Requests
                .Where(r => r.Method == HttpMethod.Put)
                .Select(r => (r.Path.Split('/').Last(), r.Body));

        public async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var uri = request.RequestUri!;
            var key = request.Headers.TryGetValues("hue-application-key", out var keys)
                ? keys.Single()
                : null;
            var body = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);

            lock (Requests)
                Requests.Add((request.Method, uri.AbsolutePath, key, body));

            if (uri.Host == "discovery.meethue.com")
            {
                return Json(
                    $$"""[{"id":"001788fffe000001","internalipaddress":"{{Address}}","port":443}]"""
                );
            }

            if (!IsReachable)
                throw new HttpRequestException("No route to host.");

            // The TLS handshake
            if (Service is not null && !Service.CheckCertificate(PresentedCertificate, uri.IdnHost))
                throw new HttpRequestException("Rejected.", new AuthenticationException());

            switch (request.Method.Method, uri.AbsolutePath)
            {
                case ("POST", "/api"):
                    return Json(
                        ++_pairingAttempts > ButtonPressedAfter
                            ? $$$"""[{"success":{"username":"{{{ExpectedKey}}}"}}]"""
                            : """[{"error":{"type":101,"address":"","description":"link button not pressed"}}]"""
                    );

                case ("GET", "/api/0/config"):
                    return Json("""{"bridgeid":"001788FFFE000001"}""");
            }

            if (key != ExpectedKey)
                return new HttpResponseMessage(HttpStatusCode.Forbidden);

            switch (request.Method.Method, uri.AbsolutePath)
            {
                case ("GET", "/clip/v2/resource/light"):
                    lock (Lights)
                    {
                        return Json(
                            "{\"errors\":[],\"data\":["
                                + string.Join(",", Lights.Select(p => ToJson(p.Key, p.Value)))
                                + "]}"
                        );
                    }

                case ("GET", "/clip/v2/resource/room"):
                    lock (Lights)
                    {
                        return Json(
                            "{\"errors\":[],\"data\":[{\"id\":\"room-1\",\"metadata\":{\"name\":\"Living room\"},\"children\":["
                                + string.Join(
                                    ",",
                                    Lights.Keys.Select(id =>
                                        $$"""{"rid":"device-{{id}}","rtype":"device"}"""
                                    )
                                )
                                + "]}]}"
                        );
                    }

                case ("GET", "/clip/v2/resource/zone"):
                    return Json("""{"errors":[],"data":[]}""");

                case ("PUT", var path) when path.StartsWith("/clip/v2/resource/light/"):
                {
                    if (BusyPuts > 0)
                    {
                        BusyPuts--;
                        return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                    }

                    if (DroppedPuts > 0)
                    {
                        DroppedPuts--;
                        return Json("""{"errors":[],"data":[]}""");
                    }

                    var id = path.Split('/').Last();
                    using var document = JsonDocument.Parse(body);
                    var root = document.RootElement;

                    lock (Lights)
                    {
                        var light = Lights[id];

                        if (root.TryGetProperty("on", out var on))
                            light = light with { IsOn = on.GetProperty("on").GetBoolean() };

                        if (root.TryGetProperty("color_temperature", out var temperature))
                            light = light with
                            {
                                Mirek = temperature.GetProperty("mirek").GetInt32(),
                            };

                        if (root.TryGetProperty("dimming", out var dimming))
                            light = light with
                            {
                                Brightness = dimming.GetProperty("brightness").GetDouble(),
                            };

                        Lights[id] = light;
                    }

                    return Json("""{"errors":[],"data":[]}""");
                }
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static string ToJson(string id, Light light) =>
            JsonSerializer.Serialize(
                new Dictionary<string, object?>
                {
                    ["id"] = id,
                    ["owner"] = new Dictionary<string, object?>
                    {
                        ["rid"] = "device-" + id,
                        ["rtype"] = "device",
                    },
                    ["on"] = new Dictionary<string, object?> { ["on"] = light.IsOn },
                    ["dimming"] = new Dictionary<string, object?>
                    {
                        ["brightness"] = light.Brightness,
                    },
                    ["color_temperature"] = new Dictionary<string, object?>
                    {
                        ["mirek"] = light.Mirek,
                        ["mirek_valid"] = light.Mirek is not null,
                        ["mirek_schema"] = new Dictionary<string, object?>
                        {
                            ["mirek_minimum"] = 153,
                            ["mirek_maximum"] = 454,
                        },
                    },
                }
            );

        private static HttpResponseMessage Json(string json) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
    }

    // Connected to the bridge, with the living room chosen, and a cycle that's warm all day
    private SettingsService CreateSettings(bool isEnabled = true, bool isPaired = true)
    {
        var settings = new SettingsService(Path.Combine(_directoryPath, "Settings.json"));
        settings.Load();

        var warm = new ColorConfiguration(4500, 0.6);
        settings.DayConfiguration = warm;
        settings.EveningConfiguration = warm;
        settings.NightConfiguration = warm;

        if (isPaired)
        {
            settings.HueBridgeAddress = Address;
            settings.HueProtectedAppKey = SecretProtection.Protect("app-key");
            settings.HueCertificateHash = Certificate;
        }

        settings.HueRoomIds = ["room-1"];
        settings.IsHueEnabled = isEnabled;

        return settings;
    }

    private (HueService Service, FakeBridge Bridge) CreateService(SettingsService settings)
    {
        var bridge = new FakeBridge
        {
            Lights =
            {
                ["lamp"] = new FakeBridge.Light(true, 200, 90),
                ["ceiling"] = new FakeBridge.Light(false, 200, 90),
            },
        };

        var service = new HueService(settings, bridge.SendAsync, () => _now);
        bridge.Service = service;
        service.Start();

        return (service, bridge);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
            await Task.Delay(10);

        condition().Should().BeTrue();
    }

    [Fact]
    public async Task I_get_hue_lights_that_are_on_following_the_cycle()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;

        // Act
        await service.PollAsync();

        // Assert
        service.IsFollowing.Should().BeTrue();
        service.Status.Should().Be(HueStatus.Following);
        service.FollowedLightCount.Should().Be(1);
        service.Rooms.Should().ContainSingle().Which.Name.Should().Be("Living room");

        bridge.Lights["lamp"].Mirek.Should().Be(387);
        bridge.Lights["lamp"].Brightness.Should().Be(90, "brightness isn't followed by default");

        // Never turned on
        bridge.Lights["ceiling"].Should().Be(new FakeBridge.Light(false, 200, 90));
        bridge.Puts.Should().ContainSingle().Which.Id.Should().Be("lamp");
        bridge.Requests.Should().OnlyContain(r => r.Key == null || r.Key == "app-key");
    }

    [Fact]
    public async Task I_get_hue_brightness_followed_when_i_choose_to()
    {
        // Arrange
        var settings = CreateSettings();
        settings.IsHueBrightnessFollowed = true;
        var (service, bridge) = CreateService(settings);
        using var _ = service;

        // Act
        await service.PollAsync();

        // Assert
        bridge.Lights["lamp"].Should().Be(new FakeBridge.Light(true, 387, 60));
    }

    [Fact]
    public async Task I_do_not_get_anything_running_or_sent_while_hue_control_is_off()
    {
        // Arrange
        var settings = CreateSettings(isEnabled: false);
        var (service, bridge) = CreateService(settings);
        using var _ = service;

        // Act
        await service.PollAsync();
        await service.RefreshRoomsAsync();

        // Assert
        service.IsFollowing.Should().BeFalse();
        service.Status.Should().Be(HueStatus.Off);
        bridge.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task I_get_hue_lights_put_back_when_i_turn_hue_control_off()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();

        // Act
        settings.IsHueEnabled = false;

        // Assert
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 200);
        service.IsFollowing.Should().BeFalse();
        service.Status.Should().Be(HueStatus.Off);
        bridge.Lights["ceiling"].IsOn.Should().BeFalse();
    }

    [Fact]
    public async Task I_get_hue_lights_put_back_while_afterglow_is_turned_off()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();

        // Act
        service.SetCycleEnabled(false);

        // Assert
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 200);
        service.Status.Should().Be(HueStatus.Paused);

        // And they follow again once it's back on
        service.SetCycleEnabled(true);
        _now += TimeSpan.FromSeconds(10);
        await service.PollAsync();
        bridge.Lights["lamp"].Mirek.Should().Be(387);
    }

    [Fact]
    public async Task I_get_hue_lights_put_back_right_away_when_afterglow_closes()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        await service.PollAsync();

        // Act
        service.Dispose();

        // Assert
        bridge.Lights["lamp"].Mirek.Should().Be(200);
    }

    [Fact]
    public async Task I_do_not_get_a_hue_light_i_adjusted_myself_changed_or_put_back()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();

        // Act: set to a cool white in the Hue app
        bridge.Lights["lamp"] = bridge.Lights["lamp"] with
        {
            Mirek = 160,
        };
        _now += TimeSpan.FromSeconds(10);
        await service.PollAsync();
        settings.IsHueEnabled = false;
        await Task.Delay(100);

        // Assert
        bridge.Lights["lamp"].Mirek.Should().Be(160);
        bridge.Puts.Should().ContainSingle();
    }

    [Fact]
    public async Task I_get_hue_lights_put_back_when_i_no_longer_choose_their_room()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();

        // Act
        settings.HueRoomIds = [];

        // Assert
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 200);
        service.Status.Should().Be(HueStatus.NoRooms);
    }

    [Fact]
    public async Task I_am_told_when_the_hue_bridge_no_longer_accepts_afterglow()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        bridge.ExpectedKey = "another-key";

        // Act
        await service.PollAsync();

        // Assert
        service.Status.Should().Be(HueStatus.Unauthorized);
        service.IsFollowing.Should().BeFalse();
    }

    [Fact]
    public async Task I_am_told_when_something_else_answers_at_the_hue_bridge_address()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        bridge.PresentedCertificate = "0BAD";

        // Act
        await service.PollAsync();

        // Assert
        service.Status.Should().Be(HueStatus.CertificateChanged);
        service.IsFollowing.Should().BeFalse();
        bridge.Puts.Should().BeEmpty();
    }

    [Fact]
    public async Task I_am_told_when_the_hue_bridge_cannot_be_reached_and_it_carries_on_afterwards()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        bridge.IsReachable = false;

        // Act
        await service.PollAsync();
        var status = service.Status;

        bridge.IsReachable = true;
        _now += TimeSpan.FromMinutes(1);
        await service.PollAsync();

        // Assert
        status.Should().Be(HueStatus.Unreachable);
        service.Status.Should().Be(HueStatus.Following);
        bridge.Lights["lamp"].Mirek.Should().Be(387);
    }

    [Fact]
    public async Task I_can_connect_to_my_hue_bridge_by_pressing_its_button()
    {
        // Arrange
        var settings = CreateSettings(isPaired: false);
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        bridge.ButtonPressedAfter = 1;
        var statuses = new List<HueStatus>();
        service.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(HueService.Status))
                statuses.Add(service.Status);
        };

        // Act
        await service.ConnectAsync();
        await service.PollAsync();

        // Assert
        statuses.Should().ContainInOrder(HueStatus.Searching, HueStatus.WaitingForButton);
        settings.HueBridgeAddress.Should().Be(Address);
        settings.HueBridgeId.Should().Be("001788FFFE000001");
        settings.HueCertificateHash.Should().Be(Certificate);
        SecretProtection.TryUnprotect(settings.HueProtectedAppKey).Should().Be("app-key");
        settings.HueProtectedAppKey.Should().NotContain("app-key");

        service.IsPaired.Should().BeTrue();
        service.Status.Should().Be(HueStatus.Following);
        bridge.Lights["lamp"].Mirek.Should().Be(387);
    }

    [Fact]
    public async Task I_can_stop_waiting_for_the_hue_bridge_button()
    {
        // Arrange
        var settings = CreateSettings(isPaired: false);
        settings.HueBridgeAddress = Address;
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        bridge.ButtonPressedAfter = int.MaxValue;

        // Act
        var connecting = service.ConnectAsync();
        service.IsConnecting.Should().BeTrue();
        service.CancelConnect();
        await connecting;

        // Assert
        service.IsConnecting.Should().BeFalse();
        service.IsPaired.Should().BeFalse();
        service.Status.Should().Be(HueStatus.NotConnected);
    }

    [Theory]
    [InlineData("192.168.1.20", "192.168.1.20")]
    [InlineData(" 192.168.1.20/ ", "192.168.1.20")]
    [InlineData("https://192.168.1.20/", "192.168.1.20")]
    [InlineData("hue-bridge.local:8443", "hue-bridge.local:8443")]
    [InlineData("", null)]
    [InlineData("192.168.1.20/api", null)]
    [InlineData("not an address", null)]
    public void I_can_enter_my_hue_bridge_address_in_several_ways(string address, string? expected)
    {
        // Act & assert
        HueService.TryNormalizeAddress(address).Should().Be(expected);
    }

    [Fact]
    public async Task I_get_hue_lights_following_what_the_screen_shows_including_the_preview()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;

        var target = new HueTarget(new ColorConfiguration(6500, 1), false, false);
        service.SetTargetSource(() => target);
        service.SetCycleEnabled(false);
        await service.PollAsync();
        bridge.Puts.Should().BeEmpty("Afterglow is off");

        // Act
        target = new HueTarget(new ColorConfiguration(1900, 1), false, true);
        service.NotifyTargetChanged();
        await service.PollAsync();

        // Assert
        service.IsFollowing.Should().BeTrue("the preview plays even while Afterglow is off");
        bridge.Lights["lamp"].Mirek.Should().Be(454);

        // And the lights go back once it's over
        target = target with
        {
            IsPreview = false,
        };
        service.NotifyTargetChanged();
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 200);
        service.Status.Should().Be(HueStatus.Paused);
    }

    [Fact]
    public async Task I_can_choose_whether_hue_lights_pause_along_with_the_screen()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;

        var target = new HueTarget(new ColorConfiguration(4500, 1), true, false);
        service.SetTargetSource(() => target);
        await service.PollAsync();
        bridge
            .Lights["lamp"]
            .Mirek.Should()
            .Be(387, "lights don't pause with the screen by default");

        // Act
        settings.IsHuePausedWithScreen = true;

        // Assert
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 200);
        service.Status.Should().Be(HueStatus.PausedWithScreen);

        // And they follow again once the screen does
        target = target with
        {
            IsScreenPaused = false,
        };
        service.NotifyTargetChanged();
        _now += TimeSpan.FromSeconds(10);
        await service.PollAsync();
        bridge.Lights["lamp"].Mirek.Should().Be(387);
    }

    [Fact]
    public async Task I_get_hue_lights_set_even_when_the_bridge_is_busy()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        bridge.BusyPuts = 1;

        // Act
        await service.PollAsync();

        // Assert
        bridge.Lights["lamp"].Mirek.Should().Be(387);
        service.FollowedLightCount.Should().Be(1);
    }

    [Fact]
    public async Task I_get_hue_lights_set_again_when_the_bridge_loses_what_it_was_sent()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        bridge.DroppedPuts = 1;

        await service.PollAsync();
        bridge.Lights["lamp"].Mirek.Should().Be(200, "the bridge lost the command");

        // Act
        _now += TimeSpan.FromSeconds(10);
        await service.PollAsync();

        // Assert
        bridge.Lights["lamp"].Mirek.Should().Be(387);
        service.FollowedLightCount.Should().Be(1);
        bridge.Puts.Should().HaveCount(2, "only the light that didn't change is sent again");
    }

    [Fact]
    public async Task I_can_leave_out_specific_hue_lights()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();
        bridge.Lights["lamp"].Mirek.Should().Be(387);

        // Act
        settings.HueExcludedLightIds = ["lamp"];
        _now += TimeSpan.FromSeconds(10);
        await service.PollAsync();

        // Assert
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 200);
        service.FollowedLightCount.Should().Be(0);
        service
            .Lights.Should()
            .ContainSingle(l => l.Id == "lamp")
            .Which.IsExcluded.Should()
            .BeTrue();

        // And it follows again once it's chosen again
        settings.HueExcludedLightIds = [];
        _now += TimeSpan.FromSeconds(10);
        await service.PollAsync();
        bridge.Lights["lamp"].Mirek.Should().Be(387);
        service
            .Lights.Should()
            .ContainSingle(l => l.Id == "lamp")
            .Which.State.Should()
            .Be(HueLightState.Following);
    }

    [Fact]
    public void I_keep_my_hue_bridge_connected_when_i_reset_all_settings()
    {
        // Arrange
        var settings = CreateSettings();
        settings.LightWarmth = 3;
        settings.LightDimming = 2;
        settings.HueBridgeId = "001788FFFE000001";

        // Act
        settings.Reset();

        // Assert
        settings.HueBridgeAddress.Should().Be(Address);
        settings.HueBridgeId.Should().Be("001788FFFE000001");
        settings.HueCertificateHash.Should().Be(Certificate);
        SecretProtection.TryUnprotect(settings.HueProtectedAppKey).Should().Be("app-key");

        // Everything else goes back to how it was
        settings.IsHueEnabled.Should().BeFalse();
        settings.HueRoomIds.Should().BeNull();
        settings.LightWarmth.Should().Be(HueLightController.DefaultWarmth);
        settings.LightDimming.Should().Be(HueLightController.DefaultDimming);
    }

    [Fact]
    public async Task I_can_disconnect_from_my_hue_bridge()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();
        bridge.Lights["lamp"].Mirek.Should().Be(387);

        // Act
        service.Disconnect();

        // Assert
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 200);
        service.IsPaired.Should().BeFalse();
        service.IsFollowing.Should().BeFalse();
        service.Status.Should().Be(HueStatus.NotConnected);
        settings.HueBridgeAddress.Should().Be(Address, "it's kept, to connect to it again");
    }

    [Fact]
    public async Task I_get_hue_lights_caught_up_after_i_change_a_setting_rather_than_left_alone()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();

        // Act (moved a slider, and the light shows something on the way)
        settings.LightWarmth = 3;
        _now += TimeSpan.FromSeconds(10);
        await service.PollAsync();
        bridge.Lights["lamp"] = bridge.Lights["lamp"] with { Mirek = 160 };
        _now += TimeSpan.FromSeconds(10);
        await service.PollAsync();

        // Assert
        service
            .Lights.Should()
            .ContainSingle(l => l.Id == "lamp")
            .Which.State.Should()
            .NotBe(HueLightState.LeftAlone);
        bridge.Lights["lamp"].Mirek.Should().NotBe(160);
    }

    [Fact]
    public async Task I_get_hue_lights_left_alone_when_i_change_them_while_afterglow_syncs_in_the_background()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();
        _now += TimeSpan.FromMinutes(1);
        await service.PollAsync();

        // Act (changed in the Hue app)
        bridge.Lights["lamp"] = bridge.Lights["lamp"] with
        {
            Mirek = 160,
        };
        _now += TimeSpan.FromSeconds(10);
        await service.PollAsync();

        // Assert
        service
            .Lights.Should()
            .ContainSingle(l => l.Id == "lamp")
            .Which.State.Should()
            .Be(HueLightState.LeftAlone);
        bridge.Lights["lamp"].Mirek.Should().Be(160);
    }

    [Fact]
    public async Task I_can_pause_hue_lights_for_a_while_from_the_tray()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();
        bridge.Lights["lamp"].Mirek.Should().Be(387);

        // Act
        service.Pause(TimeSpan.FromHours(1));

        // Assert
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 200);
        service.IsPausedByUser.Should().BeTrue();
        service.CanPause.Should().BeFalse();
        service.Status.Should().Be(HueStatus.PausedByUser);
        service.PausedUntil.Should().Be(_now + TimeSpan.FromHours(1));

        // And they follow again once the time is up
        _now += TimeSpan.FromHours(2);
        service.SetCycleEnabled(false);
        service.SetCycleEnabled(true);
        await service.PollAsync();

        service.IsPausedByUser.Should().BeFalse();
        bridge.Lights["lamp"].Mirek.Should().Be(387);
    }

    [Fact]
    public async Task I_can_pause_hue_lights_until_i_resume_them()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();

        // Act
        service.Pause();
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 200);
        _now += TimeSpan.FromDays(1);
        service.SetCycleEnabled(false);
        service.SetCycleEnabled(true);
        service.IsPausedByUser.Should().BeTrue("it lasts until it's resumed");

        service.Resume();
        await service.PollAsync();

        // Assert
        service.IsPausedByUser.Should().BeFalse();
        bridge.Lights["lamp"].Mirek.Should().Be(387);
    }

    [Fact]
    public void I_only_get_hue_lights_offered_for_pausing_while_hue_control_is_on_and_connected()
    {
        // Act & assert
        var (connected, _) = CreateService(CreateSettings());
        using (connected)
            connected.IsAvailable.Should().BeTrue();

        var (off, _) = CreateService(CreateSettings(isEnabled: false));
        using (off)
            off.IsAvailable.Should().BeFalse();

        var (notConnected, _) = CreateService(CreateSettings(isPaired: false));
        using (notConnected)
            notConnected.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task I_can_turn_on_hue_lights_and_get_them_turned_off_again_when_afterglow_lets_go()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();
        bridge.Lights["ceiling"].IsOn.Should().BeFalse();

        // Act
        service.TurnOnLights();
        await service.PollAsync();

        // Assert (turned on straight to the cycle)
        bridge.Lights["ceiling"].Should().Be(new FakeBridge.Light(true, 387, 90));

        // And off again once Afterglow lets go of it
        settings.IsHueEnabled = false;
        await WaitForAsync(() => !bridge.Lights["ceiling"].IsOn);
        bridge.Lights["lamp"].Should().Be(new FakeBridge.Light(true, 200, 90), "it was on already");
    }

    [Fact]
    public async Task I_get_hue_lights_afterglow_turned_on_left_on_in_a_neutral_white_while_paused()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();
        service.TurnOnLights();
        await service.PollAsync();

        // Act
        service.Pause();

        // Assert
        await WaitForAsync(() => bridge.Lights["ceiling"].Mirek != 387);
        bridge
            .Lights["ceiling"]
            .Should()
            .Be(new FakeBridge.Light(true, HueLightController.NeutralMirek, 100));
        bridge.Lights["lamp"].Mirek.Should().Be(200, "it goes back to how it was");
    }

    [Fact]
    public async Task I_can_still_see_my_hue_lights_while_they_are_paused()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, _) = CreateService(settings);
        using var __ = service;
        await service.PollAsync();

        // Act
        service.Pause();

        // Assert (still listed, to be chosen meanwhile, but no longer following)
        service.Lights.Should().HaveCount(2);
        service.Lights.Should().OnlyContain(l => l.State == HueLightState.NotFollowed);
        service.IsFollowingLights.Should().BeFalse();

        // And no longer listed once Hue control is off
        settings.IsHueEnabled = false;
        service.Lights.Should().BeEmpty();
    }

    [Fact]
    public async Task I_can_get_hue_lights_set_to_a_look_of_my_choosing_while_paused()
    {
        // Arrange
        var settings = CreateSettings();
        settings.HuePauseAction = HuePauseAction.Custom;
        settings.HuePauseTemperature = 2500;
        settings.HuePauseBrightness = 0.4;
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();

        // Act
        service.Pause();

        // Assert
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 400);
        bridge.Lights["lamp"].Brightness.Should().Be(40);

        // And once following again, it still goes back to how it was before Afterglow
        service.Resume();
        _now += TimeSpan.FromSeconds(10);
        await service.PollAsync();
        bridge.Lights["lamp"].Mirek.Should().Be(387);

        settings.IsHueEnabled = false;
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 200);
        bridge.Lights["lamp"].Brightness.Should().Be(90);
    }

    [Fact]
    public async Task I_can_preview_a_look_on_hue_lights_while_i_pick_it()
    {
        // Arrange
        var settings = CreateSettings();
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();

        // Act
        service.PreviewLook(2500, 0.4);
        await service.PollAsync(true);

        // Assert
        bridge.Lights["lamp"].Should().Be(new FakeBridge.Light(true, 400, 40));

        // And back to the cycle once the slider is let go of (including what isn't followed)
        service.EndLookPreview();
        _now += TimeSpan.FromSeconds(1);
        await service.PollAsync(true);
        bridge.Lights["lamp"].Should().Be(new FakeBridge.Light(true, 387, 90));
        service
            .Lights.Should()
            .ContainSingle(l => l.Id == "lamp")
            .Which.State.Should()
            .NotBe(HueLightState.LeftAlone);
    }

    [Fact]
    public async Task I_can_get_all_hue_lights_turned_on_when_afterglow_starts()
    {
        // Arrange
        var settings = CreateSettings();
        settings.IsHueTurnedOnAtStart = true;

        // Act
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();

        // Assert
        bridge.Lights["ceiling"].IsOn.Should().BeTrue();
    }

    [Fact]
    public async Task I_can_get_hue_lights_left_as_they_are_while_paused()
    {
        // Arrange
        var settings = CreateSettings();
        settings.HuePauseAction = HuePauseAction.Leave;
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();

        // Act
        service.Pause();
        await Task.Delay(50);

        // Assert
        bridge.Lights["lamp"].Mirek.Should().Be(387);

        // And following carries on afterwards (still going back to how it was before Afterglow)
        service.Resume();
        _now += TimeSpan.FromSeconds(10);
        await service.PollAsync();
        service.FollowedLightCount.Should().Be(1);

        settings.IsHueEnabled = false;
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 200);
    }

    [Fact]
    public async Task I_can_get_hue_lights_turned_off_while_paused_and_back_on_afterwards()
    {
        // Arrange
        var settings = CreateSettings();
        settings.HuePauseAction = HuePauseAction.TurnOff;
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();

        // Act
        service.Pause();

        // Assert
        await WaitForAsync(() => !bridge.Lights["lamp"].IsOn);
        bridge.Lights["ceiling"].IsOn.Should().BeFalse("it was off already");

        // And back on afterwards (only the one that was on), following the cycle
        service.Resume();
        _now += TimeSpan.FromSeconds(10);
        await service.PollAsync();
        bridge.Lights["lamp"].Should().Be(new FakeBridge.Light(true, 387, 90));
        bridge.Lights["ceiling"].IsOn.Should().BeFalse();

        // And it still goes back to how it was before Afterglow (rather than off)
        settings.IsHueEnabled = false;
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 200);
        bridge.Lights["lamp"].IsOn.Should().BeTrue();
    }

    [Fact]
    public void I_can_reset_only_the_smart_lights_settings_and_stay_connected()
    {
        // Arrange
        var settings = CreateSettings();
        settings.LightWarmth = 3;
        settings.IsHueTurnedOnAtStart = true;
        settings.DayConfiguration = new ColorConfiguration(5000, 0.9);
        using var localization = new Afterglow.Localization.LocalizationManager(settings);
        using var hueService = new HueService(settings);
        using var tab =
            new Afterglow.ViewModels.Components.Settings.SmartLightsSettingsTabViewModel(
                settings,
                localization,
                hueService
            );

        // Act
        tab.ResetTabCommand.Execute(null);

        // Assert
        settings.LightWarmth.Should().Be(HueLightController.DefaultWarmth);
        settings.IsHueTurnedOnAtStart.Should().BeFalse();
        settings.IsHueEnabled.Should().BeFalse();
        settings.HueRoomIds.Should().BeNull();

        // The connection to the bridge, and the other tabs' settings, are kept
        settings.HueBridgeAddress.Should().Be(Address);
        settings.HueCertificateHash.Should().Be(Certificate);
        SecretProtection.TryUnprotect(settings.HueProtectedAppKey).Should().Be("app-key");
        settings.DayConfiguration.Should().Be(new ColorConfiguration(5000, 0.9));
        tab.ResetTabButtonText.Should().Contain(tab.DisplayName);
    }

    [Fact]
    public void I_can_reset_only_the_cycle_settings_without_touching_my_smart_lights()
    {
        // Arrange
        var settings = CreateSettings();
        settings.LightWarmth = 3;
        settings.DayConfiguration = new ColorConfiguration(5000, 0.9);
        using var localization = new Afterglow.Localization.LocalizationManager(settings);
        using var tab = new Afterglow.ViewModels.Components.Settings.CycleSettingsTabViewModel(
            settings,
            localization
        );

        // Act
        tab.ResetTabCommand.Execute(null);

        // Assert
        settings.DayConfiguration.Should().NotBe(new ColorConfiguration(5000, 0.9));
        settings.LightWarmth.Should().Be(3);
        settings.IsHueEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task I_can_change_the_look_of_hue_lights_while_they_are_paused_to_it()
    {
        // Arrange
        var settings = CreateSettings();
        settings.HuePauseAction = HuePauseAction.Custom;
        settings.HuePauseTemperature = 2500;
        settings.HuePauseBrightness = 0.4;
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();
        service.Pause();
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 400);

        // Act
        settings.HuePauseTemperature = 2000;

        // Assert (straight away, rather than on the next pause)
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek == 454);
        bridge.Lights["lamp"].Brightness.Should().Be(40);
    }

    [Fact]
    public async Task I_get_hue_lights_paused_to_a_look_turned_off_when_the_pc_shuts_down()
    {
        // Arrange
        var settings = CreateSettings();
        settings.HuePauseAction = HuePauseAction.Custom;
        settings.HueShutdownAction = HueShutdownAction.TurnOff;
        var (service, bridge) = CreateService(settings);
        using var _ = service;
        await service.PollAsync();
        service.Pause();
        await WaitForAsync(() => bridge.Lights["lamp"].Mirek != 387);

        // Act
        service.SimulateSessionEnding();

        // Assert
        bridge.Lights["lamp"].IsOn.Should().BeFalse();
    }

    [Fact]
    public void I_only_get_afterglow_talking_to_the_hue_bridge_it_first_connected_to()
    {
        // Act & assert
        HueService.IsCertificateAccepted("ABCD", null).Should().BeTrue();
        HueService.IsCertificateAccepted("ABCD", "abcd").Should().BeTrue();
        HueService.IsCertificateAccepted("ABCD", "1234").Should().BeFalse();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directoryPath, true);
        }
        catch
        {
            // Ignore
        }
    }
}
