#nullable enable
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

public static class ComboEvaluator
{
    /// <summary>
    /// Vérifie si deux personnages adjacents forment un Combo valide :
    /// le personnage de gauche doit avoir une gemme d'ouverture de la même couleur (Orange ou Violet)
    /// que la gemme de fermeture du personnage de droite.
    /// </summary>
    public static bool CanConnect(DuelCard? leftCard, DuelCard? rightCard)
    {
        if (leftCard == null || rightCard == null)
        {
            return false;
        }

        return (leftCard.HasOpeningOrangeGem && rightCard.HasClosingOrangeGem)
            || (leftCard.HasOpeningVioletGem && rightCard.HasClosingVioletGem);
    }

    /// <summary>
    /// Vérifie si un combo est actif pour le personnage à l'index donné dans la liste spécifiée.
    /// Un combo est actif si le personnage a une gemme de fermeture compatible en couleur
    /// avec une gemme d'ouverture du personnage immédiatement à sa gauche (index - 1).
    /// </summary>
    public static bool IsComboActive(IReadOnlyList<DuelCard> chars, int characterIndex)
    {
        if (chars == null || characterIndex <= 0 || characterIndex >= chars.Count)
        {
            return false;
        }

        return CanConnect(chars[characterIndex - 1], chars[characterIndex]);
    }
}
