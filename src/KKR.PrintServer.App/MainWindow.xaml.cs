using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using KKR.PrintServer.Core.Services;

namespace KKR.PrintServer.App;

public partial class MainWindow : Window
{
    private NotifyIcon? _notifyIcon;
    private bool _isExplicitExit;
    private bool _hasShownCloseTip;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnWindowLoaded;
        InitializeSystemTray();
    }

    private void InitializeSystemTray()
    {
        try
        {
            _notifyIcon = new NotifyIcon();

            // Try to load custom icon or use application icon
            System.Drawing.Icon? appIcon = null;
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(iconPath))
            {
                try { appIcon = new System.Drawing.Icon(iconPath); } catch { }
            }

            if (appIcon == null)
            {
                try
                {
                    string? currentExe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(currentExe) && File.Exists(currentExe))
                    {
                        appIcon = System.Drawing.Icon.ExtractAssociatedIcon(currentExe);
                    }
                }
                catch { }
            }

            _notifyIcon.Icon = appIcon ?? SystemIcons.Application;
            _notifyIcon.Text = "Print Ofo — Wireless Print Server";
            _notifyIcon.Visible = true;

            // Double click opens dashboard
            _notifyIcon.DoubleClick += (s, e) => RestoreWindow();

            // Build Context Menu
            var contextMenu = new ContextMenuStrip();

            var openItem = new ToolStripMenuItem("🖨️ Open Print Ofo Dashboard");
            openItem.Font = new System.Drawing.Font(openItem.Font, System.Drawing.FontStyle.Bold);
            openItem.Click += (s, e) => RestoreWindow();
            contextMenu.Items.Add(openItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var statusItem = new ToolStripMenuItem("🟢 Server Status: Active (Port 8080/8443)");
            statusItem.Enabled = false;
            contextMenu.Items.Add(statusItem);

            var autostartItem = new ToolStripMenuItem("⚙️ Start Automatically with Windows");
            autostartItem.CheckOnClick = true;
            autostartItem.Checked = AutoStartupService.IsAutoStartEnabled();
            autostartItem.Click += (s, e) =>
            {
                AutoStartupService.SetAutoStart(autostartItem.Checked);
            };
            contextMenu.Items.Add(autostartItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("❌ Exit Print Ofo");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }
        catch
        {
            // Fallback gracefully if NotifyIcon is not available
        }
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

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitExit)
        {
            // Minimize to system tray instead of closing
            e.Cancel = true;
            Hide();

            if (!_hasShownCloseTip && _notifyIcon != null)
            {
                _hasShownCloseTip = true;
                _notifyIcon.ShowBalloonTip(
                    3000,
                    "Print Ofo is running in the background",
                    "Print Ofo is minimized to the system tray. Mobile devices can continue to connect and print.",
                    ToolTipIcon.Info
                );
            }
            return;
        }

        _notifyIcon?.Dispose();
        _notifyIcon = null;
        base.OnClosing(e);
    }

    public void RestoreWindow()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Activate();
        Focus();
    }

    public void ExitApplication()
    {
        _isExplicitExit = true;
        _notifyIcon?.Dispose();
        _notifyIcon = null;
        Application.Current.Shutdown();
    }
}
