using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Path = System.IO.Path;
using System.IO;


namespace MoreEnemies
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency("com.funkfrog_sipondo.sharesuite", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(ResourcesAPI), nameof(PlayerAPI), nameof(PrefabAPI), nameof(BuffAPI), nameof(LoadoutAPI))]
    public class KevinsAdditionsPlugin : BaseUnityPlugin
    {
        public const string ModVer =
#if DEBUG
                "0." +
#endif
            "1.0.0";
        public const string ModName = "MoreEnemies";
        public const string ModGuid = "com.MoreEnemies";

        private static ConfigFile cfgFile;
        internal static BepInEx.Logging.ManualLogSource _logger;

        private void Awake()
        {

        }

    }
}

