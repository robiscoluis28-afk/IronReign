using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronReign.Data;
using IronReign.Models;
using IronReign.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.Reflection;

namespace IronReign.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppDatabase _database;
    private readonly UserSessionService _userSessionService;
    private readonly AuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private readonly CloudBackupService _cloudBackupService;

    private UserProfile? _activeUser;

    public ObservableCollection<string> WeightUnits { get; } = new()
    {
        "kg",
        "lb"
    };

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SuccessMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FullName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Age { get; set; }

    [ObservableProperty]
    public partial string Sex { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasActiveUser { get; set; }

    [ObservableProperty]
    public partial string PreferredWeightUnit { get; set; } = "kg";

    [ObservableProperty]
    public partial string AppVersion { get; set; } = string.Empty;

    public bool IsCloudBackupConfigured => _cloudBackupService.IsConfigured;

    public SettingsViewModel(
        AppDatabase database,
        UserSessionService userSessionService,
        AuthService authService,
        IServiceProvider serviceProvider,
        CloudBackupService cloudBackupService)
    {
        _database = database;
        _userSessionService = userSessionService;
        _authService = authService;
        _serviceProvider = serviceProvider;
        _cloudBackupService = cloudBackupService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            App.ApplyTheme("Dark");

            _activeUser = await _userSessionService.LoadActiveUserAsync();

            if (_activeUser is null)
            {
                HasActiveUser = false;
                FullName = string.Empty;
                Age = 0;
                Sex = string.Empty;
                PreferredWeightUnit = "kg";
            }
            else
            {
                HasActiveUser = true;
                FullName = BuildFullName(_activeUser.FirstName, _activeUser.LastName);
                Age = _activeUser.Age;
                Sex = _activeUser.Sex;
                PreferredWeightUnit = string.IsNullOrWhiteSpace(_activeUser.PreferredWeightUnit)
                    ? "kg"
                    : _activeUser.PreferredWeightUnit;
            }

            var version = AppInfo.Current.VersionString;
            var build = AppInfo.Current.BuildString;
            AppVersion = $"{version} ({build})";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cargar ajustes: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SavePreferencesAsync()
    {
        if (_activeUser is null)
        {
            ErrorMessage = "No hay perfil activo.";
            SuccessMessage = string.Empty;
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            _activeUser.PreferredWeightUnit = PreferredWeightUnit;

            await _database.SaveUserProfileAsync(_activeUser);

            SuccessMessage = "Preferencias guardadas correctamente.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron guardar las preferencias: {ex.Message}";
            SuccessMessage = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteHistoryAsync()
    {
        if (_activeUser is null)
        {
            ErrorMessage = "No hay un perfil activo.";
            return;
        }

        var confirmed = await Shell.Current.DisplayAlert(
            "Borrar historial",
            "Se eliminarán todos tus entrenos guardados. Esta acción no se puede deshacer.",
            "Borrar",
            "Cancelar");

        if (!confirmed)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            await _database.DeleteWorkoutSessionsByUserAsync(_activeUser.Id);

            SuccessMessage = "Historial borrado correctamente.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo borrar el historial: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteRoutinesAsync()
    {
        if (_activeUser is null)
        {
            ErrorMessage = "No hay un perfil activo.";
            return;
        }

        var confirmed = await Shell.Current.DisplayAlert(
            "Borrar rutinas",
            "Se eliminarán todas tus rutinas guardadas. Esta acción no se puede deshacer.",
            "Borrar",
            "Cancelar");

        if (!confirmed)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            await _database.DeleteRoutineTemplatesByUserAsync(_activeUser.Id);

            SuccessMessage = "Rutinas borradas correctamente.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron borrar las rutinas: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ResetAllDataAsync()
    {
        if (_activeUser is null)
        {
            ErrorMessage = "No hay un perfil activo.";
            return;
        }

        var confirmed = await Shell.Current.DisplayAlert(
            "Restablecer todos los datos",
            "Se borrarán todas tus rutinas y todo tu historial de entrenos. Tu perfil y tus preferencias se mantendrán. Esta acción no se puede deshacer.",
            "Restablecer",
            "Cancelar");

        if (!confirmed)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            await _database.ResetAllUserDataAsync(_activeUser.Id);

            SuccessMessage = "Todos los datos se han restablecido.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudieron restablecer los datos: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        if (_activeUser is null)
        {
            ErrorMessage = "No hay un perfil activo.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            var (success, error) = await _cloudBackupService.BackupAsync(_activeUser);

            if (success)
                SuccessMessage = "Copia de seguridad actualizada.";
            else
                ErrorMessage = error ?? "No se pudo sincronizar.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            await _userSessionService.ClearActiveUserAsync();
            _authService.Logout();

            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = _serviceProvider.GetRequiredService<AppShell>();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"No se pudo cerrar sesión: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string BuildFullName(string firstName, string lastName)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "Perfil sin nombre" : fullName;
    }
}