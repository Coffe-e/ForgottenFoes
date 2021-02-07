using BepInEx;
using EntityStates;
using EntityStates.ImpMonster;
using KinematicCharacterController;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreMonsters;
using MoreMonsters.States.ImpSorcerer;
using MoreMonsters.Utils;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using RoR2.Projectile;
using RoR2.Skills;
using RoR2.WwiseUtils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using TILER2;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using R2API.Networking;
using static TILER2.MiscUtil;
using System.Collections;
using RoR2.Orbs;
using Rewired.ComponentControls.Effects;

namespace MoreMonsters
{
    public class BellBoss : MonsterBoilerplate
    {
        public override string displayName => "Brass Tolling Bell";
        public override string nameTag => "BellBoss";
        public override Type[] skillStates => new Type[]
        {
        };
        public override HullClassification hullSize => HullClassification.BeetleQueen;
        public override MapNodeGroup.GraphType graphType => MapNodeGroup.GraphType.Air;
        public override int creditCost => 800;
        public override bool occupyPosition => true;
        public override int selectionWeight => 1;
        public override DirectorCore.MonsterSpawnDistance spawnDistance => DirectorCore.MonsterSpawnDistance.Standard;
        public override bool ambush => false;
        public override int minimumStage => 4;
        public override DirectorAPI.Stage[] homeStages => new DirectorAPI.Stage[] { };
        public override DirectorAPI.MonsterCategory monsterCategory => DirectorAPI.MonsterCategory.Champions;
        public override bool canBeBoss => true;
        protected override string GetLoreString(string langID = null) => "lol";

        protected private SerializableEntityStateType initialStateType;
        SkillDef skillDefPrimary;
        SkillDef skillDefSecondary;
        SkillDef skillDefUtility;
        SkillDef skillDefSpecial;
        GameObject projectilePrefab;
        GameObject spikePrefab;
        GameObject eyes;

        public override void Install()
        {
            base.Install();
        }
        public override void Uninstall()
        {
            base.Uninstall();
        }

        public override void CreatePrefab()
        {
        }
        public override void CreateMaster()
        {
        }
        public override void PrimarySetup()
        {
        }
        public override void SecondarySetup()
        {
        }
        public override void UtilitySetup()
        {
        }
        public override void SpecialSetup()
        {
        }
    }
}