using System;
using HarmonyLib;

namespace WankulCrazyPlugin.patch
{
    /// <summary>
    /// Protège LoadingScreen.CloseScreen et ScreenRatioScaler.Init contre les NRE
    /// lorsque le jeu démarre ou se ferme avant que les singletons ne soient prêts.
    /// </summary>
    public static class SceneLifecyclePatches
    {
        public static bool CloseScreenPrefix()
        {
            try
            {
                if (CSingleton<LoadingScreen>.Instance == null)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool ScreenRatioScalerInitPrefix(ScreenRatioScaler __instance)
        {
            if (__instance == null)
            {
                return false;
            }

            try
            {
                if (CSingleton<InputManager>.Instance == null)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
