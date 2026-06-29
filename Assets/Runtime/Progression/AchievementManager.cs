using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RYZECHo;

/// <summary>
/// アチーブメント管理マネージャー。解除/解除済み管理、条件判定、報酬付与を行う。
/// </summary>
public sealed class AchievementManager
{
    private readonly Dictionary<string, AchievementData> _trackedAchievements;
    private readonly ProgressionManager _progressionManager;
    private readonly string _savePath;
    private readonly JsonSerializerSettings _jsonSettings;

    public event Action<AchievementData>? OnAchievementUnlocked;
    public event Action<List<AchievementData>>? OnAchievementsUnlocked;

    public IReadOnlyDictionary<string, AchievementData> TrackedAchievements => _trackedAchievements;
    public int UnlockedCount => _unlockedCount;

    private int _unlockedCount;

    public AchievementManager(ProgressionManager progressionManager, string savePath)
    {
        _progressionManager = progressionManager;
        _savePath = savePath;
        _jsonSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
        _trackedAchievements = new Dictionary<string, AchievementData>();

        // 全アチーブメントをトラッキング開始
        foreach (var manifest in AchievementManifest.All)
        {
            var cloned = AchievementManifest.Clone(manifest.Id);
            _trackedAchievements[cloned.Id] = cloned;
        }

        Load();
    }

    // ---- 解除/解除済み管理 ----

    /// <summary>アチーブメントが解除済みか。</summary>
    public bool IsUnlocked(string achievementId)
    {
        if (_trackedAchievements.TryGetValue(achievementId, out var data))
        {
            return data.Unlocked;
        }
        return false;
    }

    /// <summary>解除済みのアチーブメント一覧を取得。</summary>
    public IReadOnlyList<AchievementData> GetUnlockedAchievements()
    {
        var result = new List<AchievementData>();
        foreach (var data in _trackedAchievements.Values)
        {
            if (data.Unlocked) result.Add(data);
        }
        return result;
    }

    /// <summary>未解除のアチーブメント一覧を取得。</summary>
    public IReadOnlyList<AchievementData> GetLockedAchievements()
    {
        var result = new List<AchievementData>();
        foreach (var data in _trackedAchievements.Values)
        {
            if (!data.Unlocked) result.Add(data);
        }
        return result;
    }

    /// <summary>アチーブメントを解除（アンロック）。</summary>
    public bool Unlock(AchievementData achievement)
    {
        if (achievement.Unlocked) return false;
        if (!_trackedAchievements.ContainsKey(achievement.Id)) return false;
        if (!_progressionManager.EvaluateAchievementCondition(achievement)) return false;

        achievement.Unlocked = true;
        _unlockedCount++;

        // 報酬付与
        _progressionManager.State.AgentCredits += achievement.RewardAgentCredits;
        _progressionManager.State.CosmeticTokens += achievement.RewardCosmeticTokens;

        OnAchievementUnlocked?.Invoke(achievement);
        Save();
        return true;
    }

    /// <summary>複数アチーブメントをまとめて解除。</summary>
    public void UnlockMultiple(List<AchievementData> achievements)
    {
        var unlockedList = new List<AchievementData>();
        foreach (var achievement in achievements)
        {
            if (Unlock(achievement))
            {
                unlockedList.Add(achievement);
            }
        }

        if (unlockedList.Count > 0)
        {
            OnAchievementsUnlocked?.Invoke(unlockedList);
            Save();
        }
    }

    // ---- 条件判定 ----

    /// <summary>アチーブメントの条件が満たされたか判定。</summary>
    public bool CheckCondition(string achievementId)
    {
        if (!_trackedAchievements.TryGetValue(achievementId, out var data)) return false;
        if (data.Unlocked) return true;
        return _progressionManager.EvaluateAchievementCondition(data);
    }

    /// <summary>全アチーブメントの条件をチェック。</summary>
    public void CheckAllConditions()
    {
        var newlyUnlocked = new List<AchievementData>();

        foreach (var kvp in _trackedAchievements)
        {
            if (kvp.Value.Unlocked) continue;
            if (_progressionManager.EvaluateAchievementCondition(kvp.Value))
            {
                kvp.Value.Unlocked = true;
                _unlockedCount++;
                _progressionManager.State.AgentCredits += kvp.Value.RewardAgentCredits;
                _progressionManager.State.CosmeticTokens += kvp.Value.RewardCosmeticTokens;
                newlyUnlocked.Add(kvp.Value);
            }
        }

        if (newlyUnlocked.Count > 0)
        {
            OnAchievementsUnlocked?.Invoke(newlyUnlocked);
            Save();
        }
    }

    // ---- セーブ/ロード ----

    /// <summary>アチーブメントデータをセーブ。</summary>
    public void Save()
    {
        try
        {
            var saveData = new AchievementSaveData
            {
                unlockedAchievements = new List<string>(),
                unlockedTimestamps = new Dictionary<string, long>(),
            };

            foreach (var kvp in _trackedAchievements)
            {
                if (kvp.Value.Unlocked)
                {
                    saveData.unlockedAchievements.Add(kvp.Key);
                }
            }

            var json = JsonConvert.SerializeObject(saveData, _jsonSettings);
            File.WriteAllText(_savePath.Replace("prototype-profile.json", "prototype-achievements.json"), json);
        }
        catch
        {
            // セーブ失敗は無視
        }
    }

    /// <summary>アチーブメントデータをロード。</summary>
    public void Load()
    {
        var achievementPath = _savePath.Replace("prototype-profile.json", "prototype-achievements.json");

        try
        {
            if (!File.Exists(achievementPath))
            {
                return;
            }

            var json = File.ReadAllText(achievementPath);
            var saveData = JsonConvert.DeserializeObject<AchievementSaveData>(json, _jsonSettings);
            if (saveData == null) return;

            foreach (var id in saveData.unlockedAchievements)
            {
                if (_trackedAchievements.TryGetValue(id, out var data))
                {
                    data.Unlocked = true;
                    _unlockedCount++;
                }
            }
        }
        catch
        {
            // ロード失敗時は初期状態を使用
        }
    }

    /// <summary>アチーブメントデータをバックアップ。</summary>
    public void Backup()
    {
        var achievementPath = _savePath.Replace("prototype-profile.json", "prototype-achievements.json");
        try
        {
            if (File.Exists(achievementPath))
            {
                var backupPath = achievementPath + ".bak";
                File.Copy(achievementPath, backupPath, true);
            }
        }
        catch
        {
            // バックアップ失敗は無視
        }
    }
}

/// <summary>
/// アチーブメントのセーブデータ。
/// </summary>
public record AchievementSaveData
{
    public List<string> unlockedAchievements { get; set; } = new();
    public Dictionary<string, long> unlockedTimestamps { get; set; } = new();
}
