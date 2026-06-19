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

    [Header("Defaults")]
    [SerializeField] private Color defaultGapColor = Color.black;

    private void Start()
    {
        if (tileSwapper == null)
            tileSwapper = GetComponent<TileTextureSwapper>();

        if (tileSwapper == null)
        {
            Debug.LogWarning("[TilePatternController] Missing TileTextureSwapper reference.", this);
            return;
        }

        if (spacingSlider != null)
            spacingSlider.onValueChanged.AddListener(SetSpacing);
        if (angleSlider != null)
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

        
        if (spacingSlider != null)
            SetSpacing(spacingSlider.value);
        if (angleSlider != null)
            SetRotation(angleSlider.value);
    }

    private void SetSpacing(float value)
    {
        if (spacingValueTxt != null)
            spacingValueTxt.text = value.ToString("F1") + " mm";

        if (tileSwapper != null)
            tileSwapper.SetGapSize(value);
    }

    private void SetRotation(float value)
    {
        if (angleValueTxt != null)
            angleValueTxt.text = value.ToString("F0") + " deg";

        if (tileSwapper != null)
            tileSwapper.SetRotationOnly(value);
    }

    private void SetGapColor(Color color)
    {
        if (tileSwapper != null)
            tileSwapper.SetGapColor(color);
    }

    private void SetTexture(Texture texture)
    {
        if (tileSwapper == null || texture == null)
            return;

        Texture2D texture2D = texture as Texture2D;
        if (texture2D == null)
        {
            Debug.LogWarning("[TilePatternController] Tile texture must be Texture2D.", this);
            return;
        }

        TileTextureSwapper.TileTextureSet setData = new TileTextureSwapper.TileTextureSet
        {
            BaseMap = texture2D,
            GapColor = defaultGapColor,
            Rotation = angleSlider != null ? angleSlider.value : 0f,
            GapSizeMM = spacingSlider != null ? spacingSlider.value : 2f,
            AutoDetectFromBaseName = true
        };

        tileSwapper.SwapTextureSet(setData);

        // If explicit tile dimensions are supplied by button data, prefer them.
        TileButtonPair match = tileButtons.Find(tb => tb.texture == texture);
        if (match != null && match.width > 0 && match.height > 0)
            tileSwapper.SetTileSizeMM(match.width * 100f, match.height * 100f);
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