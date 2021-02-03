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
using static TILER2.MiscUtil;


/* To-Do List:
 * Implement model whenever that's possible
 * Do ghost version of projectiles for compatibility
 * Edit void spikes prefab (child one)
 * Add a custom component to the projectile prefab that does the special shit that it needs to
 */
namespace MoreMonsters
{
    public class ImpSorcerer : MonsterBoilerplate
    {

        public override string displayName => "Imp Sorcerer";
        public override string nameTag => "ImpSorcerer";
        public override HullClassification hullSize => HullClassification.Human;
        public override MapNodeGroup.GraphType graphType => MapNodeGroup.GraphType.Air;
        public override int creditCost => 27;
        public override bool occupyPosition => true;
        public override int selectionWeight => 1;
        public override DirectorCore.MonsterSpawnDistance spawnDistance => DirectorCore.MonsterSpawnDistance.Far;
        public override bool ambush => true;
        public override int minimumStage => 5;
        public override DirectorAPI.Stage[] homeStages => new DirectorAPI.Stage[] { DirectorAPI.Stage.ScorchedAcres, DirectorAPI.Stage.RallypointDelta, DirectorAPI.Stage.AbyssalDepths };
        public override DirectorAPI.MonsterCategory monsterCategory => DirectorAPI.MonsterCategory.BasicMonsters;
        public override bool canBeBoss => false;
        protected override string GetLoreString(string langID = null) => "lol";

        protected private SerializableEntityStateType initialStateType;
        GameObject projectilePrefab;
        GameObject spikePrefab;
        GameObject eyes;
        SkillDef skillDefPrimary;
        SkillDef skillDefSecondary;
        SkillDef skillDefUtility;
        SkillDef skillDefSpecial;

        public override void Install()
        {
            base.Install();

            IL.RoR2.Projectile.ProjectileImpactExplosion.FireChild += IL_ProjectileImpactChildFix;
        }
        public override void Uninstall()
        {
            base.Uninstall();

            IL.RoR2.Projectile.ProjectileImpactExplosion.FireChild -= IL_ProjectileImpactChildFix;
        }
        ///<summary>Because the FireChild stuff was designed in a completely **FUCKING STUPID** WAY AND TAKES THE Z OFFETS FOR BOTH Y AND Z OF THE VECTOR, this injects some code to make it take the y offset</summary>
        private void IL_ProjectileImpactChildFix(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<ProjectileImpactExplosion, Vector3, Vector3>>((self, vector) =>
            {
                if (self.gameObject)
                    return new Vector3(vector.x, -1f, vector.z);
                return vector;
            });
            c.Emit(OpCodes.Starg, 1);
        }



        public override void CreatePrefab()
        {
            bodyPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/ImpBody"), "ImpSorcererBody");

            var cb = bodyPrefab.GetComponent<CharacterBody>();
            cb.baseNameToken = "IMPSORCERER_BODY_NAME";
            cb.baseJumpCount = 0;

            #region Potential Garbage
            /*var fly = bodyPrefab.AddComponent<EntityStateMachine>();
            fly.customName = "Flight";
            fly.initialStateType = new SerializableEntityStateType(typeof(GenericCharacterMain));
            fly.mainStateType = new SerializableEntityStateType(typeof(GenericCharacterMain));*/


            /*var wispPrefab = Resources.Load<GameObject>("Prefabs/CharacterBodies/WispBody");

            //Grabs the EntityStateMachine That needs to be manipulated
            List<EntityStateMachine> bodyEntityStateMachines = new List<EntityStateMachine>();
            bodyPrefab.GetComponents<EntityStateMachine>(bodyEntityStateMachines);
            if (bodyEntityStateMachines[0].customName != "Body")
                bodyEntityStateMachines.RemoveAt(0);

            //Grabs the wisp EntityStateMachine the prefab one is being replaced with
            List<EntityStateMachine> wispEntityStateMachines = new List<EntityStateMachine>();
            wispPrefab.GetComponents<EntityStateMachine>(wispEntityStateMachines);
            if (wispEntityStateMachines[0].customName != "Body")
                wispEntityStateMachines.RemoveAt(0);

            bodyEntityStateMachines[0].mainStateType = wispEntityStateMachines[0].mainStateType;

            _______________________________________________________________________________*/

            /*bodyPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/BellBody"), "ImpSorcererBody");
            var impPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/ImpBody"), "ImpSorcererImpAssets");

            List<EntityStateMachine> bodyEntityStateMachines = new List<EntityStateMachine>();
            List<EntityStateMachine> impEntityStateMachines = new List<EntityStateMachine>();

            //Replaces the modelbase for the bell with the imp one and adds other imp gameobjects
            UnityEngine.Object.Destroy(bodyPrefab.transform.Find("mdlBell").gameObject);
            impPrefab.transform.Find("mdlImp").SetParent(bodyPrefab.transform.Find("ModelBase"));
            impPrefab.transform.Find("CameraPivot").SetParent(bodyPrefab.transform);
            impPrefab.transform.Find("AimOrigin").SetParent(bodyPrefab.transform);

            bodyPrefab.GetComponents<EntityStateMachine>(bodyEntityStateMachines);
            impPrefab.GetComponents<EntityStateMachine>(impEntityStateMachines);
            if(bodyEntityStateMachines[0].customName == "Body")
            {
                bodyEntityStateMachines[0].initialStateType = impEntityStateMachines[0].initialStateType;
                bodyEntityStateMachines[1] = impEntityStateMachines[1];
            }

            UnityEngine.Object.Destroy(bodyPrefab.GetComponent<RigidbodyDirection>());
            UnityEngine.Object.Destroy(bodyPrefab.GetComponent<RigidbodyMotor>());
            var cpt0 = bodyPrefab.AddComponent<CharacterDirection>();
            var cpt1 = bodyPrefab.AddComponent<CharacterMotor>();
            var cpt2 = bodyPrefab.AddComponent<KinematicCharacterMotor>();
            var cpt3 = bodyPrefab.GetComponent<CharacterBody>();
            var cpt4 = bodyPrefab.GetComponent<CharacterDeathBehavior>();

            cpt0 = impPrefab.GetComponent<CharacterDirection>();
            cpt1 = impPrefab.GetComponent<CharacterMotor>();
            cpt1.airControl = 1;
            cpt1.isFlying = true;
            cpt1.characterDirection = cpt0;k
            cpt1.characterDirection = cpt0;k
            cpt2 = impPrefab.GetComponent<KinematicCharacterMotor>();
            cpt3 = impPrefab.GetComponent<CharacterBody>();
            cpt1.body = cpt3;
            cpt3.baseNameToken = "IMPSORCERER_BODY_NAME";
            cpt4 = impPrefab.GetComponent<CharacterDeathBehavior>();*/


            /*//Removes the BellArmature and replaces it with the Imp Armature
            UnityEngine.Object.Destroy(bodyPrefab.transform.Find("BellArmature").gameObject);
            impPrefab.transform.Find("ImpArmature").SetParent(bodyPrefab.transform.Find("mdlBell"));
            //Removes the BellMesh and replaces it with the Imp Mesh
            UnityEngine.Object.Destroy(bodyPrefab.transform.Find("BellMesh").gameObject);
            impPrefab.transform.Find("ImpMesh").SetParent(bodyPrefab.transform.Find("mdlBell"));
            //removes the aim assist from the bell and adds the imp one
            UnityEngine.Object.Destroy(bodyPrefab.transform.Find("GameObject").gameObject);
            impPrefab.transform.Find("AimAssist").SetParent(bodyPrefab.transform.Find("mdlBell"));*/




            //var stateMachines = bodyPrefab.GetComponents<EntityStateMachine>();
            #endregion

            //GameObject model = CreateModel(bodyPrefab);
            AddEyes();
            AddProjectiles();
            //All the other shit that needs to go here

        }
        public override void SkillSetup()
        {
            foreach (GenericSkill obj in bodyPrefab.GetComponentsInChildren<GenericSkill>())
            {
                BaseUnityPlugin.DestroyImmediate(obj);
            }
            PrimarySetup();
            SecondarySetup();
            UtilitySetup();
            SpecialSetup();
        }
        public override void RegisterStates()
        {
            LoadoutAPI.AddSkill(typeof(Fly));
            LoadoutAPI.AddSkill(typeof(FireVoidCluster));
            LoadoutAPI.AddSkill(typeof(SorcererBlinkState));
            LoadoutAPI.AddSkill(typeof(EyeAttackState));
        }
        public override void CreateMaster()
        {
            masterPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterMasters/ImpMaster"), "ImpSorcererMaster");
            CharacterMaster cm = masterPrefab.GetComponent<CharacterMaster>();
            cm.bodyPrefab = bodyPrefab;
            var baseAI = masterPrefab.GetComponent<BaseAI>();
            baseAI.minDistanceFromEnemy = 50f;
            baseAI.stateMachine = masterPrefab.GetComponent<EntityStateMachine>();
            baseAI.enemyAttentionDuration = 7f;

            Component[] toDelete = masterPrefab.GetComponents<AISkillDriver>();
            foreach (AISkillDriver asd in toDelete)
            {
                switch (asd.customName)
                {
                    case "BlinkBecauseClose":
                        asd.customName = "BlinkBecauseTooClose";
                        asd.requiredSkill = skillDefUtility;
                        asd.minDistance = 0f;
                        asd.maxDistance = 25f;
                        asd.movementType = AISkillDriver.MovementType.FleeMoveTarget;
                        break;
                    case "StrafeBecausePrimaryIsntReady":
                        asd.customName = "FleeBecauseCantAttack";
                        asd.maxDistance = 30f;
                        asd.movementType = AISkillDriver.MovementType.FleeMoveTarget;
                        break;
                    case "LeaveNodeGraph":
                        asd.movementType = AISkillDriver.MovementType.StrafeMovetarget;
                        break;
                    case "PathToTarget":
                        asd.customName = "CloseDistance";
                        asd.minDistance = 80f;
                        break;
                    default:
                        UnityEngine.Object.Destroy(asd);
                        break;
                }
            }

            #region VoidCluster
            AISkillDriver voidCluster = masterPrefab.AddComponent<AISkillDriver>();
            voidCluster.skillSlot = SkillSlot.Secondary;
            voidCluster.requiredSkill = skillDefSecondary;
            voidCluster.requireSkillReady = false;
            voidCluster.requireEquipmentReady = false;
            voidCluster.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            voidCluster.minDistance = 35f;
            voidCluster.maxDistance = float.PositiveInfinity;
            voidCluster.selectionRequiresTargetLoS = true;
            voidCluster.activationRequiresTargetLoS = true;
            voidCluster.activationRequiresAimConfirmation = true;
            voidCluster.movementType = AISkillDriver.MovementType.StrafeMovetarget;
            voidCluster.aimType = AISkillDriver.AimType.None;
            voidCluster.ignoreNodeGraph = false;
            voidCluster.noRepeat = false;
            voidCluster.shouldSprint = false;
            voidCluster.shouldFireEquipment = false;
            voidCluster.shouldTapButton = false;
            voidCluster.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            #endregion
            #region Fly
            AISkillDriver fly = masterPrefab.AddComponent<AISkillDriver>();
            fly.skillSlot = SkillSlot.Primary;
            fly.requireSkillReady = true;
            fly.requireEquipmentReady = false;
            fly.nextHighPriorityOverride = voidCluster;
            fly.requiredSkill = skillDefPrimary;
            fly.moveTargetType = AISkillDriver.TargetType.Custom;
            fly.minDistance = 0f;
            fly.maxDistance = float.PositiveInfinity;
            fly.selectionRequiresTargetLoS = false;
            fly.activationRequiresTargetLoS = true;
            fly.activationRequiresAimConfirmation = false;
            fly.movementType = AISkillDriver.MovementType.StrafeMovetarget;
            fly.aimType = AISkillDriver.AimType.AtMoveTarget;
            fly.ignoreNodeGraph = true;
            fly.shouldSprint = true;
            fly.shouldFireEquipment = false;
            fly.shouldTapButton = true;
            #endregion
        }


        private void PrimarySetup()
        {
            SkillLocator component = bodyPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("IMPSORCERER_PRIMARY_FLY_NAME", "Fly");
            LanguageAPI.Add("IMPSORCERER_PRIMARY_FLY_DESCRIPTION", "");

            // set up your primary skill def here!

            skillDefPrimary = ScriptableObject.CreateInstance<SkillDef>();
            skillDefPrimary.activationState = new SerializableEntityStateType(typeof(Fly));
            skillDefPrimary.activationStateMachineName = "Body";
            skillDefPrimary.baseMaxStock = 1;
            skillDefPrimary.baseRechargeInterval = 10f;
            skillDefPrimary.beginSkillCooldownOnSkillEnd = true;
            skillDefPrimary.canceledFromSprinting = false;
            skillDefPrimary.fullRestockOnAssign = true;
            skillDefPrimary.interruptPriority = InterruptPriority.Death;
            skillDefPrimary.isBullets = false;
            skillDefPrimary.isCombatSkill = true;
            skillDefPrimary.mustKeyPress = true;
            skillDefPrimary.noSprint = true;
            skillDefPrimary.rechargeStock = 1;
            skillDefPrimary.requiredStock = 1;
            skillDefPrimary.shootDelay = 0f;
            skillDefPrimary.stockToConsume = 1;
            skillDefPrimary.icon = null;
            skillDefPrimary.skillDescriptionToken = "IMPSORCERER_PRIMARY_FLY_DESCRIPTION";
            skillDefPrimary.skillName = "IMPSORCERER_PRIMARY_FLY_NAME";
            skillDefPrimary.skillNameToken = "IMPSORCERER_PRIMARY_FLY_NAME";
            LoadoutAPI.AddSkillDef(skillDefPrimary);

            component.primary = bodyPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.primary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.primary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = skillDefPrimary,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(skillDefPrimary.skillNameToken, false, null)
            };

        }
        private void SecondarySetup()
        {
            SkillLocator component = bodyPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("IMPSORCERER_SECONDARY_VOIDCLUSTER_NAME", "Void Cluster");
            LanguageAPI.Add("IMPSORCERER_SECONDARY_VOIDCLUSTER_DESCRIPTION", "");

            // set up your secondary skill def here!

            skillDefSecondary = ScriptableObject.CreateInstance<SkillDef>();
            skillDefSecondary.activationState = new SerializableEntityStateType(typeof(FireVoidCluster));
            skillDefSecondary.activationStateMachineName = "Weapon";
            skillDefSecondary.baseMaxStock = 100;
            skillDefSecondary.baseRechargeInterval = 10f;
            skillDefSecondary.beginSkillCooldownOnSkillEnd = true;
            skillDefSecondary.canceledFromSprinting = false;
            skillDefSecondary.fullRestockOnAssign = true;
            skillDefSecondary.interruptPriority = InterruptPriority.Frozen;
            skillDefSecondary.isBullets = false;
            skillDefSecondary.isCombatSkill = true;
            skillDefSecondary.mustKeyPress = true;
            skillDefSecondary.noSprint = true;
            skillDefSecondary.rechargeStock = 1;
            skillDefSecondary.requiredStock = 1;
            skillDefSecondary.shootDelay = 0f;
            skillDefSecondary.stockToConsume = 1;
            skillDefSecondary.icon = null;
            skillDefSecondary.skillDescriptionToken = "IMPSORCERER_SECONDARY_VOIDCLUSTER_DESCRIPTION";
            skillDefSecondary.skillName = "IMPSORCERER_SECONDARY_VOIDCLUSTER_NAME";
            skillDefSecondary.skillNameToken = "IMPSORCERER_SECONDARY_VOIDCLUSTER_NAME";

            LoadoutAPI.AddSkillDef(skillDefSecondary);

            component.secondary = bodyPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.secondary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.secondary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = skillDefSecondary,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(skillDefSecondary.skillNameToken, false, null)
            };

        }
        private void UtilitySetup()
        {
            SkillLocator component = bodyPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("IMPSORCERER_UTILITY_BLINK_NAME", "Blink");
            LanguageAPI.Add("IMPSORCERER_UTILITY_BLINK_DESCRIPTION", "");

            // set up your utility skill def here!

            skillDefUtility = ScriptableObject.CreateInstance<SkillDef>();
            skillDefUtility.activationState = new SerializableEntityStateType(typeof(SorcererBlinkState));
            skillDefUtility.activationStateMachineName = "Body";
            skillDefUtility.baseMaxStock = 1;
            skillDefUtility.baseRechargeInterval = 15f;
            skillDefUtility.beginSkillCooldownOnSkillEnd = true;
            skillDefUtility.canceledFromSprinting = false;
            skillDefUtility.fullRestockOnAssign = true;
            skillDefUtility.interruptPriority = InterruptPriority.Frozen;
            skillDefUtility.isBullets = false;
            skillDefUtility.isCombatSkill = false;
            skillDefUtility.mustKeyPress = true;
            skillDefUtility.noSprint = true;
            skillDefUtility.rechargeStock = 1;
            skillDefUtility.requiredStock = 1;
            skillDefUtility.shootDelay = 0f;
            skillDefUtility.stockToConsume = 1;
            skillDefUtility.icon = null;
            skillDefUtility.skillDescriptionToken = "IMPSORCERER_UTILITY_BLINK_DESCRIPTION";
            skillDefUtility.skillName = "IMPSORCERER_UTILITY_BLINK_NAME";
            skillDefUtility.skillNameToken = "IMPSORCERER_UTILITY_BLINK_NAME";

            LoadoutAPI.AddSkillDef(skillDefUtility);

            component.utility = bodyPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.utility.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.utility.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = skillDefUtility,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(skillDefUtility.skillNameToken, false, null)
            };
        }
        private void SpecialSetup()
        {
            SkillLocator component = bodyPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("IMPSORCERER_SPECIAL_EYEATTACK_NAME", "");
            LanguageAPI.Add("IMPSORCERER_SPECIAL_EYEATTACK_DESCRIPTION", "");

            // set up your special skill def here!

            skillDefSpecial = ScriptableObject.CreateInstance<SkillDef>();
            skillDefSpecial.activationState = new SerializableEntityStateType(typeof(EyeAttackState));
            skillDefSpecial.activationStateMachineName = "Weapon";
            skillDefSpecial.baseMaxStock = 100;
            skillDefSpecial.baseRechargeInterval = 10f;
            skillDefSpecial.beginSkillCooldownOnSkillEnd = true;
            skillDefSpecial.canceledFromSprinting = false;
            skillDefSpecial.fullRestockOnAssign = true;
            skillDefSpecial.interruptPriority = InterruptPriority.Frozen;
            skillDefSpecial.isBullets = false;
            skillDefSpecial.isCombatSkill = true;
            skillDefSpecial.mustKeyPress = true;
            skillDefSpecial.noSprint = true;
            skillDefSpecial.rechargeStock = 1;
            skillDefSpecial.requiredStock = 1;
            skillDefSpecial.shootDelay = 0f;
            skillDefSpecial.stockToConsume = 1;
            skillDefSpecial.icon = null;
            skillDefSpecial.skillDescriptionToken = "IMPSORCERER_SPECIAL_EYEATTACK_DESCRIPTION";
            skillDefSpecial.skillName = "IMPSORCERER_SPECIAL_EYEATTACK_NAME";
            skillDefSpecial.skillNameToken = "IMPSORCERER_SPECIAL_EYEATTACK_NAME";

            LoadoutAPI.AddSkillDef(skillDefSpecial);

            component.special = bodyPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.special.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.special.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = skillDefSpecial,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(skillDefSpecial.skillNameToken, false, null)
            };
        }
        private void AddEyes()
        {
            /*eyes = Assets.mainAssetBundle.LoadAsset<GameObject>("EyeFollower");
            GameObject healFollower = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/networkedobjects/HealingFollower"), "HealFollower", true);
            MoreMonsters._logger.LogError("flag");

            var eyesNetworkIdentity = eyes.AddComponent<NetworkIdentity>();
            eyesNetworkIdentity = healFollower.GetComponent<NetworkIdentity>();

            ImpSorcererEyeFollowerController followerController = eyes.AddComponent<ImpSorcererEyeFollowerController>();

            Transform[] offsets = new Transform[] { eyes.transform.Find("Offset0"), eyes.transform.Find("Offset1"), eyes.transform.Find("Offset2") };
            for (int i = 0; i < offsets.Length; i++)
            {
                PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/networkedobjects/HealingFollower"), "temp" + i, false).transform.Find("Effect").SetParent(offsets[i].transform);
                MoreMonsters._logger.LogError("flag");
            }
            healFollower.transform.Find("Indicator").SetParent(eyes.transform); ;

            eyes = PrefabAPI.InstantiateClone(eyes, "EyeFollower", true);
            MoreMonsters._logger.LogError("flag");


            var addEyesComponent = bodyPrefab.AddComponent<EyeManager>();
            addEyesComponent.eyes = this.eyes;
            */

            var eyesAssetBundle = Assets.mainAssetBundle.LoadAsset<GameObject>("EyeFollower");
            eyes = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/networkedobjects/HealingFollower"), "ImpSorcererEyesFollower", true);
            UnityEngine.Object.Destroy(eyes.GetComponent<HealingFollowerController>());
            UnityEngine.Object.Destroy(eyes.transform.Find("Offset").gameObject);
            ImpSorcererEyeFollowerController followerController = eyes.AddComponent<ImpSorcererEyeFollowerController>();
            Transform[] offsets = new Transform[] { eyesAssetBundle.transform.Find("Offset0"), eyesAssetBundle.transform.Find("Offset1"), eyesAssetBundle.transform.Find("Offset2") };
            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i].SetParent(eyes.transform);
                PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/networkedobjects/HealingFollower"), "temp" + i, false).transform.Find("Offset").Find("Effect").SetParent(offsets[i]);
                //var temp = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/networkedobjects/HealingFollower"), "temp" + i, true);
                //temp.transform.Find("Offset").Find("Effect").SetParent(offsets[i]);
            }
            var addEyesComponent = bodyPrefab.AddComponent<EyeManager>();
            addEyesComponent.eyes = this.eyes;

        }
        private void AddProjectiles()
        {
            projectilePrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/VagrantTrackingBomb"), "ImpSorcererVoidClusterBomb", true);
            spikePrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/ImpVoidspikeProjectile"), "ImpSorcererVoidClusterSpikes", true);

            UnityEngine.Object.Destroy(projectilePrefab.GetComponent<ProjectileDirectionalTargetFinder>());
            UnityEngine.Object.Destroy(projectilePrefab.GetComponent<ProjectileSteerTowardTarget>());

            // add ghost version here


            //Does ProjectileController changes
            #region ProjectileController
            var projectileController = projectilePrefab.GetComponent<ProjectileController>();
            projectileController.allowPrediction = true;
            projectileController.shouldPlaySounds = true;
            #endregion

            //Does SimpleComponent changes
            #region SimpleComponent
            var simpleComponent = projectilePrefab.GetComponent<ProjectileSimple>();
            simpleComponent.updateAfterFiring = true;
            simpleComponent.velocity = 0;
            simpleComponent.lifetime = 99f;
            #endregion

            //Does DamageComponent changes
            #region DamageComponent
            var damageComponent = projectilePrefab.GetComponent<ProjectileDamage>();
            damageComponent.damageType = DamageType.BleedOnHit;
            #endregion

            //Does TargetingComponent changes
            #region TargetingComponent
            var targetingComponent = projectilePrefab.AddComponent<ProjectileSphereTargetFinder>();
            targetingComponent.lookRange = 400f;
            targetingComponent.onlySearchIfNoTarget = true;
            targetingComponent.allowTargetLoss = false;
            targetingComponent.flierAltitudeTolerance = float.PositiveInfinity;
            #endregion

            //Does SteerComponent changes
            #region SteerComponent
            var steerComponent = projectilePrefab.AddComponent<ProjectileSteerAboveTarget>();
            //steerComponent.rotationSpeed = 720f;
            steerComponent.maxVelocity = 10f;
            steerComponent.yAxisOnly = false;
            #endregion

            //Does ImpactComponentChanges
            #region ImpactComponent
            var impactComponent = projectilePrefab.GetComponent<ProjectileImpactExplosion>();
            impactComponent.lifetime = 10f;
            impactComponent.lifetimeAfterImpact = 5f;
            impactComponent.impactEffect = Resources.Load<GameObject>("prefabs/effects/ImpVoidspikeExplosion");
            impactComponent.destroyOnEnemy = true;
            impactComponent.destroyOnWorld = true;
            impactComponent.blastRadius = 2f;
            impactComponent.childrenDamageCoefficient = 0.75f;
            // These make it so the children always shoot downwards in a random radius. 0.26795 calculated with Mathf.Tan((float)Math.PI / 12f), or tan(15 deg)
            impactComponent.transformSpace = ProjectileImpactExplosion.TransformSpace.World;
            impactComponent.minAngleOffset = new Vector3(-0.26795f, -1f, -0.26795f);
            impactComponent.maxAngleOffset = new Vector3(0.26795f, -1f, 0.26795f);
            impactComponent.childrenCount = 8;
            impactComponent.childrenProjectilePrefab = spikePrefab;
            impactComponent.fireChildren = true;

            /* These need to be added once sound is done
            impactComponent.offsetForLifeTimeExpiredSound; //Subtracts this number from the lifetime to make the sound play early
            impactComponent.explosionSoundString;
            impactComponent.lifetimeExpiredSound;*/
            #endregion

            //Adds the halo
            #region Halo
            var haloPrefab = Assets.mainAssetBundle.LoadAsset<GameObject>("ImpSorcererHalo");
            haloPrefab = PrefabAPI.InstantiateClone(Assets.mainAssetBundle.LoadAsset<GameObject>("ImpSorcererHalo"), "ImpSorcererHalo", true);
            projectilePrefab.AddComponent<VoidClusterBombIndicator>();
            #endregion

            var spikeSimpleComponent = spikePrefab.GetComponent<ProjectileSimple>();
            spikeSimpleComponent.velocity *= 0.75f;

            ProjectileCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(projectilePrefab);
                list.Add(spikePrefab);
            };
            FireVoidCluster.projectilePrefab = projectilePrefab;
        }
    }
    public class VoidClusterBombIndicator : MonoBehaviour
    {
        GameObject indicator;
        private void Awake()
        {
            indicator = Instantiate(Assets.mainAssetBundle.LoadAsset<GameObject>("ImpSorcererHalo"), gameObject.transform.position, Quaternion.identity);
            indicator.GetComponentInChildren<MeshRenderer>().material = Resources.Load<GameObject>("Prefabs/Projectiles/ImpVoidspikeProjectile").transform.Find("ImpactEffect").Find("AreaIndicator").GetComponent<MeshRenderer>().material;
            var cpt = indicator.AddComponent<DestroyIndicator>();
            cpt.projectileObject = gameObject;
        }
        private void FixedUpdate()
        {
            indicator.transform.position = gameObject.transform.position;
        }
    }
    public class DestroyIndicator : MonoBehaviour
    {
        public GameObject projectileObject;

        private void FixedUpdate()
        {
            if (!projectileObject)
                Destroy(gameObject);
        }
    }
    public class ProjectileSteerAboveTarget : MonoBehaviour
    {
        public bool yAxisOnly;
        public float maxVelocity;
        private float velocity;
        private Vector3 velocityAsVector;
        private new Transform transform;
        private ProjectileTargetComponent targetComponent;
        private ProjectileSimple projectileSimple;
        private Transform model;
        private void Start()
        {
            if (!NetworkServer.active)
            {
                enabled = false;
                return;
            }
            transform = gameObject.transform;
            targetComponent = GetComponent<ProjectileTargetComponent>();
            projectileSimple = GetComponent<ProjectileSimple>();
            model = gameObject.transform.Find("Model");
            velocity = maxVelocity;
            velocityAsVector = Vector3.zero;
        }

        /*private void FixedUpdate()
        {
            if (targetComponent.target)
            {
                Vector3 vector = targetComponent.target.transform.position + new Vector3(0f, 10f) - transform.position;
                if (Mathf.Abs(vector.y) < 1f)
                    vector.y = 0f;
                if (vector != Vector3.zero)
                {
                    transform.forward = Vector3.RotateTowards(transform.forward, vector, rotationSpeed * 0.0174532924f * Time.fixedDeltaTime, 0f);
                    model.forward = Vector3.RotateTowards(model.forward, -vector, rotationSpeed * 0.0174532924f * Time.fixedDeltaTime, 0f);
                }
                if (Mathf.Abs(vector.x) < 1f && Mathf.Abs(vector.z) < 1f && projectileSimple.velocity > 0.25f && vector.y > -1f)
                    projectileSimple.velocity -= 0.25f;
                else
                if (projectileSimple.velocity < 13f)
                    projectileSimple.velocity += 1f;
                if (projectileSimple.velocity > 13f)
                    projectileSimple.velocity = 13f;
            }
        }*/

        private void FixedUpdate()
        {
            if (targetComponent.target)
            {
                Vector3 vector = targetComponent.target.transform.position + new Vector3(0f, 10f) - transform.position;
                Vector3 movePosition = targetComponent.target.transform.position + new Vector3(0f, 10f);
                //if (Mathf.Abs(vector.y) < 0.1f)
                //  vector.y = 0f;
                if (vector.sqrMagnitude < 1.3f)
                {
                    velocity -= 0.15f;
                    if (velocity < 0.15f)
                        velocity = 0.15f;
                }
                else
                if (velocity < 13f)
                    velocity += 0.2f;
                if (velocity > 13f)
                    velocity = 13f;
                if (vector != Vector3.zero)
                    transform.position = Vector3.SmoothDamp(transform.position, movePosition, ref velocityAsVector, vector.magnitude / velocity, maxVelocity, Time.deltaTime);
            }
        }
    }
    public class EyeManager : NetworkBehaviour
    {
        public GameObject eyes;
        private GameObject eyesInstance;
        public void Start()
        {
            eyesInstance = Instantiate(eyes, gameObject.transform.position, gameObject.transform.rotation);
            var followerController = eyesInstance.GetComponent<ImpSorcererEyeFollowerController>();
            followerController.NetworkownerBodyObject = gameObject;
        }
    }

    public class ImpSorcererEyeFollowerController : NetworkBehaviour
    {
        public float attackSpeed;
        public float damage;
        public float attackTelegraphTime = 3f;
        public float attackTelegraphStopMovingTime = 1.5f;
        public float attackTime = 3f;
        public float rotationAngularVelocity;
        public float acceleration = 10f;
        public float damping = 2f;
        public bool enableSpringMotion = false;
        [SyncVar]
        public GameObject ownerBodyObject;
        [SyncVar]
        public GameObject targetBodyObject;
        public GameObject burstHealEffect;
        public GameObject indicator;
        public GameObject NetworkownerBodyObject
        {
            get
            {
                return ownerBodyObject;
            }
            [param: In]
            set
            {
                SetSyncVarGameObject(value, ref ownerBodyObject, 1U, ref ___ownerBodyObjectNetId);
            }
        }
        public GameObject NetworktargetBodyObject
        {
            get
            {
                return targetBodyObject;
            }
            [param: In]
            set
            {
                SetSyncVarGameObject(value, ref targetBodyObject, 2U, ref ___targetBodyObjectNetId);
            }
        }
        private GameObject cachedTargetBodyObject;
        [SyncVar]
        public float attackTimer;
        /*public float NetworkattackTimer
        {
            get 
            {
                return attackTimer;
            }
            [param: In]
            set
            {
                SetSyncVar<float>(value, ref attackTimer, 3U);
            }
        }
        private float cachedAttackTimer;*/
        private bool attacked = false;
        private Vector3 velocity;
        private NetworkInstanceId ___ownerBodyObjectNetId;
        private NetworkInstanceId ___targetBodyObjectNetId;
        private SubState subState;
        private enum SubState
        {
            CirclingImp,
            TelegraphAttack,
            TelegraphAttackStopMoving
        }


        private void FixedUpdate()
        {
            if (!ownerBodyObject)
                UnityEngine.Object.Destroy(gameObject);
            if (cachedTargetBodyObject != targetBodyObject)
            {
                cachedTargetBodyObject = targetBodyObject;
            }
            if (NetworkServer.active)
            {
                FixedUpdateServer();
            }
        }
        private void FixedUpdateServer()
        {
            if (subState != SubState.CirclingImp)
            {
                attackTimer -= Time.fixedDeltaTime;
                if (attackTimer <= attackTime / attackSpeed && attacked == false)
                    DoAttack();
                else
                    if (attackTimer <= (attackTime + attackTelegraphStopMovingTime) / attackSpeed)
                    subState = SubState.TelegraphAttackStopMoving;
            }
            if (attackTimer <= 0f)
                AssignNewTarget(ownerBodyObject);
            if (!targetBodyObject)
            {
                NetworktargetBodyObject = ownerBodyObject;
                subState = SubState.CirclingImp;
            }
            if (!ownerBodyObject)
                UnityEngine.Object.Destroy(gameObject);
            if (NetworktargetBodyObject == ownerBodyObject || subState == SubState.CirclingImp)
            {
                attackTimer = (attackTime + attackTelegraphTime + attackTelegraphStopMovingTime) / attackSpeed;
                attacked = false;
            }
        }

        [Server]
        public void AssignNewTarget(GameObject target)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void MoreMonsters.SorcererEyeFollowerController::AssignNewTarget(UnityEngine.GameObject)' called on client");
                return;
            }
            NetworktargetBodyObject = (target ? target : ownerBodyObject);
            cachedTargetBodyObject = targetBodyObject;

            if (NetworktargetBodyObject == ownerBodyObject)
                subState = SubState.CirclingImp;
        }

        private void Update()
        {
            if (subState == SubState.TelegraphAttack || subState == SubState.CirclingImp)
            {
                UpdateMotion();
                transform.position += velocity * Time.deltaTime;
                if (subState == SubState.TelegraphAttack)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        gameObject.transform.Find("Offset" + i).forward = Vector3.RotateTowards(gameObject.transform.Find("Offset" + i).forward, (targetBodyObject.GetComponent<CharacterBody>().mainHurtBox.gameObject.transform.position) - gameObject.transform.Find("Offset" + i).position, 180f * 0.0174532924f * Time.fixedDeltaTime, 0f);
                    }
                }
                else
                    transform.rotation = Quaternion.AngleAxis(rotationAngularVelocity * Time.deltaTime, Vector3.up) * transform.rotation;
                if (targetBodyObject != ownerBodyObject)
                {
                    indicator.transform.position = GetTargetPosition();
                }
            }
        }

        [Server]
        private void DoAttack()
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void MoreMonsters.SorcererEyeFollowerController::DoAttack(System.Single)' called on client");
                return;
            }

            attacked = true;
            for (int i = 0; i < 4; i++)
            {
                gameObject.transform.Find("Offset" + i);
                ProjectileManager.instance.FireProjectile(Resources.Load<GameObject>("prefabs/projectiles/GravekeeperHookProjectileSimple"), gameObject.transform.Find("Offset" + i).position, Util.QuaternionSafeLookRotation(gameObject.transform.Find("Offset" + i).forward), ownerBodyObject, damage, 0f, false, DamageColorIndex.Default);
            }
        }


        private Vector3 GetTargetPosition()
        {
            GameObject gameObject = targetBodyObject ?? ownerBodyObject;
            if (!gameObject)
            {
                return transform.position;
            }
            CharacterBody component = gameObject.GetComponent<CharacterBody>();
            if (!component)
            {
                return gameObject.transform.position;
            }
            return component.corePosition;
        }

        private Vector3 GetDesiredPosition()
        {
            return GetTargetPosition();
        }

        private void UpdateMotion()
        {
            Vector3 desiredPosition = GetDesiredPosition();
            if ((desiredPosition - transform.position).sqrMagnitude <= 4f)
            {
                subState = SubState.TelegraphAttack;
            }
            if (enableSpringMotion)
            {
                Vector3 lhs = desiredPosition - transform.position;
                if (lhs != Vector3.zero)
                {
                    Vector3 a = lhs.normalized * acceleration;
                    Vector3 b = velocity * -damping;
                    velocity += (a + b) * Time.deltaTime;
                    return;
                }
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, damping, 14f);
            }
        }

        private void UNetVersion()
        {
        }
        public override bool OnSerialize(NetworkWriter writer, bool forceAll)
        {
            if (forceAll)
            {
                writer.Write(ownerBodyObject);
                writer.Write(targetBodyObject);
                return true;
            }
            bool flag = false;
            if ((syncVarDirtyBits & 1U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(ownerBodyObject);
            }
            if ((syncVarDirtyBits & 2U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(syncVarDirtyBits);
                    flag = true;
                }
                writer.Write(targetBodyObject);
            }
            if (!flag)
            {
                writer.WritePackedUInt32(syncVarDirtyBits);
            }
            return flag;
        }
        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                ___ownerBodyObjectNetId = reader.ReadNetworkId();
                ___targetBodyObjectNetId = reader.ReadNetworkId();
                return;
            }
            int num = (int)reader.ReadPackedUInt32();
            if ((num & 1) != 0)
            {
                ownerBodyObject = reader.ReadGameObject();
            }
            if ((num & 2) != 0)
            {
                targetBodyObject = reader.ReadGameObject();
            }
        }
        public override void PreStartClient()
        {
            if (!___ownerBodyObjectNetId.IsEmpty())
            {
                NetworkownerBodyObject = ClientScene.FindLocalObject(___ownerBodyObjectNetId);
            }
            if (!___targetBodyObjectNetId.IsEmpty())
            {
                NetworktargetBodyObject = ClientScene.FindLocalObject(___targetBodyObjectNetId);
            }
        }
        public override void OnStartClient()
        {
            base.OnStartClient();
            transform.position = GetDesiredPosition();
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            attackTimer = (attackTime + attackTelegraphTime + attackTelegraphStopMovingTime) / attackSpeed;
            subState = SubState.CirclingImp;
        }
    }

}


namespace MoreMonsters.States.ImpSorcerer
{
    /*Imp has these animation layers:
     * Layer 0: Body
     * Layer 1: Turn
     * Layer 2: Gesture, Override
     * Layer 3: Gesture, Additive
     * Layer 4: Impact
     * Layer 5: AimYaw
     * Layer 6: AimPitch
     * Layer 7: Flinch
     * Layer 8: Idle, Additive
     * Layer 9: Blink, Additive
     */
    public class Fly : BaseSkillState
    {
        public static float duration = 1f;
        public static float launchSpeed = 8f;
        //public static GameObject jumpEffectPrefab;
        //public static string jumpEffectMuzzleString;
        public ICharacterGravityParameterProvider characterGravityParameterProvider;
        public ICharacterFlightParameterProvider characterFlightParameterProvider;
        public float mecanimTransitionDuration;
        public float flyOverrideMecanimLayerWeight;
        public float movementSpeedMultiplier = 1.2f;
        public string enterSoundString;
        protected Animator animator;
        protected int flyOverrideLayer;
        public BaseAI baseAI;
        public KinematicCharacterMotor kinematicCharacterMotor;

        public override void OnEnter()
        {
            base.OnEnter();
            animator = base.GetModelAnimator();
            kinematicCharacterMotor = gameObject.GetComponent<KinematicCharacterMotor>();
            characterGravityParameterProvider = base.gameObject.GetComponent<ICharacterGravityParameterProvider>();
            characterFlightParameterProvider = base.gameObject.GetComponent<ICharacterFlightParameterProvider>();
            baseAI = gameObject.GetComponent<BaseAI>();
            if (animator)
            {
                flyOverrideLayer = animator.GetLayerIndex("FlyOverride");
            }
            if (base.characterMotor)
            {
                base.characterMotor.walkSpeedPenaltyCoefficient = movementSpeedMultiplier;
            }
            if (base.modelLocator)
            {
                base.modelLocator.normalizeToFloor = false;
            }
            characterGravityParameterProvider = base.gameObject.GetComponent<ICharacterGravityParameterProvider>();
            characterFlightParameterProvider = base.gameObject.GetComponent<ICharacterFlightParameterProvider>();
            if (characterGravityParameterProvider != null)
            {
                CharacterGravityParameters gravityParameters = characterGravityParameterProvider.gravityParameters;
                gravityParameters.channeledAntiGravityGranterCount++;
                characterGravityParameterProvider.gravityParameters = gravityParameters;
            }
            if (characterFlightParameterProvider != null)
            {
                CharacterFlightParameters flightParameters = characterFlightParameterProvider.flightParameters;
                flightParameters.channeledFlightGranterCount++;
                characterFlightParameterProvider.flightParameters = flightParameters;
            }
            if (base.characterMotor)
            {
                base.characterMotor.velocity.y = launchSpeed;
                base.characterMotor.Motor.ForceUnground();
            }
            base.PlayAnimation("Body", "Jump");
            //base.PlayAnimation("")
            /*if (jumpEffectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(jumpEffectPrefab, base.gameObject, jumpEffectMuzzleString, false);
            }*/
        }

        public override void OnExit()
        {
            if (activatorSkillSlot)
                activatorSkillSlot.SetSkillOverride(this, Resources.Load<SkillDef>("skilldefs/captainbody/CaptainSkillUsedUp"), GenericSkill.SkillOverridePriority.Replacement);
            if (baseAI)
                baseAI.PickCurrentNodeGraph();
            /*if (kinematicCharacterMotor)
                kinematicCharacterMotor._solveGrounding = false;*/
            //outer.SetNextStateToMain();
            base.OnExit();
        }
    }



    public class FireVoidCluster : BaseState
    {
        public static GameObject projectilePrefab;
        public static GameObject effectPrefab;
        public static float baseDuration = 3.5f;
        public static float damageCoefficient = 4f;
        public static float procCoefficient;
        public static float selfForce;
        public static float forceMagnitude = 16f;
        public static GameObject hitEffectPrefab;
        public static GameObject swipeEffectPrefab;
        public static string enterSoundString;
        public static string slashSoundString;
        public static float walkSpeedPenaltyCoefficient;
        private Animator modelAnimator;
        private float duration;
        private int slashCount;
        private Transform modelTransform;


        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            modelAnimator = base.GetModelAnimator();
            modelTransform = base.GetModelTransform();
            Ray aimRay = base.GetAimRay();
            base.StartAimMode(aimRay, 2f, false);
            base.characterMotor.walkSpeedPenaltyCoefficient = walkSpeedPenaltyCoefficient;
            //Util.PlayScaledSound(enterSoundString, base.gameObject, attackSpeedStat);
            if (modelAnimator)
            {
                base.PlayAnimation("Gesture, Additive", "DoubleSlash", "DoubleSlash.playbackRate", duration);
                base.PlayAnimation("Gesture, Override", "DoubleSlash", "DoubleSlash.playbackRate", duration);
            }
            if (base.isAuthority)
            {
                ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageStat * damageCoefficient, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
            }
            if (base.characterBody)
            {
                base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
                base.characterBody.SetAimTimer(this.duration + 2f);
            }
        }
        public override void OnExit()
        {
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }
    }


    public class SorcererBlinkState : BaseState
    {
        private Transform modelTransform;
        public static GameObject blinkPrefab = Resources.Load<GameObject>("prefabs/effects/ImpBlinkEffect");
        public static Material destealthMaterial = EntityStates.ImpMonster.BlinkState.destealthMaterial;
        private float stopwatch;
        private Vector3 blinkDestination = Vector3.zero;
        private Vector3 blinkStart = Vector3.zero;
        public static float duration = 0.3f;
        public static float blinkDistance = 40f;
        public static string beginSoundString;
        public static string endSoundString;
        private Animator animator;
        private CharacterModel characterModel;
        private HurtBoxGroup hurtboxGroup;

        public override void OnEnter()
        {
            base.OnEnter();
            Util.PlaySound(SorcererBlinkState.beginSoundString, base.gameObject);
            modelTransform = base.GetModelTransform();
            if (modelTransform)
            {
                animator = modelTransform.GetComponent<Animator>();
                characterModel = modelTransform.GetComponent<CharacterModel>();
                hurtboxGroup = modelTransform.GetComponent<HurtBoxGroup>();
            }
            if (characterModel)
            {
                characterModel.invisibilityCount++;
            }
            if (hurtboxGroup)
            {
                HurtBoxGroup hurtBoxGroup = hurtboxGroup;
                int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter + 1;
                hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
            }
            if (base.characterMotor)
            {
                base.characterMotor.enabled = false;
            }
            Vector3 b = base.inputBank.moveVector * SorcererBlinkState.blinkDistance;
            blinkDestination = base.transform.position;
            blinkStart = base.transform.position;
            NodeGraph groundNodes = SceneInfo.instance.airNodes;
            NodeGraph.NodeIndex nodeIndex = groundNodes.FindClosestNode(base.transform.position + b, base.characterBody.hullClassification);
            groundNodes.GetNodePosition(nodeIndex, out blinkDestination);
            blinkDestination += base.transform.position - base.characterBody.footPosition;
            CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
        }

        private void CreateBlinkEffect(Vector3 origin)
        {
            EffectData effectData = new EffectData();
            effectData.rotation = Util.QuaternionSafeLookRotation(blinkDestination - blinkStart);
            effectData.origin = origin;
            EffectManager.SpawnEffect(SorcererBlinkState.blinkPrefab, effectData, false);
        }

        private void SetPosition(Vector3 newPosition)
        {
            if (base.characterMotor)
            {
                base.characterMotor.Motor.SetPositionAndRotation(newPosition, Quaternion.identity, true);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            stopwatch += Time.fixedDeltaTime;
            if (base.characterMotor && base.characterDirection)
            {
                base.characterMotor.velocity = Vector3.zero;
            }
            SetPosition(Vector3.Lerp(blinkStart, blinkDestination, stopwatch / SorcererBlinkState.duration));
            if (stopwatch >= SorcererBlinkState.duration && base.isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            Util.PlaySound(SorcererBlinkState.endSoundString, base.gameObject);
            CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
            modelTransform = base.GetModelTransform();
            if (modelTransform && SorcererBlinkState.destealthMaterial)
            {
                TemporaryOverlay temporaryOverlay = animator.gameObject.AddComponent<TemporaryOverlay>();
                temporaryOverlay.duration = 1f;
                temporaryOverlay.destroyComponentOnEnd = true;
                temporaryOverlay.originalMaterial = SorcererBlinkState.destealthMaterial;
                temporaryOverlay.inspectorCharacterModel = animator.gameObject.GetComponent<CharacterModel>();
                temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                temporaryOverlay.animateShaderAlpha = true;
            }
            if (characterModel)
            {
                characterModel.invisibilityCount--;
            }
            if (hurtboxGroup)
            {
                HurtBoxGroup hurtBoxGroup = hurtboxGroup;
                int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter - 1;
                hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
            }
            if (base.characterMotor)
            {
                base.characterMotor.enabled = true;
            }
            base.PlayAnimation("Gesture, Additive", "BlinkEnd");
            base.OnExit();
        }
    }


    public class EyeAttackState : BaseState
    {
        public static GameObject projectilePrefab;
        public static GameObject effectPrefab;
        public static float baseDuration = 3.5f;
        public static float damageCoefficient = 4f;
        public static float procCoefficient;
        public static float selfForce;
        public static float forceMagnitude = 16f;
        public static GameObject hitEffectPrefab;
        public static GameObject swipeEffectPrefab;
        public static string enterSoundString;
        public static string slashSoundString;
        public static float walkSpeedPenaltyCoefficient;
        private Animator modelAnimator;
        private float duration;
        private int slashCount;
        private Transform modelTransform;
        private ImpSorcererEyeFollowerController eyeFollowerController;
        private BullseyeSearch bullseyeSearch;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            modelAnimator = base.GetModelAnimator();
            modelTransform = base.GetModelTransform();
            Ray aimRay = base.GetAimRay();
            base.StartAimMode(aimRay, 2f, false);
            base.characterMotor.walkSpeedPenaltyCoefficient = walkSpeedPenaltyCoefficient;
            //Util.PlayScaledSound(enterSoundString, base.gameObject, attackSpeedStat);
            eyeFollowerController = gameObject.GetComponent<ImpSorcererEyeFollowerController>();
            if (modelAnimator)
            {
                base.PlayAnimation("Gesture, Additive", "DoubleSlash", "DoubleSlash.playbackRate", duration);
                base.PlayAnimation("Gesture, Override", "DoubleSlash", "DoubleSlash.playbackRate", duration);
            }
            if (base.isAuthority && NetworkServer.active)
            {
                eyeFollowerController.damage = damageStat * damageCoefficient;
                eyeFollowerController.attackSpeed = attackSpeedStat;
                BullseyeSearch bullseyeSearch = new BullseyeSearch();
                bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
                if (base.teamComponent)
                {
                    bullseyeSearch.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);
                }
                bullseyeSearch.maxDistanceFilter = 70f;
                bullseyeSearch.maxAngleFilter = 90f;
                bullseyeSearch.searchOrigin = aimRay.origin;
                bullseyeSearch.searchDirection = aimRay.direction;
                bullseyeSearch.filterByLoS = false;
                bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
                bullseyeSearch.RefreshCandidates();
                HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
                if (hurtBox)
                {
                    eyeFollowerController.AssignNewTarget(hurtBox.gameObject);
                }
            }
            if (base.characterBody)
            {
                base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
                base.characterBody.SetAimTimer(this.duration + 2f);
            }
        }
        public override void OnExit()
        {
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

    }
}