using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RYZECHo.Unity;

/// <summary>
/// Optimized FOV renderer for the Hunt phase.
/// Uses a custom optimized shader with pre-computed vertex colors and fragment-level
/// FOV checks for efficient rendering with URP 2D Renderer.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class HuntFovRendererOptimized : MonoBehaviour
{
    #region Enums

    public enum FovMode
    {
        Standard100,
        Wide120,
        Sniper80,
        Custom,
    }

    #endregion

    #region Serialized Fields

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset;

    [Header("Vision")]
    [SerializeField] private FovMode fovMode = FovMode.Standard100;
    [SerializeField, Range(10f, 360f)] private float customFovDegrees = 100f;
    [SerializeField, Min(0.1f)] private float radius = 5f;
    [SerializeField, Min(0.1f)] private float innerRadius = 2.9f;
    [SerializeField, Min(0.1f)] private float darknessRadius = 32f;
    [SerializeField, Range(8, 160)] private int segments = 72;

    [Header("Appearance")]
    [SerializeField] private Color visionCenterColor = new(0.58f, 0.92f, 1f, 0.15f);
    [SerializeField] private Color visionEdgeColor = new(0.25f, 0.85f, 1f, 0.04f);
    [SerializeField] private Color darknessColor = new(0.01f, 0.02f, 0.035f, 0.82f);
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int darknessSortingOrder = 20;
    [SerializeField] private int visionSortingOrder = 21;

    [Header("Material")]
    [SerializeField] private Material overlayMaterial;

    #endregion

    #region Private Fields

    private MeshFilter _visionFilter;
    private MeshRenderer _visionRenderer;
    private MeshFilter _darknessFilter;
    private MeshRenderer _darknessRenderer;
    private Mesh _visionMesh;
    private Mesh _darknessMesh;
    private Material _runtimeMaterial;
    private bool _dirty = true;
    private bool _dirtyMaterial = true;
    private float _lastFovDegrees;
    private float _lastRadius;
    private float _lastInnerRadius;
    private float _lastDarknessRadius;

    #endregion

    #region Properties

    public float CurrentFovDegrees => Mathf.Clamp(ToDegrees(fovMode, customFovDegrees), 10f, 360f);
    public float Radius => radius;
    public float InnerRadius => innerRadius;
    public Transform Target => target;
    public Material RuntimeMaterial => _runtimeMaterial;

    #endregion

    #region Public API

    /// <summary>
    /// Set the FOV mode. Triggers mesh rebuild if the mode changes.
    /// </summary>
    public void SetFovMode(FovMode mode)
    {
        if (fovMode == mode)
            return;

        fovMode = mode;
        _dirty = true;
    }

    /// <summary>
    /// Set the darkness radius dynamically. Triggers mesh rebuild if the value changes.
    /// </summary>
    public void SetDarknessRadius(float radius)
    {
        radius = Mathf.Max(0.1f, radius);
        if (Mathf.Approximately(this.darknessRadius, radius))
            return;

        this.darknessRadius = radius;
        _dirty = true;
    }

    /// <summary>
    /// Rebuild meshes explicitly. Call this when properties change.
    /// </summary>
    public void RebuildMeshes()
    {
        _dirty = true;
    }

    /// <summary>
    /// Set custom FOV and radius values.
    /// </summary>
    public void SetVision(float fovDegrees, float visionRadius)
    {
        fovMode = FovMode.Custom;
        customFovDegrees = Mathf.Clamp(fovDegrees, 10f, 360f);
        radius = Mathf.Max(0.1f, visionRadius);
        innerRadius = Mathf.Max(0.1f, visionRadius * 0.58f);
        darknessRadius = Mathf.Max(darknessRadius, radius + 0.5f);
        _dirty = true;
    }

    /// <summary>
    /// Set the target transform to follow.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        if (target == newTarget)
            return;

        target = newTarget;
        _dirty = true;
    }

    /// <summary>
    /// Set the center color of the vision cone.
    /// </summary>
    public void SetCenterColor(Color color)
    {
        visionCenterColor = color;
        _dirtyMaterial = true;
    }

    /// <summary>
    /// Set the edge color of the vision cone.
    /// </summary>
    public void SetEdgeColor(Color color)
    {
        visionEdgeColor = color;
        _dirtyMaterial = true;
    }

    /// <summary>
    /// Set the darkness color for areas outside the FOV.
    /// </summary>
    public void SetDarknessColor(Color color)
    {
        darknessColor = color;
        _dirtyMaterial = true;
    }

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        EnsureRenderers();
        _dirty = true;
        _dirtyMaterial = true;
    }

    private void OnDisable()
    {
        if (_runtimeMaterial != null)
        {
            DestroyImmediateSafe(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        customFovDegrees = Mathf.Clamp(customFovDegrees, 10f, 360f);
        radius = Mathf.Max(0.1f, radius);
        innerRadius = Mathf.Max(0.1f, Mathf.Min(innerRadius, radius));
        darknessRadius = Mathf.Max(radius + 0.5f, darknessRadius);
        segments = Mathf.Clamp(segments, 8, 160);
        _dirty = true;
        _dirtyMaterial = true;
    }

    private void LateUpdate()
    {
        EnsureRenderers();
        SyncToTarget();

        if (_dirtyMaterial)
        {
            UpdateMaterialProperties();
            _dirtyMaterial = false;
        }

        if (_dirty)
        {
            RebuildMeshesInternal();
            _dirty = false;
        }
    }

    private void OnDestroy()
    {
        DestroyImmediateSafe(_visionMesh);
        DestroyImmediateSafe(_darknessMesh);
        DestroyImmediateSafe(_runtimeMaterial);
    }

    #endregion

    #region Renderer Setup

    private void EnsureRenderers()
    {
        EnsureMeshObject("FOV Darkness", ref _darknessFilter, ref _darknessRenderer, darknessSortingOrder);
        EnsureMeshObject("FOV Vision", ref _visionFilter, ref _visionRenderer, visionSortingOrder);

        _visionMesh ??= CreateMesh("Hunt FOV Vision Mesh");
        _darknessMesh ??= CreateMesh("Hunt FOV Darkness Mesh");
        _visionFilter.sharedMesh = _visionMesh;
        _darknessFilter.sharedMesh = _darknessMesh;

        var material = ResolveMaterial();
        _visionRenderer.sharedMaterial = material;
        _darknessRenderer.sharedMaterial = material;
        ApplySorting(_visionRenderer, visionSortingOrder);
        ApplySorting(_darknessRenderer, darknessSortingOrder);
    }

    private void EnsureMeshObject(string objectName, ref MeshFilter filter, ref MeshRenderer meshRenderer, int sortingOrder)
    {
        if (filter != null && meshRenderer != null)
            return;

        var child = transform.Find(objectName);
        if (child == null)
        {
            var childObject = new GameObject(objectName);
            childObject.transform.SetParent(transform, worldPositionStays: false);
            childObject.transform.localPosition = Vector3.zero;
            childObject.transform.localRotation = Quaternion.identity;
            childObject.transform.localScale = Vector3.one;
            child = childObject.transform;
        }

        filter = child.GetComponent<MeshFilter>() ?? child.gameObject.AddComponent<MeshFilter>();
        meshRenderer = child.GetComponent<MeshRenderer>() ?? child.gameObject.AddComponent<MeshRenderer>();
        ApplySorting(meshRenderer, sortingOrder);
    }

    private Material ResolveMaterial()
    {
        if (overlayMaterial != null)
            return overlayMaterial;

        if (_runtimeMaterial != null)
            return _runtimeMaterial;

        var shader = Shader.Find("RYZECHo/FovOverlayOptimized") ?? Shader.Find("Sprites/Default");
        _runtimeMaterial = new Material(shader)
        {
            name = "Runtime Hunt FOV Overlay Optimized",
            hideFlags = HideFlags.DontSave,
        };

        UpdateMaterialProperties();
        return _runtimeMaterial;
    }

    private void UpdateMaterialProperties()
    {
        if (_runtimeMaterial == null)
            return;

        _runtimeMaterial.SetColor("_CenterColor", visionCenterColor);
        _runtimeMaterial.SetColor("_EdgeColor", visionEdgeColor);
        _runtimeMaterial.SetColor("_DarknessColor", darknessColor);
        _runtimeMaterial.SetFloat("_InnerRadius", innerRadius);
        _runtimeMaterial.SetFloat("_OuterRadius", radius);
        _runtimeMaterial.SetFloat("_FovAngle", CurrentFovDegrees);
        _runtimeMaterial.SetFloat("_DarknessRadius", darknessRadius);
    }

    private static Mesh CreateMesh(string meshName)
    {
        var mesh = new Mesh
        {
            name = meshName,
            hideFlags = HideFlags.DontSave,
        };
        mesh.MarkDynamic();
        return mesh;
    }

    private void ApplySorting(MeshRenderer meshRenderer, int sortingOrder)
    {
        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }

    #endregion

    #region Target Sync

    private void SyncToTarget()
    {
        if (target == null)
            return;

        transform.SetPositionAndRotation(target.position + targetOffset, target.rotation);
    }

    #endregion

    #region Mesh Building

    private void RebuildMeshesInternal()
    {
        var fovDegrees = CurrentFovDegrees;
        var radiusChanged = !Mathf.Approximately(radius, _lastRadius);
        var fovChanged = !Mathf.Approximately(fovDegrees, _lastFovDegrees);
        var innerRadiusChanged = !Mathf.Approximately(innerRadius, _lastInnerRadius);
        var darknessRadiusChanged = !Mathf.Approximately(darknessRadius, _lastDarknessRadius);

        if (fovChanged || radiusChanged || innerRadiusChanged)
        {
            BuildVisionMesh();
            _lastFovDegrees = fovDegrees;
            _lastRadius = radius;
            _lastInnerRadius = innerRadius;
        }

        if (darknessRadiusChanged || fovChanged || radiusChanged || radiusChanged)
        {
            BuildDarknessMesh();
            _lastDarknessRadius = darknessRadius;
        }
    }

    /// <summary>
    /// Build the vision cone mesh (inside FOV area).
    /// Uses pre-computed vertex colors for performance.
    /// </summary>
    private void BuildVisionMesh()
    {
        var degrees = CurrentFovDegrees;
        var halfAngle = degrees * 0.5f;

        // For full 360 FOV, use a simple circle approach
        if (degrees >= 359.5f)
        {
            BuildCircleMesh(radius, innerRadius, segments, visionCenterColor, visionEdgeColor);
            return;
        }

        // Build the vision cone (inside FOV)
        BuildFovConeMesh(degrees, halfAngle, radius, innerRadius, visionCenterColor, visionEdgeColor);
    }

    /// <summary>
    /// Build the darkness mesh (outside FOV area).
    /// Separated for efficient rendering - only non-FOV areas are drawn.
    /// </summary>
    private void BuildDarknessMesh()
    {
        var degrees = CurrentFovDegrees;
        var halfAngle = degrees * 0.5f;

        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();

        // Add the outer darkness fan (area outside FOV cone but within darkness radius)
        if (degrees < 359.5f)
        {
            AddFanToMesh(vertices, colors, triangles, halfAngle, 360f - halfAngle, darknessRadius, darknessColor);
        }

        // Add the inner ring (between inner and outer radius, inside FOV cone)
        AddRingToMesh(vertices, colors, triangles, -halfAngle, halfAngle, innerRadius, radius, darknessColor);

        ApplyMesh(_darknessMesh, vertices.ToArray(), colors.ToArray(), triangles.ToArray());
    }

    /// <summary>
    /// Build a full circle mesh (for 360 FOV mode).
    /// </summary>
    private void BuildCircleMesh(float outerRadius, float innerRadius, int segments, Color centerColor, Color edgeColor)
    {
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();

        // Inner circle center
        vertices.Add(Vector3.zero);
        colors.Add(centerColor);

        // Inner ring vertices
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            vertices.Add(new Vector3(Mathf.Cos(angle) * innerRadius, Mathf.Sin(angle) * innerRadius, 0f));
            colors.Add(centerColor);
        }

        // Inner ring triangles
        for (int i = 0; i < segments; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
        }

        // Outer ring vertices
        var outerStart = vertices.Count;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            vertices.Add(new Vector3(Mathf.Cos(angle) * outerRadius, Mathf.Sin(angle) * outerRadius, 0f));
            colors.Add(edgeColor);
        }

        // Ring triangles
        for (int i = 0; i < segments; i++)
        {
            var a = outerStart + i * 2;
            triangles.Add(a);
            triangles.Add(a + 1);
            triangles.Add(a + 2);
            triangles.Add(a + 2);
            triangles.Add(a + 1);
            triangles.Add(a + 3);
        }

        ApplyMesh(_visionMesh, vertices.ToArray(), colors.ToArray(), triangles.ToArray());
    }

    /// <summary>
    /// Build the FOV cone mesh with gradient from center to edge.
    /// </summary>
    private void BuildFovConeMesh(float degrees, float halfAngle, float outerRadius, float innerRadius, Color centerColor, Color edgeColor)
    {
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();

        // Center point
        vertices.Add(Vector3.zero);
        colors.Add(centerColor);

        // Inner ring (for the inner circle)
        var innerRingStart = vertices.Count;
        var innerSegments = Mathf.Max(3, Mathf.CeilToInt(segments * (degrees / 360f)));
        for (int i = 0; i <= innerSegments; i++)
        {
            float angle = -halfAngle + ((degrees / innerSegments) * i) * Mathf.Deg2Rad;
            vertices.Add(new Vector3(Mathf.Cos(angle) * innerRadius, Mathf.Sin(angle) * innerRadius, 0f));
            colors.Add(centerColor);
        }

        // Triangles from center to inner ring
        for (int i = 0; i < innerSegments; i++)
        {
            triangles.Add(0);
            triangles.Add(innerRingStart + i);
            triangles.Add(innerRingStart + i + 1);
        }

        // Outer ring vertices (for the ring between inner and outer radius)
        var outerRingStart = vertices.Count;
        for (int i = 0; i <= innerSegments; i++)
        {
            float angle = -halfAngle + ((degrees / innerSegments) * i) * Mathf.Deg2Rad;
            vertices.Add(new Vector3(Mathf.Cos(angle) * outerRadius, Mathf.Sin(angle) * outerRadius, 0f));
            colors.Add(edgeColor);
        }

        // Ring triangles
        for (int i = 0; i < innerSegments; i++)
        {
            var a = outerRingStart + i * 2;
            triangles.Add(a);
            triangles.Add(a + 1);
            triangles.Add(a + 2);
            triangles.Add(a + 2);
            triangles.Add(a + 1);
            triangles.Add(a + 3);
        }

        ApplyMesh(_visionMesh, vertices.ToArray(), colors.ToArray(), triangles.ToArray());
    }

    /// <summary>
    /// Add a fan mesh (for the outer darkness area).
    /// </summary>
    private void AddFanToMesh(List<Vector3> vertices, List<Color> colors, List<int> triangles, float startAngle, float spanAngle, float fanRadius, Color darknessColor)
    {
        var span = Mathf.Max(0f, spanAngle);
        if (span <= 0.01f)
            return;

        var startIndex = vertices.Count;
        var arcSegments = Mathf.Max(3, Mathf.CeilToInt(segments * (span / 360f)));

        vertices.Add(Vector3.zero);
        colors.Add(darknessColor);

        for (int i = 0; i <= arcSegments; i++)
        {
            float angle = (startAngle + (span / arcSegments) * i) * Mathf.Deg2Rad;
            vertices.Add(new Vector3(Mathf.Cos(angle) * fanRadius, Mathf.Sin(angle) * fanRadius, 0f));
            colors.Add(darknessColor);
        }

        for (int i = 0; i < arcSegments; i++)
        {
            triangles.Add(startIndex);
            triangles.Add(startIndex + i + 1);
            triangles.Add(startIndex + i + 2);
        }
    }

    /// <summary>
    /// Add a ring mesh (between inner and outer radius).
    /// </summary>
    private void AddRingToMesh(List<Vector3> vertices, List<Color> colors, List<int> triangles, float startAngle, float endAngle, float innerRadius, float outerRadius, Color darknessColor)
    {
        if (outerRadius <= innerRadius)
            return;

        var span = Mathf.Max(0f, endAngle - startAngle);
        if (span <= 0.01f)
            return;

        var startIndex = vertices.Count;
        var arcSegments = Mathf.Max(3, Mathf.CeilToInt(segments * (span / 360f)));

        for (int i = 0; i <= arcSegments; i++)
        {
            float angle = (startAngle + (span / arcSegments) * i) * Mathf.Deg2Rad;
            vertices.Add(new Vector3(Mathf.Cos(angle) * innerRadius, Mathf.Sin(angle) * innerRadius, 0f));
            vertices.Add(new Vector3(Mathf.Cos(angle) * outerRadius, Mathf.Sin(angle) * outerRadius, 0f));
            colors.Add(darknessColor);
            colors.Add(darknessColor);
        }

        for (int i = 0; i < arcSegments; i++)
        {
            var innerA = startIndex + (i * 2);
            var outerA = innerA + 1;
            var innerB = innerA + 2;
            var outerB = innerA + 3;

            triangles.Add(innerA);
            triangles.Add(outerA);
            triangles.Add(outerB);
            triangles.Add(innerA);
            triangles.Add(outerB);
            triangles.Add(innerB);
        }
    }

    /// <summary>
    /// Apply mesh data to a Unity Mesh object.
    /// </summary>
    private static void ApplyMesh(Mesh mesh, Vector3[] vertices, Color[] colors, int[] triangles)
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    #endregion

    #region Helpers

    private static float ToDegrees(FovMode mode, float customDegrees)
    {
        return mode switch
        {
            FovMode.Standard100 => 100f,
            FovMode.Wide120 => 120f,
            FovMode.Sniper80 => 80f,
            _ => customDegrees,
        };
    }

    private static void DestroyImmediateSafe(UnityEngine.Object targetObject)
    {
        if (targetObject == null)
            return;

        if (Application.isPlaying)
            Destroy(targetObject);
        else
            DestroyImmediate(targetObject);
    }

    #endregion
}
