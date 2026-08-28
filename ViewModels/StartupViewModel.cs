using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronReign.Data;
using IronReign.Services;
using IronReign.Views;

namespace IronReign.ViewModels;

public partial class StartupViewModel : ObservableObject
{
    private readonly AppDatabase _database;
    private readonly AuthService _authService;
    private readonly UserSessionService _userSessionService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Comprobando sesión...";

    public StartupViewModel(
        AppDatabase database,
        AuthService authService,
        UserSessionService userSessionService,
        IServiceProvider serviceProvider)
    {
        _database = database;
        _authService = authService;
        _userSessionService = userSessionService;
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = "Comprobando sesión...";

            var hasValidSession = _authService.IsLoggedIn && !_authService.IsSessionExpired();
            var activeUser = hasValidSession ? await _userSessionService.LoadActiveUserAsync() : null;

            var appShell = _serviceProvider.GetRequiredService<AppShell>();

            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = appShell;

                if (activeUser is not null)
                    await Shell.Current.GoToAsync("//dashboard");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}