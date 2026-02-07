using UnityEngine;

public class ShockwaveEffect : MonoBehaviour
{
    [SerializeField] private float maxRadius = 6f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float startWidth = 0.2f;
    [SerializeField] private float endWidth = 0.05f;
    [SerializeField] private Color color = new Color(0.6f, 0.9f, 1f, 0.9f);
    [SerializeField] private int segments = 48;

    private LineRenderer line;
    private float elapsed;

    void Awake()
    {
        if (line == null)
        {
            line = gameObject.AddComponent<LineRenderer>();
        }

        line.useWorldSpace = true;
        line.loop = true;
        line.alignment = LineAlignment.TransformZ;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        line.positionCount = Mathf.Max(8, segments);
        line.startWidth = startWidth;
        line.endWidth = startWidth;
        line.sortingOrder = 10;
    }

    public void Initialize(float newMaxRadius, float newDuration, float newStartWidth, float newEndWidth, Color newColor, int newSegments)
    {
        maxRadius = Mathf.Max(0.01f, newMaxRadius);
        duration = Mathf.Max(0.01f, newDuration);
        startWidth = Mathf.Max(0.001f, newStartWidth);
        endWidth = Mathf.Max(0.001f, newEndWidth);
        color = newColor;
        segments = Mathf.Max(8, newSegments);

        if (line == null)
        {
            line = gameObject.AddComponent<LineRenderer>();
        }

        line.positionCount = segments;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = startWidth;
        line.endWidth = startWidth;
        line.sortingOrder = 10;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float radius = Mathf.Lerp(0f, maxRadius, t);
        float width = Mathf.Lerp(startWidth, endWidth, t);

        line.startWidth = width;
        line.endWidth = width;

        UpdateCircle(radius);

        if (elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateCircle(float radius)
    {
        int count = line.positionCount;
        float step = 360f / count;
        Vector3 center = transform.position;
        for (int i = 0; i < count; i++)
        {
            float angle = step * i * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            line.SetPosition(i, new Vector3(center.x + x, center.y, center.z + z));
        }
    }
}
