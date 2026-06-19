using System;
using System.Collections.Generic;
using UnityEngine;

public class TileRuntimeSetExample : MonoBehaviour
{
    [Serializable]
    private class DerivedMapBinding
    {
        public Texture2D baseMap;
        public Texture2D normalMap;
        public Texture2D metallicMap;
        public Texture2D occlusionMap;
        public Texture2D emissionMap;
        public Texture2D detailMap;
        public Texture2D detailNormalMap;
    }

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
    [SerializeField] private Texture2D baseMap;
    [SerializeField] private Texture2D normalMap;
    [SerializeField] private Texture2D metallicMap;
    [SerializeField] private Texture2D occlusionMap;
    [SerializeField] private Texture2D emissionMap;
    [SerializeField] private Texture2D detailMap;
    [SerializeField] private Texture2D detailNormalMap;

    [Header("Auto Map Assignment")]
    [SerializeField] private bool useDerivedMapBindings = true;
    [SerializeField] private List<DerivedMapBinding> derivedMapBindings = new();
    [SerializeField] private bool autoAssignGeneratedMapsByName = true;
    [SerializeField] private bool clearMissingGeneratedMaps = true;
    [SerializeField] private Texture2D[] generatedMapsPool;

    [Header("Layout Controls")]
    [SerializeField] [Range(0f, 20f)] private float gapSizeMM = 2f;
    [SerializeField] private Color gapColor = Color.black;
    [SerializeField] [Range(0f, 360f)] private float rotation;

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
        ResolveMapsForBase(baseMap);
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

    private void ResolveMapsForBase(Texture2D selectedBaseMap)
    {
        if (selectedBaseMap == null) return;

        if (useDerivedMapBindings && TryGetDerivedBinding(selectedBaseMap, out DerivedMapBinding binding))
        {
            normalMap = binding.normalMap;
            metallicMap = binding.metallicMap;
            occlusionMap = binding.occlusionMap;
            emissionMap = binding.emissionMap;
            detailMap = binding.detailMap;
            detailNormalMap = binding.detailNormalMap;
            return;
        }

        if (!autoAssignGeneratedMapsByName)
            return;

        Texture2D foundNormal = FindGeneratedMap(selectedBaseMap.name, "_Normal");
        Texture2D foundOcclusion = FindGeneratedMap(selectedBaseMap.name, "_Occlusion");
        Texture2D foundMetallic = FindGeneratedMap(selectedBaseMap.name, "_Metallic");
        Texture2D foundEmission = FindGeneratedMap(selectedBaseMap.name, "_Emission");
        Texture2D foundDetail = FindGeneratedMap(selectedBaseMap.name, "_Detail");
        Texture2D foundDetailNormal = FindGeneratedMap(selectedBaseMap.name, "_DetailNormal");

        normalMap = foundNormal != null ? foundNormal : (clearMissingGeneratedMaps ? null : normalMap);
        occlusionMap = foundOcclusion != null ? foundOcclusion : (clearMissingGeneratedMaps ? null : occlusionMap);
        metallicMap = foundMetallic != null ? foundMetallic : (clearMissingGeneratedMaps ? null : metallicMap);
        emissionMap = foundEmission != null ? foundEmission : (clearMissingGeneratedMaps ? null : emissionMap);
        detailMap = foundDetail != null ? foundDetail : (clearMissingGeneratedMaps ? null : detailMap);
        detailNormalMap = foundDetailNormal != null ? foundDetailNormal : (clearMissingGeneratedMaps ? null : detailNormalMap);
    }

    private bool TryGetDerivedBinding(Texture2D selectedBaseMap, out DerivedMapBinding binding)
    {
        binding = null;
        if (selectedBaseMap == null || derivedMapBindings == null || derivedMapBindings.Count == 0)
            return false;

        for (int i = 0; i < derivedMapBindings.Count; i++)
        {
            DerivedMapBinding candidate = derivedMapBindings[i];
            if (candidate == null || candidate.baseMap == null)
                continue;

            if (candidate.baseMap == selectedBaseMap)
            {
                binding = candidate;
                return true;
            }
        }

        return false;
    }

    private Texture2D FindGeneratedMap(string baseName, string suffix)
    {
        if (generatedMapsPool == null || generatedMapsPool.Length == 0)
            return null;

        string expected = baseName + suffix;
        for (int i = 0; i < generatedMapsPool.Length; i++)
        {
            Texture2D tex = generatedMapsPool[i];
            if (tex == null) continue;

            if (string.Equals(tex.name, expected, StringComparison.OrdinalIgnoreCase))
                return tex;
        }

        return null;
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
