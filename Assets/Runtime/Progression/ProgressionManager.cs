using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace RYZECHo;

/// <summary>
/// プログレス管理マネージャー。セーブ/ロード、進行状況管理、難易度補正を行う。
/// </summary>
public sealed class ProgressionManager
{
    private readonly ProgressionState _state;
    private readonly List<AchievementData> _unlockedAchievements;
    private readonly string _savePath;
    private readonly JsonSerializerSettings _jsonSettings;
    private float _autoSaveTimer;
    private const float AutoSaveInterval = 30f;

    public event Action<ProgressionState>? OnProgressChanged;
    public event Action<string>? OnLevelUp;
    public event Action<AchievementData>? OnAchievementUnlocked;

    public ProgressionState State => _state;
    public IReadOnlyList<AchievementData> UnlockedAchievements => _unlockedAchievements;
    public int UnlockedAchievementCount => _unlockedAchievements.Count;

    public ProgressionManager(ProgressionState? initialState = null)
    {
        _state = initialState ?? new ProgressionState();
        _unlockedAchievements = new List<AchievementData>();
        _savePath = Path.Combine(AppContext.BaseDirectory, "prototype-profile.json");
        _jsonSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
        _autoSaveTimer = 0f;

        // アンロック済みアチーブメントを初期化
        foreach (var manifest in AchievementManifest.All)
        {
            var unlocked = IsAchievementUnlocked(manifest);
            if (unlocked)
            {
                _unlockedAchievements.Add(AchievementManifest.Clone(manifest.Id));
                _unlockedAchievements[_unlockedAchievements.Count - 1].Unlocked = true;
            }
        }
    }

    // ---- レベル/XP ----

    /// <summary>次のレベルに必要なXP。</summary>
    public static int ExperienceForNextLevel(int level)
    {
        return 180 + ((Math.Max(1, level) - 1) * 120);
    }

    /// <summary>XPを追加し、レベルアップがあればイベントを発火。</summary>
    public int AddXp(int amount)
    {
        var before = _state.AccountLevel;
        _state.CurrentXp += amount;

        var levelUps = 0;
        while (_state.CurrentXp >= ExperienceForNextLevel(_state.AccountLevel))
        {
            _state.CurrentXp -= ExperienceForNextLevel(_state.AccountLevel);
            _state.AccountLevel++;
            levelUps++;
            OnLevelUp?.Invoke($"レベルアップ! LV {_state.AccountLevel}");
            _unlockedAchievements.Clear();
            RefreshAchievements();
        }

        NormalizeState();
        OnProgressChanged?.Invoke(_state);
        return levelUps;
    }

    /// <summary>勝利時のマッチ報酬を適用。</summary>
    public void AwardMatchProgression(bool won, int playerRoundWins, int enemyRoundWins, int matchTeamEliminations, int matchPlayerDeaths)
    {
        var xpGain = 180 + (playerRoundWins * 28) + (enemyRoundWins * 14) + (matchTeamEliminations * 12) + (won ? 120 : 55);
        var rankDelta = (won ? 30 : -16) + (playerRoundWins - enemyRoundWins) * 4 - (matchPlayerDeaths * 2);

        _state.MatchesPlayed++;
        if (won) _state.MatchesWon++;

        _state.AgentCredits += won ? 2 : 1;
        _state.CosmeticTokens += won ? 3 : 2;
        _state.RankRating = Math.Max(0, _state.RankRating + rankDelta);
        _state.CurrentXp += xpGain;
        _state.ActiveContractProgress += Math.Max(1, (won ? 3 : 1) + (matchTeamEliminations / 4));

        var levelUps = 0;
        while (_state.CurrentXp >= ExperienceForNextLevel(_state.AccountLevel))
        {
            _state.CurrentXp -= ExperienceForNextLevel(_state.AccountLevel);
            _state.AccountLevel++;
            levelUps++;
            OnLevelUp?.Invoke($"レベルアップ! LV {_state.AccountLevel}");
        }

        if (_state.ActiveContractProgress >= 12)
        {
            _state.ActiveContractProgress -= 12;
            _state.ContractsCompleted++;
            _state.AgentCredits += 2;
        }

        NormalizeState();
        RefreshAchievements();
        OnProgressChanged?.Invoke(_state);
    }

    // ---- 進行状況補正（難易度） ----

    /// <summary>難易度に応じた進行状況補正を適用。</summary>
    public void ApplyDifficultyModifier(float difficulty)
    {
        _state.DifficultyModifier = Math.Clamp(difficulty, 0.5f, 2.0f);
    }

    /// <summary>難易度補正後のXP値を取得。</summary>
    public int GetXpWithModifier(int baseXp)
    {
        return (int)(baseXp * _state.DifficultyModifier);
    }

    // ---- アチーブメント ----

    /// <summary>アチーブメントがアンロック済みかチェック。</summary>
    public bool IsAchievementUnlocked(AchievementData achievement)
    {
        return _unlockedAchievements.Exists(a => a.Id == achievement.Id);
    }

    /// <summary>アチーブメントの条件を評価。</summary>
    public bool EvaluateAchievementCondition(AchievementData achievement)
    {
        var cond = achievement.Condition;
        return cond.Type switch
        {
            "matchesWon" => _state.MatchesWon >= cond.Target,
            "accountLevel" => _state.AccountLevel >= cond.Target,
            "contractsCompleted" => _state.ContractsCompleted >= cond.Target,
            "totalEliminations" => GetTotalEliminations() >= cond.Target,
            "rankRating" => _state.RankRating >= cond.Target,
            "storePurchases" => GetStorePurchaseCount() >= cond.Target,
            "lifetimeAdImpressions" => _state.LifetimeAdImpressions >= cond.Target,
            "structureSkinsUnlocked" => _state.UnlockedStructureSkins.Count >= cond.Target,
            "bannersUnlocked" => _state.UnlockedBanners.Count >= cond.Target,
            "killEffectsUnlocked" => _state.UnlockedKillEffects.Count >= cond.Target,
            _ => false,
        };
    }

    /// <summary>アチーブメントを解除（アンロック）。</summary>
    public bool UnlockAchievement(AchievementData achievement)
    {
        if (achievement.Unlocked) return false;
        if (!EvaluateAchievementCondition(achievement)) return false;

        achievement.Unlocked = true;
        _unlockedAchievements.Add(achievement);

        // 報酬付与
        _state.AgentCredits += achievement.RewardAgentCredits;
        _state.CosmeticTokens += achievement.RewardCosmeticTokens;

        OnAchievementUnlocked?.Invoke(achievement);
        OnProgressChanged?.Invoke(_state);
        return true;
    }

    /// <summary>全アチーブメントをチェックして新規アンロックを処理。</summary>
    public void CheckAllAchievements()
    {
        foreach (var manifest in AchievementManifest.All)
        {
            if (IsAchievementUnlocked(manifest)) continue;
            var cloned = AchievementManifest.Clone(manifest.Id);
            if (EvaluateAchievementCondition(cloned))
            {
                UnlockAchievement(cloned);
            }
        }
    }

    private void RefreshAchievements()
    {
        _unlockedAchievements.Clear();
        foreach (var manifest in AchievementManifest.All)
        {
            if (IsAchievementUnlocked(manifest))
            {
                _unlockedAchievements.Add(AchievementManifest.Clone(manifest.Id));
                _unlockedAchievements[_unlockedAchievements.Count - 1].Unlocked = true;
            }
        }
    }

    // ---- セーブ/ロード ----

    /// <summary>データをセーブ。</summary>
    public void Save()
    {
        NormalizeState();
        _state.IntegritySalt = GenerateSalt();
        _state.IntegrityStamp = CreateIntegrityStamp(_state);
        var json = JsonConvert.SerializeObject(_state, _jsonSettings);
        File.WriteAllText(_savePath, json);
    }

    /// <summary>データをロード。存在しない場合は新規作成。</summary>
    public ProgressionState Load()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                var json = File.ReadAllText(_savePath);
                var loaded = JsonConvert.DeserializeObject<ProgressionState>(json, _jsonSettings);
                if (loaded is not null && ValidateIntegrity(loaded))
                {
                    _state.AccountLevel = loaded.AccountLevel;
                    _state.CurrentXp = loaded.CurrentXp;
                    _state.AgentCredits = loaded.AgentCredits;
                    _state.CosmeticTokens = loaded.CosmeticTokens;
                    _state.RankRating = loaded.RankRating;
                    _state.MatchesPlayed = loaded.MatchesPlayed;
                    _state.MatchesWon = loaded.MatchesWon;
                    _state.ContractsCompleted = loaded.ContractsCompleted;
                    _state.ActiveContract = loaded.ActiveContract;
                    _state.ActiveContractProgress = loaded.ActiveContractProgress;
                    _state.UnlockedAgents = loaded.UnlockedAgents;
                    _state.UnlockedStructureSkins = loaded.UnlockedStructureSkins;
                    _state.UnlockedAdThemes = loaded.UnlockedAdThemes;
                    _state.UnlockedBanners = loaded.UnlockedBanners;
                    _state.UnlockedKillEffects = loaded.UnlockedKillEffects;
                    _state.SelectedStructureSkin = loaded.SelectedStructureSkin;
                    _state.SelectedAdTheme = loaded.SelectedAdTheme;
                    _state.SelectedBanner = loaded.SelectedBanner;
                    _state.SelectedKillEffect = loaded.SelectedKillEffect;
                    _state.StoreCursor = loaded.StoreCursor;
                    _state.LifetimeAdImpressions = loaded.LifetimeAdImpressions;
                    _state.DifficultyModifier = loaded.DifficultyModifier;
                    return _state;
                }
            }
        }
        catch
        {
            // ロード失敗時は新規状態を使用
        }

        NormalizeState();
        OnProgressChanged?.Invoke(_state);
        return _state;
    }

    /// <summary>データをバックアップセーブ。</summary>
    public void BackupSave()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                var backupPath = _savePath + ".bak";
                File.Copy(_savePath, backupPath, true);
            }
        }
        catch
        {
            // バックアップ失敗は無視
        }
    }

    // ---- ランタイム ----

    /// <summary>自動セーブを更新。</summary>
    public void UpdateAutoSave(float deltaTime)
    {
        _autoSaveTimer += deltaTime;
        if (_autoSaveTimer >= AutoSaveInterval)
        {
            _autoSaveTimer = 0f;
            BackupSave();
            Save();
        }
    }

    /// <summary>現在のランクラベルを取得。</summary>
    public string CurrentRankLabel()
    {
        return _state.RankRating switch
        {
            < 100 => "UNRANKED",
            < 240 => "IRON",
            < 420 => "BRONZE",
            < 640 => "SILVER",
            < 900 => "GOLD",
            < 1200 => "PLATINUM",
            _ => "DIAMOND",
        };
    }

    /// <summary>プロフィールサマリーラインを取得。</summary>
    public string ProfileSummaryLine()
    {
        return $"LV {_state.AccountLevel}  {CurrentRankLabel()}  AGC {_state.AgentCredits}  CT {_state.CosmeticTokens}";
    }

    // ---- ヘルパー ----

    private void NormalizeState()
    {
        _state.AccountLevel = Math.Clamp(_state.AccountLevel, 1, 999);
        _state.CurrentXp = Math.Max(0, _state.CurrentXp);
        _state.AgentCredits = Math.Clamp(_state.AgentCredits, 0, 999999);
        _state.CosmeticTokens = Math.Clamp(_state.CosmeticTokens, 0, 999999);
        _state.RankRating = Math.Max(0, _state.RankRating);
        _state.MatchesPlayed = Math.Max(0, _state.MatchesPlayed);
        _state.MatchesWon = Math.Clamp(_state.MatchesWon, 0, _state.MatchesPlayed);
        _state.ContractsCompleted = Math.Max(0, _state.ContractsCompleted);
        _state.ActiveContractProgress = Math.Clamp(_state.ActiveContractProgress, 0, 11);
        _state.StoreCursor = Math.Clamp(_state.StoreCursor, 0, Math.Max(0, 10 - 1));
        _state.LifetimeAdImpressions = Math.Max(0, _state.LifetimeAdImpressions);

        if (_state.UnlockedAgents == null) _state.UnlockedAgents = new List<string> { "ヴェール" };
        if (_state.UnlockedStructureSkins == null) _state.UnlockedStructureSkins = new List<string> { "シグナル標準" };
        if (_state.UnlockedAdThemes == null) _state.UnlockedAdThemes = new List<string> { "NEO CORE" };
        if (_state.UnlockedBanners == null) _state.UnlockedBanners = new List<string> { "SIGNAL//STANDARD" };
        if (_state.UnlockedKillEffects == null) _state.UnlockedKillEffects = new List<string> { "SIGNAL BURST" };
    }

    private static bool ValidateIntegrity(ProgressionState state)
    {
        if (string.IsNullOrEmpty(state.IntegritySalt)) return false;
        if (string.IsNullOrEmpty(state.IntegrityStamp)) return false;
        var expected = CreateIntegrityStamp(state);
        return state.IntegrityStamp == expected;
    }

    private static string GenerateSalt()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return BitConverter.ToString(bytes).Replace("-", string.Empty);
    }

    private static string CreateIntegrityStamp(ProgressionState state)
    {
        var data = $"{state.AccountLevel}:{state.CurrentXp}:{state.RankRating}:{state.MatchesPlayed}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    private static int GetTotalEliminations() => 0; // GameModelから取得
    private static int GetStorePurchaseCount() => 0; // GameModelから取得
}
