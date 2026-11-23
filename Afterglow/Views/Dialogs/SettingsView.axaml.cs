using Afterglow.Framework;
using Afterglow.ViewModels.Dialogs;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace Afterglow.Views.Dialogs;

public partial class SettingsView : UserControl<SettingsViewModel>
{
    private const string CompactTabsClass = "compact";

    private bool _isFittingTabs;

    public SettingsView()
    {
        InitializeComponent();

        // Every tab is labelled while they all fit next to the back button, and otherwise only the
        // active one is (e.g. in languages with longer names). Checked again whenever the header
        // or the tabs change size (e.g. when the language changes).
        HeaderGrid.SizeChanged += (_, _) => QueueFitTabs();
        TabsBorder.SizeChanged += (_, _) => QueueFitTabs();
    }

    private void QueueFitTabs() => Dispatcher.UIThread.Post(FitTabs, DispatcherPriority.Background);

    private void FitTabs()
    {
        if (_isFittingTabs || HeaderGrid.Bounds.Width <= 0)
            return;

        _isFittingTabs = true;
        try
        {
            // How wide the tabs are with every one labelled
            TabsBorder.Classes.Remove(CompactTabsClass);
            TabsBorder.Measure(Size.Infinity);
            var labelledWidth = TabsBorder.DesiredSize.Width;

            // The outer columns are equal (keeping the tabs centered), so the back button takes
            // up its width on both sides
            var availableWidth = HeaderGrid.Bounds.Width - 2 * (BackButton.Bounds.Width + 6);

            TabsBorder.Classes.Set(CompactTabsClass, labelledWidth > availableWidth);
        }
        finally
        {
            _isFittingTabs = false;
        }
    }

    // The settings page covers the main window's title bar, so it needs to support dragging too
    private void HeaderBorder_OnPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
            window.BeginMoveDrag(args);
    }
}
