using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronReign.Data;
using IronReign.Models;
using IronReign.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IronReign.ViewModels;

public partial class OnboardingProfileViewModel : ObservableObject
{
    private readonly AppDatabase _database;
    private readonly IServiceProvider _serviceProvider;
    private readonly UserSessionService _userSessionService;

    [ObservableProperty]
    public partial string FirstName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AgeText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedSex { get; set; } = "Hombre";

    [ObservableProperty]
    public partial string SelectedWeightUnit { get; set; } = "kg";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public bool IsMaleSelected => SelectedSex == "Hombre";
    public bool IsFemaleSelected => SelectedSex == "Mujer";
    public bool IsOtherSelected => SelectedSex == "Otro";

    public bool IsKgSelected => SelectedWeightUnit == "kg";
    public bool IsLbSelected => SelectedWeightUnit == "lb";

    public OnboardingProfileViewModel(
        AppDatabase database,
        IServiceProvider serviceProvider,
        UserSessionService userSessionService)
    {
        _database = database;
        _serviceProvider = serviceProvider;
        _userSessionService = userSessionService;
    }

    partial void OnSelectedSexChanged(string value)
    {
        OnPropertyChanged(nameof(IsMaleSelected));
        OnPropertyChanged(nameof(IsFemaleSelected));
        OnPropertyChanged(nameof(IsOtherSelected));
    }

    partial void OnSelectedWeightUnitChanged(string value)
    {
        OnPropertyChanged(nameof(IsKgSelected));
        OnPropertyChanged(nameof(IsLbSelected));
    }

    [RelayCommand]
    private void SelectSex(string sex)
    {
        if (!string.IsNullOrWhiteSpace(sex))
            SelectedSex = sex;
    }

    [RelayCommand]
    private void SelectWeightUnit(string unit)
    {
        if (!string.IsNullOrWhiteSpace(unit))
            SelectedWeightUnit = unit;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            ErrorMessage = "El nombre es obligatorio.";
            return;
        }

        if (!int.TryParse(AgeText, out int age) || age <= 0 || age > 120)
        {
            ErrorMessage = "Introduce una edad válida.";
            return;
        }

        try
        {
            IsBusy = true;

            var existingUsers = await _database.GetUserProfilesAsync();

            foreach (var user in existingUsers)
            {
                user.IsActive = false;
                await _database.SaveUserProfileAsync(user);
            }

            var profile = new UserProfile
            {
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Age = age,
                Sex = SelectedSex,
                PreferredWeightUnit = SelectedWeightUnit,
                PreferredTheme = "dark",
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            await _database.SaveUserProfileAsync(profile);
            await _userSessionService.LoadActiveUserAsync();

            if (Application.Current?.Windows.Count > 0)
            {
                var appShell = _serviceProvider.GetRequiredService<AppShell>();
                Application.Current.Windows[0].Page = appShell;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al guardar el perfil: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}