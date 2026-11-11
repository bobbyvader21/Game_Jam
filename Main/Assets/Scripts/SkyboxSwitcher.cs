using UnityEngine;

public class SkyboxSwitcher : MonoBehaviour
{
    // Assign your skybox materials in the Unity Inspector
    public Material Skybox2D;
    public Material Skybox3D;

    public void Set2DSkybox()
    {
        RenderSettings.skybox = Skybox2D;

        // Updates ambient lighting to match the new skybox
        DynamicGI.UpdateEnvironment();
    }

    public void Set3DSkybox()
    {
        RenderSettings.skybox = Skybox3D;
        DynamicGI.UpdateEnvironment();
    }
}