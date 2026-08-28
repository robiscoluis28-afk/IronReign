using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronReign.Services;

namespace IronReign.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly UserSessionService _userSessionService;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public LoginViewModel(AuthService authService, UserSessionService userSessionService)
    {
        _authService = authService;
        _userSessionService = userSessionService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Introduce tu correo y contraseña.";
            return;
        }

        try
        {
            IsBusy = true;

            var (success, error) = await _authService.LoginAsync(Email.Trim(), Password);

            if (!success)
            {
                ErrorMessage = error ?? "No se pudo iniciar sesión.";
                return;
            }

            var uid = _authService.CurrentUserId;
            var email = _authService.CurrentUserEmail ?? Email.Trim();
            var displayName = _authService.CurrentUserDisplayName;

            if (!string.IsNullOrWhiteSpace(uid))
            {
                await _userSessionService.EnsureLocalProfileForFirebaseUserAsync(uid, email, displayName);
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

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("///register");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error de navegación: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Introduce tu correo para restablecer la contraseña.";
            return;
        }

        var (success, error) = await _authService.ResetPasswordAsync(Email.Trim());

        ErrorMessage = success
            ? "Te hemos enviado un correo para restablecer tu contraseña."
            : error ?? "No se pudo enviar el correo.";
    }
}