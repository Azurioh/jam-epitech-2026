using UnityEngine;

/// <summary>
/// ScriptableObject contenant toutes les informations d'un personnage
/// Créer via: Assets > Create > Game > Character Data
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("📋 Informations")]
    public string characterName = "Unknown Hero";
    
    [TextArea(3, 6)]
    public string biography = "A mysterious hero...";

    [Header("❤️ Stats de Base")]
    [Range(50, 300)]
    public int health = 100;
    
    [Range(5, 50)]
    public int damage = 15;
    
    [Range(0.5f, 3f)]
    public float attackSpeed = 1.0f;
    
    [Range(3f, 10f)]
    public float moveSpeed = 5f;

    [Header("⚔️ Compétences")]
    public AbilityInfo[] abilities;
}

/// <summary>
/// Informations sur une compétence
/// </summary>
[System.Serializable]
public class AbilityInfo
{
    public string abilityName = "New Ability";
    
    [TextArea(2, 4)]
    public string description = "Ability description...";
    
    [Range(0f, 30f)]
    public float cooldown = 5f;
    
    public Sprite icon;
}
