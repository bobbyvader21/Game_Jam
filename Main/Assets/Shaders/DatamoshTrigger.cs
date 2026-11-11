using UnityEngine;
using UnityEngine.InputSystem;

public class DatamoshTrigger : MonoBehaviour
{
    [Tooltip("How fast the datamosh fades in when pressed (seconds)")]
    public float fadeInDuration = 0.25f;

    [Tooltip("How fast the datamosh clears when released (seconds)")]
    public float fadeOutDuration = 0.5f;

    private float _currentBlend = 0f;

    void Update()
    {
        bool isPressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
        float target = isPressed ? 1f : 0f;

        float duration = isPressed ? fadeInDuration : fadeOutDuration;
        float speed = (duration > 0.001f) ? (1f / duration) : 1000f;

        _currentBlend = Mathf.MoveTowards(_currentBlend, target, speed * Time.deltaTime);

        Shader.SetGlobalFloat("_Button", _currentBlend);
    }
}
