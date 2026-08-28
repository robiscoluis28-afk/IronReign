using IronReign.ViewModels;

namespace IronReign.Views;

public partial class RoutinesPage : ContentPage
{
    public RoutinesViewModel VM => (RoutinesViewModel)BindingContext;

    public RoutinesPage(RoutinesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await VM.LoadAsync();
    }
}