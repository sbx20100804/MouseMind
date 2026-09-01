using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using MouseMind.App.Models;

namespace MouseMind.App.Services;

public sealed class ProfileStore
{
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MouseMind", "profiles.json");

    public async Task<ObservableCollection<MouseProfile>> LoadAsync()
    {
        if (File.Exists(_path))
        {
            try
            {
                await using var input = File.OpenRead(_path);
                var profiles = await JsonSerializer.DeserializeAsync<ObservableCollection<MouseProfile>>(input);
                if (profiles is { Count: > 0 }) return profiles;
            }
            catch { /* 损坏配置回退到内置预设。 */ }
        }

        return CreateDefaults();
    }

    public async Task SaveAsync(IEnumerable<MouseProfile> profiles)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await using var output = File.Create(_path);
        await JsonSerializer.SerializeAsync(output, profiles, new JsonSerializerOptions { WriteIndented = true });
    }

    private static ObservableCollection<MouseProfile> CreateDefaults() =>
    [
        new()
        {
            Name = "浏览与阅读", ProcessName = "msedge / chrome", Accent = "#7C5CFC",
            Mappings =
            [
                new() { Trigger = "侧键 1", Action = "AI 总结", Description = "总结当前选中的网页内容" },
                new() { Trigger = "侧键 2", Action = "翻译", Description = "翻译选中文本并复制结果" },
                new() { Trigger = "按住侧键 →", Action = "稍后阅读", Description = "保存当前页面" }
            ]
        },
        new()
        {
            Name = "代码工作台", ProcessName = "Code", Accent = "#21C7A8",
            Mappings =
            [
                new() { Trigger = "侧键 1", Action = "解释代码", Description = "解释当前选择" },
                new() { Trigger = "侧键 2", Action = "快速修复", Description = "生成修复建议" }
            ]
        },
        new()
        {
            Name = "视频剪辑", ProcessName = "剪辑软件", Accent = "#FFB547",
            Mappings =
            [
                new() { Trigger = "侧键 1", Action = "分割片段", Description = "在播放头位置分割" },
                new() { Trigger = "侧键 2", Action = "撤销", Description = "撤销上一步编辑" },
                new() { Trigger = "滚轮左右", Action = "逐帧移动", Description = "精确调整时间线" }
            ]
        }
    ];
}

