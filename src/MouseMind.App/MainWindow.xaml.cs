using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseMind.App.Services;
using MouseMind.Core.Actions;
using MouseMind.Core.Configuration;
using MouseMind.Core.Models;
using MouseMind.Core.Profiles;
using MouseMind.Windows.Actions;
using MouseMind.Windows.Foreground;
using MouseMind.Windows.Input;

namespace MouseMind.App;

public partial class MainWindow : Window
{
    private readonly ProfileStore _store = new();
    private readonly MouseHookService _mouseHook = new();
    private readonly ActionExecutionService _actions = new([new KeyboardShortcutExecutor()]);
    private readonly ProfileMatcher _profileMatcher = new();
    private readonly ForegroundWindowService _foregroundWindow = new();
    private readonly CancellationTokenSource _inputLifetime = new();
    private ObservableCollection<MouseProfile> _profiles = [];
    private MouseProfile? _selectedProfile;
    private Task? _inputLoop;
    private bool _canSaveProfiles = true;
    private int _sessionActionCount;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowBackdropService.Apply(this);
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        _mouseHook.Diagnostic += MouseHook_Diagnostic;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var loadResult = await _store.LoadAsync();
        _profiles = loadResult.Profiles;
        _canSaveProfiles = loadResult.CanSave;
        ProfileList.ItemsSource = _profiles;
        DashboardProfilesList.ItemsSource = _profiles;
        ProfileList.SelectedIndex = 0;
        _inputLoop = ProcessMouseEventsAsync(_inputLifetime.Token);
        StartMonitoring();
        AddLog("MouseMind 已启动，配置已从本地加载。", "SYSTEM");
        if (loadResult.Diagnostic is not null)
            AddLog(loadResult.Diagnostic, loadResult.CanSave ? "RECOVER" : "READONLY");
        if (loadResult.NeedsSave && loadResult.CanSave)
            await SaveAsync();
        MotionService.Reveal(OverviewPanel, 7);
    }

    private void StartMonitoring()
    {
        try
        {
            _mouseHook.Start();
            MonitorText.Text = "监听已开启";
            MonitorText.Foreground = new SolidColorBrush(Color.FromRgb(121, 231, 207));
            LiveDot.Fill = new SolidColorBrush(Color.FromRgb(57, 214, 180));
            MonitorButton.Content = "暂停监听";
            SidebarLiveDot.Fill = new SolidColorBrush(Color.FromRgb(78, 214, 177));
            SidebarStatusText.Text = "正在运行";
            SidebarMonitorSwitch.IsChecked = true;
        }
        catch (Exception ex) { AddLog(ex.Message, "ERROR"); }
    }

    private void MonitorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_mouseHook.IsRunning)
        {
            if (!_mouseHook.Stop())
            {
                AddLog("鼠标监听未能安全停止，请查看输入诊断。", "ERROR");
                return;
            }
            MonitorText.Text = "监听已暂停";
            MonitorText.Foreground = Brushes.Gray;
            LiveDot.Fill = Brushes.Gray;
            MonitorButton.Content = "开启监听";
            SidebarLiveDot.Fill = Brushes.Gray;
            SidebarStatusText.Text = "已暂停";
            SidebarMonitorSwitch.IsChecked = false;
            AddLog("全局鼠标监听已暂停。", "SYSTEM");
        }
        else { StartMonitoring(); AddLog("全局鼠标监听已开启。", "SYSTEM"); }
    }

    private async Task ProcessMouseEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var input in _mouseHook.ReadEventsAsync(cancellationToken))
            {
                var processName = _foregroundWindow.GetProcessName(input.ForegroundWindow);
                await Dispatcher.InvokeAsync(() => HandleMouseInputAsync(input, processName)).Task.Unwrap();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() => AddLog($"输入处理管线已停止：{ex.Message}", "ERROR"));
        }
    }

    private async Task HandleMouseInputAsync(MouseInputEvent input, string processName)
    {
        MotionService.Pulse(SignalCore);
        ForegroundAppText.Text = processName;
        DashboardAppText.Text = processName;
        var profile = FindProfile(processName);
        var mapping = profile?.Mappings.FirstOrDefault(m => m.Trigger == input.Trigger);
        if (profile is not null)
        {
            DashboardProfileText.Text = profile.Name;
            DashboardMappingSummaryText.Text = GetMappingSummary(profile);
            if (mapping is null)
                AddLog($"{input.Trigger} · 匹配“{profile.Name}”，暂无对应动作", "MOUSE");
            else
            {
                if (!_foregroundWindow.IsStillForeground(input.ForegroundWindow))
                {
                    AddLog($"{input.Trigger} · 目标窗口已切换，动作已取消", "STALE");
                    return;
                }

                AddLog($"{input.Trigger} → {mapping.Action} · {profile.Name}", "MOUSE");
                var result = await _actions.ExecuteAsync(mapping,
                    new ActionContext(processName, input.Timestamp), _inputLifetime.Token);
                if (result.Success) IncrementSessionActions();
                AddLog(result.Message, ResultLogType(result.Status));
                ShowActionToast(result.Success, mapping.Action, result.Message);
            }
        }
        else AddLog($"{input.Trigger} · {processName} · 未匹配配置", "MOUSE");
    }

    private MouseProfile? FindProfile(string processName) => _profileMatcher.Find(_profiles, processName);

    private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedProfile = ProfileList.SelectedItem as MouseProfile;
        if (_selectedProfile is null) return;
        ProfileTitle.Text = _selectedProfile.Name;
        ProfileSubtitle.Text = $"当前匹配：{_selectedProfile.ProcessName}";
        DashboardProfileText.Text = _selectedProfile.Name;
        DashboardMappingSummaryText.Text = GetMappingSummary(_selectedProfile);
        ProfileEnabled.IsChecked = _selectedProfile.IsEnabled;
        MappingList.ItemsSource = _selectedProfile.Mappings;
    }

    private async void ProfileEnabled_Changed(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile is null) return;
        _selectedProfile.IsEnabled = ProfileEnabled.IsChecked == true;
        await SaveAsync();
        ProfileList.Items.Refresh();
        DashboardProfilesList.Items.Refresh();
        AddLog($"配置“{_selectedProfile.Name}”已{(_selectedProfile.IsEnabled ? "启用" : "停用")}。", "CONFIG");
    }

    private async void AddProfile_Click(object sender, RoutedEventArgs e)
    {
        var profile = new MouseProfile { Name = $"新配置 {_profiles.Count + 1}", ProcessName = "TARGET_APP" };
        profile.Mappings.Add(new MouseMapping());
        _profiles.Add(profile);
        ProfileList.SelectedItem = profile;
        await SaveAsync();
        AddLog("已创建一个新的应用配置。", "CONFIG");
    }

    private async void AddMapping_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile is null) return;
        _selectedProfile.Mappings.Add(new MouseMapping { Trigger = "侧键 1", Action = "新动作", Description = "待配置" });
        await SaveAsync();
        ProfileList.Items.Refresh();
        DashboardProfilesList.Items.Refresh();
        DashboardMappingSummaryText.Text = GetMappingSummary(_selectedProfile);
        AddLog($"已向“{_selectedProfile.Name}”添加动作。", "CONFIG");
    }

    private async void TestAction_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is MouseMapping mapping)
        {
            var result = await _actions.ExecuteAsync(mapping,
                new ActionContext("MouseMind", DateTimeOffset.Now), _inputLifetime.Token);
            if (result.Success) IncrementSessionActions();
            AddLog(result.Message, result.Success ? "TEST" : ResultLogType(result.Status));
            MotionService.Pulse(SignalCore);
            ShowActionToast(result.Success, mapping.Action, result.Message);
        }
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        EventLog.Items.Clear();
        DashboardActivityList.Items.Clear();
        DashboardActivityHint.Visibility = Visibility.Visible;
    }

    private void AddLog(string message, string type = "INFO")
    {
        var entry = $"{DateTime.Now:HH:mm:ss}  [{type,-6}]  {message}";
        EventLog.Items.Insert(0, entry);
        DashboardActivityList.Items.Insert(0, entry);
        while (EventLog.Items.Count > 50) EventLog.Items.RemoveAt(EventLog.Items.Count - 1);
        while (DashboardActivityList.Items.Count > 5)
            DashboardActivityList.Items.RemoveAt(DashboardActivityList.Items.Count - 1);
        DashboardActivityHint.Visibility = DashboardActivityList.Items.Count <= 1
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void IncrementSessionActions()
    {
        _sessionActionCount++;
        SessionActionCountText.Text = $"{_sessionActionCount} 次动作";
    }

    private void Navigation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string destination }) return;
        NavigateTo(destination);
    }

    private void QuickProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MouseProfile profile }) return;
        ProfileList.SelectedItem = profile;
        NavigateTo("Profiles");
    }

    private void NavigateTo(string destination)
    {

        FrameworkElement nextPanel = destination switch
        {
            "Profiles" => ProfilesPanel,
            "Activity" => ActivityPanel,
            "Settings" => SettingsPanel,
            _ => OverviewPanel
        };

        OverviewPanel.Visibility = destination == "Overview" ? Visibility.Visible : Visibility.Collapsed;
        ProfilesPanel.Visibility = destination == "Profiles" ? Visibility.Visible : Visibility.Collapsed;
        ActivityPanel.Visibility = destination == "Activity" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = destination == "Settings" ? Visibility.Visible : Visibility.Collapsed;

        OverviewNav.Style = (Style)FindResource(destination == "Overview" ? "ActiveNavButton" : "NavButton");
        ProfilesNav.Style = (Style)FindResource(destination == "Profiles" ? "ActiveNavButton" : "NavButton");
        ActivityNav.Style = (Style)FindResource(destination == "Activity" ? "ActiveNavButton" : "NavButton");
        SettingsNav.Style = (Style)FindResource(destination == "Settings" ? "ActiveNavButton" : "NavButton");

        (PageHeader.Text, PageDescription.Text) = destination switch
        {
            "Profiles" => ("应用配置", "为每个应用设置刚刚好的侧键动作。"),
            "Activity" => ("活动记录", "查看匹配、输入与动作结果。"),
            "Settings" => ("设置", "调整 MouseMind 的行为、外观与数据。"),
            _ => ("概览", "你的鼠标，已经准备好了。")
        };

        MotionService.Reveal(nextPanel);
    }

    private static string GetMappingSummary(MouseProfile profile)
    {
        var mappings = profile.Mappings.Take(2)
            .Select(mapping => $"{mapping.Trigger}：{mapping.Action}")
            .ToArray();
        return mappings.Length == 0 ? "尚未配置侧键动作" : string.Join("  ·  ", mappings);
    }

    private void ShowActionToast(bool success, string title, string message)
    {
        ToastTitle.Text = success ? title : "动作未执行";
        ToastMessage.Text = message;
        ToastIcon.Data = Geometry.Parse(success ? "M7,15 L12,20 L22,9" : "M8,8 L22,22 M22,8 L8,22");
        ToastIcon.Stroke = success
            ? (Brush)FindResource("SuccessBrush")
            : (Brush)FindResource("ErrorBrush");
        ToastIconBackground.Fill = success
            ? new SolidColorBrush(Color.FromArgb(0x24, 0x3E, 0xD2, 0xA9))
            : new SolidColorBrush(Color.FromArgb(0x24, 0xF0, 0x78, 0x83));
        MotionService.ShowToast(ActionToastHost);
    }

    private void SidebarMonitorSwitch_Click(object sender, RoutedEventArgs e)
    {
        var shouldRun = SidebarMonitorSwitch.IsChecked == true;
        if (shouldRun && !_mouseHook.IsRunning)
        {
            StartMonitoring();
            AddLog("全局鼠标监听已开启。", "SYSTEM");
        }
        else if (!shouldRun && _mouseHook.IsRunning)
        {
            MonitorButton_Click(MonitorButton, new RoutedEventArgs());
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private static string ResultLogType(ActionStatus status) => status switch
    {
        ActionStatus.Cancelled => "CANCEL",
        ActionStatus.TimedOut => "TIMEOUT",
        ActionStatus.Failed => "ERROR",
        ActionStatus.Skipped => "SKIP",
        _ => "ACTION"
    };

    private void MouseHook_Diagnostic(object? sender, string message) =>
        Dispatcher.BeginInvoke(() => AddLog(message, "INPUT"));

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _inputLifetime.Cancel();
        _mouseHook.Diagnostic -= MouseHook_Diagnostic;
        _mouseHook.Dispose();
        _inputLifetime.Dispose();
    }

    private async Task SaveAsync()
    {
        if (!_canSaveProfiles)
        {
            AddLog("当前配置来自更高版本，已启用只读保护。", "READONLY");
            return;
        }

        try { await _store.SaveAsync(_profiles, _inputLifetime.Token); }
        catch (OperationCanceledException) when (_inputLifetime.IsCancellationRequested) { }
        catch (Exception ex) { AddLog($"配置保存失败：{ex.Message}", "ERROR"); }
    }
}
