using IronReign.ViewModels;

namespace IronReign.Views;

public partial class RoutineEditorPage : ContentPage
{
    private readonly RoutineEditorViewModel _viewModel;

    public RoutineEditorPage(RoutineEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private void OnDecreaseSetsClicked(object sender, EventArgs e)
    {
        var sets = int.TryParse(_viewModel.NewExercisePlannedSets, out var value) ? value : 1;
        _viewModel.NewExercisePlannedSets = Math.Max(1, sets - 1).ToString();
    }

    private void OnIncreaseSetsClicked(object sender, EventArgs e)
    {
        var sets = int.TryParse(_viewModel.NewExercisePlannedSets, out var value) ? value : 0;
        _viewModel.NewExercisePlannedSets = (sets + 1).ToString();
    }

    private void OnDecreaseRestClicked(object sender, EventArgs e)
    {
        var rest = int.TryParse(_viewModel.NewExerciseRestSeconds, out var value) ? value : 15;
        _viewModel.NewExerciseRestSeconds = Math.Max(0, rest - 15).ToString();
    }

    private void OnIncreaseRestClicked(object sender, EventArgs e)
    {
        var rest = int.TryParse(_viewModel.NewExerciseRestSeconds, out var value) ? value : 0;
        _viewModel.NewExerciseRestSeconds = (rest + 15).ToString();
    }
}