using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseMind.App.Models;
using MouseMind.App.Services;

namespace MouseMind.App;

public partial class MainWindow : Window
{
    private readonly ProfileStore _store = new();
    private readonly MouseHookService _mouseHook = new();
    private readonly ActionExecutionService _actions = new([new KeyboardShortcutExecutor()]);
    private readonly ProfileMatcher _profileMatcher = new();
    private ObservableCollection<MouseProfile> _profiles = [];
    private MouseProfile? _selectedProfile;
    private int _sessionActionCount;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += (_, _) => _mouseHook.Dispose();
        _mouseHook.SideButtonPressed += MouseHook_SideButtonPressed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _profiles = await _store.LoadAsync();
        ProfileList.ItemsSource = _profiles;
        ProfileList.SelectedIndex = 0;
        StartMonitoring();
        AddLog("MouseMind 已启动，配置已从本地加载。", "SYSTEM");
        MotionService.Reveal(OverviewPanel, 7);
        MotionService.StartOrbit(SignalOrbit, 0, 360, 22);
        MotionService.StartOrbit(SignalOrbitReverse, 360, 0, 16);
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
            _mouseHook.Stop();
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

    private async void MouseHook_SideButtonPressed(object? sender, MouseSideButtonEventArgs e)
    {
        MotionService.Pulse(SignalCore);
        ForegroundAppText.Text = e.ProcessName;
        DashboardAppText.Text = e.ProcessName;
        var profile = FindProfile(e.ProcessName);
        var mapping = profile?.Mappings.FirstOrDefault(m => m.Trigger == e.Button);
        if (profile is not null)
        {
            ProfileList.SelectedItem = profile;
            if (mapping is null)
                AddLog($"{e.Button} · 匹配“{profile.Name}”，暂无对应动作", "MOUSE");
            else
            {
                AddLog($"{e.Button} → {mapping.Action} · {profile.Name}", "MOUSE");
                var result = await _actions.ExecuteAsync(mapping,
                    new ActionContext(e.ProcessName, DateTimeOffset.Now));
                if (result.Success) IncrementSessionActions();
                AddLog(result.Message, result.Success ? "ACTION" : "SKIP");
                ShowActionToast(result.Success, mapping.Action, result.Message);
            }
        }
        else AddLog($"{e.Button} · {e.ProcessName} · 未匹配配置", "MOUSE");
    }

    private MouseProfile? FindProfile(string processName) => _profileMatcher.Find(_profiles, processName);

    private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedProfile = ProfileList.SelectedItem as MouseProfile;
        if (_selectedProfile is null) return;
        ProfileTitle.Text = _selectedProfile.Name;
        ProfileSubtitle.Text = $"当前匹配：{_selectedProfile.ProcessName}";
        DashboardProfileText.Text = _selectedProfile.Name;
        ProfileEnabled.IsChecked = _selectedProfile.IsEnabled;
        MappingList.ItemsSource = _selectedProfile.Mappings;
    }

    private async void ProfileEnabled_Changed(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile is null) return;
        _selectedProfile.IsEnabled = ProfileEnabled.IsChecked == true;
        await SaveAsync();
        ProfileList.Items.Refresh();
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
        AddLog($"已向“{_selectedProfile.Name}”添加动作。", "CONFIG");
    }

    private async void TestAction_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is MouseMapping mapping)
        {
            var result = await _actions.ExecuteAsync(mapping,
                new ActionContext("MouseMind", DateTimeOffset.Now));
            if (result.Success) IncrementSessionActions();
            AddLog(result.Message, result.Success ? "TEST" : "SKIP");
            MotionService.Pulse(SignalCore);
            ShowActionToast(result.Success, mapping.Action, result.Message);
        }
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        EventLog.Items.Clear();
        DashboardActivityList.Items.Clear();
    }

    private void AddLog(string message, string type = "INFO")
    {
        var entry = $"{DateTime.Now:HH:mm:ss}  [{type,-6}]  {message}";
        EventLog.Items.Insert(0, entry);
        DashboardActivityList.Items.Insert(0, entry);
        while (EventLog.Items.Count > 50) EventLog.Items.RemoveAt(EventLog.Items.Count - 1);
        while (DashboardActivityList.Items.Count > 5)
            DashboardActivityList.Items.RemoveAt(DashboardActivityList.Items.Count - 1);
    }

    private void IncrementSessionActions()
    {
        _sessionActionCount++;
        SessionActionCountText.Text = $"{_sessionActionCount} 次动作";
    }

    private void Navigation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string destination }) return;

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
            "Profiles" => ("应用配置", "为不同应用分配专属鼠标动作。"),
            "Activity" => ("活动记录", "观察配置匹配、输入触发与动作结果。"),
            "Settings" => ("设置", "调整 MouseMind 的行为、外观与数据。"),
            _ => ("概览", "你的鼠标工作流，正在安静运行。")
        };

        MotionService.Reveal(nextPanel);
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

    private Task SaveAsync() => _store.SaveAsync(_profiles);
}
