using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RYZECHo.Integrity
{
    /// <summary>
    /// セーブデータの整合性チェックを行うクラス
    /// ハッシュ値の検証、セーブデータの復元、改ざん検知を担当
    /// </summary>
    public static class IntegritySaveDataValidator
    {
        private const string SaveDataVersion = "RYZECHo.SaveData.v1";
        private const int MaxSaveDataSizeBytes = 1024 * 1024; // 1MB

        /// <summary>
        /// セーブデータの整合性をチェック
        /// ハッシュ値の検証と形式のチェックを行う
        /// </summary>
        public static SaveDataValidationResult Validate(IntegritySaveData saveData)
        {
            var errors = new List<string>();

            if (saveData == null)
            {
                return new SaveDataValidationResult { IsValid = false, Errors = new List<string> { "セーブデータがnullです" } };
            }

            // IDの検証
            if (string.IsNullOrEmpty(saveData.Id))
            {
                errors.Add("セーブデータIDが空です");
            }

            // コンテンツサイズの検証
            if (saveData.Content != null)
            {
                var contentBytes = Encoding.UTF8.GetBytes(saveData.Content);
                if (contentBytes.Length > MaxSaveDataSizeBytes)
                {
                    errors.Add($"セーブデータが大きすぎます: {contentBytes.Length}bytes (上限: {MaxSaveDataSizeBytes}bytes)");
                }
            }

            // ハッシュ値の検証
            if (!string.IsNullOrEmpty(saveData.Content) && !string.IsNullOrEmpty(saveData.Hash))
            {
                var expectedHash = ComputeContentHash(saveData.Content);
                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(expectedHash),
                        Encoding.UTF8.GetBytes(saveData.Hash)))
                {
                    errors.Add("セーブデータのハッシュ値が一致しません（改ざんの可能性があります）");
                }
            }

            // バージョンの検証
            if (saveData.Version != SaveDataVersion)
            {
                errors.Add($"セーブデータバージョンの不整合: 期待値={SaveDataVersion}, 実際={saveData.Version}");
            }

            // 時刻の整合性
            if (saveData.UpdatedAt < saveData.CreatedAt)
            {
                errors.Add("更新時刻が作成時刻より前です");
            }

            return new SaveDataValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                SaveData = saveData,
            };
        }

        /// <summary>
        /// セーブデータを復元（JSON文字列から）
        /// </summary>
        public static IntegritySaveData? RestoreSaveData(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var saveData = JsonUtility.FromJson<IntegritySaveData>(json);
                if (saveData == null)
                {
                    return null;
                }

                // ハッシュ値がない場合は再生成
                if (string.IsNullOrEmpty(saveData.Hash) && !string.IsNullOrEmpty(saveData.Content))
                {
                    saveData.Hash = ComputeContentHash(saveData.Content);
                }

                // 整合性チェック
                var result = Validate(saveData);
                if (!result.IsValid && result.HasCriticalError())
                {
                    Debug.LogWarning($"[Integrity] セーブデータの重大な整合性エラー: {string.Join(", ", result.Errors)}");
                }

                return saveData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Integrity] セーブデータの復元失敗: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// コンテンツのハッシュ値を計算
        /// </summary>
        public static string ComputeContentHash(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(content);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// セーブデータをファイルに保存
        /// </summary>
        public static bool SaveToFile(IntegritySaveData saveData, string filePath)
        {
            try
            {
                var json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(filePath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Integrity] セーブデータの保存失敗 ({filePath}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// セーブデータをファイルから読み込み
        /// </summary>
        public static IntegritySaveData? LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(filePath, Encoding.UTF8);
                return RestoreSaveData(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Integrity] セーブデータの読み込み失敗 ({filePath}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 複数のセーブデータの整合性を一括チェック
        /// </summary>
        public static BulkValidationResult ValidateBulk(IList<IntegritySaveData> saveDatas)
        {
            var validCount = 0;
            var invalidCount = 0;
            var criticalErrors = new List<string>();

            foreach (var saveData in saveDatas)
            {
                var result = Validate(saveData);
                if (result.IsValid)
                {
                    validCount++;
                }
                else
                {
                    invalidCount++;
                    if (result.HasCriticalError())
                    {
                        criticalErrors.Add($"[{saveData.Id}] {string.Join("; ", result.Errors)}");
                    }
                }
            }

            return new BulkValidationResult
            {
                IsValid = invalidCount == 0,
                TotalCount = saveDatas.Count,
                ValidCount = validCount,
                InvalidCount = invalidCount,
                CriticalErrors = criticalErrors,
            };
        }
    }

    /// <summary>
    /// セーブデータの検証結果
    /// </summary>
    public class SaveDataValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public IntegritySaveData? SaveData { get; set; }

        public bool HasCriticalError()
        {
            foreach (var error in Errors)
            {
                if (error.Contains("ハッシュ値が一致しません") ||
                    error.Contains("セーブデータIDが空"))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 一括検証結果
    /// </summary>
    public class BulkValidationResult
    {
        public bool IsValid { get; set; }
        public int TotalCount { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public List<string> CriticalErrors { get; set; } = new();
    }
}
