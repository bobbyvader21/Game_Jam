using UnityEngine;

public class Enable3DVisuals : MonoBehaviour
{
    // the parent gameobject that contains all the canvas visuals to enable when we are in 3D mode
    [SerializeField] private GameObject objectsToEnableIn3D;

    private static Enable3DVisuals instance;

    public static Enable3DVisuals Instance { get => instance; }

    private void Awake()
    {
        // Ensure only one instance of Enable3DVisuals exists
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            //DontDestroyOnLoad(this.gameObject); // MAYBE: Keep this object across scenes
        }
    }
    private void Start()
    {
        if (objectsToEnableIn3D != null)
        {
            // Disable the objects at the start
            objectsToEnableIn3D.SetActive(false);
        }
    }

    public void SetEnable3DObjects(bool value)
    {
        if (objectsToEnableIn3D != null)
        {
            objectsToEnableIn3D.SetActive(value);
        }
    }

}
