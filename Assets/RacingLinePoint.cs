using UnityEngine;

public class RacingLinePoint : MonoBehaviour
{
    public float turnSeverity;
    public Vector2 coordinates;
    public bool IsActive { get; private set; }
    private Renderer pointRenderer;

    void Awake()
    {
        pointRenderer = GetComponentInChildren<Renderer>();
        Debug.Log(pointRenderer);
    }

    public void SetColor(Color color)
    {
        if (IsActive && pointRenderer != null)
        {
            pointRenderer.material.color = color;
        }
    }

    public void SetActiveState(bool active)
    {
        IsActive = active;

        if (pointRenderer != null)
        {
            pointRenderer.enabled = active; // Toggles visibility
        }
    }
}