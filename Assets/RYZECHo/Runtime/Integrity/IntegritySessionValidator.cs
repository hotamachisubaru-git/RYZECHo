using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RYZECHo.Integrity
{
    /// <summary>
    /// セッションデータの整合性検証を行うクラス
    /// セッションIDの有効性、トークンの検証、データの復元を担当
    /// </summary>
    public static class IntegritySessionValidator
    {
        private const string SessionVersion = "RYZECHo.Session.v1";
        private const int IntegrityTokenLength = 32;

        /// <summary>
        /// セッションデータの整合性をチェック
        /// </summary>
        public static ValidationResult Validate(IntegritySessionData session, string expectedToken = null)
        {
            var errors = new System.Collections.Generic.List<string>();

            // セッションIDの検証
            if (string.IsNullOrEmpty(session.SessionId))
            {
                errors.Add("セッションIDが空です");
            }

            // 開始時刻の検証
            if (session.StartTime > DateTime.UtcNow.AddMinutes(5))
            {
                errors.Add($"開始時刻が未来すぎます: {session.StartTime:O}");
            }

            // 終了時刻の整合性
            if (session.EndTime.HasValue && session.EndTime.Value < session.StartTime)
            {
                errors.Add($"終了時刻が開始時刻より前です");
            }

            // トークンの検証
            if (!string.IsNullOrEmpty(session.IntegrityToken))
            {
                var tokenValid = VerifyToken(session, expectedToken);
                if (!tokenValid)
                {
                    errors.Add("セッショントークンの検証に失敗しました");
                }
            }

            // バージョンの検証
            if (session.Version != SessionVersion)
            {
                errors.Add($"セッションバージョンの不整合: 期待値={SessionVersion}, 実際={session.Version}");
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Session = session,
            };
        }

        /// <summary>
        /// セッションデータを復元（破損したセッションから復旧を試みる）
        /// </summary>
        public static IntegritySessionData? RestoreSession(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var session = JsonUtility.FromJson<IntegritySessionData>(json);
                if (session == null)
                {
                    return null;
                }

                // 復元後に整合性チェック
                var result = Validate(session);
                if (!result.IsValid)
                {
                    // 重大なエラーの場合は破損としてマーク
                    if (result.HasCriticalError())
                    {
                        session.Corrupt();
                        return session;
                    }
                }

                return session;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Integrity] セッションデータの復元失敗: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// セッションのバックアップJSONを作成
        /// </summary>
        public static string CreateBackupJson(IntegritySessionData session)
        {
            session.IntegrityToken = GenerateToken(session);
            return JsonUtility.ToJson(session, true);
        }

        /// <summary>
        /// トークンを検証
        /// </summary>
        private static bool VerifyToken(IntegritySessionData session, string expectedToken)
        {
            try
            {
                var computedToken = GenerateToken(session);
                if (expectedToken != null)
                {
                    return CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(computedToken),
                        Encoding.UTF8.GetBytes(expectedToken));
                }
                return session.IntegrityToken == computedToken;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// セッションからトークンを生成
        /// </summary>
        private static string GenerateToken(IntegritySessionData session)
        {
            var payload = $"{SessionVersion}|{session.SessionId}|{session.StartTime:O}|{session.Version}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// セッションデータをファイルに保存
        /// </summary>
        public static bool SaveSessionToFile(IntegritySessionData session, string filePath)
        {
            try
            {
                var json = CreateBackupJson(session);
                File.WriteAllText(filePath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Integrity] セッションの保存失敗 ({filePath}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// セッションデータをファイルから読み込み
        /// </summary>
        public static IntegritySessionData? LoadSessionFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(filePath, Encoding.UTF8);
                return RestoreSession(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Integrity] セッションの読み込み失敗 ({filePath}): {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// 検証結果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public System.Collections.Generic.List<string> Errors { get; set; } = new();
        public IntegritySessionData? Session { get; set; }

        public bool HasCriticalError()
        {
            foreach (var error in Errors)
            {
                if (error.Contains("セッションIDが空") ||
                    error.Contains("トークンの検証に失敗"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
