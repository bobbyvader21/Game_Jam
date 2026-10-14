using UnityEngine;

public class levelFollow2D : MonoBehaviour

    
{
    [SerializeField] private Transform targetObject;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(targetObject.position.x, transform.position.y, transform.position.z);
    }
}
