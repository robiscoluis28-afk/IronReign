using IronReign.ViewModels;

namespace IronReign.Views;

public partial class StartupPage : ContentPage
{
    private readonly StartupViewModel _viewModel;

    public StartupPage(StartupViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_viewModel.IsBusy)
            _viewModel.InitializeCommand.Execute(null);
    }
}