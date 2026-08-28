using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronReign.Services;

namespace IronReign.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly UserSessionService _userSessionService;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public RegisterViewModel(AuthService authService, UserSessionService userSessionService)
    {
        _authService = authService;
        _userSessionService = userSessionService;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(DisplayName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Completa todos los campos.";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Las contraseñas no coinciden.";
            return;
        }

        try
        {
            IsBusy = true;

            var (success, error) = await _authService.RegisterAsync(Email.Trim(), Password, DisplayName.Trim());

            if (!success)
            {
                ErrorMessage = error ?? "No se pudo crear la cuenta.";
                return;
            }

            var uid = _authService.CurrentUserId;
            var email = _authService.CurrentUserEmail ?? Email.Trim();

            if (!string.IsNullOrWhiteSpace(uid))
            {
                await _userSessionService.EnsureLocalProfileForFirebaseUserAsync(uid, email, DisplayName.Trim());
            }

            await Shell.Current.GoToAsync("//dashboard");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}