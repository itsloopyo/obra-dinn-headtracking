using HarmonyLib;
using UnityEngine.SceneManagement;

namespace HeadTracking.Patches
{
    /// <summary>
    /// Harmony patch for scene loading events.
    /// </summary>
    [HarmonyPatch]
    public static class SceneManagerPatches
    {
        [HarmonyPatch(typeof(SceneManager), "Internal_SceneLoaded")]
        [HarmonyPostfix]
        public static void OnInternalSceneLoaded()
        {
            CameraPatches.NotifySceneLoaded();
        }
    }
}
