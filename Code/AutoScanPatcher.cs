using HarmonyLib;
using Game.Systems;
using UnityEngine;
using KL.Utils;

public sealed class AutoScanPatcher
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        D.Warn("AutoScan is being registered, applying the Harmony patches.");
        var harmony = new Harmony("com.AutoScanPatcher.patch");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(ScanSys))]
class ScanSysPatch
{
    [HarmonyPatch("OnScanDone")]
    [HarmonyPostfix]
    static void OnScanDonePatch(ScanSys __instance)
    {
        if (__instance.ScanProgress != 1f)
        {
            // then the scan isn't actually done
            D.Warn("AutoScan: OnScanDone called but ScanProgress is {0}, ignoring.", __instance.ScanProgress);
            return;
        }
        // We start a new deep scan if another kind finished (to not waste time) OR if the a deep scan finishes successfully
        if (__instance.Mode != ScanMode.Deep || __instance.FoundSO != null)
        {
            // Is that the right way to get the LogSys instance?..
            __instance.S.Sys.Log.AddLine("[AutoScan] Starting new Deep Scan.");
            // We assume that if the scan finished, we can probably start a new one - so won't do CheckHyperspace like SetScanMode does.
            __instance.SetMode(ScanMode.Deep, null);
        }
        else
        {
            // Otherwise a deep scan just failed, so don't start a new one.
            __instance.S.Sys.Log.AddLine("[AutoScan] Not starting a new Deep Scan as this one seems to have failed.");
        }

    }
}