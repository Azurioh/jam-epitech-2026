using UnityEngine;
using TMPro;
using System.Collections;

public class DamageNumber : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatDistance = 2f;
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private float fadeDelay = 0.5f;

    [Header("Movement")]
    [SerializeField] private Vector3 randomOffset = new Vector3(0.5f, 0f, 0.5f);
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color criticalColor = new Color(1f, 0.3f, 0f);
    [SerializeField] private Color healColor = new Color(0.3f, 1f, 0.3f);

    private TextMeshPro textMesh;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float elapsedTime = 0f;
    private Color initialColor;

    public void Initialize(float damage, bool isCritical = false, bool isHeal = false)
    {
        textMesh = gameObject.AddComponent<TextMeshPro>();

        if (textMesh == null)
        {
            Debug.LogError("Failed to create TextMeshPro component!");
            Destroy(gameObject);
            return;
        }

        textMesh.fontSize = 4;
        textMesh.alignment = TMPro.TextAlignmentOptions.Center;
        textMesh.enableWordWrapping = false;
        textMesh.overflowMode = TMPro.TextOverflowModes.Overflow;
        textMesh.sortingOrder = 100;

        if (isHeal)
        {
            textMesh.text = $"+{Mathf.RoundToInt(damage)}";
            initialColor = healColor;
        }
        else
        {
            textMesh.text = Mathf.RoundToInt(damage).ToString();
            initialColor = isCritical ? criticalColor : normalColor;
        }

        if (isCritical)
        {
            textMesh.fontSize *= 1.3f;
            textMesh.fontStyle = FontStyles.Bold;
        }

        textMesh.color = initialColor;

        startPosition = transform.position;
        Vector3 randomDir = new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            0f,
            Random.Range(-randomOffset.z, randomOffset.z)
        );
        targetPosition = startPosition + Vector3.up * floatDistance + randomDir;

        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }

        StartCoroutine(AnimateAndDestroy());
    }

    void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }
    }

    IEnumerator AnimateAndDestroy()
    {
        elapsedTime = 0f;

        while (elapsedTime < lifetime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / lifetime;

            float curveValue = movementCurve.Evaluate(progress);
            transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);

            if (elapsedTime > fadeDelay)
            {
                float fadeProgress = (elapsedTime - fadeDelay) / (lifetime - fadeDelay);
                Color currentColor = initialColor;
                currentColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
                textMesh.color = currentColor;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
