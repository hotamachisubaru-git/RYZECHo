using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace RYZECHo;

/// <summary>
/// セーブデータの書き込みマネージャー。非同期書き込みとバックアップを行う。
/// </summary>
public sealed class SaveDataWriter
{
    private readonly string _basePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private const int MaxBackupCount = 5;

    public event Action<string>? OnSaveCompleted;
    public event Action<string, Exception>? OnSaveFailed;

    public SaveDataWriter(string basePath)
    {
        _basePath = basePath;
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    /// <summary>進行状況データを同期的にセーブ。</summary>
    public void SaveProgressionState(ProgressionState state)
    {
        if (state == null) return;

        try
        {
            BackupExisting("prototype-profile.json");

            NormalizeState(state);
            state.IntegritySalt = GenerateSalt();
            state.IntegrityStamp = CreateIntegrityStamp(state);

            var path = Path.Combine(_basePath, "prototype-profile.json");
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            File.WriteAllText(path, json);

            OnSaveCompleted?.Invoke("prototype-profile.json");
        }
        catch (Exception ex)
        {
            OnSaveFailed?.Invoke("prototype-profile.json", ex);
        }
    }

    /// <summary>進行状況データを非同期的にセーブ（Unity Coroutine用）。</summary>
    public System.Collections.Generic.IEnumerator<UnityEngine.Coroutine> SaveProgressionStateAsync(ProgressionState state, UnityEngine.Coroutine coroutine)
    {
        if (state == null) yield break;

        try
        {
            BackupExisting("prototype-profile.json");

            NormalizeState(state);
            state.IntegritySalt = GenerateSalt();
            state.IntegrityStamp = CreateIntegrityStamp(state);

            var path = Path.Combine(_basePath, "prototype-profile.json");
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            File.WriteAllText(path, json);

            OnSaveCompleted?.Invoke("prototype-profile.json");
        }
        catch (Exception ex)
        {
            OnSaveFailed?.Invoke("prototype-profile.json", ex);
        }

        yield break;
    }

    /// <summary>アチーブメントデータを同期的にセーブ。</summary>
    public void SaveAchievementData(AchievementSaveData data)
    {
        if (data == null) return;

        try
        {
            BackupExisting("prototype-achievements.json");

            var path = Path.Combine(_basePath, "prototype-achievements.json");
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(path, json);

            OnSaveCompleted?.Invoke("prototype-achievements.json");
        }
        catch (Exception ex)
        {
            OnSaveFailed?.Invoke("prototype-achievements.json", ex);
        }
    }

    /// <summary>アチーブメントデータを非同期的にセーブ（Unity Coroutine用）。</summary>
    public System.Collections.Generic.IEnumerator<UnityEngine.Coroutine> SaveAchievementDataAsync(AchievementSaveData data, UnityEngine.Coroutine coroutine)
    {
        if (data == null) yield break;

        try
        {
            BackupExisting("prototype-achievements.json");

            var path = Path.Combine(_basePath, "prototype-achievements.json");
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(path, json);

            OnSaveCompleted?.Invoke("prototype-achievements.json");
        }
        catch (Exception ex)
        {
            OnSaveFailed?.Invoke("prototype-achievements.json", ex);
        }

        yield break;
    }

    /// <summary>既存のファイルをバックアップ。</summary>
    public void BackupExisting(string fileName)
    {
        var path = Path.Combine(_basePath, fileName);
        if (!File.Exists(path)) return;

        var backupDir = Path.Combine(_basePath, ".backups", fileName);
        Directory.CreateDirectory(backupDir);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var backupPath = Path.Combine(backupDir, $"{timestamp}.bak");
        File.Copy(path, backupPath, true);

        // 古いバックアップを削除（最大数制限）
        CleanupOldBackups(backupDir, fileName);
    }

    /// <summary>全バックアップをクリーンアップ。</summary>
    public void CleanupAllBackups()
    {
        var backupDir = Path.Combine(_basePath, ".backups");
        if (Directory.Exists(backupDir))
        {
            Directory.Delete(backupDir, true);
        }
    }

    // ---- ヘルパー ----

    private void NormalizeState(ProgressionState state)
    {
        state.AccountLevel = Math.Clamp(state.AccountLevel, 1, 999);
        state.CurrentXp = Math.Max(0, state.CurrentXp);
        state.AgentCredits = Math.Clamp(state.AgentCredits, 0, 999999);
        state.CosmeticTokens = Math.Clamp(state.CosmeticTokens, 0, 999999);
        state.RankRating = Math.Max(0, state.RankRating);
        state.MatchesPlayed = Math.Max(0, state.MatchesPlayed);
        state.MatchesWon = Math.Clamp(state.MatchesWon, 0, state.MatchesPlayed);
        state.ContractsCompleted = Math.Max(0, state.ContractsCompleted);
        state.ActiveContractProgress = Math.Clamp(state.ActiveContractProgress, 0, 11);
        state.StoreCursor = Math.Clamp(state.StoreCursor, 0, Math.Max(0, 10 - 1));
        state.LifetimeAdImpressions = Math.Max(0, state.LifetimeAdImpressions);
        state.DifficultyModifier = Math.Clamp(state.DifficultyModifier, 0.5f, 2.0f);
    }

    private static string GenerateSalt()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes);
    }

    private static string CreateIntegrityStamp(ProgressionState state)
    {
        var data = $"{state.AccountLevel}:{state.CurrentXp}:{state.RankRating}:{state.MatchesPlayed}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash);
    }

    private void CleanupOldBackups(string backupDir, string fileName)
    {
        try
        {
            var files = Directory.GetFiles(backupDir, "*.bak");
            Array.Sort(files); // 古い順

            while (files.Length > MaxBackupCount)
            {
                File.Delete(files[0]);
                files = Directory.GetFiles(backupDir, "*.bak");
            }
        }
        catch
        {
            // クリーンアップ失敗は無視
        }
    }
}
