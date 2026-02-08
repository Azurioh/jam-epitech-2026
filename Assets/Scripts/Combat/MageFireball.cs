using Unity.Netcode;
using UnityEngine;

public class MageFireball : NetworkBehaviour, IAbility
{
    [Header("Fireball Settings")]
    [SerializeField] private float cooldown = 4f;
    [SerializeField] private float attackLockDuration = 0.5f;
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float targetDistance = 4f;
    [SerializeField] private float arcHeight = 2.5f;

    private float lastUseTime = -999f;
    private Camera playerCamera;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    void Awake()
    {
        if (fireballPrefab == null)
        {
            fireballPrefab = Resources.Load<GameObject>("Fireball");
        }

        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    public void Activate()
    {
        if (!IsReady)
        {
            return;
        }

        lastUseTime = Time.time;

        ShootFireball();

        if (IsOwner)
        {
            ActivateFireballServerRpc();
        }
    }

    void ShootFireball()
    {
        if (fireballPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = shootPoint != null ? shootPoint.position : transform.position + Vector3.up * 2.5f;

        Vector3 forwardDir;
        if (playerCamera != null)
        {
            forwardDir = playerCamera.transform.forward;
            forwardDir.y = 0f;
            forwardDir.Normalize();
        }
        else
        {
            forwardDir = transform.forward;
        }

        Vector3 targetPos = transform.position + forwardDir * targetDistance;

        Vector3 velocity = CalculateArcVelocity(spawnPos, targetPos, arcHeight);

        GameObject fireball = Instantiate(fireballPrefab, spawnPos, Quaternion.LookRotation(forwardDir));

        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.linearVelocity = velocity;
        }
    }

    Vector3 CalculateArcVelocity(Vector3 start, Vector3 target, float height)
    {
        float gravity = Physics.gravity.magnitude;
        Vector3 displacement = target - start;
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0f, displacement.z);
        float horizontalDistance = horizontalDisplacement.magnitude;

        float timeToApex = Mathf.Sqrt(2f * height / gravity);

        float verticalVelocity = Mathf.Sqrt(2f * gravity * height);

        float totalTime = timeToApex + Mathf.Sqrt(2f * (height + displacement.y) / gravity);

        Vector3 horizontalVelocity = horizontalDisplacement / totalTime;

        return horizontalVelocity + Vector3.up * verticalVelocity;
    }

    [ServerRpc]
    private void ActivateFireballServerRpc()
    {
        ActivateFireballClientRpc();
    }

    [ClientRpc]
    private void ActivateFireballClientRpc()
    {
        if (!IsOwner)
        {
            ShootFireball();
        }
    }
}
