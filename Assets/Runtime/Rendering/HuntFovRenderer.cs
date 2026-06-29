using UnityEngine;
using Color = UnityEngine.Color;

namespace RYZECHo.Unity;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class HuntFovRenderer : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset;

    [Header("Vision")]
    [SerializeField] private HuntFovMode fovMode = HuntFovMode.Standard100;
    [SerializeField, Range(10f, 360f)] private float customFovDegrees = 100f;
    [SerializeField, Min(0.1f)] private float radius = 5f;
    [SerializeField, Min(1f)] private float darknessRadius = 32f;
    [SerializeField, Range(8, 160)] private int segments = 72;

    [Header("Appearance")]
    [SerializeField] private UnityEngine.Color visionCenterColor = new(0.58f, 0.92f, 1f, 0.15f);
    [SerializeField] private UnityEngine.Color visionEdgeColor = new(0.25f, 0.85f, 1f, 0.04f);
    [SerializeField] private UnityEngine.Color darknessColor = new(0.01f, 0.02f, 0.035f, 0.82f);
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int darknessSortingOrder = 20;
    [SerializeField] private int visionSortingOrder = 21;
    [SerializeField] private Material overlayMaterial;

    private MeshFilter _visionFilter;
    private MeshRenderer _visionRenderer;
    private MeshFilter _darknessFilter;
    private MeshRenderer _darknessRenderer;
    private Mesh _visionMesh;
    private Mesh _darknessMesh;
    private Material _runtimeMaterial;
    private bool _dirty = true;

    public float CurrentFovDegrees => Mathf.Clamp(fovMode.ToDegrees(customFovDegrees), 10f, 360f);
    public float Radius => radius;
    public Transform Target => target;

    public void SetTarget(Transform newTarget)
    {
        if (target == newTarget)
        {
            return;
        }

        target = newTarget;
        _dirty = true;
    }

    public void SetVision(float fovDegrees, float visionRadius)
    {
        fovMode = HuntFovMode.Custom;
        customFovDegrees = Mathf.Clamp(fovDegrees, 10f, 360f);
        radius = Mathf.Max(0.1f, visionRadius);
        darknessRadius = Mathf.Max(darknessRadius, radius + 0.5f);
        _dirty = true;
    }

    private void OnEnable()
    {
        EnsureRenderers();
        _dirty = true;
    }

    private void OnValidate()
    {
        customFovDegrees = Mathf.Clamp(customFovDegrees, 10f, 360f);
        radius = Mathf.Max(0.1f, radius);
        darknessRadius = Mathf.Max(radius + 0.5f, darknessRadius);
        segments = Mathf.Clamp(segments, 8, 160);
        _dirty = true;
    }

    private void LateUpdate()
    {
        EnsureRenderers();
        SyncToTarget();

        if (_dirty)
        {
            RebuildMeshes();
            _dirty = false;
        }
    }

    private void OnDisable()
    {
        if (_runtimeMaterial != null)
        {
            DestroyImmediateSafe(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }

    private void OnDestroy()
    {
        DestroyImmediateSafe(_visionMesh);
        DestroyImmediateSafe(_darknessMesh);
        DestroyImmediateSafe(_runtimeMaterial);
    }

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
        {
            return;
        }

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
        {
            return overlayMaterial;
        }

        if (_runtimeMaterial != null)
        {
            return _runtimeMaterial;
        }

        var shader = Shader.Find("RYZECHo/FovOverlay") ?? Shader.Find("Sprites/Default");
        _runtimeMaterial = new Material(shader)
        {
            name = "Runtime Hunt FOV Overlay",
            hideFlags = HideFlags.DontSave,
        };
        _runtimeMaterial.SetColor("_Color", UnityEngine.Color.white);
        return _runtimeMaterial;
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

    private void SyncToTarget()
    {
        if (target == null)
        {
            return;
        }

        transform.SetPositionAndRotation(target.position + targetOffset, target.rotation);
    }

    private void RebuildMeshes()
    {
        BuildVisionMesh();
        BuildDarknessMesh();
    }

    private void BuildVisionMesh()
    {
        var degrees = CurrentFovDegrees;
        var half = degrees * 0.5f;
        var arcSegments = SegmentCount(degrees);
        var vertices = new Vector3[arcSegments + 2];
        var colors = new UnityEngine.Color[vertices.Length];
        var triangles = new int[arcSegments * 3];

        vertices[0] = Vector3.zero;
        colors[0] = visionCenterColor;

        for (var index = 0; index <= arcSegments; index++)
        {
            var angle = -half + ((degrees / arcSegments) * index);
            vertices[index + 1] = AngleToVertex(angle, radius);
            colors[index + 1] = visionEdgeColor;
        }

        for (var index = 0; index < arcSegments; index++)
        {
            var triangle = index * 3;
            triangles[triangle] = 0;
            triangles[triangle + 1] = index + 1;
            triangles[triangle + 2] = index + 2;
        }

        ApplyMesh(_visionMesh, vertices, colors, triangles);
    }

    private void BuildDarknessMesh()
    {
        var degrees = CurrentFovDegrees;
        var half = degrees * 0.5f;
        var vertices = new List<Vector3>();
        var colors = new List<UnityEngine.Color>();
        var triangles = new List<int>();

        if (degrees < 359.5f)
        {
            AddFan(vertices, colors, triangles, half, 360f - half, darknessRadius);
        }

        AddRing(vertices, colors, triangles, -half, half, radius, darknessRadius);

        ApplyMesh(_darknessMesh, vertices.ToArray(), colors.ToArray(), triangles.ToArray());
    }

    private void AddFan(List<Vector3> vertices, List<UnityEngine.Color> colors, List<int> triangles, float startAngle, float endAngle, float fanRadius)
    {
        var span = Mathf.Max(0f, endAngle - startAngle);
        if (span <= 0.01f)
        {
            return;
        }

        var startIndex = vertices.Count;
        var arcSegments = SegmentCount(span);
        vertices.Add(Vector3.zero);
        colors.Add(darknessColor);

        for (var index = 0; index <= arcSegments; index++)
        {
            var angle = startAngle + ((span / arcSegments) * index);
            vertices.Add(AngleToVertex(angle, fanRadius));
            colors.Add(darknessColor);
        }

        for (var index = 0; index < arcSegments; index++)
        {
            triangles.Add(startIndex);
            triangles.Add(startIndex + index + 1);
            triangles.Add(startIndex + index + 2);
        }
    }

    private void AddRing(List<Vector3> vertices, List<UnityEngine.Color> colors, List<int> triangles, float startAngle, float endAngle, float innerRadius, float outerRadius)
    {
        if (outerRadius <= innerRadius)
        {
            return;
        }

        var span = Mathf.Max(0f, endAngle - startAngle);
        if (span <= 0.01f)
        {
            return;
        }

        var startIndex = vertices.Count;
        var arcSegments = SegmentCount(span);

        for (var index = 0; index <= arcSegments; index++)
        {
            var angle = startAngle + ((span / arcSegments) * index);
            vertices.Add(AngleToVertex(angle, innerRadius));
            vertices.Add(AngleToVertex(angle, outerRadius));
            colors.Add(darknessColor);
            colors.Add(darknessColor);
        }

        for (var index = 0; index < arcSegments; index++)
        {
            var innerA = startIndex + (index * 2);
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

    private int SegmentCount(float degrees)
    {
        return Mathf.Clamp(Mathf.CeilToInt(segments * (degrees / 360f)), 3, segments);
    }

    private static Vector3 AngleToVertex(float degrees, float vertexRadius)
    {
        var radians = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radians) * vertexRadius, Mathf.Sin(radians) * vertexRadius, 0f);
    }

    private static void ApplyMesh(Mesh mesh, Vector3[] vertices, UnityEngine.Color[] colors, int[] triangles)
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private static void DestroyImmediateSafe(UnityEngine.Object targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(targetObject);
        }
        else
        {
            DestroyImmediate(targetObject);
        }
    }
}
