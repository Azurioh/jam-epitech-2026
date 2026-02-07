using UnityEngine;
using TMPro;

public class CharacterSelectorUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI characterNameText;
    public CharacterSelector selector;

    void Update()
    {
        if (characterNameText != null && selector != null)
        {
            characterNameText.text = selector.GetCurrentCharacterName();
        }
    }

    public void OnNextButton()
    {
        if (selector != null) selector.NextCharacter();
    }

    public void OnPreviousButton()
    {
        if (selector != null) selector.PreviousCharacter();
    }

    public void OnConfirmButton()
    {
        if (selector != null) selector.ConfirmSelection();
    }
}
