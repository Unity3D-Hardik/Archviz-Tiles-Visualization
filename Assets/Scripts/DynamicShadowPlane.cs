using UnityEngine;

[ExecuteInEditMode]
public class DynamicShadowPlane : MonoBehaviour
{
    [Header("Shadow Properties")]
    [Tooltip("Circle, Square, Soft Circle, Soft Square, or Rounded Square")]
    public int shadowShape = 0; // 0=Circle, 1=Square, 2=SoftCircle, 3=SoftSquare, 4=RoundedSquare

    [Range(0, 1)]
    public float shadowIntensity = 0.7f;

    [Range(0.1f, 2f)]
    public float shadowSize = 1.0f;

    [Range(0.1f, 5f)]
    public float shadowHardness = 1.5f;

    [Range(0, 1)]
    public float cornerRadius = 0.2f;

    [Range(0, 1)]
    public float opacity = 1.0f;

    private Material shadowMaterial;
    private MeshRenderer meshRenderer;

    private void OnEnable()
    {
        InitializeShadow();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            UpdateShadowProperties();
    }

    public void InitializeShadow()
    {
        // Get or create mesh renderer
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Get or create mesh filter
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        // Create plane mesh if needed
        if (meshFilter.mesh == null || meshFilter.mesh.vertices.Length == 0)
        {
            meshFilter.mesh = CreatePlaneMesh();
        }

        // Create material with DynamicShadow shader
        Shader shader = Shader.Find("Aimision/DynamicShadow");
        if (shader != null)
        {
            shadowMaterial = new Material(shader);
            meshRenderer.material = shadowMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Debug.Log("DynamicShadow initialized on: " + gameObject.name);
        }
        else
        {
            Debug.LogError("DynamicShadow shader not found!");
        }
    }

    private Mesh CreatePlaneMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ShadowPlaneMesh";

        mesh.vertices = new[] {
            new Vector3(-1, 0, -1),
            new Vector3( 1, 0, -1),
            new Vector3( 1, 0,  1),
            new Vector3(-1, 0,  1)
        };

        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };

        mesh.uv = new[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void UpdateShadowProperties()
    {
        if (shadowMaterial == null)
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (meshRenderer != null)
                shadowMaterial = meshRenderer.material;
        }

        if (shadowMaterial != null)
        {
            shadowMaterial.SetFloat("_ShadowShape", shadowShape);
            shadowMaterial.SetFloat("_ShadowIntensity", shadowIntensity);
            shadowMaterial.SetFloat("_ShadowSize", shadowSize);
            shadowMaterial.SetFloat("_ShadowHardness", shadowHardness);
            shadowMaterial.SetFloat("_CornerRadius", cornerRadius);
            shadowMaterial.SetFloat("_Opacity", opacity);
        }
    }

    private void Update()
    {
        if (Application.isPlaying)
            UpdateShadowProperties();
    }
}
