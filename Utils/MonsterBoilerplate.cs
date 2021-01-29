﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static TILER2.MiscUtil;
using TILER2;
using RoR2;
using RoR2.Navigation;
using R2API;

namespace MoreMonsters.Utils
{
    /*public abstract class MonsterBoilerPlate<T> : MonsterBoilerplate where T : MonsterBoilerplate<T>
    {
        public static T instance { get; private set; }

        public MonsterBoilerPlate()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBoilerplate/Item was instantiated twice");
            instance = this as T;
        }
    }*/

    public abstract class MonsterBoilerplate : T2Module
    {
        public string nameToken { get; private protected set; }
        public string loreToken { get; private protected set; } 
        public SpawnCard spawnCard { get; private set; }
        public DirectorCard directorCard { get; private set; }

        /// <summary>Used by TILER2 to request language token value updates (object name). If langID is null, the request is for the invariant token.</summary>
        protected string GetNameString(string langID = null)
        {
            return displayName;
        }

        /// <summary>Used by TILER2 to request language token value updates (lore text, where applicable). If langID is null, the request is for the invariant token.</summary>
        protected abstract string GetLoreString(string langID = null);

        ///<summary>The object's display name in the mod's default language. Will be used in config files; should also be used in generic language tokens.</summary>
        public abstract string displayName { get; }

        ///<summary>The standard named used in files without any prefixes.</summary>
        public abstract string nameTag { get; }

        /// <summary>Stores the body prefab that will be used for the monster. Should be declared with PrefabAPI.InstantiateClone().</summary>
        public abstract GameObject bodyPrefab { get; }

        ///<summary>How big the monster is.</summary>
        public abstract HullClassification hullSize { get; }

        ///<summary>How the monster moves around.</summary>
        public abstract MapNodeGroup.GraphType graphType { get; }

        ///<summary>How many Director Credits it costs to spawn this monster in.</summary>
        public abstract int creditCost { get; }

        ///<summary>Whether the monster should occupy its position.</summary>
        public abstract bool occupyPosition { get; }

        ///<summary>How likely the monster is to spawn.</summary>
        public abstract int selectionWeight { get; }

        ///<summary>How far away should this monster spawn.</summary>
        public abstract DirectorCore.MonsterSpawnDistance spawnDistance { get; }

        ///<summary>Can the monster ambush the player.</summary>
        public abstract bool ambush { get; }

        ///<summary>The index of the stage the monster should begin spawning at. This should usually be set to 5.</summary>
        public abstract int minimumStage { get; }

        ///<summary>What stages should the monster spawn on normally. Create empty array if the monster should spawn on all stages.</summary>
        public abstract DirectorAPI.Stage[] homeStages { get; }

        ///<summary>What category of monster the monster is.</summary>
        public abstract DirectorAPI.MonsterCategory monsterCategory { get; }

        ///<summary>Whether the monster can spawn as a boss (e.g. Elder Lemurians can spawn as bosses).</summary>
        public abstract bool canBeBoss { get; }







        /// <summary>Creates the prefab for the monster.</summary>
        public abstract void CreatePrefab();

        /// <summary>Replaces the old model with a new one.</summary>
        public virtual GameObject CreateModel(GameObject main)
        {
            UnityEngine.Object.Destroy(main.transform.Find("ModelBase").gameObject);
            UnityEngine.Object.Destroy(main.transform.Find("CameraPivot").gameObject);
            UnityEngine.Object.Destroy(main.transform.Find("AimOrigin").gameObject);

            GameObject model = null; //Assets.mainAssetBundle.LoadAsset<GameObject>("mdl" + nameTag); //Change this to the monster's model
            return model;
        }

        /// <summary>Adds skills to a bodyPrefab. Should normally be called by CreatePrefab()</summary>
        public abstract void SkillSetup();

        /// <summary>Registers entity states, like skills.</summary>
        public abstract void RegisterStates();

        //public RoR2.UI.LogBook.Entry logbookEntry { get; internal set; }

        public override void SetupConfig()
        {
            base.SetupConfig();

            ConfigEntryChanged += (sender, args) =>
            {
                if (args.target.boundProperty.Name == nameof(enabled))
                {
                    if (args.oldValue != args.newValue)
                    {
                        if ((bool)args.newValue == true)
                        {
                            if (Run.instance != null && Run.instance.enabled) Chat.AddMessage($"<color=#{ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Blood)}>{displayName}</color> has been <color=#aaffaa>ENABLED</color>. It will now spawn, and existing copies will start working again.");
                        }
                        else
                        {
                            if (Run.instance != null && Run.instance.enabled) Chat.AddMessage($"<color=#{ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Blood)}>{displayName}</color> has been <color=#ffaaaa>DISABLED</color>. It will no longer spawn, and existing copies will stop working.");
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

            CreatePrefab();
            SkillSetup();
            RegisterStates();
            //adds the bodyPrefab to the entry list
            BodyCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(bodyPrefab);
            };
            RegisterStates();

            //Makes a SpawnCard for the enemy
            CharacterSpawnCard spawnCard1 = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            spawnCard1.name = "csc" + nameTag;
            spawnCard1.prefab = bodyPrefab;
            spawnCard1.sendOverNetwork = true;
            spawnCard1.hullSize = hullSize;
            spawnCard1.nodeGraphType = graphType;
            spawnCard1.requiredFlags = NodeFlags.None;
            spawnCard1.forbiddenFlags = NodeFlags.None;
            spawnCard1.directorCreditCost = creditCost;
            spawnCard1.occupyPosition = true;
            spawnCard1.loadout = new SerializableLoadout();
            spawnCard1.noElites = false;
            spawnCard1.forbiddenAsBoss = canBeBoss;
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

            //If the monster has home stages, add them to those stages only. Else, add it to all stages.
            if (homeStages.Length > 0)
                foreach (DirectorAPI.Stage stage in homeStages)
                    DirectorAPI.Helpers.AddNewMonsterToStage(directorCard, monsterCategory, stage);
            else
                DirectorAPI.Helpers.AddNewMonster(directorCard, monsterCategory);
        }
    }
}
