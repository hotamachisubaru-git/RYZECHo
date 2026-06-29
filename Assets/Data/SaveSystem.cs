using System;
using System.IO;
using UnityEngine;
using System.Text;
using RYZECHo.Data;

namespace RYZECHo.Data
{
    /// <summary>
    /// JSONベースのセーブシステム。
    /// ProgressProfile連携、PlayerPrefs + JSONファイル両方の保存方法をサポート。
    /// 自動セーブ機能を含む。
    /// </summary>
    public class SaveSystem : MonoBehaviour, IDisposable
    {
        #region Singleton

        private static SaveSystem _instance;
        public static SaveSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SaveSystem");
                    _instance = go.AddComponent<SaveSystem>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        #endregion

        #region Configuration

        /// <summary>セーブデータバージョン</summary>
        public const string SaveVersion = "1.0.0";

        /// <summary>JSONセーブファイルのキー（PlayerPrefs用）</summary>
        public const string SaveDataKey = "RYZECHo_GameSaveData";

        /// <summary>自動セーブの間隔（秒）</summary>
        public float autoSaveInterval = 30f;

        /// <summary>自動セーブ有効フラグ</summary>
        public bool autoSaveEnabled = true;

        /// <summary>JSONファイルの保存パス</summary>
        public string SaveFilePath => Path.Combine(Application.persistentDataPath, "ryzecho_save.json");

        #endregion

        #region State

        private float _autoSaveTimer;
        private bool _dataDirty = false;
        private bool _isDisposed = false;

        // 現在のゲーム進行状況（ProgressProfileのラッパー）
        private ProgressProfile _currentProfile;

        #endregion

        #region Events

        /// <summary>セーブ完了時に発火</summary>
        public event Action<string> OnSaveCompleted;

        /// <summary>ロード完了時に発火</summary>
        public event Action OnLoadCompleted;

        /// <summary>セーブエラー時に発火</summary>
        public event Action<string> OnSaveError;

        /// <summary>ロードエラー時に発火</summary>
        public event Action<string> OnLoadError;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (_isDisposed) return;

            // 自動セーブ
            if (autoSaveEnabled && _dataDirty)
            {
                _autoSaveTimer += Time.deltaTime;
                if (_autoSaveTimer >= autoSaveInterval)
                {
                    _autoSaveTimer = 0f;
                    AutoSave();
                }
            }
        }

        private void OnDestroy()
        {
            // 終了時に未保存データを保存
            if (_dataDirty)
            {
                Save();
            }
            _isDisposed = true;
        }

        #endregion

        #region ProgressProfile Integration

        /// <summary>現在のProgressProfileを設定</summary>
        public void SetProgressProfile(ProgressProfile profile)
        {
            _currentProfile = profile;
        }

        /// <summary>現在のProgressProfileを取得</summary>
        public ProgressProfile GetProgressProfile()
        {
            return _currentProfile;
        }

        /// <summary>ProgressProfileからセーブデータを生成</summary>
        public GameSaveData CreateSaveDataFromProfile(ProgressProfile profile)
        {
            var saveData = new GameSaveData();
            saveData.FromProgressProfile(profile);
            saveData.saveVersion = SaveVersion;
            return saveData;
        }

        /// <summary>セーブデータをProgressProfileに適用</summary>
        public void ApplySaveDataToProfile(GameSaveData saveData, ProgressProfile profile)
        {
            saveData.ToProgressProfile(profile);
            // Integrity検証
            if (profile.IntegritySalt != null && profile.IntegrityStamp != null)
            {
                // Integrity検証はGameModel側で行う
            }
        }

        #endregion

        #region Save Operations

        /// <summary>ゲーム進行状況を保存（PlayerPrefs + JSONファイル両方）</summary>
        public bool Save(ProgressProfile profile = null)
        {
            try
            {
                var saveData = CreateSaveData(profile);

                // PlayerPrefsにJSONとして保存
                var json = JsonUtility.ToJson(saveData, true);
                PlayerPrefs.SetString(SaveDataKey, json);
                PlayerPrefs.Save();

                // JSONファイルにもバックアップ保存
                File.WriteAllText(SaveFilePath, json, Encoding.UTF8);

                _dataDirty = false;
                _autoSaveTimer = 0f;

                Debug.Log($"[SaveSystem] Game saved. File: {SaveFilePath}");
                OnSaveCompleted?.Invoke(SaveFilePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
                OnSaveError?.Invoke(e.Message);
                return false;
            }
        }

        /// <summary>指定したProgressProfileからセーブデータを生成</summary>
        private GameSaveData CreateSaveData(ProgressProfile profile)
        {
            var saveData = GameSaveData.CreateDefault();

            if (profile != null)
            {
                saveData.FromProgressProfile(profile);
            }
            else if (_currentProfile != null)
            {
                saveData.FromProgressProfile(_currentProfile);
            }

            saveData.saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return saveData;
        }

        /// <summary>プレイヤーのデータをマーク（自動セーブ用）</summary>
        public void MarkDirty()
        {
            _dataDirty = true;
        }

        /// <summary>自動セーブ</summary>
        public bool AutoSave()
        {
            if (!_dataDirty) return false;
            return Save();
        }

        #endregion

        #region Load Operations

        /// <summary>ゲームデータをロード（PlayerPrefs → JSONファイル → デフォルト）</summary>
        public GameSaveData Load()
        {
            try
            {
                // 1. PlayerPrefsから試す
                var playerPrefsData = LoadFromPlayerPrefs();
                if (playerPrefsData != null)
                {
                    Debug.Log("[SaveSystem] Loaded from PlayerPrefs.");
                    OnLoadCompleted?.Invoke();
                    return playerPrefsData;
                }

                // 2. JSONファイルから試す
                var fileData = LoadFromFile();
                if (fileData != null)
                {
                    Debug.Log("[SaveSystem] Loaded from JSON file.");
                    OnLoadCompleted?.Invoke();
                    return fileData;
                }

                // 3. デフォルトデータ
                Debug.Log("[SaveSystem] No save data found. Using default.");
                OnLoadCompleted?.Invoke();
                return GameSaveData.CreateDefault();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
                OnLoadError?.Invoke(e.Message);
                return GameSaveData.CreateDefault();
            }
        }

        /// <summary>ロードしたデータをProgressProfileに適用</summary>
        public bool LoadAndApply(ProgressProfile profile)
        {
            var saveData = Load();
            if (saveData == null) return false;

            ApplySaveDataToProfile(saveData, profile);
            return true;
        }

        /// <summary>PlayerPrefsからデータをロード</summary>
        private GameSaveData LoadFromPlayerPrefs()
        {
            if (!PlayerPrefs.HasKey(SaveDataKey)) return null;

            try
            {
                var json = PlayerPrefs.GetString(SaveDataKey);
                return JsonUtility.FromJson<GameSaveData>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>JSONファイルからデータをロード</summary>
        private GameSaveData LoadFromFile()
        {
            if (!File.Exists(SaveFilePath)) return null;

            try
            {
                var json = File.ReadAllText(SaveFilePath, Encoding.UTF8);
                return JsonUtility.FromJson<GameSaveData>(json);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region File Operations

        /// <summary>セーブファイルが存在するか確認</summary>
        public bool HasSaveFile()
        {
            return File.Exists(SaveFilePath) || PlayerPrefs.HasKey(SaveDataKey);
        }

        /// <summary>セーブファイルを削除</summary>
        public bool DeleteSaveFile()
        {
            var deleted = false;

            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                deleted = true;
            }

            if (PlayerPrefs.HasKey(SaveDataKey))
            {
                PlayerPrefs.DeleteKey(SaveDataKey);
                PlayerPrefs.Save();
                deleted = true;
            }

            _dataDirty = false;
            return deleted;
        }

        /// <summary>セーブファイルのサイズ（バイト）</summary>
        public long GetSaveFileSize()
        {
            if (!File.Exists(SaveFilePath)) return 0;
            return new FileInfo(SaveFilePath).Length;
        }

        /// <summary>セーブファイルの最終更新日時</summary>
        public DateTime? GetSaveFileLastWriteTime()
        {
            if (!File.Exists(SaveFilePath)) return null;
            return File.GetLastWriteTimeUtc(SaveFilePath);
        }

        #endregion

        #region Utility

        /// <summary>セーブデータの統計情報を取得</summary>
        public SaveStatistics GetSaveStatistics()
        {
            var stats = new SaveStatistics();
            stats.hasPlayerPrefsSave = PlayerPrefs.HasKey(SaveDataKey);
            stats.hasFileSave = File.Exists(SaveFilePath);
            stats.fileSizeBytes = GetSaveFileSize();
            stats.lastWriteTime = GetSaveFileLastWriteTime();
            return stats;
        }

        /// <summary>セーブデータのエクスポート（Base64文字列）</summary>
        public string ExportSaveData(ProgressProfile profile)
        {
            var saveData = CreateSaveData(profile);
            var json = JsonUtility.ToJson(saveData, true);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        /// <summary>セーブデータのエクスポート（JSON文字列）</summary>
        public string ExportSaveDataToJson(ProgressProfile profile)
        {
            var saveData = CreateSaveData(profile);
            return JsonUtility.ToJson(saveData, true);
        }

        /// <summary>Base64文字列からセーブデータをインポート</summary>
        public GameSaveData ImportSaveData(string base64Data)
        {
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64Data));
                return JsonUtility.FromJson<GameSaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Import failed: {e.Message}");
                return null;
            }
        }

        /// <summary>JSON文字列からセーブデータをインポート</summary>
        public GameSaveData ImportSaveDataFromJson(string jsonData)
        {
            try
            {
                return JsonUtility.FromJson<GameSaveData>(jsonData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] JSON import failed: {e.Message}");
                return null;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_dataDirty)
                {
                    Save();
                }
                _isDisposed = true;
            }
        }

        #endregion
    }

    /// <summary>セーブデータの統計情報</summary>
    public class SaveStatistics
    {
        public bool hasPlayerPrefsSave;
        public bool hasFileSave;
        public long fileSizeBytes;
        public DateTime? lastWriteTime;
        public string SaveFilePath => Path.Combine(Application.persistentDataPath, "ryzecho_save.json");
    }
}
