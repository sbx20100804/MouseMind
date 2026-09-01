using System.Collections.ObjectModel;

namespace MouseMind.App.Models;

public sealed class MouseProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "新配置";
    public string ProcessName { get; set; } = "*";
    public string Accent { get; set; } = "#7C5CFC";
    public bool IsEnabled { get; set; } = true;
    public ObservableCollection<MouseMapping> Mappings { get; set; } = [];
    public string StatusText => IsEnabled ? "已启用" : "已停用";
    public string MappingSummary => $"{Mappings.Count} 个动作 · {ProcessName}";
}

public sealed class MouseMapping
{
    public string Trigger { get; set; } = "侧键 1";
    public string Action { get; set; } = "AI 总结选中文本";
    public string Description { get; set; } = "按下后执行动作";
}

