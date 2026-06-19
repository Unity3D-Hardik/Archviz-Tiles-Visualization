using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileRuntimeSetExample : MonoBehaviour
{
    [Serializable]
    public class ManualTextureSet
    {
        public Texture2D baseMap;
        public Texture2D normalMap;
        public Texture2D metallicMap;
        public Texture2D occlusionMap;
        public Texture2D emissionMap;
        public Texture2D detailMap;
        public Texture2D detailNormalMap;
    }

    [Header("References")]
    [SerializeField] private TileTextureSwapper tileSwapper;

    [Header("Texture Set")]
    private Texture2D baseMap;
    private Texture2D normalMap;
    private Texture2D metallicMap;
    private Texture2D occlusionMap;
    private Texture2D emissionMap;
    private Texture2D detailMap;
    private Texture2D detailNormalMap;

    [Header("Layout Controls")]
    [SerializeField] [Range(0f, 20f)] private float gapSizeMM = 2f;
    [SerializeField] private Color gapColor = Color.black;
    [SerializeField] [Range(0f, 360f)] private float rotation;

    [Header("UI Sliders")]
    public Slider groutSizeSlider;
    public Slider rotationSlider;

    [Header("PBR Overrides (Optional)")]
    [SerializeField] private bool overridePbrValues;
    [SerializeField] [Range(0f, 2f)] private float bumpScale = 1f;
    [SerializeField] [Range(0f, 1f)] private float smoothness = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float metallic;
    [SerializeField] [Range(0f, 1f)] private float occlusionStrength = 1f;
    [SerializeField] private Color emissionColor = Color.black;

    [Header("Auto Detection")]
    [SerializeField] private bool autoDetectFromBaseName = true;

    [Header("Runtime Fallback Generation")]
    [SerializeField] private bool fallbackToRuntimeGeneratedMaps = true;
    [SerializeField] [Range(0.2f, 6f)] private float generatedNormalStrength = 2.0f;
    [SerializeField] [Range(0.2f, 4f)] private float generatedOcclusionStrength = 1.25f;

    [Header("Manual Full Texture Sets")]
    [SerializeField] private bool useManualTextureSets = true;
    [SerializeField] private List<ManualTextureSet> manualTextureSets = new();

    private void Awake()
    {
        if (tileSwapper == null)
            tileSwapper = GetComponent<TileTextureSwapper>();
    }

    private void OnEnable()
    {
        RegisterSliderCallbacks();
        SyncSliderValues();
    }

    private void OnDisable()
    {
        UnregisterSliderCallbacks();
    }

    public void SetTexture(int index)
    {
        if (index < 0)
            return;

        if (!useManualTextureSets)
            return;

        if (!TryApplyManualTextureSet(index))
            return;

        ApplyFullSet();
    }

    public void SetTexture(Texture2D texture)
    {
        if (texture == null)
            return;

        baseMap = texture;
        ApplyFullSet();
    }

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

            AutoDetectFromBaseName = autoDetectFromBaseName,
            AutoGenerateMapsFromBase = fallbackToRuntimeGeneratedMaps,
            GeneratedNormalStrength = generatedNormalStrength,
            GeneratedOcclusionStrength = generatedOcclusionStrength
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

    public void SetGapSize(float value)
    {
        gapSizeMM = value;
        if (tileSwapper != null)
            tileSwapper.SetGapSize(value);
    }

    public void SetGapColor(Color value)
    {
        gapColor = value;
        if (tileSwapper != null)
            tileSwapper.SetGapColor(value);
    }

    public void SetRotation(float value)
    {
        rotation = value;
        if (tileSwapper != null)
            tileSwapper.SetRotationOnly(value);
    }

    public void SetTileSizeMM(float widthMM, float heightMM)
    {
        if (tileSwapper != null)
            tileSwapper.SetTileSizeMM(widthMM, heightMM);
    }

    public void OnGroutSizeSliderChanged(float value)
    {
        SetGapSize(value);
    }

    public void OnRotationSliderChanged(float value)
    {
        SetRotation(value);
    }

    public void OnGapColorHexButtonClicked(string hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
            return;

        if (TryParseColorFromButtonValue(hexColor, out Color parsedColor))
            SetGapColor(parsedColor);
        else
            Debug.LogWarning($"[TileRuntimeSetExample] Invalid color value '{hexColor}'. Use formats like #RRGGBB, RRGGBB, #RRGGBBAA, RGB(255,170,0), or 255,170,0.", this);
    }

    public void OnGapColorRgbButtonClicked(float r, float g, float b, float a = 1f)
    {
        SetGapColor(new Color(r, g, b, a));
    }

    private static bool TryParseColorFromButtonValue(string rawValue, out Color parsedColor)
    {
        parsedColor = Color.black;
        if (string.IsNullOrWhiteSpace(rawValue))
            return false;

        string value = rawValue.Trim();

        if (ColorUtility.TryParseHtmlString(value, out parsedColor))
            return true;

        if (!value.StartsWith("#", StringComparison.Ordinal))
        {
            if (ColorUtility.TryParseHtmlString("#" + value, out parsedColor))
                return true;
        }

        // Supports formats like "RGB(255,170,0)", "255,170,0", and "1,0.66,0".
        string normalized = value.Replace("RGB(", "", StringComparison.OrdinalIgnoreCase)
            .Replace("RGBA(", "", StringComparison.OrdinalIgnoreCase)
            .Replace("(", string.Empty, StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal);

        string[] parts = normalized.Split(',');
        if (parts.Length != 3 && parts.Length != 4)
            return false;

        if (!float.TryParse(parts[0].Trim(), out float r) ||
            !float.TryParse(parts[1].Trim(), out float g) ||
            !float.TryParse(parts[2].Trim(), out float b))
            return false;

        float a = 1f;
        if (parts.Length == 4 && !float.TryParse(parts[3].Trim(), out a))
            return false;

        bool usesByteRange = r > 1f || g > 1f || b > 1f || a > 1f;
        if (usesByteRange)
        {
            r /= 255f;
            g /= 255f;
            b /= 255f;
            a /= 255f;
        }

        parsedColor = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), Mathf.Clamp01(a));
        return true;
    }

    private void RegisterSliderCallbacks()
    {
        if (groutSizeSlider != null)
        {
            groutSizeSlider.onValueChanged.RemoveListener(OnGroutSizeSliderChanged);
            groutSizeSlider.onValueChanged.AddListener(OnGroutSizeSliderChanged);
        }

        if (rotationSlider != null)
        {
            rotationSlider.onValueChanged.RemoveListener(OnRotationSliderChanged);
            rotationSlider.onValueChanged.AddListener(OnRotationSliderChanged);
        }
    }

    private void UnregisterSliderCallbacks()
    {
        if (groutSizeSlider != null)
            groutSizeSlider.onValueChanged.RemoveListener(OnGroutSizeSliderChanged);

        if (rotationSlider != null)
            rotationSlider.onValueChanged.RemoveListener(OnRotationSliderChanged);
    }

    private void SyncSliderValues()
    {
        if (groutSizeSlider != null)
            groutSizeSlider.SetValueWithoutNotify(gapSizeMM);

        if (rotationSlider != null)
            rotationSlider.SetValueWithoutNotify(rotation);
    }

    private bool TryApplyManualTextureSet(int index)
    {
        if (manualTextureSets == null || index >= manualTextureSets.Count)
            return false;

        ManualTextureSet set = manualTextureSets[index];
        if (set == null || set.baseMap == null)
            return false;

        baseMap = set.baseMap;
        normalMap = set.normalMap;
        metallicMap = set.metallicMap;
        occlusionMap = set.occlusionMap;
        emissionMap = set.emissionMap;
        detailMap = set.detailMap;
        detailNormalMap = set.detailNormalMap;
        return true;
    }
}
