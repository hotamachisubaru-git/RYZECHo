using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// T026 - 品質設定（Quality Settings）の設定用スクリプト。
/// Unity 6 の公開APIで安全に触れる範囲だけを設定する。
/// </summary>
public class QualitySettingsConfigurator : EditorWindow
{
    [MenuItem("Tools/RYZECHo/Build/Configure Quality Settings")]
    public static void ConfigureQualitySettings()
    {
        SetQualityLevelIfExists("PC");

        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.vSyncCount = 0;
        QualitySettings.antiAliasing = 0;
        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.shadowDistance = 150f;
        QualitySettings.shadowResolution = UnityEngine.ShadowResolution.Medium;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.shadowCascades = 2;
        QualitySettings.lodBias = 1.0f;

        EditorUtility.DisplayDialog(
            "Quality Settings",
            "Quality settings configured.\n\nPC: Anisotropic=ON, VSync=OFF, AA=OFF, ShadowDist=150, 2Cascades",
            "OK");

        Debug.Log("[QualitySettingsConfigurator] Quality settings configured.");
    }

    [MenuItem("Tools/RYZECHo/Build/Configure URP Asset")]
    public static void ConfigureURPAsset()
    {
        var urpAsset = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset")
            .Select(guid => AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid)))
            .FirstOrDefault(asset => asset != null);

        if (urpAsset == null)
        {
            Debug.LogWarning("[QualitySettingsConfigurator] No URP Asset found. Create one first.");
            return;
        }

        GraphicsSettings.defaultRenderPipeline = urpAsset;
        QualitySettings.renderPipeline = urpAsset;
        EditorUtility.SetDirty(urpAsset);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "URP Asset",
            $"URP Asset assigned:\n{urpAsset.name}",
            "OK");

        Debug.Log("[QualitySettingsConfigurator] URP Asset assigned: " + urpAsset.name);
    }

    private static void SetQualityLevelIfExists(string qualityName)
    {
        var index = QualitySettings.names.ToList().IndexOf(qualityName);
        if (index >= 0)
        {
            QualitySettings.SetQualityLevel(index, true);
        }
    }
}
