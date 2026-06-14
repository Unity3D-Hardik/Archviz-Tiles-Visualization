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

    [Header("TileAngle Buttons")]
    [SerializeField] private Slider angleSlider;
    [SerializeField] private TMP_Text angleValueTxt;

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
                ApplyGapColor(color);
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

    private void ApplyGapColor(Color color)
    {
        targetMaterial.SetColor(GapColorID, color);
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
}