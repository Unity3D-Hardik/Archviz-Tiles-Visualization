using System.IO;
using System;
using UnityEditor;
using UnityEngine;

public class BaseMapDerivedTextureGeneratorWindow : EditorWindow
{
    private enum StylePreset
    {
        Unknown,
        HighGloss,
        Glossy,
        Satin,
        Matte,
        Rustic,
        Stone,
        Wood,
        Concrete,
        Metal
    }

    private Texture2D baseTexture;
    private string outputFolder = "Assets/Test/GeneratedMaps";
    private bool saveInSameFolderAsBase = true;
    private bool autoStyleFromTextureName = true;

    private bool generateNormal = true;
    private bool generateOcclusion = true;
    private bool generateSmoothness = true;
    private bool generateHeight = false;

    private float normalStrength = 2.0f;
    private float occlusionStrength = 1.25f;
    private float smoothnessContrast = 1.1f;
    private float heightContrast = 1.0f;
    private StylePreset detectedStyle = StylePreset.Unknown;

    [MenuItem("Tools/Tile/Generate Derived Maps From Base")]
    public static void ShowWindow()
    {
        GetWindow<BaseMapDerivedTextureGeneratorWindow>("Tile Map Generator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Base Texture", EditorStyles.boldLabel);
        baseTexture = (Texture2D)EditorGUILayout.ObjectField("Base Map", baseTexture, typeof(Texture2D), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

        saveInSameFolderAsBase = EditorGUILayout.ToggleLeft("Save In Same Folder As Base Texture", saveInSameFolderAsBase);

        using (new EditorGUI.DisabledScope(saveInSameFolderAsBase))
        {
            EditorGUILayout.BeginHorizontal();
            outputFolder = EditorGUILayout.TextField("Folder", outputFolder);
            if (GUILayout.Button("Select", GUILayout.Width(70)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Output Folder", Application.dataPath, string.Empty);
                if (!string.IsNullOrEmpty(selected))
                {
                    if (selected.StartsWith(Application.dataPath))
                    {
                        outputFolder = "Assets" + selected.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder inside this Unity project (under Assets).", "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Maps To Generate", EditorStyles.boldLabel);
        generateNormal = EditorGUILayout.ToggleLeft("Normal Map", generateNormal);
        generateOcclusion = EditorGUILayout.ToggleLeft("Occlusion Map", generateOcclusion);
        generateSmoothness = EditorGUILayout.ToggleLeft("Smoothness Map", generateSmoothness);
        generateHeight = EditorGUILayout.ToggleLeft("Height Map", generateHeight);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        autoStyleFromTextureName = EditorGUILayout.ToggleLeft("Auto Style From Texture Name", autoStyleFromTextureName);

        if (autoStyleFromTextureName && baseTexture != null)
        {
            detectedStyle = DetectStylePreset(baseTexture.name);
            EditorGUILayout.HelpBox("Detected style: " + detectedStyle, MessageType.Info);
        }

        normalStrength = EditorGUILayout.Slider("Normal Strength", normalStrength, 0.2f, 6f);
        occlusionStrength = EditorGUILayout.Slider("Occlusion Strength", occlusionStrength, 0.2f, 4f);
        smoothnessContrast = EditorGUILayout.Slider("Smoothness Contrast", smoothnessContrast, 0.2f, 2f);
        heightContrast = EditorGUILayout.Slider("Height Contrast", heightContrast, 0.2f, 2f);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(baseTexture == null || !AnyMapSelected()))
        {
            if (GUILayout.Button("Generate Derived Maps", GUILayout.Height(32)))
            {
                Generate();
            }
        }
    }

    private bool AnyMapSelected()
    {
        return generateNormal || generateOcclusion || generateSmoothness || generateHeight;
    }

    private void Generate()
    {
        if (baseTexture == null)
        {
            EditorUtility.DisplayDialog("Missing Base Map", "Assign a base texture first.", "OK");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(baseTexture);
        if (string.IsNullOrEmpty(assetPath))
        {
            EditorUtility.DisplayDialog("Invalid Texture", "Could not resolve texture asset path.", "OK");
            return;
        }

        if (!EnsureReadable(assetPath))
        {
            EditorUtility.DisplayDialog("Read Error", "Could not make the base texture readable.", "OK");
            return;
        }

        Texture2D readable = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (readable == null)
        {
            EditorUtility.DisplayDialog("Read Error", "Failed to reload readable texture.", "OK");
            return;
        }

        if (autoStyleFromTextureName)
        {
            ApplyStylePreset(baseTexture.name);
        }

        string resolvedOutputFolder = outputFolder;
        if (saveInSameFolderAsBase)
        {
            resolvedOutputFolder = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(resolvedOutputFolder))
                resolvedOutputFolder = "Assets";
            resolvedOutputFolder = resolvedOutputFolder.Replace("\\", "/");
        }

        EnsureOutputFolder(resolvedOutputFolder);

        int width = readable.width;
        int height = readable.height;
        Color32[] src = readable.GetPixels32();
        float[] luma = BuildLuma(src);

        string baseName = SanitizeName(baseTexture.name);

        if (generateNormal)
        {
            Texture2D normal = BuildNormalMap(luma, width, height, normalStrength);
            SaveTexture(normal, resolvedOutputFolder, baseName + "_Normal.png", true);
        }

        if (generateOcclusion)
        {
            Texture2D occ = BuildOcclusionMap(luma, width, height, occlusionStrength);
            SaveTexture(occ, resolvedOutputFolder, baseName + "_Occlusion.png", false);
        }

        if (generateSmoothness)
        {
            Texture2D smooth = BuildSmoothnessMap(luma, width, height, smoothnessContrast);
            SaveTexture(smooth, resolvedOutputFolder, baseName + "_Smoothness.png", false);
        }

        if (generateHeight)
        {
            Texture2D heightMap = BuildHeightMap(luma, width, height, heightContrast);
            SaveTexture(heightMap, resolvedOutputFolder, baseName + "_Height.png", false);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", "Derived maps generated successfully in: " + resolvedOutputFolder, "OK");
    }

    private void ApplyStylePreset(string textureName)
    {
        detectedStyle = DetectStylePreset(textureName);

        switch (detectedStyle)
        {
            case StylePreset.HighGloss:
                normalStrength = 1.2f;
                occlusionStrength = 0.8f;
                smoothnessContrast = 0.7f;
                heightContrast = 0.9f;
                break;
            case StylePreset.Glossy:
                normalStrength = 1.4f;
                occlusionStrength = 0.95f;
                smoothnessContrast = 0.8f;
                heightContrast = 0.95f;
                break;
            case StylePreset.Satin:
                normalStrength = 1.7f;
                occlusionStrength = 1.1f;
                smoothnessContrast = 1.0f;
                heightContrast = 1.0f;
                break;
            case StylePreset.Matte:
                normalStrength = 2.1f;
                occlusionStrength = 1.35f;
                smoothnessContrast = 1.35f;
                heightContrast = 1.1f;
                break;
            case StylePreset.Rustic:
            case StylePreset.Stone:
                normalStrength = 2.8f;
                occlusionStrength = 1.7f;
                smoothnessContrast = 1.6f;
                heightContrast = 1.25f;
                break;
            case StylePreset.Wood:
                normalStrength = 2.2f;
                occlusionStrength = 1.4f;
                smoothnessContrast = 1.2f;
                heightContrast = 1.2f;
                break;
            case StylePreset.Concrete:
                normalStrength = 2.6f;
                occlusionStrength = 1.6f;
                smoothnessContrast = 1.7f;
                heightContrast = 1.3f;
                break;
            case StylePreset.Metal:
                normalStrength = 1.1f;
                occlusionStrength = 0.9f;
                smoothnessContrast = 0.75f;
                heightContrast = 0.85f;
                break;
            default:
                break;
        }
    }

    private static StylePreset DetectStylePreset(string textureName)
    {
        if (string.IsNullOrEmpty(textureName)) return StylePreset.Unknown;

        string lower = textureName.ToLowerInvariant();

        if (ContainsAny(lower, "highgloss", "high_gloss", "high-gloss", "mirror", "polished")) return StylePreset.HighGloss;
        if (ContainsAny(lower, "gloss", "glossy", "glazed", "ceramic")) return StylePreset.Glossy;
        if (ContainsAny(lower, "satin", "semi_gloss", "semigloss")) return StylePreset.Satin;
        if (ContainsAny(lower, "matte", "matt", "flat", "unglazed")) return StylePreset.Matte;
        if (ContainsAny(lower, "rustic", "rough", "natural", "textured", "structured", "embossed")) return StylePreset.Rustic;
        if (ContainsAny(lower, "stone", "slate", "cobble", "brick", "marble", "travertine", "travertino", "onyx")) return StylePreset.Stone;
        if (ContainsAny(lower, "wood", "timber", "oak", "walnut", "maple", "parquet")) return StylePreset.Wood;
        if (ContainsAny(lower, "concrete", "cement", "screed")) return StylePreset.Concrete;
        if (ContainsAny(lower, "metal", "steel", "iron", "aluminum", "copper")) return StylePreset.Metal;

        return StylePreset.Unknown;
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (text.Contains(keywords[i]))
                return true;
        }

        return false;
    }

    private static float[] BuildLuma(Color32[] src)
    {
        float[] luma = new float[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            Color32 c = src[i];
            float r = c.r / 255f;
            float g = c.g / 255f;
            float b = c.b / 255f;
            luma[i] = 0.2126f * r + 0.7152f * g + 0.0722f * b;
        }

        return luma;
    }

    private static Texture2D BuildNormalMap(float[] luma, int width, int height, float strength)
    {
        Color32[] pixels = new Color32[luma.Length];
        float s = Mathf.Clamp(strength, 0.01f, 10f);

        for (int y = 0; y < height; y++)
        {
            int yUp = (y + 1) % height;
            int yDown = (y - 1 + height) % height;

            for (int x = 0; x < width; x++)
            {
                int xLeft = (x - 1 + width) % width;
                int xRight = (x + 1) % width;

                float hL = luma[y * width + xLeft];
                float hR = luma[y * width + xRight];
                float hD = luma[yDown * width + x];
                float hU = luma[yUp * width + x];

                float dx = (hR - hL) * s;
                float dy = (hU - hD) * s;

                Vector3 n = new Vector3(-dx, -dy, 1f).normalized;
                byte nx = (byte)Mathf.Clamp(Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f), 0, 255);
                byte ny = (byte)Mathf.Clamp(Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f), 0, 255);
                byte nz = (byte)Mathf.Clamp(Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f), 0, 255);

                pixels[y * width + x] = new Color32(nx, ny, nz, 255);
            }
        }

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true, true);
        tex.SetPixels32(pixels);
        tex.Apply(true, false);
        return tex;
    }

    private static Texture2D BuildOcclusionMap(float[] luma, int width, int height, float strength)
    {
        Color32[] pixels = new Color32[luma.Length];
        float s = Mathf.Clamp(strength, 0.01f, 10f);

        for (int y = 0; y < height; y++)
        {
            int yUp = (y + 1) % height;
            int yDown = (y - 1 + height) % height;

            for (int x = 0; x < width; x++)
            {
                int xLeft = (x - 1 + width) % width;
                int xRight = (x + 1) % width;

                float hL = luma[y * width + xLeft];
                float hR = luma[y * width + xRight];
                float hD = luma[yDown * width + x];
                float hU = luma[yUp * width + x];

                float contrast = Mathf.Abs(hR - hL) + Mathf.Abs(hU - hD);
                float ao = Mathf.Clamp01(1f - contrast * s);
                byte v = (byte)Mathf.Clamp(Mathf.RoundToInt(ao * 255f), 0, 255);

                pixels[y * width + x] = new Color32(v, v, v, 255);
            }
        }

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true, false);
        tex.SetPixels32(pixels);
        tex.Apply(true, false);
        return tex;
    }

    private static Texture2D BuildSmoothnessMap(float[] luma, int width, int height, float contrast)
    {
        Color32[] pixels = new Color32[luma.Length];
        float c = Mathf.Clamp(contrast, 0.01f, 4f);

        for (int i = 0; i < luma.Length; i++)
        {
            float v = Mathf.Clamp01(Mathf.Pow(luma[i], c));
            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
            pixels[i] = new Color32(b, b, b, 255);
        }

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true, false);
        tex.SetPixels32(pixels);
        tex.Apply(true, false);
        return tex;
    }

    private static Texture2D BuildHeightMap(float[] luma, int width, int height, float contrast)
    {
        Color32[] pixels = new Color32[luma.Length];
        float c = Mathf.Clamp(contrast, 0.01f, 4f);

        for (int i = 0; i < luma.Length; i++)
        {
            float v = Mathf.Clamp01(Mathf.Pow(luma[i], c));
            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
            pixels[i] = new Color32(b, b, b, 255);
        }

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true, false);
        tex.SetPixels32(pixels);
        tex.Apply(true, false);
        return tex;
    }

    private static bool EnsureReadable(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null) return false;

        bool changed = false;

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            changed = true;
        }

        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }

        return true;
    }

    private static void EnsureOutputFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets") return;

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string SanitizeName(string source)
    {
        if (string.IsNullOrEmpty(source)) return "Texture";

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            source = source.Replace(c, '_');
        }

        return source;
    }

    private static void SaveTexture(Texture2D texture, string folder, string filename, bool asNormalMap)
    {
        byte[] png = texture.EncodeToPNG();
        if (png == null || png.Length == 0) return;

        string path = folder + "/" + filename;
        File.WriteAllBytes(path, png);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = asNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = asNormalMap ? false : importer.sRGBTexture;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
    }
}
