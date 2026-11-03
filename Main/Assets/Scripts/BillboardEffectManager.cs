using System.Collections.Generic;
using UnityEngine;

public class BillboardEffectManager : MonoBehaviour
{
    private List<GameObject> billboardObjects = new List<GameObject>();
    private void Start()
    {
        // Find all objects with the "Billboard" tag and add them to the list
        GameObject[] foundObjects = GameObject.FindGameObjectsWithTag("Billboard");
        billboardObjects.AddRange(foundObjects);
    }
    private void Update()
    {
        // Make each billboard object face the camera
        foreach (GameObject billboard in billboardObjects)
        {
            if (billboard != null)
            {
                Vector3 direction = Camera.main.transform.position - billboard.transform.position;
                direction.y = 0; // Keep the billboard upright
                billboard.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}
