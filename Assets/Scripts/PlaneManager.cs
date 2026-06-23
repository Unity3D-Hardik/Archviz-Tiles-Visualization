using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class PlaneManager : MonoBehaviour
{

    //this is our floor tile.
    [SerializeField]
    private Material _planeMaterial;

    [SerializeField]
    private Button[] _rawImages;

    [SerializeField]
    private Texture[] _textures;

    private ARPlaneManager _arPlaneManager;
    private ARPlane _firstPlane;

    // Awake is called as soon as the app starts.
    void Awake()
    {
        _arPlaneManager = FindObjectOfType<ARPlaneManager>();
        if (_arPlaneManager != null) {
            _arPlaneManager.planesChanged += OnPlanesChanged;
        }

        for(int i=0; i < 5; i++) {
            addImageListeners(i);
        }
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        if (_firstPlane == null && args.added.Count > 0)
        {
            _firstPlane = args.added[0];
        }

        foreach (var plane in args.added)
        {
            if (plane != _firstPlane)
            {
                plane.gameObject.SetActive(false);
            }
        }

        foreach (var plane in args.updated)
        {
            if (plane != _firstPlane && plane.gameObject.activeSelf)
            {
                plane.gameObject.SetActive(false);
            }
        }
    }

    void onButtonClick(int i) {
        _planeMaterial.mainTexture=_textures[i];
    }

    void addImageListeners(int i)
    {
        _rawImages[i].onClick.AddListener(() => onButtonClick(i));
    }
}