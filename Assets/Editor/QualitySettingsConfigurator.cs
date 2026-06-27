using UnityEngine;
using UnityEditor;

/// <summary>
/// T026 - 品質設定（Quality Settings）の設定用スクリプト
/// 開発/本番向けにプリセットを構成
/// </summary>
public class QualitySettingsConfigurator : EditorWindow
{
    [MenuItem("Tools/RYZECHo/Build/Configure Quality Settings")]
    public static void ConfigureQualitySettings()
    {
        // --- PC (本番) ---
        QualitySettings.SetQualityByName("PC");
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.vSyncCount = 0; // VSync OFF (FPS重視)

        // --- Mobile (Android) ---
        var mobileQs = QualitySettings.GetQualityLevelByName("Medium") ?? QualitySettings.GetQualityLevelByName("Simple");
        // Mobile 向けの設定が既存の場合は上書き

        // --- シェーダーストライプ ---
        Shader.streamingMaxMipMapSize = 2048;

        // --- Anti-Aliasing ---
        PlayerSettings.SetAntiAliasing(1, 1); // PC: 1x
        PlayerSettings.SetAntiAliasing(2, 2); // Mobile: 2x

        // --- Texture Quality ---
        QualitySettings.masterTextureLimit = 0;

        // --- Fog ---
        QualitySettings.fogMode = FogMode.ExponentialSquared;

        // --- Shadows ---
        QualitySettings.shadowDistance = 150f;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.shadowProjection = ShadowProjection.Close2Near;
        QualitySettings.shadowCascades = ShadowCascades.TwoCascades;

        // --- LOD Bias ---
        QualitySettings.lodBias = new[] { 1.0f, 1.0f, 1.0f };

        // --- Max LOD Level ---
        QualitySettings.maxLODLevel = -1; // All LODs

        EditorUtility.DisplayDialog("Quality Settings",
            "Quality settings configured.\n\n" +
            "PC: Anisotropic=ON, VSync=OFF, AA=1x, ShadowDist=150, 2Cascades\n" +
            "Mobile: AA=2x\n" +
            "Fog: ExponentialSquared\n" +
            "Shadows: Close2Near, Medium Res",
            "OK");

        Debug.Log("[QualitySettingsConfigurator] Quality settings configured.");
    }

    [MenuItem("Tools/RYZECHo/Build/Configure URP Asset")]
    public static void ConfigureURPAsset()
    {
        var urpAsset = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset")
            .Select(guid => AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid)))
            .FirstOrDefault(a => a != null);

        if (urpAsset == null)
        {
            Debug.LogWarning("[QualitySettingsConfigurator] No URP Asset found. Create one first.");
            return;
        }

        // --- Rendering ---
        urpAsset.frameSettings.EnableKeyword(ShaderKeywordStrings.MainLightShadows);
        urpAsset.frameSettings.EnableKeyword(ShaderKeywordStrings.MainLightShadowMasks);
        urpAsset.frameSettings.EnableKeyword(ShaderKeywordStrings.SublightShadows);
        urpAsset.frameSettings.EnableKeyword(ShaderKeywordStrings.SublightShadowsMasks);
        urpAsset.frameSettings.EnableKeyword(ShaderKeywordStrings.CoordsFromShader);
        urpAsset.frameSettings.EnableKeyword(ShaderKeywordStrings.MotionStrings);

        urpAsset.shadowDistance = 150f;
        urpAsset.shadowCascadeCount = 2;
        urpAsset.shadowCascade2Split = 0.333f;
        urpAsset.shadowCascade4Split = new Vector3(0.067f, 0.2f, 0.466f);
        urpAsset.shadowBinding = ShadowBinding.SubdivideNormals;
        urpAsset.shadowResolution = RenderTextureFormat.ARGB32;
        urpAsset.maxShadowPropagation = 0;

        urpAsset.visibleShadowModes = (int)ShadowMaskMode.ClosestMask | (int)ShadowMaskMode.MixedMask;

        urpAsset.mainLightShadowmapSize = 2048;
        urpAsset.additionalLightsResolution = AdditionalLightResolution.PerPixel;
        urpAsset.additionalLightsPerRegion = 64;

        urpAsset.allowDepthPrinterPass = false;
        urpAsset.supportsTerrainHoles = true;
        urpAsset.supportsRuntimeStencilMask = true;

        urpAsset.renderScale = 1.0f;
        urpAsset.fullScreenPass = FullScreenPass.FilmGrain;

        // --- Renderer ---
        var renderer = urpAsset.defaultRenderer;
        if (renderer != null)
        {
            var rendererFeatures = renderer.rendererFeatures;
            // Post-processing 用
            urpAsset.postProcessingFeatures = new System.Collections.Generic.List<UniversalRendererFeature>();
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("URP Asset",
            "URP Asset configured:\n" +
            "Shadow Dist=150, 2Cascades, Res=2048\n" +
            "Main Light Shadows: ON\n" +
            "Additional Lights: Per-Pixel (64/region)\n" +
            "Render Scale: 1.0x\n" +
            "Film Grain: ON",
            "OK");

        Debug.Log("[QualitySettingsConfigurator] URP Asset configured: " + urpAsset.name);
    }
}
