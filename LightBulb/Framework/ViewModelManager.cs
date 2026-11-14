using System;
using LightBulb.Localization;
using LightBulb.ViewModels;
using LightBulb.ViewModels.Components;
using LightBulb.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace LightBulb.Framework;

public class ViewModelManager(IServiceProvider services, LocalizationManager localizationManager)
{
    public MainViewModel GetMainViewModel() => services.GetRequiredService<MainViewModel>();

    public DashboardViewModel GetDashboardViewModel() =>
        services.GetRequiredService<DashboardViewModel>();

    public MessageBoxViewModel GetMessageBoxViewModel(
        string title,
        string message,
        string? okButtonText,
        string? cancelButtonText
    )
    {
        var viewModel = services.GetRequiredService<MessageBoxViewModel>();

        viewModel.Title = title;
        viewModel.Message = message;
        viewModel.DefaultButtonText = okButtonText;
        viewModel.CancelButtonText = cancelButtonText;

        return viewModel;
    }

    public MessageBoxViewModel GetMessageBoxViewModel(string title, string message) =>
        GetMessageBoxViewModel(title, message, localizationManager.CloseButton, null);

    // Settings (and their tabs) are created anew every time they're opened, so they're created in
    // their own scope, which the caller disposes once they're closed
    public Owned<SettingsViewModel> CreateSettingsViewModel() =>
        CreateOwned<SettingsViewModel>(services);

    internal static Owned<T> CreateOwned<T>(IServiceProvider services)
        where T : notnull
    {
        var scope = services.CreateScope();

        try
        {
            return new Owned<T>(scope.ServiceProvider.GetRequiredService<T>(), scope);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }
}
