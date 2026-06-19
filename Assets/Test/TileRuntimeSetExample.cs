using UnityEngine;


public class TileRuntimeSetExample : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileTextureSwapper tileSwapper;

    [Header("Texture Set")] // Will get this from API
    [SerializeField] private Texture2D baseMap;
    [SerializeField] private Texture2D normalMap;
    [SerializeField] private Texture2D metallicMap;
    [SerializeField] private Texture2D occlusionMap;
    [SerializeField] private Texture2D emissionMap;
    [SerializeField] private Texture2D detailMap;
    [SerializeField] private Texture2D detailNormalMap;

    [Header("Layout Controls")]
    [SerializeField] [Range(0f, 20f)] private float gapSizeMM = 2f;
    [SerializeField] private Color gapColor = Color.black;
    [SerializeField] [Range(0f, 360f)] private float rotation = 0f;

    [Header("PBR Overrides (Optional)")]
    [SerializeField] private bool overridePbrValues;
    [SerializeField] [Range(0f, 2f)] private float bumpScale = 1f;
    [SerializeField] [Range(0f, 1f)] private float smoothness = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float metallic = 0f;
    [SerializeField] [Range(0f, 1f)] private float occlusionStrength = 1f;
    [SerializeField] private Color emissionColor = Color.black;

    [Header("Auto Detection")]
    [SerializeField] private bool autoDetectFromBaseName = true;


    public Texture2D[] TestbaseMap;

    private void Awake()
    {
        if (tileSwapper == null)
            tileSwapper = GetComponent<TileTextureSwapper>();
    }

    public void SetTexture(int index)
    {
        baseMap = TestbaseMap[index];
        ApplyFullSet();
    }

    // Call from a UI button for full runtime update.
    public void ApplyFullSet()
    {
        if (tileSwapper == null) return;

        TileTextureSwapper.TileTextureSet setData = new TileTextureSwapper.TileTextureSet
        {
            BaseMap = baseMap,
            NormalMap = normalMap,
            MetallicMap = metallicMap,
            OcclusionMap = occlusionMap,
            EmissionMap = emissionMap,
            DetailMap = detailMap,
            DetailNormalMap = detailNormalMap,

            GapSizeMM = gapSizeMM,
            GapColor = gapColor,
            Rotation = rotation,

            AutoDetectFromBaseName = autoDetectFromBaseName
        };

        if (overridePbrValues)
        {
            setData.BumpScale = bumpScale;
            setData.Smoothness = smoothness;
            setData.Metallic = metallic;
            setData.OcclusionStrength = occlusionStrength;
            setData.EmissionColor = emissionColor;
        }

        tileSwapper.SwapTextureSet(setData);
    }

    // Call from slider OnValueChanged for lightweight updates.
    public void SetGapSize(float value)
    {
        gapSizeMM = value;
        if (tileSwapper != null)
            tileSwapper.SetGapSize(value);
    }

    // Call from color picker changes.
    public void SetGapColor(Color value)
    {
        gapColor = value;
        if (tileSwapper != null)
            tileSwapper.SetGapColor(value);
    }

    // Call from slider OnValueChanged for rotation updates.
    public void SetRotation(float value)
    {
        rotation = value;
        if (tileSwapper != null)
            tileSwapper.SetRotationOnly(value);
    }
}
