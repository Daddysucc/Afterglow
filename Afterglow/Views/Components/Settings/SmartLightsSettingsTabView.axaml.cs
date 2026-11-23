using Afterglow.Framework;
using Afterglow.ViewModels.Components.Settings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Afterglow.Views.Components.Settings;

public partial class SmartLightsSettingsTabView : UserControl<SmartLightsSettingsTabViewModel>
{
    public SmartLightsSettingsTabView()
    {
        InitializeComponent();

        // The custom looks for the Hue lights are shown on them only while one of their sliders is
        // held down (so that quickly moving a slider, or using the keyboard, leaves them alone)
        WatchLookSlider(HuePauseTemperatureSlider, true);
        WatchLookSlider(HuePauseBrightnessSlider, true);
        WatchLookSlider(HueShutdownTemperatureSlider, false);
        WatchLookSlider(HueShutdownBrightnessSlider, false);
    }

    private void WatchLookSlider(Slider slider, bool isPause)
    {
        // (the slider handles these itself, so they're watched even once handled)
        slider.AddHandler(
            PointerPressedEvent,
            (_, args) =>
            {
                if (args.GetCurrentPoint(slider).Properties.IsLeftButtonPressed)
                    DataContext.BeginHueLookPreview(isPause);
            },
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            true
        );

        slider.AddHandler(
            PointerReleasedEvent,
            (_, _) => DataContext.EndHueLookPreview(),
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            true
        );
    }
}
