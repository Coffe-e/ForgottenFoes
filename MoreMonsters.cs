using BepInEx;
using BepInEx.Configuration;
using MoreMonsters.Utils;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TILER2;
using UnityEngine;
using static TILER2.MiscUtil;
using Path = System.IO.Path;


namespace MoreMonsters
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
        public const string ModName = "MoreMonsters";
        public const string ModGuid = "com.MoreMonsters";

        private static ConfigFile cfgFile;
        internal static FilingDictionary<MonsterBoilerplate> masterMonsterList = new FilingDictionary<MonsterBoilerplate>();
        internal static BepInEx.Logging.ManualLogSource _logger;

        private void Awake()
        {
            _logger = Logger;

            //Assets.PopulateAssets(); This does not need to be used until we actually make an assetbundle

            _logger.LogDebug("Adding Monsters...");
            masterMonsterList = T2Module.InitAll<MonsterBoilerplate>(new T2Module.ModInfo
            {
                displayName = "More Monsters",
                longIdentifier = "MoreMonsters",
                shortIdentifier = "ME",
                mainConfigFile = cfgFile
            });
            T2Module.SetupAll_PluginAwake(masterMonsterList);
            _logger.LogDebug("Adding Monsters Complete.");


        }

    }

    public static class Assets
    {
        public static AssetBundle mainAssetBundle = null;
        public static AssetBundleResourcesProvider Provider;

        public static IResourceProvider PopulateAssets()
        {
            using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MoreMonsters.moreenemies_assets"))
            {
                mainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                Provider = new AssetBundleResourcesProvider("@MoreMonsters", mainAssetBundle);
            }
            return Provider;
        }

    }
}

