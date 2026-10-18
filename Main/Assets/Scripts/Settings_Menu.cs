using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Settings_Menu : MonoBehaviour
{
    public GameObject panel;
    public bool toggle = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        panel.SetActive(toggle);
    }

    
    public void ShowPanel()
    {
        if (toggle == false)
            toggle = true;
        else
            toggle = false;
    }
}
