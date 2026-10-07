#nullable enable
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

public static class ComboEvaluator
{
    /// <summary>
    /// Vérifie si un combo est actif pour le personnage à l'index donné dans la liste spécifiée.
    /// Un combo est actif si le personnage a une gemme de fermeture (HasClosingGem)
    /// et que le personnage immédiatement à sa gauche (index - 1) a une gemme d'ouverture (HasOpeningGem).
    /// </summary>
    public static bool IsComboActive(IReadOnlyList<DuelCard> chars, int characterIndex)
    {
        if (chars == null || characterIndex <= 0 || characterIndex >= chars.Count)
        {
            return false;
        }

        var currentCard = chars[characterIndex];
        var leftCard = chars[characterIndex - 1];

        return currentCard.HasClosingGem && leftCard.HasOpeningGem;
    }
}
