using HarmonyLib;
using System;
using Game;
using Game.Systems;
using Game.Constants;
using UnityEngine;
using KL.Utils;
using static Misc;

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

[HarmonyPatch]
class Misc
{
    public static void Message(string msg)
    {
        A.S.Sys.Log.AddLine($"[AutoScan] {msg}", canRepeat: true);
    }

    [HarmonyPatch(typeof(ScanUI), "CheckCanScanDeep")]
    [HarmonyReversePatch]
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
            Message("Not starting a new Deep Scan as this one seems to have failed.");
            return;
        }
        // If another kind of scan failed, switch to deep scan.
        else if (__instance.FoundSO == null)
        {
            Message($"Scan for {__instance.ScanSubject} failed - switching to deep scanning.");
            newMode = ScanMode.Deep;
        }
        else
        {
            // Is that the right way to get the LogSys instance?..
            Message("Repeating scan.");
            newMode = __instance.Mode;
            newMatTarget = __instance.MatTarget;
        }

        StartScan(__instance, newMode, newMatTarget);
    }

    public static void StartScan(ScanSys instance, ScanMode newMode, MatType newMatTarget)
    {
        if (newMode == ScanMode.Deep && !CheckCanScanDeep(instance.UI))
        {
            Message("Nothing else to deep scan.");
            return;
        }
        instance.SetMode(newMode, newMatTarget);
    }
}

[HarmonyPatch(typeof(ShipNavSys))]
class ShipNavSysPatch
{
    [HarmonyPatch(nameof(ShipNavSys.ExitHyperspace))]
    [HarmonyPostfix]
    static void ExitHyperspacePatch()
    {
        if (A.IsInHyperspace)
        {
            // exiting hyperspace must have failed
            return;
        }
        ScanSys s = A.S.Sys.Scan;
        if (s.Mode != ScanMode.None)
        {
            return;
        }
        Message("Exited hyperspace with no scan - starting a new Deep Scan.");
        ScanSysPatch.StartScan(s, ScanMode.Deep, null);
    }
}