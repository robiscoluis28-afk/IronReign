using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace IronReign.ViewModels;

public partial class WorkoutRoutineListItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private bool isWorkoutActive;

    [ObservableProperty]
    private string elapsedTimeDisplay = "00:00:00";

    public ObservableCollection<WorkoutExerciseRunItemViewModel> Exercises { get; } = new();

    public IRelayCommand? StartWorkoutCommand { get; set; }

    public IRelayCommand? FinishWorkoutCommand { get; set; }

    public IRelayCommand? ToggleExpandCommand { get; set; }

    public void SetElapsed(int elapsedSeconds)
    {
        ElapsedTimeDisplay = TimeSpan.FromSeconds(elapsedSeconds).ToString(@"hh\:mm\:ss");
    }
}