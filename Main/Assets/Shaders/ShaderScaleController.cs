using UnityEngine;

public class ShaderScaleController : MonoBehaviour
{
    [Header("External Script Reference")]
    // Replace 'YourOtherScript' with the actual name of your script
    public Camera_Persp_Change dataScript;

    [Header("Shader Settings")]
    public float bufferDistance = 0.1f;

    private Renderer[] childRenderers;
    private MaterialPropertyBlock propBlock;
    private static readonly int ScaleFactorID = Shader.PropertyToID("_ScaleFactor");
    private static readonly int BufferID = Shader.PropertyToID("_Buffer");
    private static readonly int ParentPosXID = Shader.PropertyToID("_ParentPosX");

    void Start()
    {
        // Get all renderers in the children
        childRenderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (dataScript == null) return;

        // Fetch the dynamic variable from your other script
        float currentScaleFactor = dataScript.globalSquishFactor;

        // Apply properties to all child renderers
        foreach (Renderer rend in childRenderers)
        {
            rend.GetPropertyBlock(propBlock);

            propBlock.SetFloat(ScaleFactorID, currentScaleFactor);
            propBlock.SetFloat(BufferID, bufferDistance);

            // Pass the parent's world X position so vertices know where to scale towards
            propBlock.SetFloat(ParentPosXID, transform.position.x);

            rend.SetPropertyBlock(propBlock);
        }
    }
}