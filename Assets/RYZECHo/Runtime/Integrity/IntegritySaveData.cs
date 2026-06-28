using System;

namespace RYZECHo.Integrity
{
    /// <summary>
    /// セーブデータ — ID、内容、ハッシュ値を保持
    /// </summary>
    [Serializable]
    public class IntegritySaveData
    {
        /// <summary>セーブデータのID</summary>
        public string Id { get; set; }

        /// <summary>セーブデータの内容（JSON文字列）</summary>
        public string Content { get; set; }

        /// <summary>セーブデータのハッシュ値</summary>
        public string Hash { get; set; }

        /// <summary>セーブデータの作成時刻</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>セーブデータの更新時刻</summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>セーブデータのバージョン</summary>
        public string Version { get; set; }

        /// <summary>セーブデータのラベル（ユーザー表示用）</summary>
        public string Label { get; set; }

        public IntegritySaveData()
        {
            Id = Guid.NewGuid().ToString("N");
            Content = string.Empty;
            Hash = string.Empty;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Version = "1.0.0";
            Label = "Unnamed Save";
        }

        public IntegritySaveData(string content, string label = "Unnamed Save")
        {
            Id = Guid.NewGuid().ToString("N");
            Content = content ?? string.Empty;
            Hash = ComputeHash(content);
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = CreatedAt;
            Version = "1.0.0";
            Label = label;
        }

        /// <summary>
        /// コンテンツを更新してハッシュ値を再生成
        /// </summary>
        public void UpdateContent(string newContent, string newLabel = null)
        {
            Content = newContent ?? string.Empty;
            Hash = ComputeHash(Content);
            UpdatedAt = DateTime.UtcNow;
            if (newLabel != null)
            {
                Label = newLabel;
            }
        }

        /// <summary>
        /// コンテンツのハッシュ値を計算
        /// </summary>
        private static string ComputeHash(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// 現在のハッシュ値がコンテンツと一致するか確認
        /// </summary>
        public bool IsHashValid()
        {
            if (string.IsNullOrEmpty(Content) || string.IsNullOrEmpty(Hash))
            {
                return true;
            }
            return Hash == ComputeHash(Content);
        }
    }
}
