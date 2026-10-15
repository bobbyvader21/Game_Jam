using UnityEngine;

public class ShaderScaleController : MonoBehaviour
{
    [Header("External References")]
    public Camera_Persp_Change dataScript;
    public Transform targetAnchor;

    [Header("Shader Settings")]
    public float bufferDistance = 0.01f;

    [Header("Culling Fix")]
    [Tooltip("Expands the CPU bounding box so the camera doesn't accidentally hide the object.")]
    public float boundsExpansion = 50f;

    private Renderer[] childRenderers;
    private MaterialPropertyBlock propBlock;

    private static readonly int ScaleFactorID = Shader.PropertyToID("_ScaleFactor");
    private static readonly int BufferID = Shader.PropertyToID("_Buffer");
    private static readonly int TargetPosXID = Shader.PropertyToID("_TargetPosX");

    void Start()
    {
        childRenderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        // Expands the mesh bounds so the CPU doesn't cull the GPU-scaled vertices
        foreach (Renderer rend in childRenderers)
        {
            MeshFilter mf = rend.GetComponent<MeshFilter>();
            if (mf != null && mf.mesh != null)
            {
                Bounds newBounds = mf.mesh.bounds;
                newBounds.Expand(boundsExpansion);
                mf.mesh.bounds = newBounds;
            }
        }
    }

    void Update()
    {
        if (dataScript == null || targetAnchor == null) return;

        float currentScaleFactor = dataScript.globalSquishFactor;

        foreach (Renderer rend in childRenderers)
        {
            rend.GetPropertyBlock(propBlock);

            propBlock.SetFloat(ScaleFactorID, currentScaleFactor);
            propBlock.SetFloat(BufferID, bufferDistance);
            propBlock.SetFloat(TargetPosXID, targetAnchor.position.x);

            rend.SetPropertyBlock(propBlock);
        }
    }
}