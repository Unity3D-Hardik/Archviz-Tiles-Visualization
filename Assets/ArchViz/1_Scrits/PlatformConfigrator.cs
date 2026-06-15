using UnityEngine;

public class PlatformConfigrator : MonoBehaviour
{

    public MeshRenderer platformMeshRenderer;
    public MeshRenderer kitchenMeshRenderer;


    // update material of the platform and kitchen based on the selected material in the configurator
    public void UpdatePlatformMaterial(Material material)
    {
        
        Material[] materials = platformMeshRenderer.materials;
        materials[0] = material;
        platformMeshRenderer.materials = materials;

      
        Material[] materials1 = kitchenMeshRenderer.materials;
        materials1[1] = material;
        kitchenMeshRenderer.materials = materials1;


        this.gameObject.SetActive(false);
    }
}
