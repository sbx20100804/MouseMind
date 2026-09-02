using System.Collections.ObjectModel;
using System.Text.Json;
using MouseMind.Core.Configuration;
using MouseMind.Core.Models;

namespace MouseMind.Tests;

public sealed class ProfileStoreTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsVersionedDocument()
    {
        using var scope = new TempScope();
        var store = new ProfileStore(scope.File("profiles.json"));
        await store.SaveAsync(Profiles("Editor"));

        var result = await store.LoadAsync();

        Assert.Equal(ProfileLoadOutcome.Loaded, result.Outcome);
        Assert.Equal("Editor", Assert.Single(result.Profiles).Name);
        var json = await File.ReadAllTextAsync(store.Path);
        Assert.Contains("SchemaVersion", json);
        Assert.DoesNotContain("MappingSummary", json);
        Assert.DoesNotContain("StatusText", json);
    }

    [Fact]
    public async Task Load_MigratesLegacyArray()
    {
        using var scope = new TempScope();
        var path = scope.File("profiles.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(Profiles("Legacy")));
        var result = await new ProfileStore(path).LoadAsync();
        Assert.Equal(ProfileLoadOutcome.Migrated, result.Outcome);
        Assert.True(result.NeedsSave);
    }

    [Fact]
    public async Task Load_RecoversPreviousVersionFromBackup()
    {
        using var scope = new TempScope();
        var store = new ProfileStore(scope.File("profiles.json"));
        await store.SaveAsync(Profiles("Version 1"));
        await store.SaveAsync(Profiles("Version 2"));
        await File.WriteAllTextAsync(store.Path, "{broken");

        var result = await store.LoadAsync();

        Assert.Equal(ProfileLoadOutcome.RecoveredBackup, result.Outcome);
        Assert.Equal("Version 1", Assert.Single(result.Profiles).Name);
        Assert.Contains("备份", result.Diagnostic);
    }

    [Fact]
    public async Task Load_ProtectsFutureSchemaFromWrites()
    {
        using var scope = new TempScope();
        var path = scope.File("profiles.json");
        await File.WriteAllTextAsync(path, "{\"SchemaVersion\":999,\"SavedAtUtc\":\"2026-01-01T00:00:00Z\",\"Profiles\":[]}");
        var result = await new ProfileStore(path).LoadAsync();
        Assert.Equal(ProfileLoadOutcome.UnsupportedVersion, result.Outcome);
        Assert.False(result.CanSave);
    }

    [Fact]
    public async Task ConcurrentSaves_LeaveValidDocument()
    {
        using var scope = new TempScope();
        var store = new ProfileStore(scope.File("profiles.json"));
        await Task.WhenAll(
            store.SaveAsync(Profiles("One")),
            store.SaveAsync(Profiles("Two")),
            store.SaveAsync(Profiles("Three")));
        var result = await store.LoadAsync();
        Assert.Equal(ProfileLoadOutcome.Loaded, result.Outcome);
        Assert.Single(result.Profiles);
    }

    private static ObservableCollection<MouseProfile> Profiles(string name) =>
    [
        new MouseProfile
        {
            Name = name,
            ProcessName = "Code",
            Mappings = [new MouseMapping { Trigger = "侧键 1", ActionType = "KeyboardShortcut", Payload = "Ctrl+Z" }]
        }
    ];

    private sealed class TempScope : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MouseMind.Tests", Guid.NewGuid().ToString("N"));
        public TempScope() => Directory.CreateDirectory(_directory);
        public string File(string name) => System.IO.Path.Combine(_directory, name);
        public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    }
}
