// TileTextureSwapper.cs
// Runtime component — works in Editor AND in VR builds (Quest, PC VR, etc.).
// Attach to any GameObject that owns a tile Renderer.
// Call SwapTexture() / SwapTextureSet() from script to update tile maps and
// have PBR values + shader keywords synced automatically.

using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

public class TileTextureSwapper : MonoBehaviour
{
    public struct FinishPreset
    {
        public string DisplayName;
        public float Metallic;
        public float Smoothness;
        public float NormalScale;
        public float ClearCoat;
        public float ClearCoatSmooth;
        public string[] Keywords;

        public FinishPreset(string display, float metallic, float smoothness,
            float normalScale, float clearCoat, float clearCoatSmooth,
            params string[] keywords)
        {
            DisplayName = display;
            Metallic = metallic;
            Smoothness = smoothness;
            NormalScale = normalScale;
            ClearCoat = clearCoat;
            ClearCoatSmooth = clearCoatSmooth;
            Keywords = keywords;
        }
    }

    // Full map payload for runtime swaps.
    public struct TileTextureSet
    {
        public Texture2D BaseMap;
        public Texture2D NormalMap;
        public Texture2D MetallicMap;
        public Texture2D OcclusionMap;
        public Texture2D EmissionMap;
        public Texture2D DetailMap;
        public Texture2D DetailNormalMap;

        // Optional scalar controls to set alongside maps.
        public float? BumpScale;
        public float? Smoothness;
        public float? Metallic;
        public float? OcclusionStrength;
        public Color? EmissionColor;

        // Optional tile controls
        // GapSizeMM applies to both X and Y and is clamped [0, 20].
        public float? GapSizeMM;
        public Color? GapColor;
        // Rotation is clamped [0, 360].
        public float? Rotation;

        // If true, parse BaseMap name for size + finish preset.
        public bool AutoDetectFromBaseName;
    }

    public static readonly FinishPreset[] FinishPresets =
    {
        new FinishPreset("High Gloss", 0.0f, 0.97f, 0.6f, 1f, 0.95f,
            "highgloss", "high_gloss", "high-gloss", "polished", "mirror"),
        new FinishPreset("Glossy", 0.0f, 0.88f, 0.7f, 1f, 0.85f,
            "gloss", "glossy", "glazed", "ceramic"),
        new FinishPreset("Satin", 0.0f, 0.65f, 0.8f, 0f, 0.0f,
            "satin", "semi_gloss", "semigloss"),
        new FinishPreset("Matte", 0.0f, 0.25f, 1.0f, 0f, 0.0f,
            "matte", "matt", "flat", "unglazed"),
        new FinishPreset("Rustic", 0.0f, 0.18f, 1.4f, 0f, 0.0f,
            "rustic", "rough", "natural", "stone", "slate", "cobble", "brick", "travertine"),
        new FinishPreset("Textured", 0.0f, 0.30f, 1.2f, 0f, 0.0f,
            "texture", "textured", "structured", "embossed", "carved"),
        new FinishPreset("Wood", 0.0f, 0.35f, 1.1f, 0f, 0.0f,
            "wood", "timber", "oak", "walnut", "maple", "parquet"),
        new FinishPreset("Marble", 0.0f, 0.82f, 0.5f, 1f, 0.80f,
            "marble", "onyx", "travertino"),
        new FinishPreset("Concrete", 0.0f, 0.20f, 1.3f, 0f, 0.0f,
            "concrete", "cement", "screed"),
        new FinishPreset("Metal", 0.9f, 0.75f, 0.6f, 0f, 0.0f,
            "metal", "steel", "iron", "aluminum", "copper"),
    };

    private static readonly Regex SizeRegex = new Regex(
        @"(?<!\d)(\d{2,5})\s*[xX_-]\s*(\d{2,5})(?!\d)",
        RegexOptions.Compiled
    );

    [Tooltip("Leave empty to auto-resolve from this GameObject's Renderer.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("Which material slot index on the Renderer to update.")]
    [SerializeField] private int materialIndex = 0;

    private Material cachedMaterial;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        RefreshCachedMaterial();
    }

    public void RefreshCachedMaterial()
    {
        cachedMaterial = null;
        TryGetTargetMaterial(out _);
    }

    public void SwapTexture(Texture2D newTexture)
    {
        if (!TryGetTargetMaterial(out Material mat)) return;
        ApplyTextureToMaterial(mat, newTexture);
    }

    public void SwapTexture(Material mat, Texture2D newTexture)
    {
        ApplyTextureToMaterial(mat, newTexture);
    }

    // One-call runtime update for all related maps.
    public void SwapTextureSet(TileTextureSet textureSet)
    {
        if (!TryGetTargetMaterial(out Material mat)) return;
        ApplyTextureSetToMaterial(mat, textureSet);
    }

    public void SwapTextureSet(Material mat, TileTextureSet textureSet)
    {
        ApplyTextureSetToMaterial(mat, textureSet);
    }

    // Utility runtime API: set common tile layout controls directly.
    public void SetTileLayout(float gapSizeMM, Color gapColor, float rotation)
    {
        if (!TryGetTargetMaterial(out Material mat)) return;
        ApplyTileLayoutToMaterial(mat, gapSizeMM, gapColor, rotation);
    }

    // Lightweight single-purpose runtime calls for UI sliders/color pickers.
    public void SetGapSize(float gapSizeMM)
    {
        if (!TryGetTargetMaterial(out Material mat)) return;
        SetUniformGapSizeMM(mat, gapSizeMM);
    }

    public void SetGapColor(Color gapColor)
    {
        if (!TryGetTargetMaterial(out Material mat)) return;
        if (mat.HasProperty("_GapColor")) mat.SetColor("_GapColor", gapColor);
    }

    public void SetRotationOnly(float rotation)
    {
        if (!TryGetTargetMaterial(out Material mat)) return;
        SetRotation(mat, rotation);
    }

    // Sets tile size in millimeters (X = width, Y = height) and enables real-world mode.
    public void SetTileSizeMM(float widthMM, float heightMM)
    {
        if (!TryGetTargetMaterial(out Material mat)) return;
        SetTileSizeMM(mat, widthMM, heightMM);
    }

    public static void ApplyTextureToMaterial(Material mat, Texture2D newTexture)
    {
        if (mat == null || newTexture == null) return;

        TileTextureSet textureSet = new TileTextureSet
        {
            BaseMap = newTexture,
            AutoDetectFromBaseName = true
        };

        ApplyTextureSetToMaterial(mat, textureSet);
    }

    public static void ApplyTextureSetToMaterial(Material mat, TileTextureSet textureSet)
    {
        if (mat == null) return;

        SetTextureIfProvided(mat, "_BaseMap", textureSet.BaseMap);
        SetTextureIfProvided(mat, "_BumpMap", textureSet.NormalMap);
        SetTextureIfProvided(mat, "_MetallicGlossMap", textureSet.MetallicMap);
        SetTextureIfProvided(mat, "_OcclusionMap", textureSet.OcclusionMap);
        SetTextureIfProvided(mat, "_EmissionMap", textureSet.EmissionMap);
        SetTextureIfProvided(mat, "_DetailMap", textureSet.DetailMap);
        SetTextureIfProvided(mat, "_DetailNormalMap", textureSet.DetailNormalMap);

        SetFloatIfHasValue(mat, "_BumpScale", textureSet.BumpScale);
        SetFloatIfHasValue(mat, "_Smoothness", textureSet.Smoothness);
        SetFloatIfHasValue(mat, "_Metallic", textureSet.Metallic);
        SetFloatIfHasValue(mat, "_OcclusionStrength", textureSet.OcclusionStrength);
        SetColorIfHasValue(mat, "_EmissionColor", textureSet.EmissionColor);

        if (textureSet.GapSizeMM.HasValue) SetUniformGapSizeMM(mat, textureSet.GapSizeMM.Value);
        if (textureSet.GapColor.HasValue && mat.HasProperty("_GapColor")) mat.SetColor("_GapColor", textureSet.GapColor.Value);
        if (textureSet.Rotation.HasValue) SetRotation(mat, textureSet.Rotation.Value);

        Texture baseMap = textureSet.BaseMap != null ? textureSet.BaseMap : mat.GetTexture("_BaseMap");
        if (textureSet.AutoDetectFromBaseName && baseMap != null)
        {
            string texName = baseMap.name;

            if (TryParseSizeFromName(texName, out float widthMM, out float heightMM))
            {
                Vector4 currentSize = mat.GetVector("_TileSizeMM");
                mat.SetVector("_TileSizeMM", new Vector4(widthMM, heightMM, currentSize.z, currentSize.w));
                mat.SetFloat("_UseRealWorldMM", 1f);
            }

            if (TryDetectFinish(texName, out FinishPreset preset))
            {
                // Keep explicit user overrides if provided in TileTextureSet.
                if (!textureSet.Metallic.HasValue) mat.SetFloat("_Metallic", preset.Metallic);
                if (!textureSet.Smoothness.HasValue) mat.SetFloat("_Smoothness", preset.Smoothness);
                if (!textureSet.BumpScale.HasValue) mat.SetFloat("_BumpScale", preset.NormalScale);
                mat.SetFloat("_ClearCoat", preset.ClearCoat);
                mat.SetFloat("_ClearCoatSmoothness", preset.ClearCoatSmooth);
            }
        }

        UpdateMaterialKeywords(mat);
    }

    // Applies same gap size on X and Y, clamped 0..20 mm.
    public static void SetUniformGapSizeMM(Material mat, float gapSizeMM)
    {
        if (mat == null || !mat.HasProperty("_GapSizeMM")) return;

        float gap = Mathf.Clamp(gapSizeMM, 0f, 20f);
        mat.SetVector("_GapSizeMM", new Vector4(gap, gap, 0f, 0f));
    }

    // Applies rotation clamped 0..360 degrees.
    public static void SetRotation(Material mat, float rotation)
    {
        if (mat == null || !mat.HasProperty("_Rotation")) return;

        float rot = Mathf.Clamp(rotation, 0f, 360f);
        mat.SetFloat("_Rotation", rot);
    }

    // Applies tile size in millimeters and switches shader scaling to real-world mode.
    public static void SetTileSizeMM(Material mat, float widthMM, float heightMM)
    {
        if (mat == null || !mat.HasProperty("_TileSizeMM")) return;

        float safeWidth = Mathf.Max(widthMM, 1f);
        float safeHeight = Mathf.Max(heightMM, 1f);
        mat.SetVector("_TileSizeMM", new Vector4(safeWidth, safeHeight, 0f, 0f));

        if (mat.HasProperty("_UseRealWorldMM"))
            mat.SetFloat("_UseRealWorldMM", 1f);
    }

    // Convenience API to apply all 3 layout controls together.
    public static void ApplyTileLayoutToMaterial(Material mat, float gapSizeMM, Color gapColor, float rotation)
    {
        if (mat == null) return;

        SetUniformGapSizeMM(mat, gapSizeMM);
        if (mat.HasProperty("_GapColor")) mat.SetColor("_GapColor", gapColor);
        SetRotation(mat, rotation);
    }

    public static void UpdateMaterialKeywords(Material mat)
    {
        if (mat == null) return;

        SetKeyword(mat, "_NORMALMAP", mat.GetTexture("_BumpMap") != null);
        SetKeyword(mat, "_METALLICSPECGLOSSMAP", mat.GetTexture("_MetallicGlossMap") != null);
        SetKeyword(mat, "_OCCLUSIONMAP", mat.GetTexture("_OcclusionMap") != null);
        SetKeyword(mat, "_DETAILMAP", mat.GetTexture("_DetailMap") != null);

        bool hasEmissionTex = mat.GetTexture("_EmissionMap") != null;
        bool hasEmissionColor = mat.HasProperty("_EmissionColor") && mat.GetColor("_EmissionColor").maxColorComponent > 0.0001f;
        SetKeyword(mat, "_EMISSION", hasEmissionTex || hasEmissionColor);

        SetKeyword(mat, "_ALPHATEST_ON", mat.HasProperty("_AlphaClip") && mat.GetFloat("_AlphaClip") > 0.5f);
        SetKeyword(mat, "_CLEARCOAT", mat.HasProperty("_ClearCoat") && mat.GetFloat("_ClearCoat") > 0.5f);
    }

    public static bool TryDetectFinish(string textureName, out FinishPreset preset)
    {
        string lower = textureName.ToLowerInvariant();
        foreach (FinishPreset p in FinishPresets)
        {
            foreach (string kw in p.Keywords)
            {
                if (lower.Contains(kw))
                {
                    preset = p;
                    return true;
                }
            }
        }

        preset = default;
        return false;
    }

    public static bool TryParseSizeFromName(string textureName, out float widthMM, out float heightMM)
    {
        widthMM = 0f;
        heightMM = 0f;

        Match match = SizeRegex.Match(textureName);
        if (!match.Success) return false;

        if (!float.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out widthMM)) return false;
        if (!float.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out heightMM)) return false;

        return widthMM > 0f && heightMM > 0f;
    }

    private bool TryGetTargetMaterial(out Material mat)
    {
        if (cachedMaterial != null)
        {
            mat = cachedMaterial;
            return true;
        }

        mat = null;

        if (targetRenderer == null)
        {
            Debug.LogWarning("[TileTextureSwapper] No Renderer found.", this);
            return false;
        }

        Material[] mats = targetRenderer.materials;
        if (materialIndex < 0 || materialIndex >= mats.Length)
        {
            Debug.LogWarning("[TileTextureSwapper] Invalid material index: " + materialIndex, this);
            return false;
        }

        cachedMaterial = mats[materialIndex];
        mat = cachedMaterial;
        return mat != null;
    }

    private static void SetKeyword(Material mat, string keyword, bool enabled)
    {
        if (enabled) mat.EnableKeyword(keyword);
        else mat.DisableKeyword(keyword);
    }

    private static void SetTextureIfProvided(Material mat, string propertyName, Texture texture)
    {
        if (mat == null || texture == null || !mat.HasProperty(propertyName)) return;
        mat.SetTexture(propertyName, texture);
    }

    private static void SetFloatIfHasValue(Material mat, string propertyName, float? value)
    {
        if (mat == null || !value.HasValue || !mat.HasProperty(propertyName)) return;
        mat.SetFloat(propertyName, value.Value);
    }

    private static void SetColorIfHasValue(Material mat, string propertyName, Color? value)
    {
        if (mat == null || !value.HasValue || !mat.HasProperty(propertyName)) return;
        mat.SetColor(propertyName, value.Value);
    }
}
