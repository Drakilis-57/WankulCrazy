using System.Collections.Generic;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Accesseurs typés pour les champs privés de CardOpeningSequence.
    /// Remplace les Plugin.GetPProperty / SetPProperty dispersés dans CardOpening.
    /// Aucune logique ici — uniquement des wrappers de lecture / écriture.
    /// </summary>
    internal static class CardOpeningHelpers
    {
        // ── Flags ──────────────────────────────────────────────────────────────

        public static bool GetIsScreenActive(CardOpeningSequence s)
            => (bool)Plugin.GetPProperty(s, "m_IsScreenActive");
        public static void SetIsScreenActive(CardOpeningSequence s, bool v)
            => Plugin.SetPProperty(s, "m_IsScreenActive", v);

        public static bool GetIsReadyingToOpen(CardOpeningSequence s)
            => (bool)Plugin.GetPProperty(s, "m_IsReadyingToOpen");
        public static void SetIsReadyingToOpen(CardOpeningSequence s, bool v)
            => Plugin.SetPProperty(s, "m_IsReadyingToOpen", v);

        public static bool GetIsReadyToOpen(CardOpeningSequence s)
            => (bool)Plugin.GetPProperty(s, "m_IsReadyToOpen");
        public static void SetIsReadyToOpen(CardOpeningSequence s, bool v)
            => Plugin.SetPProperty(s, "m_IsReadyToOpen", v);

        public static bool GetIsCanceling(CardOpeningSequence s)
            => (bool)Plugin.GetPProperty(s, "m_IsCanceling");
        public static void SetIsCanceling(CardOpeningSequence s, bool v)
            => Plugin.SetPProperty(s, "m_IsCanceling", v);

        public static bool GetIsAutoFire(CardOpeningSequence s)
            => (bool)Plugin.GetPProperty(s, "m_IsAutoFire");
        public static void SetIsAutoFire(CardOpeningSequence s, bool v)
            => Plugin.SetPProperty(s, "m_IsAutoFire", v);

        public static bool GetIsAutoFireKeydown(CardOpeningSequence s)
            => (bool)Plugin.GetPProperty(s, "m_IsAutoFireKeydown");
        public static void SetIsAutoFireKeydown(CardOpeningSequence s, bool v)
            => Plugin.SetPProperty(s, "m_IsAutoFireKeydown", v);

        public static bool GetIsGetHighValueCard(CardOpeningSequence s)
            => (bool)Plugin.GetPProperty(s, "m_IsGetHighValueCard");
        public static void SetIsGetHighValueCard(CardOpeningSequence s, bool v)
            => Plugin.SetPProperty(s, "m_IsGetHighValueCard", v);

        public static bool GetHasFoilCard(CardOpeningSequence s)
            => (bool)Plugin.GetPProperty(s, "m_HasFoilCard");

        // ── Timers / Sliders ───────────────────────────────────────────────────

        public static float GetSlider(CardOpeningSequence s)
            => (float)Plugin.GetPProperty(s, "m_Slider");
        public static void SetSlider(CardOpeningSequence s, float v)
            => Plugin.SetPProperty(s, "m_Slider", v);

        public static float GetStateTimer(CardOpeningSequence s)
            => (float)Plugin.GetPProperty(s, "m_StateTimer");
        public static void SetStateTimer(CardOpeningSequence s, float v)
            => Plugin.SetPProperty(s, "m_StateTimer", v);

        public static float GetAutoFireTimer(CardOpeningSequence s)
            => (float)Plugin.GetPProperty(s, "m_AutoFireTimer");
        public static void SetAutoFireTimer(CardOpeningSequence s, float v)
            => Plugin.SetPProperty(s, "m_AutoFireTimer", v);

        public static float GetLerpPosTimer(CardOpeningSequence s)
            => (float)Plugin.GetPProperty(s, "m_LerpPosTimer");
        public static void SetLerpPosTimer(CardOpeningSequence s, float v)
            => Plugin.SetPProperty(s, "m_LerpPosTimer", v);

        public static float GetLerpPosSpeed(CardOpeningSequence s)
            => (float)Plugin.GetPProperty(s, "m_LerpPosSpeed");

        public static float GetMultiplierStateTimer(CardOpeningSequence s)
            => (float)Plugin.GetPProperty(s, "m_MultiplierStateTimer");

        // ── Indexes ────────────────────────────────────────────────────────────

        public static int GetCurrentOpenedCardIndex(CardOpeningSequence s)
            => (int)Plugin.GetPProperty(s, "m_CurrentOpenedCardIndex");
        public static void SetCurrentOpenedCardIndex(CardOpeningSequence s, int v)
            => Plugin.SetPProperty(s, "m_CurrentOpenedCardIndex", v);

        public static int GetTempIndex(CardOpeningSequence s)
            => (int)Plugin.GetPProperty(s, "m_TempIndex");
        public static void SetTempIndex(CardOpeningSequence s, int v)
            => Plugin.SetPProperty(s, "m_TempIndex", v);

        public static int GetTotalExpGained(CardOpeningSequence s)
            => (int)Plugin.GetPProperty(s, "m_TotalExpGained");
        public static void SetTotalExpGained(CardOpeningSequence s, int v)
            => Plugin.SetPProperty(s, "m_TotalExpGained", v);

        // ── Values ─────────────────────────────────────────────────────────────

        public static float GetTotalCardValue(CardOpeningSequence s)
            => (float)Plugin.GetPProperty(s, "m_TotalCardValue");
        public static void SetTotalCardValue(CardOpeningSequence s, float v)
            => Plugin.SetPProperty(s, "m_TotalCardValue", v);

        public static float GetHighValueCardThreshold(CardOpeningSequence s)
            => (float)Plugin.GetPProperty(s, "m_HighValueCardThreshold");

        // ── Lists ──────────────────────────────────────────────────────────────

        public static List<float> GetCardValueList(CardOpeningSequence s)
            => (List<float>)Plugin.GetPProperty(s, "m_CardValueList");

        public static List<bool> GetIsNewlList(CardOpeningSequence s)
            => (List<bool>)Plugin.GetPProperty(s, "m_IsNewlList");

        public static List<CardData> GetRolledCardDataList(CardOpeningSequence s)
            => (List<CardData>)Plugin.GetPProperty(s, "m_RolledCardDataList");

        // ── Item ───────────────────────────────────────────────────────────────

        public static Item GetCurrentItem(CardOpeningSequence s)
            => (Item)Plugin.GetPProperty(s, "m_CurrentItem");
        public static void SetCurrentItem(CardOpeningSequence s, Item v)
            => Plugin.SetPProperty(s, "m_CurrentItem", (object)v);
    }
}
