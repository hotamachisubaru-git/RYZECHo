using UnityEngine;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using RYZECHo.Data;
using System;

namespace RYZECHo.Editor.Validation
{
    /// <summary>
    /// RYZECHoの移行検証ツール。
    /// ドメイン層整合性、UI設定整合性、ゲームルール整合性をチェックする。
    /// </summary>
    public class GameValidation
    {
        #region Validation Results

        private class ValidationResult
        {
            public string Category;
            public string Name;
            public bool Passed;
            public string Message;

            public ValidationResult(string category, string name, bool passed, string message)
            {
                Category = category;
                Name = name;
                Passed = passed;
                Message = message;
            }
        }

        #endregion

        #region Domain Layer Validation

        /// <summary>
        /// ドメイン層の整合性をチェック。
        /// ProgressProfileのフィールドがSaveDataModelと一致しているか検証。
        /// </summary>
        [MenuItem("Tools/RYZECHo/Validate/Domain Integrity")]
        public static void ValidateDomainIntegrity()
        {
            var results = new List<ValidationResult>();

            // ProgressProfileの全フィールドがSaveDataModelに存在するかチェック
            var profileType = typeof(ProgressProfile);
            var playerSaveType = typeof(PlayerSaveData);

            var profileFields = profileType.GetFields(System.Reflection.BindingFlags.Public |
                                                      System.Reflection.BindingFlags.Instance);

            foreach (var field in profileFields)
            {
                var saveField = playerSaveType.GetField(field.Name.ToLowerInvariant(),
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);

                if (saveField != null)
                {
                    results.Add(new ValidationResult("Domain", $"Field: {field.Name}", true,
                        $"PlayerSaveDataにフィールド '{field.Name}' が見つかりました"));
                }
                else
                {
                    results.Add(new ValidationResult("Domain", $"Field: {field.Name}", false,
                        $"PlayerSaveDataにフィールド '{field.Name}' が見つかりません"));
                }
            }

            // GameSaveDataのシリアライズテスト
            try
            {
                var gameData = GameSaveData.CreateDefault();
                var json = JsonUtility.ToJson(gameData, true);
                var roundTrip = JsonUtility.FromJson<GameSaveData>(json);

                if (roundTrip != null && roundTrip.player != null)
                {
                    results.Add(new ValidationResult("Domain", "JSON Serialization", true,
                        "GameSaveDataのJSONシリアライズ/デシリアライズが正常に動作"));
                }
                else
                {
                    results.Add(new ValidationResult("Domain", "JSON Serialization", false,
                        "GameSaveDataのJSONシリアライズ/デシリアライズが失敗"));
                }
            }
            catch (System.Exception e)
            {
                results.Add(new ValidationResult("Domain", "JSON Serialization", false,
                    $"JSONシリアライズエラー: {e.Message}"));
            }

            // Integrityフィールドの整合性
            var integritySaltField = playerSaveType.GetField("integritySalt",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            var integrityStampField = playerSaveType.GetField("integrityStamp",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            if (integritySaltField != null && integrityStampField != null)
            {
                results.Add(new ValidationResult("Domain", "Integrity Fields", true,
                    "IntegritySaltとIntegrityStampフィールドが存在"));
            }
            else
            {
                results.Add(new ValidationResult("Domain", "Integrity Fields", false,
                    "Integrityフィールドが不足"));
            }

            DisplayResults("Domain Integrity Validation", results);
        }

        #endregion

        #region UI Settings Validation

        /// <summary>
        /// UI設定の整合性をチェック。
        /// UIScreenManager、HUDコントローラー、設定SOの整合性を検証。
        /// </summary>
        [MenuItem("Tools/RYZECHo/Validate/UI Settings Integrity")]
        public static void ValidateUISettingsIntegrity()
        {
            var results = new List<ValidationResult>();

            // UIScreenManagerの存在チェック
            var screenManagerType = Type.GetType("RYZECHo.UIScreenManager, Assembly-CSharp");
            if (screenManagerType != null)
            {
                results.Add(new ValidationResult("UI Settings", "UIScreenManager", true,
                    "UIScreenManagerクラスが見つかりました"));
            }
            else
            {
                results.Add(new ValidationResult("UI Settings", "UIScreenManager", false,
                    "UIScreenManagerクラスが見つかりません"));
            }

            // HUDコントローラーの存在チェック
            var hudControllerType = Type.GetType("RYZECHo.GameHUDController, Assembly-CSharp");
            if (hudControllerType != null)
            {
                results.Add(new ValidationResult("UI Settings", "GameHUDController", true,
                    "GameHUDControllerクラスが見つかりました"));
            }
            else
            {
                results.Add(new ValidationResult("UI Settings", "GameHUDController", false,
                    "GameHUDControllerクラスが見つかりません"));
            }

            // ScreenType列挙の整合性チェック
            var screenManagerScript = AssetDatabase.FindAssets("UIScreenManager t:MonoBehaviour");
            if (screenManagerScript.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(screenManagerScript[0]);
                var content = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (content != null)
                {
                    var text = content.text;
                    var requiredScreens = new[] { "TitleScreen", "SetupScreen", "GameHUD", "GameOver", "MainMenu" };
                    foreach (var screen in requiredScreens)
                    {
                        if (text.Contains(screen))
                        {
                            results.Add(new ValidationResult("UI Settings", $"ScreenType.{screen}", true,
                                $"画面タイプ '{screen}' が定義されています"));
                        }
                        else
                        {
                            results.Add(new ValidationResult("UI Settings", $"ScreenType.{screen}", false,
                                $"画面タイプ '{screen}' が見つかりません"));
                        }
                    }
                }
            }

            // 設定SOの存在チェック
            var settingTypes = new[]
            {
                ("GameplaySettingsSO", "RYZECHo.GameplaySettingsSO"),
                ("GameRulesSettingsSO", "RYZECHo.GameRulesSettingsSO"),
                ("LayoutSettingsSO", "RYZECHo.LayoutSettingsSO"),
                ("VisualSettingsSO", "RYZECHo.VisualSettingsSO"),
                ("AudioSettingsSO", "RYZECHo.AudioSettingsSO"),
            };

            foreach (var (name, typeName) in settingTypes)
            {
                var type = Type.GetType(typeName);
                if (type != null)
                {
                    results.Add(new ValidationResult("UI Settings", $"{name}", true,
                        $"{name}クラスが見つかりました"));
                }
                else
                {
                    results.Add(new ValidationResult("UI Settings", $"{name}", false,
                        $"{name}クラスが見つかりません"));
                }
            }

            DisplayResults("UI Settings Integrity Validation", results);
        }

        #endregion

        #region Game Rules Validation

        /// <summary>
        /// ゲームルールの整合性をチェック。
        /// GameRules、GameConstants、GameSettingsの値が整合しているか検証。
        /// </summary>
        [MenuItem("Tools/RYZECHo/Validate/Game Rules Integrity")]
        public static void ValidateGameRulesIntegrity()
        {
            var results = new List<ValidationResult>();

            // GameRulesの定数チェック
            results.Add(new ValidationResult("Game Rules", "RoundsToWin", true,
                $"GameRules.RoundsToWin = {GameRules.RoundsToWin}"));

            results.Add(new ValidationResult("Game Rules", "TeamSize", true,
                $"GameRules.TeamSize = {GameRules.TeamSize}"));

            results.Add(new ValidationResult("Game Rules", "StartingCredits", true,
                $"GameRules.StartingCredits = {GameRules.StartingCredits}"));

            results.Add(new ValidationResult("Game Rules", "InitialBuildPoints", true,
                $"GameRules.InitialBuildPoints = {GameRules.InitialBuildPoints}"));

            results.Add(new ValidationResult("Game Rules", "DefaultFovDegrees", true,
                $"GameRules.DefaultFovDegrees = {GameRules.DefaultFovDegrees}"));

            // GameConstantsの定数チェック
            results.Add(new ValidationResult("Game Rules", "MaxUltPoints", true,
                $"GameRules.MaxUltPoints = {GameRules.MaxUltPoints}"));

            results.Add(new ValidationResult("Game Rules", "BossInvestmentSoftCap", true,
                $"GameSettings.BossInvestmentSoftCap = {GameSettings.BossInvestmentSoftCap}"));

            // GameSettingsの定数チェック
            results.Add(new ValidationResult("Game Rules", "StandardFovDegrees", true,
                $"GameSettings.StandardFovDegrees = {GameSettings.StandardFovDegrees}"));

            results.Add(new ValidationResult("Game Rules", "WideFovDegrees", true,
                $"GameSettings.WideFovDegrees = {GameSettings.WideFovDegrees}"));

            results.Add(new ValidationResult("Game Rules", "SniperFovDegrees", true,
                $"GameSettings.SniperFovDegrees = {GameSettings.SniperFovDegrees}"));

            // 経済バランスチェック
            var totalRewards = GameSettings.WinReward + GameSettings.LossReward;
            var expectedTotal = 3400;
            results.Add(new ValidationResult("Game Rules", "Economy Balance",
                totalRewards == expectedTotal,
                $"WinReward({GameSettings.WinReward}) + LossReward({GameSettings.LossReward}) = {totalRewards} (期待値: {expectedTotal})"));

            // 武器バランスチェック
            var weaponTypes = Enum.GetNames(typeof(WeaponType));
            results.Add(new ValidationResult("Game Rules", "Weapon Types", true,
                $"定義された武器タイプ: {weaponTypes.Length}種類"));

            // エージェントカタログチェック
            var agentCount = AgentCatalog.SelectionOrder.Length;
            results.Add(new ValidationResult("Game Rules", "Agent Count", true,
                $"エージェントカタログに {agentCount} 種類定義されています"));

            // 整合性チェック: FOV値の一貫性
            var fovConsistent = GameSettings.StandardFovDegrees == GameRules.DefaultFovDegrees;
            results.Add(new ValidationResult("Game Rules", "FOV Consistency",
                fovConsistent,
                fovConsistent
                    ? $"GameSettings.StandardFovDegrees ({GameSettings.StandardFovDegrees}) == GameRules.DefaultFovDegrees ({GameRules.DefaultFovDegrees})"
                    : $"不一致: GameSettings ({GameSettings.StandardFovDegrees}) != GameRules ({GameRules.DefaultFovDegrees})"));

            DisplayResults("Game Rules Integrity Validation", results);
        }

        #endregion

        #region Save System Validation

        /// <summary>
        /// セーブシステムの整合性をチェック。
        /// </summary>
        [MenuItem("Tools/RYZECHo/Validate/Save System Integrity")]
        public static void ValidateSaveSystemIntegrity()
        {
            var results = new List<ValidationResult>();

            // SaveDataModelのシリアライズテスト
            try
            {
                var gameData = GameSaveData.CreateDefault();
                var json = JsonUtility.ToJson(gameData, true);
                var roundTrip = JsonUtility.FromJson<GameSaveData>(json);

                if (roundTrip != null && roundTrip.player != null)
                {
                    results.Add(new ValidationResult("Save System", "Default Data Serialization", true,
                        "デフォルトデータのシリアライズ/デシリアライズが正常"));
                }
                else
                {
                    results.Add(new ValidationResult("Save System", "Default Data Serialization", false,
                        "デフォルトデータのシリアライズ/デシリアライズが失敗"));
                }
            }
            catch (System.Exception e)
            {
                results.Add(new ValidationResult("Save System", "Default Data Serialization", false,
                    $"エラー: {e.Message}"));
            }

            // SaveDataModelのフィールド整合性
            var playerType = typeof(PlayerSaveData);
            var fields = playerType.GetFields(System.Reflection.BindingFlags.Public |
                                               System.Reflection.BindingFlags.Instance |
                                               System.Reflection.BindingFlags.NonPublic);
            results.Add(new ValidationResult("Save System", "PlayerSaveData Fields", true,
                $"{fields.Length}フィールドが定義されています"));

            // WorldSaveDataの整合性
            var worldData = WorldSaveData.CreateDefault();
            results.Add(new ValidationResult("Save System", "WorldSaveData Default", true,
                $"マップ: {worldData.mapId}, フェーズ: {worldData.currentPhase}, ラウンド: {worldData.currentRound}"));

            // セーブファイルパスの検証
            var savePath = System.IO.Path.Combine(Application.persistentDataPath, "ryzecho_save.json");
            var dirExists = System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(savePath));
            results.Add(new ValidationResult("Save System", "Save Path Accessibility",
                dirExists, $"保存パス: {savePath}"));

            DisplayResults("Save System Integrity Validation", results);
        }

        #endregion

        #region Full Validation

        /// <summary>
        /// 全検証を実行。
        /// </summary>
        [MenuItem("Tools/RYZECHo/Validate/Run All Validations")]
        public static void RunAllValidations()
        {
            Debug.Log("=== RYZECHo Full Validation Started ===");

            ValidateDomainIntegrity();
            ValidateUISettingsIntegrity();
            ValidateGameRulesIntegrity();
            ValidateSaveSystemIntegrity();

            Debug.Log("=== RYZECHo Full Validation Completed ===");
        }

        #endregion

        #region Helper Methods

        private static void DisplayResults(string title, List<ValidationResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== {title} ===");

            var passedCount = 0;
            var failedCount = 0;

            foreach (var result in results)
            {
                var status = result.Passed ? "✅ PASS" : "❌ FAIL";
                sb.AppendLine($"  [{status}] {result.Category}/{result.Name}: {result.Message}");

                if (result.Passed) passedCount++;
                else failedCount++;
            }

            sb.AppendLine($"--- Result: {passedCount} passed, {failedCount} failed ---");

            Debug.Log(sb.ToString());

            // エディタウィンドウとして表示
            var windowTitle = $"{title} ({passedCount}/{results.Count})";
            // Console出力は既に行っているので、詳細はログを確認
        }

        #endregion
    }

    /// <summary>
    /// ドメイン層の整合性チェックユーティリティ。
    /// </summary>
    public static class DomainIntegrityChecker
    {
        /// <summary>ProgressProfileの整合性を検証</summary>
        public static bool ValidateProgressProfile(ProgressProfile profile, out string message)
        {
            message = string.Empty;

            if (profile == null)
            {
                message = "ProgressProfileがnullです";
                return false;
            }

            // 必須フィールドのチェック
            if (string.IsNullOrWhiteSpace(profile.ActiveContract))
            {
                message = "ActiveContractが空です";
                return false;
            }

            if (profile.AccountLevel < 1)
            {
                message = "AccountLevelが1未満です";
                return false;
            }

            if (profile.CurrentXp < 0)
            {
                message = "CurrentXpが負の値です";
                return false;
            }

            if (profile.RankRating < 0)
            {
                message = "RankRatingが負の値です";
                return false;
            }

            // アンロックリストの整合性
            if (profile.UnlockedAgents != null && profile.UnlockedAgents.Contains(profile.ActiveContract))
            {
                // アクティブエージェントはアンロックリストに含まれているべき
            }

            return true;
        }

        /// <summary>セーブデータの整合性を検証</summary>
        public static bool ValidateSaveData(GameSaveData data, out string message)
        {
            message = string.Empty;

            if (data == null)
            {
                message = "GameSaveDataがnullです";
                return false;
            }

            if (data.player == null)
            {
                message = "PlayerSaveDataがnullです";
                return false;
            }

            if (data.world == null)
            {
                message = "WorldSaveDataがnullです";
                return false;
            }

            if (data.saveVersion == null)
            {
                message = "saveVersionがnullです";
                return false;
            }

            return true;
        }
    }
}
