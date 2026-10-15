using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class UpdateCell : MonoBehaviour
{
    public TextMeshProUGUI Textfield;

    //Toggle State
    private bool isOriginalText = true;
    public void ToggleText()
    {
        if (isOriginalText)
        {
            Textfield.text = "Clicked!";
        }
        else
        {
            Textfield.text = "Settings";
        }

        isOriginalText = !isOriginalText;
    }
    public void SetText(string text)
    {
        Textfield.text = text;
    }


}
