using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace RYZECHo;

/// <summary>
/// セーブデータの読み込みマネージャー。非同期読み込みと検証を行う。
/// </summary>
public sealed class SaveDataLoader
{
    private readonly string _basePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public event Action<ProgressionState>? OnLoadCompleted;
    public event Action<AchievementSaveData>? OnAchievementLoadCompleted;

    public SaveDataLoader(string basePath)
    {
        _basePath = basePath;
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    /// <summary>進行状況データを同期的にロード。</summary>
    public ProgressionState LoadProgressionState()
    {
        var path = Path.Combine(_basePath, "prototype-profile.json");
        return LoadFromPath<ProgressionState>(path);
    }

    /// <summary>進行状況データを非同期的にロード（Unity Coroutine用）。</summary>
    public System.Collections.Generic.IEnumerator<UnityEngine.Coroutine> LoadProgressionStateAsync(UnityEngine.Coroutine coroutine)
    {
        var path = Path.Combine(_basePath, "prototype-profile.json");
        var state = LoadFromPath<ProgressionState>(path);

        if (state != null)
        {
            OnLoadCompleted?.Invoke(state);
        }

        yield break;
    }

    /// <summary>アチーブメントデータを同期的にロード。</summary>
    public AchievementSaveData LoadAchievementData()
    {
        var path = Path.Combine(_basePath, "prototype-achievements.json");
        var data = LoadFromPath<AchievementSaveData>(path);
        if (data != null)
        {
            OnAchievementLoadCompleted?.Invoke(data);
        }
        return data ?? new AchievementSaveData();
    }

    /// <summary>アチーブメントデータを非同期的にロード（Unity Coroutine用）。</summary>
    public System.Collections.Generic.IEnumerator<UnityEngine.Coroutine> LoadAchievementDataAsync(UnityEngine.Coroutine coroutine)
    {
        var path = Path.Combine(_basePath, "prototype-achievements.json");
        var data = LoadFromPath<AchievementSaveData>(path);

        if (data != null)
        {
            OnAchievementLoadCompleted?.Invoke(data);
        }

        yield break;
    }

    /// <summary>セーブデータを検証。有効なデータならtrue。</summary>
    public bool ValidateProgressionState(ProgressionState state)
    {
        if (state == null) return false;
        if (string.IsNullOrEmpty(state.IntegritySalt)) return false;
        if (string.IsNullOrEmpty(state.IntegrityStamp)) return false;

        var expected = CreateIntegrityStamp(state);
        return state.IntegrityStamp == expected;
    }

    /// <summary>アチーブメントデータを検証。</summary>
    public bool ValidateAchievementData(AchievementSaveData data)
    {
        if (data == null) return false;
        // 空のデータも有効（新規プレイヤー）
        return true;
    }

    // ---- ヘルパー ----

    private T LoadFromPath<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path))
            {
                return CreateDefault<T>();
            }

            var json = File.ReadAllText(path);
            var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            return result ?? CreateDefault<T>();
        }
        catch
        {
            return CreateDefault<T>();
        }
    }

    private static T CreateDefault<T>() where T : class
    {
        if (typeof(T) == typeof(ProgressionState))
        {
            return new ProgressionState() as T;
        }
        if (typeof(T) == typeof(AchievementSaveData))
        {
            return new AchievementSaveData() as T;
        }
        return null;
    }

    private static string CreateIntegrityStamp(ProgressionState state)
    {
        var data = $"{state.AccountLevel}:{state.CurrentXp}:{state.RankRating}:{state.MatchesPlayed}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash);
    }
}
