using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// T026 - プラットフォーム固有の設定
/// プラットフォームの判定と設定の切り替え
/// </summary>
[Serializable]
public class PlatformSpecificConfig
{
    [Header("Standalone")]
    public int StandaloneWidth = 1920;
    public int StandaloneHeight = 1080;
    public FullScreenMode StandaloneFullScreenMode = FullScreenMode.FullScreenWindow;

    [Header("Android")]
    public int AndroidWidth = 1280;
    public int AndroidHeight = 720;
#if UNITY_EDITOR
    public AndroidSdkVersions AndroidMinSDK = AndroidSdkVersions.AndroidApiLevel25;
    public AndroidSdkVersions AndroidTargetSDK = AndroidSdkVersions.AndroidApiLevelAuto;
#endif
    public bool AndroidEnableARM64 = true;
    public bool AndroidEnableARMv7 = true;

    [Header("iOS")]
    public string iOSBundleVersion = "1.0.0";
    public string iOSTargetOSVersion = "15.0";
    public bool iOSSupportsRTL = false;

    // プラットフォーム判定
    public string CurrentPlatformName => Application.platform.ToString();
    public bool IsStandalone =>
        Application.platform == RuntimePlatform.WindowsPlayer ||
        Application.platform == RuntimePlatform.OSXPlayer ||
        Application.platform == RuntimePlatform.LinuxPlayer ||
        Application.platform == RuntimePlatform.WindowsEditor ||
        Application.platform == RuntimePlatform.OSXEditor ||
        Application.platform == RuntimePlatform.LinuxEditor;
    public bool IsAndroid => Application.platform == RuntimePlatform.Android;
    public bool IsiOS => Application.platform == RuntimePlatform.IPhonePlayer;
    public bool IsEditor =>
        Application.platform == RuntimePlatform.WindowsEditor ||
        Application.platform == RuntimePlatform.OSXEditor ||
        Application.platform == RuntimePlatform.LinuxEditor;
    public bool IsMobile => IsAndroid || IsiOS;

    private static PlatformSpecificConfig _instance;
    public static PlatformSpecificConfig Instance
    {
        get { if (_instance == null) _instance = new PlatformSpecificConfig(); return _instance; }
    }

    /// <summary>プラットフォームに応じた解像度を取得</summary>
    public (int width, int height) GetResolution()
    {
        if (IsAndroid) return (AndroidWidth, AndroidHeight);
        if (IsiOS) return (1125, 2436);
        return (StandaloneWidth, StandaloneHeight);
    }

    /// <summary>プラットフォームに応じたフルスクリーンモードを取得</summary>
    public FullScreenMode GetFullScreenMode()
    {
        return IsMobile ? FullScreenMode.ExclusiveFullScreen : StandaloneFullScreenMode;
    }

    /// <summary>プラットフォーム名の簡略化</summary>
    public string GetPlatformShortName()
    {
        if (IsEditor) return "Editor";
        if (IsAndroid) return "Android";
        if (IsiOS) return "iOS";
        if (Application.platform == RuntimePlatform.WindowsPlayer) return "Windows";
        if (Application.platform == RuntimePlatform.OSXPlayer) return "Mac";
        if (Application.platform == RuntimePlatform.LinuxPlayer) return "Linux";
        return Application.platform.ToString();
    }

    /// <summary>プラットフォーム固有のログパスを取得</summary>
    public string GetLogPath()
    {
        if (IsMobile) return Application.persistentDataPath + "/logs/production.log";
        return "logs/production.log";
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class AndroidPlatformSettingsAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class iOSPlatformSettingsAttribute : Attribute { }
