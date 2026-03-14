using System;
using System.IO;
using System.Reflection;
using BepInEx;
using liquidclient.Classes;
using UnityEngine;
using TMPro;

namespace liquidclient
{
    [System.ComponentModel.Description(PluginInfo.Description)]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class HarmonyPatches : BaseUnityPlugin
    {
        private void Awake() =>
            GorillaTagger.OnPlayerSpawned(OnPlayerSpawned);

        public void OnPlayerSpawned() => Patches.PatchHandler.PatchAll();
        
        void Start() => CXS.CXS.LoadCXS();
    }
}
