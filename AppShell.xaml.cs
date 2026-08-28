using IronReign.Views;

namespace IronReign;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(
            nameof(RoutineEditorPage),
            typeof(RoutineEditorPage));
    }
}