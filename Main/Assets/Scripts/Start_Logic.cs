using UnityEngine;
using UnityEngine.SceneManagement;

public class Start_Logic : MonoBehaviour
{
    public string SampleScene;

    public void LoadLevel()
    {
        SceneManager.LoadScene(SampleScene);
    }
}
