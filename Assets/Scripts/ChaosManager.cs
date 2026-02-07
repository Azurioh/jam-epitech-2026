using Unity.Netcode;
using UnityEngine;

public class ChaosManager : NetworkBehaviour
{
    public static ChaosManager Instance;

    public NetworkVariable<float> Stability = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            UpdateStabilityUI(Stability.Value);

            Stability.OnValueChanged += (oldVal, newVal) => UpdateStabilityUI(newVal);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            Stability.OnValueChanged -= (oldVal, newVal) => UpdateStabilityUI(newVal);
        }
    }

    private void UpdateStabilityUI(float value)
    {
        if (HUDController.Instance != null)
        {
            HUDController.Instance.UpdateStability(value);
        }
    }

    void Update()
    {
        if (IsServer)
        {
            if (Stability.Value >= 100f)
            {
                Debug.Log("GAME OVER - STABILITY REACHED 100%");
            }
        }
    }

    public void IncreaseChaos(float amount)
    {
        if (IsServer)
        {
            Stability.Value += amount;
            if (Stability.Value > 100f) Stability.Value = 100f;
        }
        else
        {
            IncreaseChaosServerRpc(amount);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void IncreaseChaosServerRpc(float amount)
    {
        Stability.Value += amount;
        if (Stability.Value > 100f) Stability.Value = 100f;
    }
}
