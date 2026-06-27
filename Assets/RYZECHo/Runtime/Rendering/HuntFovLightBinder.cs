using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RYZECHo.Unity;

[ExecuteAlways]
[RequireComponent(typeof(HuntFovRenderer))]
public sealed class HuntFovLightBinder : MonoBehaviour
{
    [SerializeField] private Light2D visionLight;
    [SerializeField, Range(0f, 1f)] private float innerRadiusRatio = 0.58f;
    [SerializeField, Range(0f, 45f)] private float angleFeatherDegrees = 10f;
    [SerializeField, Min(0f)] private float intensity = 0.85f;
    [SerializeField, Range(0f, 1f)] private float falloffIntensity = 0.65f;
    [SerializeField] private UnityEngine.Color lightColor = new(0.55f, 0.88f, 1f, 1f);

    private HuntFovRenderer _fovRenderer;

    private void OnEnable()
    {
        _fovRenderer = GetComponent<HuntFovRenderer>();
        EnsureLight();
        Apply();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        _fovRenderer = GetComponent<HuntFovRenderer>();
        EnsureLight();
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void EnsureLight()
    {
        if (visionLight != null)
        {
            return;
        }

        visionLight = GetComponentInChildren<Light2D>();
        if (visionLight != null)
        {
            return;
        }

        var lightObject = new GameObject("FOV Light 2D");
        lightObject.transform.SetParent(transform, worldPositionStays: false);
        lightObject.transform.localPosition = Vector3.zero;
        lightObject.transform.localRotation = Quaternion.identity;
        lightObject.transform.localScale = Vector3.one;
        visionLight = lightObject.AddComponent<Light2D>();
    }

    private void Apply()
    {
        if (_fovRenderer == null || visionLight == null)
        {
            return;
        }

        visionLight.lightType = Light2D.LightType.Point;
        visionLight.color = lightColor;
        visionLight.intensity = intensity;
        visionLight.falloffIntensity = falloffIntensity;
        visionLight.pointLightOuterRadius = _fovRenderer.Radius;
        visionLight.pointLightInnerRadius = _fovRenderer.Radius * innerRadiusRatio;
        visionLight.pointLightOuterAngle = _fovRenderer.CurrentFovDegrees;
        visionLight.pointLightInnerAngle = Mathf.Max(0f, _fovRenderer.CurrentFovDegrees - angleFeatherDegrees);

        visionLight.transform.SetPositionAndRotation(_fovRenderer.transform.position, _fovRenderer.transform.rotation);
    }
}
