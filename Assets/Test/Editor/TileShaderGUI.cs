// TileShaderGUI.cs — Editor only (Assets/Editor folder).
// All detection/keyword logic lives in TileTextureSwapper.cs (runtime).
// This file is purely the Inspector UI layer — delegates everything to TileTextureSwapper.
using System.Globalization;
using UnityEditor;
using UnityEngine;

public class TileShaderGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);

        // Sync keywords on every inspector repaint so the material stays correct.
        foreach (Object target in materialEditor.targets)
        {
            if (target is Material mat)
                TileTextureSwapper.UpdateMaterialKeywords(mat);
        }

        MaterialProperty baseMapProp  = FindProperty("_BaseMap",                    properties, false);
        MaterialProperty autoReadProp = FindProperty("_AutoReadSizeFromTextureName", properties, false);
        MaterialProperty tileSizeProp = FindProperty("_TileSizeMM",                 properties, false);

        if (baseMapProp == null || autoReadProp == null || tileSizeProp == null) return;
        if (autoReadProp.floatValue <= 0.5f) return;
        if (baseMapProp.textureValue == null) return;

        string texName = baseMapProp.textureValue.name;

        // ---- Tile size from name -----------------------------------------------
        if (TileTextureSwapper.TryParseSizeFromName(texName, out float widthMM, out float heightMM))
        {
            Vector4 current = tileSizeProp.vectorValue;
            if (!Mathf.Approximately(current.x, widthMM) || !Mathf.Approximately(current.y, heightMM))
            {
                materialEditor.RegisterPropertyChangeUndo("Auto tile size from texture name");
                tileSizeProp.vectorValue = new Vector4(widthMM, heightMM, current.z, current.w);
                foreach (Object t in materialEditor.targets)
                    if (t is Material m) EditorUtility.SetDirty(m);
            }
            GUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Size auto-set: " + widthMM.ToString(CultureInfo.InvariantCulture)
                + " x " + heightMM.ToString(CultureInfo.InvariantCulture) + " mm",
                MessageType.Info);
        }

        // ---- Finish type from name ---------------------------------------------
        if (TileTextureSwapper.TryDetectFinish(texName, out TileTextureSwapper.FinishPreset preset))
        {
            MaterialProperty metallicProp  = FindProperty("_Metallic",           properties, false);
            MaterialProperty smoothProp    = FindProperty("_Smoothness",          properties, false);
            MaterialProperty bumpProp      = FindProperty("_BumpScale",           properties, false);
            MaterialProperty clearCoatProp = FindProperty("_ClearCoat",           properties, false);
            MaterialProperty clearSmProp   = FindProperty("_ClearCoatSmoothness", properties, false);

            bool needsApply =
                (metallicProp  != null && !Mathf.Approximately(metallicProp.floatValue,  preset.Metallic))    ||
                (smoothProp    != null && !Mathf.Approximately(smoothProp.floatValue,    preset.Smoothness))  ||
                (bumpProp      != null && !Mathf.Approximately(bumpProp.floatValue,      preset.NormalScale)) ||
                (clearCoatProp != null && !Mathf.Approximately(clearCoatProp.floatValue, preset.ClearCoat));

            if (needsApply)
            {
                materialEditor.RegisterPropertyChangeUndo("Auto finish: " + preset.DisplayName);
                if (metallicProp  != null) metallicProp.floatValue  = preset.Metallic;
                if (smoothProp    != null) smoothProp.floatValue    = preset.Smoothness;
                if (bumpProp      != null) bumpProp.floatValue      = preset.NormalScale;
                if (clearCoatProp != null) clearCoatProp.floatValue = preset.ClearCoat;
                if (clearSmProp   != null) clearSmProp.floatValue   = preset.ClearCoatSmooth;

                foreach (Object t in materialEditor.targets)
                    if (t is Material m)
                    {
                        TileTextureSwapper.UpdateMaterialKeywords(m);
                        EditorUtility.SetDirty(m);
                    }
            }

            GUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Finish: " + preset.DisplayName
                + "  |  Metallic: "   + preset.Metallic.ToString("F2",    CultureInfo.InvariantCulture)
                + "  Smoothness: "    + preset.Smoothness.ToString("F2",  CultureInfo.InvariantCulture)
                + "  NormalScale: "   + preset.NormalScale.ToString("F2", CultureInfo.InvariantCulture),
                MessageType.Info);
        }
    }
}
