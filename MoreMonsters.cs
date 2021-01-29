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

/* TO DO:
 * Add Logbook support
 * Flesh out config options for the boilerplate
 * Fuck with entity states
 */
namespace MoreMonsters
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency(TILER2Plugin.ModGuid, "3.0.4")]
    [BepInDependency("com.funkfrog_sipondo.sharesuite", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(ResourcesAPI), nameof(PlayerAPI), nameof(PrefabAPI), nameof(BuffAPI), nameof(LoadoutAPI), nameof(DirectorAPI))]

    public class MoreMonsters : BaseUnityPlugin
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

            cfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, ModGuid + ".cfg"), true);

            _logger.LogDebug("Adding Monsters...");
            masterMonsterList = T2Module.InitAll<MonsterBoilerplate>(new T2Module.ModInfo
            {
                displayName = "MoreMonsters",
                longIdentifier = "MoreMonsters",
                shortIdentifier = "ME",
                mainConfigFile = cfgFile
            });
            T2Module.SetupAll_PluginAwake(masterMonsterList);
            _logger.LogDebug("Adding Monsters Complete.");
        }

        private void Start()
        {
            T2Module.SetupAll_PluginStart(masterMonsterList);
        }


    }

    /*public static class Assets
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

    }*/
}

