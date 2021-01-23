using BepInEx;
using EntityStates;
using KinematicCharacterController;
using MoreMonsters.Utils;
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

namespace MoreMonsters
{
    public class ImpSorcerer : MonsterBoilerplate
    {

        public override string displayName => "Imp Sorcerer";
        public override string modelName => "mdlImpSorcerer";
        public override GameObject bodyPrefab => CreatePrefab();
        public override HullClassification hullSize => HullClassification.Human;
        public override MapNodeGroup.GraphType graphType => MapNodeGroup.GraphType.Ground;
        public override int creditCost => 2;
        public override bool occupyPosition => true;
        public override int selectionWeight => 1;
        public override DirectorCore.MonsterSpawnDistance spawnDistance => DirectorCore.MonsterSpawnDistance.Standard;
        public override bool ambush => true;
        public override int minimumStage => 5;
        public override DirectorAPI.Stage[] homeStages => new DirectorAPI.Stage[] { DirectorAPI.Stage.ScorchedAcres, DirectorAPI.Stage.RallypointDelta, DirectorAPI.Stage.AbyssalDepths };
        public override DirectorAPI.MonsterCategory monsterCategory => DirectorAPI.MonsterCategory.BasicMonsters;
        protected override string GetLoreString(string langID = null) => "lol";


        public override GameObject CreatePrefab()
        {
            var prefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/ImpBody"), "ImpSorcererBody");
            //GameObject model = CreateModel(bodyPrefab);
            //All the other shit that needs to go here

            return prefab;
        }

        public override void RegisterStates()
        {
        }

    }
}
