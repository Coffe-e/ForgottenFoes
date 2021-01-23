using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static TILER2.MiscUtil;
using TILER2;
using RoR2;
using RoR2.Navigation;
using R2API;

namespace MoreEnemies.Utils
{
    /*public abstract class EnemyBoilerPlate<T> : EnemyBoilerplate where T : EnemyBoilerplate<T>
    {
        public static T instance { get; private set; }

        public EnemyBoilerPlate()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBoilerplate/Item was instantiated twice");
            instance = this as T;
        }
    }*/

    public abstract class EnemyBoilerplate : T2Module
    {
        public string nameToken { get; private protected set; }
        public string loreToken { get; private protected set; }

        /// <summary>Used by TILER2 to request language token value updates (object name). If langID is null, the request is for the invariant token</summary>
        protected string GetNameString(string langID = null)
        {
            return displayName;
        }

        /// <summary>Used by TILER2 to request language token value updates (lore text, where applicable). If langID is null, the request is for the invariant token</summary>
        protected abstract string GetLoreString(string langID = null);


        ///<summary>The object's display name in the mod's default language. Will be used in config files; should also be used in generic language tokens</summary>
        public abstract string displayName { get; }

        /// <summary>Stores the body prefab that will be used for the enemy</summary>
        public abstract GameObject bodyPrefab { get; }

        ///<summary>How big the enemy is</summary>
        public abstract HullClassification hullSize { get; }

        ///<summary>How does the enemy move around</summary>
        public abstract MapNodeGroup.GraphType graphType { get; }

        ///<summary>How many Director Credits does it cost to spawn this enemy in</summary>
        public abstract int creditCost { get; }

        ///<summary>Should the enemy occupy its position</summary>
        public abstract bool occupyPosition { get; }

        ///<summary>How likely is this enemy to spawn</summary>
        public abstract int selectionWeight { get; }

        ///<summary>How far away should this enemy spawn</summary>
        public abstract DirectorCore.MonsterSpawnDistance spawnDistance { get; }

        ///<summary>Can the enemy ambush the player</summary>
        public abstract bool ambush { get; }

        ///<summary>the index of the stage the enemy should begin spawning at</summary>
        public abstract int minimumStage { get; }

        ///<summary>What Category of monster the enemy is</summary>
        public abstract DirectorAPI.MonsterCategory monsterCategory { get; }

        public SpawnCard spawnCard { get; private set; }
        public DirectorCard directorCard { get; private set; }





        /// <summary>Destroys the Old model's stuff and replaces it with the one used in its place</summary>
        public virtual GameObject CreateModel(GameObject main)
        {
            UnityEngine.Object.Destroy(main.transform.Find("ModelBase").gameObject);
            UnityEngine.Object.Destroy(main.transform.Find("CameraPivot").gameObject);
            UnityEngine.Object.Destroy(main.transform.Find("AimOrigin").gameObject);

            GameObject model = Assets.mainAssetBundle.LoadAsset<GameObject>("mdlExampleSurvivor"); //Change this to the enemy's model

            return model;
        }

        /// <summary>Creates the prefab for the enemy.</summary>
        public abstract void CreatePrefab();

        /// <summary>Registers entity states, like skills.</summary>
        public abstract void RegisterStates();


        //public RoR2.UI.LogBook.Entry logbookEntry { get; internal set; }

        public override void SetupConfig()
        {
            base.SetupConfig();

            ConfigEntryChanged += (sender, args) => {
                if (args.target.boundProperty.Name == nameof(enabled))
                {
                    if (args.oldValue != args.newValue)
                    {
                        if ((bool)args.newValue == true)
                        {
                            if (Run.instance != null && Run.instance.enabled) Chat.AddMessage($"<color=#{ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Blood)}>{displayName}</color> has been <color=#aaffaa>ENABLED</color>. It will now drop, and existing copies will start working again.");
                        }
                        else
                        {
                            if (Run.instance != null && Run.instance.enabled) Chat.AddMessage($"<color=#{ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Blood)}>{displayName}</color> has been <color=#ffaaaa>DISABLED</color>. It will no longer drop, and existing copies will stop working.");
                        }
                    }
                }
            };
        }

        public override void InstallLanguage()
        {
            genericLanguageTokens[nameToken] = GetNameString();
            genericLanguageTokens[loreToken] = GetLoreString();
            base.InstallLanguage();
        }

        public override void SetupAttributes()
        {
            base.SetupAttributes();
            nameToken = $"{modInfo.longIdentifier}_{name.ToUpper()}_NAME";
            loreToken = $"{modInfo.longIdentifier}_{name.ToUpper()}_LORE";

            spawnCard = new SpawnCard
            {
                prefab = bodyPrefab,
                sendOverNetwork = true,
                hullSize = hullSize,
                nodeGraphType = graphType,
                requiredFlags = NodeFlags.None,
                forbiddenFlags = NodeFlags.None,
                directorCreditCost = creditCost,
                occupyPosition = occupyPosition
            };
            directorCard = new DirectorCard
            {
                spawnCard = spawnCard,
                selectionWeight = selectionWeight,
                spawnDistance = spawnDistance,
                allowAmbushSpawn = ambush,
                preventOverhead = true,
                requiredUnlockable = null,
                forbiddenUnlockable = null
            };
            DirectorAPI.Helpers.AddNewMonster(directorCard, monsterCategory);
        }


    }
}
