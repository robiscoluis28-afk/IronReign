using IronReign.ViewModels;

namespace IronReign.Views;

public partial class HistoryPage : ContentPage
{
    public HistoryViewModel VM => (HistoryViewModel)BindingContext;

    public HistoryPage(HistoryViewModel viewModel)
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