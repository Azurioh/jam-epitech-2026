using UnityEngine;

public class LagEffectReceiver : MonoBehaviour
{
    [SerializeField] private float lagFps = 10f;

    private const float DefaultFps = 60f;

    private bool isLagged;
    private int lagSources;
    private float nextAllowedTime;
    private float nextAnimationTime;
    private float nextMovementTime;

    public bool IsLagged => isLagged;

    public float GetSpeedMultiplier(float baseFps = DefaultFps)
    {
        if (!isLagged) return 1f;

        float safeBase = Mathf.Max(1f, baseFps);
        return Mathf.Clamp(lagFps / safeBase, 0.05f, 1f);
    }

    public void AddLag(float overrideFps)
    {
        lagSources = Mathf.Max(0, lagSources + 1);
        isLagged = lagSources > 0;
        if (overrideFps > 0f)
        {
            lagFps = overrideFps;
        }

        if (!isLagged)
        {
            nextAllowedTime = 0f;
            nextAnimationTime = 0f;
            nextMovementTime = 0f;
        }
    }

    public void RemoveLag()
    {
        lagSources = Mathf.Max(0, lagSources - 1);
        isLagged = lagSources > 0;
        if (!isLagged)
        {
            nextAllowedTime = 0f;
            nextAnimationTime = 0f;
            nextMovementTime = 0f;
        }
    }

    public bool CanProcessUpdate()
    {
        if (!isLagged)
        {
            return true;
        }

        float step = 1f / Mathf.Max(lagFps, 1f);
        if (Time.time >= nextAllowedTime)
        {
            nextAllowedTime = Time.time + step;
            return true;
        }

        return false;
    }

    public bool TryGetAnimationDelta(out float delta)
    {
        if (!isLagged)
        {
            delta = 0f;
            return false;
        }

        float step = 1f / Mathf.Max(lagFps, 1f);
        if (Time.time >= nextAnimationTime)
        {
            nextAnimationTime = Time.time + step;
            delta = step;
            return true;
        }

        delta = 0f;
        return false;
    }

    public bool TryConsumeLagStep(out float step)
    {
        if (!isLagged)
        {
            step = 0f;
            return false;
        }

        float lagStep = 1f / Mathf.Max(lagFps, 1f);
        if (Time.time >= nextMovementTime)
        {
            nextMovementTime = Time.time + lagStep;
            step = lagStep;
            return true;
        }

        step = 0f;
        return false;
    }
}
