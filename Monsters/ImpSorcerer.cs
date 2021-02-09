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


/* To-Do List:
 * Note: The Fly State is broken as shit right now due to not having viable animation layers. Come back to it when actual characters are ready to be implemented.
 * Implement model whenever that's possible
 */
namespace MoreMonsters
{
    public class ImpSorcerer : MonsterBoilerplate
    {
        public override string displayName => "Imp Sorcerer";
        public override string nameTag => "ImpSorcerer";
        public override Type[] skillStates => new Type[]
        {
            typeof(Fly),
            typeof(FireVoidCluster),
            typeof(SorcererBlinkState),
            typeof(EyeAttackState)
        };
        public override HullClassification hullSize => HullClassification.Human;
        public override MapNodeGroup.GraphType graphType => MapNodeGroup.GraphType.Air;
        public override int creditCost => 51;
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
        SkillDef skillDefPrimary;
        SkillDef skillDefSecondary;
        SkillDef skillDefUtility;
        SkillDef skillDefSpecial;
        GameObject projectilePrefab;
        GameObject spikePrefab;
        public static string deleteWhenYouGetModel = "ModelBase/mdlImp";

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
        ///<summary>Because the FireChild stuff was designed in a completely **FUCKING STUPID** WAY AND USES THE PROVIDED Z-OFFET FOR BOTH Y AND Z OF THE VECTOR... This injects some code to make it take the y offset instead.</summary>
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
            bodyPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/ImpBody"), "ImpSorcererBody", true);
            var cb = bodyPrefab.GetComponent<CharacterBody>();
            cb.baseNameToken = "IMPSORCERER_BODY_NAME";
            cb.baseJumpCount = 0;

            //GameObject model = CreateModel(bodyPrefab);
            //AddEyes();
            AddCrystals();
            AddProjectiles();
            //All the other shit that needs to go here

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
        public override void PrimarySetup()
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
        public override void SecondarySetup()
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
        public override void UtilitySetup()
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
        public override void SpecialSetup()
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
        /*private void AddEyes()
        {
            var eyesAssetBundle = PrefabAPI.InstantiateClone(Assets.mainAssetBundle.LoadAsset<GameObject>("EyeFollower"), "EyeFollowerAssetBundle", false);
            eyes = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/networkedobjects/HealingFollower"), "ImpSorcererEyesFollower", true);

            UnityEngine.Object.Destroy(eyes.GetComponent<HealingFollowerController>());
            var eyeController = eyes.AddComponent<ImpSorcererEyeController>();
            var addEyesComponent = bodyPrefab.AddComponent<EyeManager>();

            #region Adding Offsets
            eyes.transform.position = Vector3.zero;
            Transform[] offsets = new Transform[] { eyesAssetBundle.transform.Find("Offset0"), eyesAssetBundle.transform.Find("Offset1"), eyesAssetBundle.transform.Find("Offset2") };
            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i].SetParent(eyes.transform, true);
                //PrefabAPI.InstantiateClone(eyes.transform.Find("Offset/Effect/mdlWoodSprite").gameObject, "Model", false).transform.SetParent(offsets[i], false);


                PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/pickupmodels/PickupDiamond"), "Model", false).transform.SetParent(offsets[i], false);
            }
            UnityEngine.Object.Destroy(eyes.transform.Find("Offset").gameObject);
            #endregion

            #region Indicator
            UnityEngine.Object.Destroy(eyes.transform.Find("Indicator").gameObject);
            var beetleGuardIndicator = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/characterbodies/BeetleGuardBody").transform.Find("ModelBase/mdlBeetleGuard/SlamZone/HitboxGroup/GroundSlamIndicator").gameObject, "Indicator", false);
            beetleGuardIndicator.transform.SetParent(eyes.transform, false);
            beetleGuardIndicator.transform.localScale *= 1.5f;
            beetleGuardIndicator.transform.localPosition = Vector3.zero;
            beetleGuardIndicator.GetComponent<MeshFilter>().mesh = Resources.Load<GameObject>("Prefabs/Projectiles/ImpVoidspikeProjectile").transform.Find("ImpactEffect/AreaIndicator").GetComponent<MeshFilter>().mesh;
            beetleGuardIndicator.GetComponent<MeshRenderer>().material = Resources.Load<GameObject>("Prefabs/Projectiles/ImpVoidspikeProjectile").transform.Find("ImpactEffect/AreaIndicator").GetComponent<MeshRenderer>().material;
            UnityEngine.Object.Destroy(beetleGuardIndicator.transform.Find("Arrow Holder").gameObject);
            for (int i = 1; i < 8; i++)
                UnityEngine.Object.Destroy(beetleGuardIndicator.transform.Find("Arrow Holder (" + i + ")").gameObject);
            #endregion

            #region Adding Effects
            var eyeEffect = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/effects/ImpDeathEffect"), "ImpSorcererEyeAttackEffect", false);
            //UnityEngine.Object.Destroy(eyeEffect.transform.Find("PP").gameObject);

            eyeEffect.GetComponent<EffectComponent>().effectIndex = EffectIndex.Invalid;
            eyeEffect.GetComponent<EffectComponent>().effectData = null;
            eyeEffect.GetComponent<EffectComponent>().applyScale = true;
            eyeEffect.GetComponent<VFXAttributes>().secondaryParticleSystem = null;
            eyeEffect.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var particleSystems = eyeEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                var main = particleSystem.main;
                main.simulationSpeed = 1.5f;
            }

            EffectAPI.AddEffect(eyeEffect);
            #endregion

            #region Defining stuff
            eyeController.effectPrefab = eyeEffect;
            eyeController.acceleration = 10f;
            eyeController.rotationAngularVelocity = 30f;
            eyeController.damage = 0.3f;
            eyeController.enableSpringMotion = false;

            eyeController.attackTelegraphTime = 2f;
            eyeController.attackTelegraphStopMovingTime = 1.2f;
            eyeController.attackTime = 2f;
            eyeController.attackInterval = (eyeController.attackTime + eyeController.attackTelegraphTime + eyeController.attackTelegraphStopMovingTime);
            eyeController.attackTimer = eyeController.attackInterval;
            #endregion

            addEyesComponent.eyes = eyes;
        }*/
        private void AddCrystals()
        {
            #region Stuff to be removed when model exists
            MoreMonsters._logger.LogWarning("flag");
            var crystals = PrefabAPI.InstantiateClone(Assets.mainAssetBundle.LoadAsset<GameObject>("CrystalFollower"), "Crystals", false);
            crystals.transform.SetParent(bodyPrefab.transform.Find("ModelBase/mdlImp"), false);


            Transform[] offsets = new Transform[] { crystals.transform.Find("Offset0"), crystals.transform.Find("Offset1"), crystals.transform.Find("Offset2") };
            for (int i = 0; i < offsets.Length; i++)
            {
                offsets[i].SetParent(crystals.transform, true);

                var diamond = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/pickupmodels/PickupDiamond"), "Model", false);
                diamond.transform.SetParent(offsets[i], false);
                var rigidBody = diamond.AddComponent<Rigidbody>();
                rigidBody.mass = 60f;
                rigidBody.drag = 0.05f;
                rigidBody.angularDrag = 0.02f;
            }
            #endregion

            #region Attack Crystals added
            MoreMonsters._logger.LogWarning("flag");
            var crystalClones = GameObject.Instantiate(bodyPrefab.transform.Find(deleteWhenYouGetModel + "/Crystals").gameObject); // When model is imported, change mdlImp to mdlImpSorcerer or else there will be a NRE!
            crystalClones.name = "Attack Crystals";
            crystalClones.AddComponent<NetworkIdentity>(); //networkidentity should default to server only(?)
            crystalClones.AddComponent<ImpSorcererCrystalAttackManager>();
            PrefabAPI.RegisterNetworkPrefab(crystalClones);
            ImpSorcererCrystalController.clonePrefab = crystalClones;
            #endregion

            #region Adding Effects
            MoreMonsters._logger.LogWarning("flag");
            var attackEffect = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/effects/ImpDeathEffect"), "ImpSorcererEyeAttackEffect", false);

            attackEffect.GetComponent<EffectComponent>().effectIndex = EffectIndex.Invalid;
            attackEffect.GetComponent<EffectComponent>().effectData = null;
            attackEffect.GetComponent<EffectComponent>().applyScale = true;
            attackEffect.GetComponent<VFXAttributes>().secondaryParticleSystem = null;
            attackEffect.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var particleSystems = attackEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                var main = particleSystem.main;
                main.simulationSpeed = 1.5f;
            }
            EffectAPI.AddEffect(attackEffect);
            ImpSorcererCrystalAttackManager.effectPrefab = attackEffect;
            #endregion


            var crystalController = crystals.AddComponent<ImpSorcererCrystalController>();
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

            //Adds the halo. This is set up in a really terrible way right now.
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

    public class ImpSorcererCrystalController : MonoBehaviour
    {
        public static GameObject clonePrefab;
        public float rotationAngularVelocity = 30f;
        public float acceleration = 15f;
        public float damping = 0.3f;
        public bool enableSpringMotion = false;
        public GameObject parentBaseObject;
        private Vector3 velocity = Vector3.zero;
        public MovementType movementType;
        public enum MovementType
        {
            //This will contain all of the movement types that the crystals may have while doing stuff
        }
        private void Start()
        {
            //This snippet of code just grabs the highest parent, which SHOULD be the body prefab
            Transform nextParentUp = gameObject.transform.parent;
            while (nextParentUp.parent)
                nextParentUp = nextParentUp.parent;
            parentBaseObject = nextParentUp.gameObject;
        }

        private void Update()
        {
            //UpdateMotion();
            //transform.position += velocity * Time.deltaTime;
            UpdateRotation();
        }

        // This shit does not need to be implemented until the unique movement types are developed
        /*private void UpdateMotion()
        {
            Vector3 desiredPosition = gameObject.transform.parent.position;
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
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, damping, 14f);
        }*/

        private void UpdateRotation()
        {
            switch (movementType)
            {
                default:
                    var aimRay = parentBaseObject.GetComponent<InputBankTest>().GetAimRay();
                    for (int i = 0; i < 3; i++)
                    {
                        var offset = transform.Find("Offset" + i);
                        offset.forward = Vector3.RotateTowards(offset.forward, aimRay.direction, 180f * 0.0174532924f * Time.fixedDeltaTime, 0f);
                    }
                    transform.rotation = Quaternion.AngleAxis(rotationAngularVelocity * Time.deltaTime, Vector3.up) * transform.rotation;
                    break;
            }
        }

        public void SetTarget(GameObject target, float attackSpeed, float attackInterval, float damage)
        {
            GameObject crystalClones = Instantiate(clonePrefab, gameObject.transform.position, gameObject.transform.rotation);
            var attackManager = crystalClones.GetComponent<ImpSorcererCrystalAttackManager>();
            attackManager.ownerBodyObject = parentBaseObject;
            attackManager.targetBodyObject = target;

            attackManager.attackSpeed = attackSpeed;
            attackManager.attackInterval = attackInterval;
            attackManager.damage = damage;
            gameObject.SetActive(false);
        }
    }

    public class ImpSorcererCrystalAttackManager : NetworkBehaviour
    {
        public static GameObject effectPrefab;
        [SyncVar]
        public GameObject ownerBodyObject;
        [SyncVar]
        public GameObject targetBodyObject;
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

        // attackTime is what percent of the attack interval it is through when they attack. This just makes it easier to work with attack speeds.
        public float attackSpeed;
        public float attackInterval;
        public float damage;
        public float rotationAngularVelocity;
        public float acceleration = 15f;
        public float damping = 0.3f;
        public bool enableSpringMotion = false;

        // attack timer only starts ticking down once they get close enough to the target
        private float attackTimer = 0f;
        private float attackTime = 0.75f;
        private bool attacked = false;
        private bool hasDoneEffect = false;
        private Vector3 velocity = Vector3.zero;
        private Vector3 velocityOffsets = Vector3.zero;
        private Vector3 lastKnownPosition;
        private GameObject cachedTargetBodyObject;
        private HurtBox cachedTargetHurtBox;
        private NetworkInstanceId ___ownerBodyObjectNetId;
        private NetworkInstanceId ___targetBodyObjectNetId;
        private SubState subState = SubState.MovingToTarget;
        public enum SubState
        {
            MovingToTarget,
            TelegraphAttack,
            MovingToOwner
        }

        private void FixedUpdate()
        {
            if (cachedTargetBodyObject != targetBodyObject)
            {
                cachedTargetBodyObject = targetBodyObject;
                OnTargetChanged();
            }
            if (NetworkServer.active)
                FixedUpdateServer();
        }
        public void FixedUpdateServer()
        {
            if (subState == SubState.TelegraphAttack)
            {
                attackTimer += Time.fixedDeltaTime;
                if (attackTimer >= attackInterval)
                    subState = SubState.MovingToOwner;
                else
                {
                    if (attackTimer >= attackInterval * attackTime && attacked == false)
                        DoAttack();
                }
            }
            if (!ownerBodyObject)
                Destroy(gameObject);
            if (subState == SubState.MovingToOwner && transform.position == ownerBodyObject.transform.Find(ImpSorcerer.deleteWhenYouGetModel + "/Crystals").position)
            {
                ownerBodyObject.transform.Find(ImpSorcerer.deleteWhenYouGetModel + "/Crystals").gameObject.SetActive(true);
                Destroy(gameObject);
            }
        }
        public void Update()
        {
            if (cachedTargetHurtBox)
                lastKnownPosition = cachedTargetHurtBox.transform.position;
            if (subState == SubState.TelegraphAttack)
            {
                if (hasDoneEffect == false)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var offset = transform.Find("Offset" + i);
                        EffectData effectData = new EffectData
                        {
                            origin = offset.position,
                            scale = 0.6f,
                            rotation = Quaternion.Euler(90f, offset.eulerAngles.y, 0f)
                        };
                        EffectManager.SpawnEffect(effectPrefab, effectData, false);
                        hasDoneEffect = true;
                    }
                }
            }
            else
            {
                UpdateMotion();
                UpdateRotation();
                if (Vector3.Distance(targetBodyObject.GetComponent<CharacterBody>().corePosition, transform.position) <= 0.1f)
                {
                    subState = SubState.TelegraphAttack;
                    velocity = Vector3.zero;
                }
            }
        }
        private void UpdateMotion()
        {
            if (subState == SubState.MovingToTarget)
            {
                Vector3 desiredPosition = GetDesiredPosition();
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
                    transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, damping, 14f);
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, ownerBodyObject.transform.Find(ImpSorcerer.deleteWhenYouGetModel + "/Crystals").position, ref velocity, damping, 14f);
                for (int i = 0; i < 3; i++)
                {
                    var offset = gameObject.transform.Find("Offset" + i);
                    var tempVelocity = velocityOffsets;
                    offset.localPosition = Vector3.SmoothDamp(offset.position, ownerBodyObject.transform.Find(ImpSorcerer.deleteWhenYouGetModel + "/Crystals/Offset" + i).localPosition, ref tempVelocity, damping, 14f);
                    if (i == 2)
                        velocityOffsets = tempVelocity;
                }
            }
        }
        private void UpdateRotation()
        {
            if (subState == SubState.MovingToTarget)
            {
                for (int i = 0; i < 3; i++)
                {
                    var offset = gameObject.transform.Find("Offset" + i);
                    offset.forward = Vector3.RotateTowards(offset.forward, lastKnownPosition - offset.position, 180f * 0.0174532924f * Time.fixedDeltaTime, 0f);
                }
                transform.rotation = Quaternion.AngleAxis(rotationAngularVelocity * Time.deltaTime, Vector3.up) * transform.rotation;
            }
            else
            {
                transform.rotation = Quaternion.AngleAxis(rotationAngularVelocity * Time.deltaTime, Vector3.up) * transform.rotation;
                for (int i = 0; i < 3; i++)
                {
                    var offset = gameObject.transform.Find("Offset" + i);
                    offset.forward = Vector3.RotateTowards(offset.forward, ownerBodyObject.transform.Find(ImpSorcerer.deleteWhenYouGetModel + "/Crystals/Offset" + i).forward, 180f * 0.0174532924f * Time.fixedDeltaTime, 0f);
                }
            }
        }

        [Server]
        private void DoAttack()
        {
            attacked = true;
            for (int i = 0; i < 3; i++)
            {
                var offset = gameObject.transform.Find("Offset" + i);
                ProjectileManager.instance.FireProjectile(Resources.Load<GameObject>("prefabs/projectiles/GravekeeperHookProjectileSimple"), offset.position, Util.QuaternionSafeLookRotation(offset.forward), ownerBodyObject, damage, 0f, false, DamageColorIndex.Default);
            }
        }
        private Vector3 GetTargetPosition()
        {
            GameObject gameObject = targetBodyObject;
            if (!gameObject)
                return lastKnownPosition;
            CharacterBody component = gameObject.GetComponent<CharacterBody>();
            if (!component)
                return gameObject.transform.position;
            return component.corePosition;
        }
        private Vector3 GetDesiredPosition()
        {
            return GetTargetPosition();
        }
        private void OnTargetChanged()
        {
            cachedTargetHurtBox = (cachedTargetBodyObject ? cachedTargetBodyObject.GetComponent<CharacterBody>().mainHurtBox : null);
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
            OnStartClient();
            transform.position = ownerBodyObject.transform.position;
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
            OnEnter();
            animator = GetModelAnimator();
            kinematicCharacterMotor = gameObject.GetComponent<KinematicCharacterMotor>();
            characterGravityParameterProvider = gameObject.GetComponent<ICharacterGravityParameterProvider>();
            characterFlightParameterProvider = gameObject.GetComponent<ICharacterFlightParameterProvider>();
            baseAI = gameObject.GetComponent<BaseAI>();
            if (animator)
            {
                flyOverrideLayer = animator.GetLayerIndex("FlyOverride");
            }
            if (characterMotor)
            {
                characterMotor.walkSpeedPenaltyCoefficient = movementSpeedMultiplier;
            }
            if (modelLocator)
            {
                modelLocator.normalizeToFloor = false;
            }
            characterGravityParameterProvider = gameObject.GetComponent<ICharacterGravityParameterProvider>();
            characterFlightParameterProvider = gameObject.GetComponent<ICharacterFlightParameterProvider>();
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
            if (characterMotor)
            {
                characterMotor.velocity.y = launchSpeed;
                characterMotor.Motor.ForceUnground();
            }
            PlayAnimation("Body", "Jump");
            //PlayAnimation("")
            /*if (jumpEffectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(jumpEffectPrefab, gameObject, jumpEffectMuzzleString, false);
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
            OnExit();
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
            OnEnter();
            duration = baseDuration / attackSpeedStat;
            modelAnimator = GetModelAnimator();
            modelTransform = GetModelTransform();
            Ray aimRay = GetAimRay();
            StartAimMode(aimRay, 2f, false);
            characterMotor.walkSpeedPenaltyCoefficient = walkSpeedPenaltyCoefficient;
            //Util.PlayScaledSound(enterSoundString, gameObject, attackSpeedStat);
            if (modelAnimator)
            {
                PlayAnimation("Gesture, Additive", "DoubleSlash", "DoubleSlash.playbackRate", duration);
                PlayAnimation("Gesture, Override", "DoubleSlash", "DoubleSlash.playbackRate", duration);
            }
            if (isAuthority)
            {
                ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), gameObject, this.damageStat * damageCoefficient, 0f, Util.CheckRoll(this.critStat, characterBody.master), DamageColorIndex.Default, null, -1f);
            }
            if (characterBody)
            {
                characterMotor.walkSpeedPenaltyCoefficient = 1f;
                characterBody.SetAimTimer(this.duration + 2f);
            }
        }
        public override void OnExit()
        {
            OnExit();
        }

        public override void FixedUpdate()
        {
            FixedUpdate();
            if (fixedAge >= duration && isAuthority)
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
            OnEnter();
            Util.PlaySound(SorcererBlinkState.beginSoundString, gameObject);
            modelTransform = GetModelTransform();
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
            if (characterMotor)
            {
                characterMotor.enabled = false;
            }
            Vector3 b = inputBank.moveVector * SorcererBlinkState.blinkDistance;
            blinkDestination = transform.position;
            blinkStart = transform.position;
            NodeGraph groundNodes = SceneInfo.instance.airNodes;
            NodeGraph.NodeIndex nodeIndex = groundNodes.FindClosestNode(transform.position + b, characterBody.hullClassification);
            groundNodes.GetNodePosition(nodeIndex, out blinkDestination);
            blinkDestination += transform.position - characterBody.footPosition;
            CreateBlinkEffect(Util.GetCorePosition(gameObject));
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
            if (characterMotor)
            {
                characterMotor.Motor.SetPositionAndRotation(newPosition, Quaternion.identity, true);
            }
        }

        public override void FixedUpdate()
        {
            FixedUpdate();
            stopwatch += Time.fixedDeltaTime;
            if (characterMotor && characterDirection)
            {
                characterMotor.velocity = Vector3.zero;
            }
            SetPosition(Vector3.Lerp(blinkStart, blinkDestination, stopwatch / SorcererBlinkState.duration));
            if (stopwatch >= SorcererBlinkState.duration && isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            Util.PlaySound(SorcererBlinkState.endSoundString, gameObject);
            CreateBlinkEffect(Util.GetCorePosition(gameObject));
            modelTransform = GetModelTransform();
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
            if (characterMotor)
            {
                characterMotor.enabled = true;
            }
            PlayAnimation("Gesture, Additive", "BlinkEnd");
            OnExit();
        }
    }

    public class EyeAttackState : BaseState
    {
        public static float baseDuration = 3.5f;
        public static float damageCoefficient = 4f;
        public static float procCoefficient;
        public static float selfForce;
        public static float attackInterval = 3f;
        public static float forceMagnitude = 16f;
        public static GameObject hitEffectPrefab;
        public static GameObject swipeEffectPrefab;
        public static string enterSoundString;
        public static string slashSoundString;
        public static float walkSpeedPenaltyCoefficient;
        private Animator modelAnimator;
        private float duration;
        private Transform modelTransform;
        private ImpSorcererCrystalController crystalController;
        public static string deleteWhenYouGetModel = "ModelBase/mdlImp";

        public override void OnEnter()
        {
            OnEnter();
            duration = baseDuration / attackSpeedStat;
            modelAnimator = GetModelAnimator();
            modelTransform = GetModelTransform();
            Ray aimRay = GetAimRay();
            StartAimMode(aimRay, 2f, false);
            characterMotor.walkSpeedPenaltyCoefficient = walkSpeedPenaltyCoefficient;
            //Util.PlayScaledSound(enterSoundString, gameObject, attackSpeedStat);
            crystalController = gameObject.transform.Find(deleteWhenYouGetModel + "/Crystals").GetComponent<ImpSorcererCrystalController>();
            if (modelAnimator)
            {
                PlayAnimation("Gesture, Additive", "DoubleSlash", "DoubleSlash.playbackRate", duration);
                PlayAnimation("Gesture, Override", "DoubleSlash", "DoubleSlash.playbackRate", duration);
            }
            if (isAuthority && NetworkServer.active)
            {
                BullseyeSearch bullseyeSearch = new BullseyeSearch();
                bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
                if (teamComponent)
                {
                    bullseyeSearch.teamMaskFilter.RemoveTeam(teamComponent.teamIndex);
                }
                bullseyeSearch.maxDistanceFilter = 70f;
                bullseyeSearch.maxAngleFilter = 100f;
                bullseyeSearch.searchOrigin = aimRay.origin;
                bullseyeSearch.searchDirection = aimRay.direction;
                bullseyeSearch.filterByLoS = false;
                bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
                bullseyeSearch.RefreshCandidates();
                HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
                if (hurtBox)
                    crystalController.SetTarget(hurtBox.healthComponent.body.gameObject, attackSpeedStat, attackInterval, damageStat * damageCoefficient);
            }
            if (characterBody)
            {
                characterMotor.walkSpeedPenaltyCoefficient = 1f;
                characterBody.SetAimTimer(this.duration + 2f);
            }
        }
        public override void OnExit()
        {
            OnExit();
        }

        public override void FixedUpdate()
        {
            FixedUpdate();
            if (fixedAge >= duration && isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

    }
}