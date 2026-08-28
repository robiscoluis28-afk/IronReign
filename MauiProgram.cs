using CommunityToolkit.Maui;
using Firebase.Auth;
using Firebase.Auth.Providers;
using IronReign.Data;
using IronReign.Services;
using IronReign.ViewModels;
using IronReign.Views;
using Microsoft.Extensions.Logging;

namespace IronReign;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var firebaseApiKey = "AIzaSyC26sfWX2vBfhBSY7pEQet8Np24sTpFx_k";
        var firebaseAuthDomain = "ironreign-f618e.firebaseapp.com";

        var firebaseConfig = new FirebaseAuthConfig
        {
            ApiKey = firebaseApiKey,
            AuthDomain = firebaseAuthDomain,
            Providers = new FirebaseAuthProvider[]
            {
                new EmailProvider()
            }
        };

        builder.Services.AddSingleton(firebaseConfig);
        builder.Services.AddSingleton(new FirebaseAuthClient(firebaseConfig));
        builder.Services.AddSingleton<AuthService>();

        builder.Services.AddSingleton<AppDatabase>();
        builder.Services.AddSingleton<UserSessionService>();
        builder.Services.AddSingleton<WorkoutSessionState>();
        builder.Services.AddSingleton<CloudBackupService>();

        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<StartupViewModel>();
        builder.Services.AddTransient<StartupPage>();
        builder.Services.AddTransient<OnboardingProfileViewModel>();
        builder.Services.AddTransient<OnboardingProfilePage>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();

        builder.Services.AddTransient<WorkoutRunnerViewModel>();
        builder.Services.AddTransient<WorkoutRunnerPage>();

        builder.Services.AddTransient<RoutinesViewModel>();
        builder.Services.AddTransient<RoutinesPage>();

        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<HistoryPage>();

        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<RoutineEditorViewModel>();
        builder.Services.AddTransient<RoutineEditorPage>();
        



        return builder.Build();
    }
}