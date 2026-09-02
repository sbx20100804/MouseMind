using System.Collections.ObjectModel;
using System.Text.Json;
using MouseMind.Core.Models;

namespace MouseMind.Core.Configuration;

public enum ProfileLoadOutcome
{
    Loaded,
    Migrated,
    RecoveredBackup,
    DefaultsCreated,
    DefaultsAfterFailure,
    UnsupportedVersion
}

public sealed record ProfileLoadResult(
    ObservableCollection<MouseProfile> Profiles,
    ProfileLoadOutcome Outcome,
    bool NeedsSave = false,
    bool CanSave = true,
    string? Diagnostic = null);

public sealed class ProfileStore
{
    public const int CurrentSchemaVersion = 1;
    private readonly string _path;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ProfileStore(string? path = null)
    {
        _path = path ?? System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MouseMind", "profiles.json");
    }

    public string Path => _path;
    public string BackupPath => _path + ".bak";

    public async Task<ProfileLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
            return new ProfileLoadResult(CreateDefaults(), ProfileLoadOutcome.DefaultsCreated, NeedsSave: true);

        try
        {
            var loaded = await ReadProfilesAsync(_path, cancellationToken).ConfigureAwait(false);
            return new ProfileLoadResult(loaded.Profiles,
                loaded.WasLegacy ? ProfileLoadOutcome.Migrated : ProfileLoadOutcome.Loaded,
                NeedsSave: loaded.WasLegacy);
        }
        catch (UnsupportedSchemaException ex)
        {
            return new ProfileLoadResult(CreateDefaults(), ProfileLoadOutcome.UnsupportedVersion,
                CanSave: false, Diagnostic: ex.Message);
        }
        catch (Exception primaryError) when (IsRecoverable(primaryError))
        {
            if (File.Exists(BackupPath))
            {
                try
                {
                    var backup = await ReadProfilesAsync(BackupPath, cancellationToken).ConfigureAwait(false);
                    RestorePrimaryFromBackup();
                    return new ProfileLoadResult(backup.Profiles, ProfileLoadOutcome.RecoveredBackup,
                        NeedsSave: backup.WasLegacy,
                        Diagnostic: $"主配置损坏，已从备份恢复：{primaryError.Message}");
                }
                catch (UnsupportedSchemaException ex)
                {
                    return new ProfileLoadResult(CreateDefaults(), ProfileLoadOutcome.UnsupportedVersion,
                        CanSave: false, Diagnostic: ex.Message);
                }
                catch (Exception backupError) when (IsRecoverable(backupError))
                {
                    return new ProfileLoadResult(CreateDefaults(), ProfileLoadOutcome.DefaultsAfterFailure,
                        NeedsSave: true,
                        Diagnostic: $"主配置和备份均不可用，已载入默认配置：{backupError.Message}");
                }
            }

            return new ProfileLoadResult(CreateDefaults(), ProfileLoadOutcome.DefaultsAfterFailure,
                NeedsSave: true, Diagnostic: $"配置不可用，已载入默认配置：{primaryError.Message}");
        }
    }

    public async Task SaveAsync(IEnumerable<MouseProfile> profiles, CancellationToken cancellationToken = default)
    {
        await _saveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var temporaryPath = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            var snapshot = new ObservableCollection<MouseProfile>(profiles.Select(CloneProfile));
            Validate(snapshot);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
            var document = new ProfileDocument(CurrentSchemaVersion, DateTimeOffset.UtcNow, snapshot);

            await using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                             FileShare.None, 16 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(output, document, _jsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                output.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            await ReadProfilesAsync(temporaryPath, CancellationToken.None).ConfigureAwait(false);

            if (File.Exists(_path)) File.Replace(temporaryPath, _path, BackupPath, ignoreMetadataErrors: true);
            else File.Move(temporaryPath, _path);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            _saveGate.Release();
        }
    }

    private async Task<ReadResult> ReadProfilesAsync(string path, CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        using var parsed = JsonDocument.Parse(json);
        ObservableCollection<MouseProfile>? profiles;
        var wasLegacy = parsed.RootElement.ValueKind == JsonValueKind.Array;

        if (wasLegacy)
        {
            profiles = JsonSerializer.Deserialize<ObservableCollection<MouseProfile>>(json, _jsonOptions);
        }
        else
        {
            var document = JsonSerializer.Deserialize<ProfileDocument>(json, _jsonOptions)
                           ?? throw new JsonException("配置文档为空。");
            if (document.SchemaVersion > CurrentSchemaVersion)
                throw new UnsupportedSchemaException($"配置版本 {document.SchemaVersion} 高于当前支持版本 {CurrentSchemaVersion}，已进入只读保护。");
            profiles = document.Profiles;
        }

        if (profiles is null) throw new JsonException("配置集合为空值。");
        Normalize(profiles);
        Validate(profiles);
        return new ReadResult(profiles, wasLegacy);
    }

    private void RestorePrimaryFromBackup()
    {
        var corruptPath = $"{_path}.corrupt-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json";
        if (File.Exists(_path)) File.Move(_path, corruptPath, true);
        var restorePath = $"{_path}.{Guid.NewGuid():N}.restore";
        File.Copy(BackupPath, restorePath, true);
        File.Move(restorePath, _path, true);
    }

    private static bool IsRecoverable(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException;

    private static MouseProfile CloneProfile(MouseProfile profile) => new()
    {
        Id = profile.Id,
        Name = profile.Name,
        ProcessName = profile.ProcessName,
        Accent = profile.Accent,
        IsEnabled = profile.IsEnabled,
        Priority = profile.Priority,
        Mappings = new ObservableCollection<MouseMapping>(profile.Mappings.Select(mapping => new MouseMapping
        {
            Trigger = mapping.Trigger,
            Action = mapping.Action,
            Description = mapping.Description,
            ActionType = mapping.ActionType,
            Payload = mapping.Payload,
            CooldownMs = mapping.CooldownMs,
            TimeoutMs = mapping.TimeoutMs
        }))
    };

    private static void Validate(IEnumerable<MouseProfile> profiles)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id)) throw new InvalidDataException("配置 ID 为空。");
            if (!ids.Add(profile.Id)) throw new InvalidDataException($"存在重复配置 ID：{profile.Id}");
            if (string.IsNullOrWhiteSpace(profile.Name)) throw new InvalidDataException("配置名称为空。");
            if (string.IsNullOrWhiteSpace(profile.ProcessName)) throw new InvalidDataException($"配置“{profile.Name}”缺少进程规则。");
            if (profile.Mappings is null) throw new InvalidDataException($"配置“{profile.Name}”的动作集合为空值。");
            foreach (var mapping in profile.Mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.Trigger)) throw new InvalidDataException($"配置“{profile.Name}”存在空触发器。");
                if (mapping.CooldownMs < 0) throw new InvalidDataException("动作冷却时间不可小于 0。");
                if (mapping.TimeoutMs < 0) throw new InvalidDataException("动作超时时间不可小于 0。");
            }
        }
    }

    private static void Normalize(IEnumerable<MouseProfile> profiles)
    {
        foreach (var profile in profiles)
        {
            profile.Mappings ??= [];
            foreach (var mapping in profile.Mappings)
            {
                if (!string.IsNullOrWhiteSpace(mapping.Payload)) continue;
                if (mapping.Action.Equals("撤销", StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrWhiteSpace(mapping.ActionType) || mapping.ActionType == "Preview"))
                {
                    mapping.ActionType = "KeyboardShortcut";
                    mapping.Payload = "Ctrl+Z";
                }
            }
        }
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
                new() { Trigger = "侧键 1", Action = "打开命令面板", Description = "发送 Ctrl+Shift+P", ActionType = "KeyboardShortcut", Payload = "Ctrl+Shift+P" },
                new() { Trigger = "侧键 2", Action = "撤销", Description = "发送 Ctrl+Z", ActionType = "KeyboardShortcut", Payload = "Ctrl+Z" }
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

    private sealed record ReadResult(ObservableCollection<MouseProfile> Profiles, bool WasLegacy);
    private sealed record ProfileDocument(int SchemaVersion, DateTimeOffset SavedAtUtc, ObservableCollection<MouseProfile> Profiles);
    private sealed class UnsupportedSchemaException(string message) : Exception(message);
}
