using HarmonyLib;
using System;
using Game.Systems;
using Game.Constants;
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

class Misc
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ScanUI), "CheckCanScanDeep")]
    public static bool CheckCanScanDeep(ScanUI instance)
    {
        throw new NotSupportedException("Stub");
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
        ScanMode newMode = ScanMode.Deep;
        MatType newMatTarget = null;
        // If a deep scan failed, stop scanning
        if (__instance.Mode == ScanMode.Deep && __instance.FoundSO == null)
        {
            __instance.S.Sys.Log.AddLine("[AutoScan] Not starting a new Deep Scan as this one seems to have failed.");
            return;
        }
        // If another kind of scan failed, switch to deep scan.
        else if (__instance.FoundSO == null)
        {
            __instance.S.Sys.Log.AddLine($"[AutoScan] Scan for {__instance.ScanSubject} failed - switching to deep scanning.");
            newMode = ScanMode.Deep;
        }
        else
        {
            // Is that the right way to get the LogSys instance?..
            __instance.S.Sys.Log.AddLine("[AutoScan] Repeating scan.");
            newMode = __instance.Mode;
            newMatTarget = __instance.MatTarget;
        }

        if (newMode == ScanMode.Deep && !Misc.CheckCanScanDeep(__instance.UI))
        {
            __instance.S.Sys.Log.AddLine("[AutoScan] Nothing else to deep scan.");
            return;
        }
        __instance.SetMode(newMode, newMatTarget);
    }
}