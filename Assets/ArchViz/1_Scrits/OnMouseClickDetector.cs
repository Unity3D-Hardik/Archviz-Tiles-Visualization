using UnityEngine;

public class OnMouseClickDetector : MonoBehaviour
{

    [SerializeField]PlatformConfigrator configrator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // on mouse click on 3D sphere, change the color of the sphere to a random color
    private void OnMouseDown()
    {
       // Get material of the sphere
        Material material = GetComponent<Renderer>().material;
        configrator.UpdatePlatformMaterial(material);
    }
}
