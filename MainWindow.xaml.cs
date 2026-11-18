using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DynamicIsland;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_MEDIA_PLAY_PAUSE = 0xB3, VK_MEDIA_NEXT_TRACK = 0xB0, VK_MEDIA_PREV_TRACK = 0xB1;

    private readonly DispatcherTimer _fullscreenTimer = new() { Interval = TimeSpan.FromMilliseconds(650) };
    private bool _expanded;
    private bool _webReady;
    private bool _hiddenForFullscreen;
    private const double CompactWidth = 250, CompactHeight = 62;
    private const double ExpandedWidth = 560, ExpandedHeight = 405;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        _fullscreenTimer.Tick += (_, _) => CheckFullscreen();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionWindow(CompactWidth, CompactHeight);
        try
        {
            await Browser.EnsureCoreWebView2Async();
            Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            Browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            Browser.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DynamicIsland.Assets.index.html");
            if (stream is null) throw new FileNotFoundException("Не найден встроенный интерфейс приложения.");
            using var reader = new StreamReader(stream);
            string html = await reader.ReadToEndAsync();
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DynamicIsland");
            Directory.CreateDirectory(appData);
            string pagePath = Path.Combine(appData, "index.html");
            await File.WriteAllTextAsync(pagePath, html);
            Browser.Source = new Uri(pagePath);
            _webReady = true;
            _fullscreenTimer.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось запустить интерфейс WebView2. Проверь, установлен ли Microsoft Edge WebView2 Runtime.\n\n" + ex.Message,
                "Dynamic Island", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private void PositionWindow(double width, double height)
    {
        Width = width; Height = height;
        Left = (SystemParameters.PrimaryScreenWidth - width) / 2;
        Top = 7;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string message;
        try { message = e.TryGetWebMessageAsString(); } catch { return; }
        if (message == "expand") SetExpanded(true);
        else if (message == "collapse") SetExpanded(false);
        else if (message.StartsWith("copy:", StringComparison.Ordinal))
        {
            string value = message[5..];
            Dispatcher.BeginInvoke(new Action(() => { try { Clipboard.SetText(value); } catch { } }));
        }
        else if (message == "hide") Dispatcher.BeginInvoke(new Action(Hide));
        else if (message == "show") Dispatcher.BeginInvoke(new Action(ShowIsland));
        else if (message == "media:playpause") MediaKey(VK_MEDIA_PLAY_PAUSE);
        else if (message == "media:next") MediaKey(VK_MEDIA_NEXT_TRACK);
        else if (message == "media:previous") MediaKey(VK_MEDIA_PREV_TRACK);
        else if (message == "startup:on") SetStartup(true);
        else if (message == "startup:off") SetStartup(false);
    }

    private static void MediaKey(byte key)
    {
        keybd_event(key, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static void SetStartup(bool enabled)
    {
        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (run is null) return;
            const string name = "DynamicIsland";
            if (enabled) run.SetValue(name, '"' + Environment.ProcessPath! + '"');
            else run.DeleteValue(name, false);
        }
        catch { }
    }

    private void SetExpanded(bool expanded)
    {
        if (!_webReady || _expanded == expanded) return;
        _expanded = expanded;
        Dispatcher.BeginInvoke(() => PositionWindow(expanded ? ExpandedWidth : CompactWidth,
            expanded ? ExpandedHeight : CompactHeight));
    }

    private void ShowIsland()
    {
        Show(); Topmost = true; _hiddenForFullscreen = false;
    }

    private void CheckFullscreen()
    {
        IntPtr foreground = GetForegroundWindow();
        IntPtr own = new WindowInteropHelper(this).Handle;
        if (foreground == IntPtr.Zero || foreground == own) return;
        if (!GetWindowRect(foreground, out RECT rect)) return;
        double sw = SystemParameters.PrimaryScreenWidth, sh = SystemParameters.PrimaryScreenHeight;
        double ww = rect.Right - rect.Left, wh = rect.Bottom - rect.Top;
        bool fullscreen = !IsIconic(foreground) && ww >= sw - 3 && wh >= sh - 3;
        if (fullscreen && !_hiddenForFullscreen)
        {
            _hiddenForFullscreen = true; Hide();
        }
        else if (!fullscreen && _hiddenForFullscreen)
        {
            _hiddenForFullscreen = false; Show(); Topmost = true;
        }
    }
}
