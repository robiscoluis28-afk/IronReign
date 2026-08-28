using IronReign.ViewModels;

namespace IronReign.Views;

public partial class WorkoutRunnerPage : ContentPage
{
    private readonly WorkoutRunnerViewModel viewModel;
    private IDispatcherTimer? timer;

    public WorkoutRunnerPage(WorkoutRunnerViewModel viewModel)
    {
        InitializeComponent();

        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await viewModel.LoadAsync();
        StartTimer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        StopTimer();
    }

    private void StartTimer()
    {
        if (timer is not null)
            return;

        timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += (_, _) => viewModel.Tick();
        timer.Start();
    }

    private void StopTimer()
    {
        if (timer is null)
            return;

        timer.Stop();
        timer = null;
    }
}