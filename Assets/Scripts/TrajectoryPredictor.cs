using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPredictor : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    [SerializeField]private float maxTime = 2.0f;
    [SerializeField]private int resolution = 30;

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        Hide();
    }

    public void PredictTrajectory(Vector3 startPoint, Vector3 initialVelocity)
    {
        _lineRenderer.positionCount = resolution + 1;
        Vector3 gravity = Physics.gravity;

        for (int i = 0; i <= resolution; i++)
        {
            float time = (i / (float)resolution * maxTime);
            Vector3 position = startPoint + initialVelocity * time + gravity * (0.5f * time * time);
            _lineRenderer.SetPosition(i, position);
        }
    }
    public void Show()
    {
        _lineRenderer.enabled = true;
    }

    public void Hide()
    {
        _lineRenderer.enabled = false;
    }
}
