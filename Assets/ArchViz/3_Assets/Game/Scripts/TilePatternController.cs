using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TilePatternController : MonoBehaviour
{
    [Header("Material")]
    [SerializeField] private Material targetMaterial;

    [Header("Size")]
    [SerializeField] private Slider widthSlider;
    [SerializeField] private TMP_Text widthValueTxt;
    [SerializeField] private Slider heightSlider;
    [SerializeField] private TMP_Text heightValueTxt;

    [Header("Spacing")]
    [SerializeField] private Slider spacingSlider;
    [SerializeField] private TMP_Text spacingValueTxt;

    [Header("Gap Color Buttons")]
    [SerializeField] private List<ColorButtonPair> colorButtons = new();

    [Header("Tile Buttons")]
    [SerializeField] private List<TileButtonPair> tileButtons = new();

    [Header("TileAngle Buttons")]
    [SerializeField] private Slider angleSlider;
    [SerializeField] private TMP_Text angleValueTxt;

    private static readonly int TextureID = Shader.PropertyToID("_MainTex");
    private static readonly int ImageWidthID = Shader.PropertyToID("_ImageWidth");
    private static readonly int ImageHeightID = Shader.PropertyToID("_ImageHeight");
    private static readonly int SpacingXID = Shader.PropertyToID("_SpacingX");
    private static readonly int SpacingYID = Shader.PropertyToID("_SpacingY");
    private static readonly int GapColorID = Shader.PropertyToID("_GapColor");
    private static readonly int RotationAngleID = Shader.PropertyToID("_Rotation");

    private void Start()
    {
        widthSlider.onValueChanged.AddListener(SetWidth);
        heightSlider.onValueChanged.AddListener(SetHeight);

        spacingSlider.onValueChanged.AddListener(SetSpacing);
        angleSlider.onValueChanged.AddListener(SetRotation);

        foreach (var pair in colorButtons)
        {
            if (pair.button == null)
                continue;

            Color color = pair.color;

            pair.button.onClick.AddListener(() =>
            {
                SetGapColor(color);
            });
        }

        foreach (var tileData in tileButtons)
        {
            
            if (tileData.button == null)
                continue;

            float height = tileData.height;
            float width = tileData.width;
            Texture texture = tileData.texture;

            tileData.button.onClick.AddListener(() =>
            {
                SetTexture(texture);
                SetHeight(height);
                SetWidth(width);
            });
        }

        SetWidth(widthSlider.value);
        SetHeight(heightSlider.value);
        SetSpacing(spacingSlider.value);
        SetRotation(angleSlider.value);
    }

    private void SetWidth(float value)
    {
        value /= 10;
        targetMaterial.SetFloat(ImageWidthID, value);
        widthValueTxt.text = (value * 1000) + "mm";
    }

    private void SetHeight(float value)
    {
        value /= 10;
        targetMaterial.SetFloat(ImageHeightID, value);
        heightValueTxt.text = (value * 1000) + "mm";
    }

    private void SetSpacing(float value)
    {
        spacingValueTxt.text = value  + "mm";
        value /= 10;
        targetMaterial.SetFloat(SpacingXID, value);
        targetMaterial.SetFloat(SpacingYID, value);
        
    }

    private void SetRotation(float value)
    {
        targetMaterial.SetFloat(RotationAngleID, value);
        angleValueTxt.text = value.ToString();
    }

    private void SetGapColor(Color color)
    {
        targetMaterial.SetColor(GapColorID, color);
    }

    private void SetTexture(Texture texture)
    {
        targetMaterial.SetTexture(TextureID, texture);
    }


    [Serializable]
    public class AngleButtonPair
    {
        public Button button;
        public float angle;
    }


    [Serializable]
    public class ColorButtonPair
    {
        public Button button;
        public Color color;
    }


    [Serializable]
    public class TileButtonPair
    {
        public Button button;
        public Texture texture;

        [Header("These values will be multiply by 100.")]
        [Range(2, 24)] public int height;
        [Range(2, 24)] public int width;
    }
}