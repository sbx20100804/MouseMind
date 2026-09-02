using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace MouseMind.Core.Models;

public sealed class MouseProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "新配置";
    public string ProcessName { get; set; } = "*";
    public string Accent { get; set; } = "#7C5CFC";
    public bool IsEnabled { get; set; } = true;
    public int Priority { get; set; }
    public ObservableCollection<MouseMapping> Mappings { get; set; } = [];
    [JsonIgnore] public string StatusText => IsEnabled ? "已启用" : "已停用";
    [JsonIgnore] public string MappingSummary => $"{Mappings.Count} 个动作 · {ProcessName}";
}

public sealed class MouseMapping
{
    public string Trigger { get; set; } = "侧键 1";
    public string Action { get; set; } = "新动作";
    public string Description { get; set; } = "待配置";
    public string ActionType { get; set; } = "Preview";
    public string Payload { get; set; } = "";
    public int CooldownMs { get; set; } = 500;
    public int TimeoutMs { get; set; } = 5000;
}
