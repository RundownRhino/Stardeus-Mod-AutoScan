using HarmonyLib;
using Game.Systems;
using UnityEngine;
using UnityAPIBridge;

public sealed class AutoScanPatcher
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        UnityLogger.LogInfo("AutoScan is registering the Harmony patch.");
        var harmony = new Harmony("com.AutoScanPatcher.patch");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(ScanSys))]
class ScanSysPatch
{
    [HarmonyPatch("OnScanDeep")]
    [HarmonyPostfix]
    static void OnScanDeepPatch(ScanSys __instance)
    {
        if (__instance.FoundSO != null)
        {
            // Is that the right way to get the LogSys instance?..
            __instance.S.Sys.Log.AddLine("[AutoScan] Starting new Deep Scan.");
            // We assume that if the scan finished, we can probably start a new one - so won't do CheckHyperspace like SetScanMode does.
            __instance.SetMode(ScanMode.Deep, null);
        }
        else
        {
            // Otherwise the scan was saturated.
            __instance.S.Sys.Log.AddLine("[AutoScan] Not starting a new Deep Scan as this one seems to have failed.");
        }

    }
}