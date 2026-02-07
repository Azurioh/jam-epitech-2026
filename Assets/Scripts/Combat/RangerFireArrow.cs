using Unity.Netcode;
using UnityEngine;

public class RangerFireArrow : NetworkBehaviour, IAbility
{
    [Header("Fire Arrow Settings")]
    [SerializeField] private float cooldown = 8f;
    [SerializeField] private float attackLockDuration = 0.5f;
    [SerializeField] private float fireArrowDuration = 6f;
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField] private GameObject fireArrowPrefab;

    private float lastUseTime = -999f;
    private bool isFireArrowActive = false;
    private Weapon weaponComponent;
    private float normalDamage;
    private GameObject normalArrowPrefab;
    private PlayerController playerController;

    public float Cooldown => cooldown;
    public float AttackLockDuration => attackLockDuration;
    public bool IsReady => Time.time - lastUseTime >= cooldown;
    public float TimeUntilReady => Mathf.Max(0f, cooldown - (Time.time - lastUseTime));

    void Awake()
    {
        weaponComponent = GetComponentInChildren<Weapon>();
        playerController = GetComponent<PlayerController>();

        if (weaponComponent != null)
        {
            var damageField = typeof(Weapon).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                normalDamage = (float)damageField.GetValue(weaponComponent);
            }
        }
    }

    public void Activate()
    {
        if (!IsReady)
        {
            return;
        }

        if (isFireArrowActive)
        {
            return;
        }

        lastUseTime = Time.time;

        StartCoroutine(FireArrowMode());

        if (IsOwner)
        {
            ActivateFireArrowServerRpc();
        }
    }

    System.Collections.IEnumerator FireArrowMode()
    {
        isFireArrowActive = true;

        if (playerController != null)
        {
            var projectileField = typeof(PlayerController).GetField("projectilePrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (projectileField != null)
            {
                normalArrowPrefab = (GameObject)projectileField.GetValue(playerController);

                if (fireArrowPrefab != null)
                {
                    projectileField.SetValue(playerController, fireArrowPrefab);
                }
            }
        }

        if (weaponComponent != null)
        {
            var damageField = typeof(Weapon).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                float boostedDamage = normalDamage * damageMultiplier;
                damageField.SetValue(weaponComponent, boostedDamage);
            }
        }

        yield return new WaitForSeconds(fireArrowDuration);

        if (playerController != null && normalArrowPrefab != null)
        {
            var projectileField = typeof(PlayerController).GetField("projectilePrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (projectileField != null)
            {
                projectileField.SetValue(playerController, normalArrowPrefab);
            }
        }

        if (weaponComponent != null)
        {
            var damageField = typeof(Weapon).GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null)
            {
                damageField.SetValue(weaponComponent, normalDamage);
            }
        }

        isFireArrowActive = false;
    }

    [ServerRpc]
    private void ActivateFireArrowServerRpc()
    {
        ActivateFireArrowClientRpc();
    }

    [ClientRpc]
    private void ActivateFireArrowClientRpc()
    {
        if (!IsOwner)
        {
            StartCoroutine(FireArrowMode());
        }
    }
}
