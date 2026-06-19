using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TilePatternController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TileTextureSwapper tileSwapper;

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

    private void Start()
    {
      
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
               
            });
        }

        
        SetSpacing(spacingSlider.value);
        SetRotation(angleSlider.value);
    }

    private void SetSpacing(float value)
    {
       
    }

    private void SetRotation(float value)
    {
      
    }

    private void SetGapColor(Color color)
    {
        
    }

    private void SetTexture(Texture texture)
    {
        
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