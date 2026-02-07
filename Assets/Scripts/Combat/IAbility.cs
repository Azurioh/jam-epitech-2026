using UnityEngine;

/// <summary>
/// Interface pour les compétences (Special1 - Clic droit)
/// Exemple: Shield du Knight, Rage du Barbarian
/// </summary>
public interface IAbility
{
    /// <summary>
    /// Active la compétence
    /// </summary>
    void Activate();

    /// <summary>
    /// Le cooldown de la compétence en secondes
    /// </summary>
    float Cooldown { get; }

    /// <summary>
    /// Durée pendant laquelle le joueur ne peut pas attaquer après activation
    /// </summary>
    float AttackLockDuration { get; }

    /// <summary>
    /// Est-ce que la compétence est prête à être utilisée ?
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Temps restant avant de pouvoir réutiliser la compétence
    /// </summary>
    float TimeUntilReady { get; }
}
