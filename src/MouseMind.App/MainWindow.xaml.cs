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
    private ObservableCollection<MouseProfile> _profiles = [];
    private MouseProfile? _selectedProfile;

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
            AddLog("全局鼠标监听已暂停。", "SYSTEM");
        }
        else { StartMonitoring(); AddLog("全局鼠标监听已开启。", "SYSTEM"); }
    }

    private void MouseHook_SideButtonPressed(object? sender, MouseSideButtonEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            ForegroundAppText.Text = e.ProcessName;
            var profile = FindProfile(e.ProcessName);
            var mapping = profile?.Mappings.FirstOrDefault(m => m.Trigger == e.Button);
            if (profile is not null)
            {
                ProfileList.SelectedItem = profile;
                AddLog(mapping is null
                    ? $"{e.Button} · 匹配“{profile.Name}”，暂无对应动作"
                    : $"{e.Button} → {mapping.Action} · {profile.Name}", "MOUSE");
            }
            else AddLog($"{e.Button} · {e.ProcessName} · 未匹配配置", "MOUSE");
        });
    }

    private MouseProfile? FindProfile(string processName) => _profiles.FirstOrDefault(p =>
        p.IsEnabled && p.ProcessName.Split('/', StringSplitOptions.TrimEntries)
            .Any(name => processName.Contains(name, StringComparison.OrdinalIgnoreCase)));

    private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedProfile = ProfileList.SelectedItem as MouseProfile;
        if (_selectedProfile is null) return;
        ProfileTitle.Text = _selectedProfile.Name;
        ProfileSubtitle.Text = $"当前匹配：{_selectedProfile.ProcessName}";
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

    private void TestAction_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is MouseMapping mapping)
            AddLog($"测试成功：{mapping.Trigger} → {mapping.Action}", "TEST");
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e) => EventLog.Items.Clear();

    private void AddLog(string message, string type = "INFO")
    {
        EventLog.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  [{type,-6}]  {message}");
        while (EventLog.Items.Count > 50) EventLog.Items.RemoveAt(EventLog.Items.Count - 1);
    }

    private Task SaveAsync() => _store.SaveAsync(_profiles);
}

