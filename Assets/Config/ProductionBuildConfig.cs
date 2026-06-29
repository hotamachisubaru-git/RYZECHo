using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// T026 - 本番ビルドのパラメータ
/// 本番ビルドのパラメータ、パフォーマンス最適化、アセットバンドルの設定
/// </summary>
[Serializable]
public class ProductionBuildConfig
{
    [Header("本番ビルド設定")]
    public bool EnableProductionBuild = true;
#if UNITY_EDITOR
    public BuildOptions BuildOptions = BuildOptions.None;
    public CompressionType Compression = CompressionType.Lz4;
#endif
    public bool EnableIL2CPP = true;

    [Header("パフォーマンス最適化")]
    public int TargetFPS = 60;
    public bool EnableVSync = true;
    public int VSyncCount = 1;
    public int QualityLevel = 3;
    public bool OptimizeLoadingScreen = true;
    public bool CompressAssetBundles = true;
    public int StreamingBufferSizeMB = 64;
    public int MaxTextureSize = 2048;
    public int MaxLODLevel = 2;

    [Header("アセットバンドル設定")]
    public string AssetBundleBaseURL = "https://assets.ryzecho.com/";
    public bool EnableAssetBundleCache = true;
    public int MaxCacheSizeMB = 512;
    public bool ValidateAssetBundles = true;
    public string HashFilePath = "AssetBundles/hash.json";
    public bool EnableCDN = true;
    public string CDNEndpoint = "https://cdn.ryzecho.com/";
    public string FallbackURL = "https://backup.ryzecho.com/";

    private static ProductionBuildConfig _instance;
    public static ProductionBuildConfig Instance
    {
        get { if (_instance == null) _instance = new ProductionBuildConfig(); return _instance; }
    }

    public bool IsProductionBuild
    {
        get { return !IsDevelopmentBuildFlag && EnableProductionBuild; }
    }

    private bool IsDevelopmentBuildFlag
    {
        get {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }

    public void ApplyProductionSettings()
    {
        Application.targetFrameRate = TargetFPS;
        QualitySettings.vSyncCount = VSyncCount;
        if (QualityLevel >= 0) QualitySettings.SetQualityLevel(QualityLevel, true);
    }

    public string GetAssetBundleURL(string bundleName)
    {
        string baseURL = EnableCDN ? CDNEndpoint : AssetBundleBaseURL;
        return $"{baseURL}{bundleName}";
    }

    public string GetCachePath()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, "AssetBundlesCache");
    }
}
