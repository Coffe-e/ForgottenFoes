using BepInEx;
using EntityStates;
using KinematicCharacterController;
using MoreEnemies.Utils;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static TILER2.MiscUtil;
using TILER2;
using RoR2.Navigation;

namespace MoreEnemies
{
    public class ImpSorcerer : EnemyBoilerplate
    {

        public override string displayName => "Imp Sorcerer";
        public override GameObject bodyPrefab => PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/ImpBody"), "ImpSorcererBody");
        public override HullClassification hullSize => HullClassification.Human;
        public override MapNodeGroup.GraphType graphType => MapNodeGroup.GraphType.Ground;
        public override int creditCost => 1;
        public override bool occupyPosition => true;
        public override int selectionWeight => 2;
        public override DirectorCore.MonsterSpawnDistance spawnDistance => DirectorCore.MonsterSpawnDistance.Standard;
        public override bool ambush => true;
        public override int minimumStage => 2;
        public override DirectorAPI.MonsterCategory monsterCategory => DirectorAPI.MonsterCategory.BasicMonsters;



        public override void CreatePrefab()
        {
            //CreateModel();
            //All the other shit that needs to go here
        }

        public override void RegisterStates()
        {
        }

        protected override string GetLoreString(string langID = null)
        {
            throw new NotImplementedException();
        }
    }
}
