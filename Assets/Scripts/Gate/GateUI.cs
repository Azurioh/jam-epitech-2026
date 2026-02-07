using UnityEngine;
using UnityEngine.UI;

public class GateUI : MonoBehaviour
{
    public Image healthImage;
    private GateInit gate;
    private Transform playerCam;

    void Start()
    {
        gate = GetComponentInParent<GateInit>();
    }

    public static Color GetColor(float value)
    {
        float r = 1f - value;
        float g = value;
        float b = 0f;

        return new Color(r, g, b);
    }

    void Update()
    {
        if (playerCam == null)  {
            GameObject mainCam = GameObject.FindWithTag("MainCamera");

            if (mainCam != null) {
                playerCam = mainCam.transform;
            } else {
                return;
            }
        }

        if (gate == null || healthImage == null) return;

        float percent = (float)gate.health / gate.maxHealth;

        healthImage.fillAmount = percent;
        healthImage.color = GetColor(percent);

        Vector3 playerLocalPos = transform.parent.InverseTransformPoint(playerCam.position);

        // X gauche droite
        if (
            (gate.direction == GateInit.TypeSelection.East && playerLocalPos.y <= 0) ||
            (gate.direction == GateInit.TypeSelection.West && playerLocalPos.y >= 0) ||
            (gate.direction == GateInit.TypeSelection.North && playerLocalPos.y >= 0) ||
            (gate.direction == GateInit.TypeSelection.South && playerLocalPos.y >= 0)
        ) {
            healthImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        } else {
            healthImage.fillOrigin = (int)Image.OriginHorizontal.Right;
        }
    }
}