using System;
using HarmonyLib;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Protège PlayCardSetUI.Update contre les NullReferenceException du jeu de base.
    /// Quand PlayCardSetUI est actif sur une table ou dans la scène mais que m_PlayCardSet
    /// ou sa référence m_PlayTableGame n'est pas encore initialisée ou est détruite,
    /// Update() déclenche un NRE à chaque frame.
    /// </summary>
    public static class PlayCardSetUIPatch
    {
        public static bool Prefix(PlayCardSetUI __instance, PlayCardSet ___m_PlayCardSet)
        {
            if (__instance == null)
            {
                return false;
            }

            if (___m_PlayCardSet == null || ___m_PlayCardSet.m_PlayTableGame == null)
            {
                return false;
            }

            return true;
        }

        public static bool LateUpdatePrefix(PlayCardSetUI __instance, PlayCardSet ___m_PlayCardSet)
        {
            if (__instance == null)
            {
                return false;
            }

            if (___m_PlayCardSet == null || ___m_PlayCardSet.m_PlayTableGame == null)
            {
                return false;
            }

            return true;
        }
    }
}
