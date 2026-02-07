using UnityEngine;
using System.Collections;
using TMPro;

public class HUDAnimator : MonoBehaviour
{
    
    public void ShakeObject(RectTransform target, float duration, float strength)
    {
        if (target != null) StartCoroutine(ShakeCoroutine(target, duration, strength));
    }

    public void PulseObject(RectTransform target, float duration, float scaleFactor)
    {
        if (target != null) StartCoroutine(PulseCoroutine(target, duration, scaleFactor));
    }


    private IEnumerator ShakeCoroutine(RectTransform target, float duration, float strength)
    {
        Vector3 originalPos = target.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * strength;
            float y = UnityEngine.Random.Range(-1f, 1f) * strength;

            target.anchoredPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.anchoredPosition = originalPos;
    }

    private IEnumerator PulseCoroutine(RectTransform target, float duration, float scaleFactor)
    {
        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale * scaleFactor;

        float halfDuration = duration / 2f;
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            target.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            target.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localScale = originalScale;
    }
}
