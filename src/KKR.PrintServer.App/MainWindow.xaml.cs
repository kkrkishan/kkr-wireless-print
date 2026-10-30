using System;
using System.Windows;

namespace KKR.PrintServer.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Fit within current screen work area (excluding Windows Taskbar)
        // Accommodates 1366x768, 1080p with 125%/150% scaling, and high-DPI displays
        var workArea = SystemParameters.WorkArea;

        if (Height > workArea.Height - 30)
        {
            Height = Math.Max(460, workArea.Height - 40);
        }

        if (Width > workArea.Width - 30)
        {
            Width = Math.Max(720, workArea.Width - 40);
        }

        // Center within visible work area so title bar with minimize/maximize/close is always on-screen
        Top = Math.Max(workArea.Top + 10, workArea.Top + (workArea.Height - Height) / 2);
        Left = Math.Max(workArea.Left + 10, workArea.Left + (workArea.Width - Width) / 2);
    }
}
