using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;
public class UpdateCell : MonoBehaviour
{
    public GameObject panel;
    public bool toggle = false;
    //public GameObject panel;
    
    public void Open_Settings()
    {
        
        panel.SetActive(true);
        
        //GameObject.FindGameObjectWithTag("Settings_Menu").SetActive(true);
    }


}
