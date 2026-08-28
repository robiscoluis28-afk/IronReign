using CommunityToolkit.Mvvm.ComponentModel;
using IronReign.ViewModels;

namespace IronReign.Views;

public partial class OnboardingProfilePage : ContentPage
{
    public OnboardingProfilePage(OnboardingProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

    }
}
