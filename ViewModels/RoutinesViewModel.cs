using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronReign.Data;
using IronReign.Models;
using IronReign.Services;
using System.Collections.ObjectModel;

namespace IronReign.ViewModels;

public partial class RoutinesViewModel : ObservableObject
{
    private readonly AppDatabase _database;
    private readonly UserSessionService _userSessionService;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string newRoutineName = string.Empty;

    [ObservableProperty]
    private string newRoutineNotes = string.Empty;

    [RelayCommand]
    private async Task EditRoutineAsync(RoutineTemplate? routine)
    {
        if (routine is null || routine.Id <= 0)
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(Views.RoutineEditorPage)}?RoutineId={routine.Id}");
    }

    public ObservableCollection<RoutineTemplate> Routines { get; } = new();

    public RoutinesViewModel(
        AppDatabase database,
        UserSessionService userSessionService)
    {
        _database = database;
        _userSessionService = userSessionService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            Routines.Clear();

            var activeUser = await _userSessionService.LoadActiveUserAsync();

            if (activeUser is null)
            {
                ErrorMessage = "No hay un usuario activo.";
                return;
            }

            var routines = await _database.GetRoutineTemplatesByUserAsync(activeUser.Id);

            foreach (var routine in routines.OrderBy(x => x.DisplayOrder))
                Routines.Add(routine);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task AddRoutineAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRoutineName))
        {
            ErrorMessage = "Escribe un nombre para la rutina.";
            return;
        }

        var activeUser = await _userSessionService.LoadActiveUserAsync();

        if (activeUser is null)
        {
            ErrorMessage = "No hay un usuario activo.";
            return;
        }

        var routine = new RoutineTemplate
        {
            UserProfileId = activeUser.Id,
            Name = NewRoutineName.Trim(),
            Notes = NewRoutineNotes?.Trim() ?? string.Empty
        };

        await _database.SaveRoutineTemplateAsync(routine);

        NewRoutineName = string.Empty;
        NewRoutineNotes = string.Empty;

        await LoadAsync();
    }

    [RelayCommand]
    public async Task DeleteRoutineAsync(RoutineTemplate routine)
    {
        if (routine is null)
            return;

        await _database.DeleteRoutineTemplateAsync(routine);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task MoveUpAsync(RoutineTemplate routine)
    {
        if (routine is null)
            return;

        var activeUser = await _userSessionService.LoadActiveUserAsync();

        if (activeUser is null)
            return;

        await _database.MoveRoutineUpAsync(activeUser.Id, routine.Id);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task MoveDownAsync(RoutineTemplate routine)
    {
        if (routine is null)
            return;

        var activeUser = await _userSessionService.LoadActiveUserAsync();

        if (activeUser is null)
            return;

        await _database.MoveRoutineDownAsync(activeUser.Id, routine.Id);
        await LoadAsync();
    }

    public async Task DeleteAllRoutinesForActiveUserAsync()
    {
        var activeUser = await _userSessionService.LoadActiveUserAsync();

        if (activeUser is null)
            return;

        var routines = await _database.GetRoutineTemplatesByUserAsync(activeUser.Id);

        foreach (var routine in routines)
            await _database.DeleteRoutineTemplateAsync(routine);

        await LoadAsync();
    }
}