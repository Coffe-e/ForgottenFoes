using BepInEx;
using EntityStates;
using KinematicCharacterController;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreMonsters;
using MoreMonsters.States.ImpSorcererStates;
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
            typeof(FireVoidCluster),
            typeof(SorcererBlinkState),
            typeof(EyeAttackState)
        };
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
        ///<summary>Because the FireChild stuff was designed in a completely **FUCKING STUPID** WAY AND TAKES THE Z OFFETS FOR BOTH Y AND Z OF THE VECTOR... This injects some code to make it take the y offset</summary>
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
            AddCrystals();
            AddProjectiles();
            //All the other shit that needs to go here

        }
        public override void SkillSetup()
        {
            foreach (GenericSkill obj in bodyPrefab.GetComponentsInChildren<GenericSkill>())
            {
                BaseUnityPlugin.DestroyImmediate(obj);
            }
            SecondarySetup();
            UtilitySetup();
            SpecialSetup();
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
        private void AddCrystals()
        {
            #region Stuff to be removed when model exists
            var crystals = PrefabAPI.InstantiateClone(Assets.mainAssetBundle.LoadAsset<GameObject>("CrystalFollower"), "Crystals", false);
            crystals.transform.SetParent(bodyPrefab.transform.Find("ModelBase/mdlImp"), false);
            crystals.transform.localPosition += new Vector3(0f, 1.2f);
            #endregion

            #region Rotation Constraint
            //ImpSorcererCrystalController.constraintPrefab = PrefabAPI.InstantiateClone(Assets.mainAssetBundle.LoadAsset<GameObject>("CrystalRotationController"), "ImpSorcererCrystalRotationController", true);
            #endregion

            #region Attack Crystals added
            var crystalClones = GameObject.Instantiate(bodyPrefab.transform.Find(deleteWhenYouGetModel + "/Crystals").gameObject); // When model is imported, change mdlImp to mdlImpSorcerer or else there will be a NRE!

            Transform[] offsets = new Transform[] { crystalClones.transform.Find("Offset0"), crystalClones.transform.Find("Offset1"), crystalClones.transform.Find("Offset2") };
            for (int i = 0; i < offsets.Length; i++)
            {
                var diamond = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/pickupmodels/PickupDiamond"), "Model", false);
                diamond.transform.SetParent(offsets[i], false);
                diamond.transform.localPosition += new Vector3(0, 1);
                diamond.transform.localScale *= 0.6f;
                diamond.transform.localRotation = Quaternion.identity;
                var rigidBody = offsets[i].gameObject.AddComponent<Rigidbody>();
                rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidBody.mass = 30f;
                rigidBody.drag = 0.3f;
                rigidBody.angularDrag = 0.2f;
                rigidBody.maxAngularVelocity = 180f * Mathf.Deg2Rad;
                rigidBody.isKinematic = true;
                rigidBody.useGravity = false;
            }

            var attackManager = crystalClones.AddComponent<ImpSorcererCrystalAttackManager>();
            attackManager.enabled = false;
            crystalClones.AddComponent<ImpSorcererCrystalMovementManager>();
            crystalClones.AddComponent<NetworkIdentity>(); //networkidentity should default to server only(?)
            crystalClones = PrefabAPI.InstantiateClone(crystalClones, "Attack Crystals", true);
            ImpSorcererCrystalController.clonePrefab = crystalClones;
            #endregion

            var crystalController = bodyPrefab.AddComponent<ImpSorcererCrystalController>();

            #region Adding Effects
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
                    transform.forward = Vector3.RotateTowards(transform.forward, vector, rotationSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
                    model.forward = Vector3.RotateTowards(model.forward, -vector, rotationSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
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
        private GameObject crystals;
        private GameObject crystalClones;
        private Transform constraint;
        private ImpSorcererCrystalMovementManager movementManager;
        private ImpSorcererCrystalAttackManager attackManager;
        private Vector3 velocity = Vector3.zero;
        public MovementType movementType = MovementType.BasicFollowing;
        public enum MovementType
        {
            //This will contain all of the movement types that the crystals may have while doing stuff
            BasicFollowing,
            Attacking,
        }
        private void Start()
        {
            crystals = gameObject.transform.Find(ImpSorcerer.deleteWhenYouGetModel + "/Crystals").gameObject;
            crystalClones = Instantiate(clonePrefab, crystals.transform.position, crystals.transform.rotation);
            movementManager = crystalClones.GetComponent<ImpSorcererCrystalMovementManager>();
            attackManager = crystalClones.GetComponent<ImpSorcererCrystalAttackManager>();

            movementManager.originalCrystalsObject = crystals;



            //Instantiates the constraint
            constraint = new GameObject().transform;
            var rotationConstraint = crystals.GetComponent<RotationConstraint>();
            var constraintSource = new ConstraintSource
            {
                sourceTransform = constraint,
                weight = 1f
            };
            rotationConstraint.AddSource(constraintSource);
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
            Vector3 desiredPosition = crystals.transform.parent.position;
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
                    /*var aimRay = parentBaseObject.GetComponent<InputBankTest>().GetAimRay();
                    for (int i = 0; i < 3; i++)
                    {
                        var offset = transform.Find("Offset" + i);
                        offset.forward = Vector3.RotateTowards(offset.forward, aimRay.direction, 180f * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
                    }
                    transform.rotation = Quaternion.AngleAxis(rotationAngularVelocity * Time.deltaTime, Vector3.up) * transform.rotation;*/
                    constraint.rotation = Quaternion.AngleAxis(rotationAngularVelocity * Time.deltaTime, Vector3.up) * constraint.rotation;
                    break;
            }
        }

        public void SetTarget(GameObject target, float attackSpeed, float attackInterval, float damage)
        {
            if (movementType == MovementType.Attacking)
                return;
            movementType = MovementType.Attacking;
            movementManager.enabled = false;



            attackManager.ownerBodyObject = gameObject;
            attackManager.targetBodyObject = target;

            attackManager.attackSpeed = attackSpeed;
            attackManager.attackInterval = attackInterval;
            attackManager.damage = damage;
            attackManager.enabled = true;
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
        public float rotationAngularVelocity = 30f;
        public float acceleration = 15f;
        public float damping = 0.3f;
        public bool enableSpringMotion = false;

        // attack timer only starts ticking down once they get close enough to the target
        private float attackTimer = 0f;
        private float attackTime = 0.75f;
        private bool isTelegraphing = false;
        private bool attacked = false;
        private bool hasDoneEffect = false;
        private float angle = 2.0944f;
        private Vector3 velocity = Vector3.zero;
        private Vector3 lastKnownPosition;
        private GameObject cachedTargetBodyObject;
        private HurtBox cachedTargetHurtBox;
        private GameObject originalCrystalsObject;
        private Transform[] crystals = new Transform[3];
        private Rigidbody[] rigidBodies = new Rigidbody[3];
        private NetworkInstanceId ___ownerBodyObjectNetId;
        private NetworkInstanceId ___targetBodyObjectNetId;

        private void Start()
        {
            originalCrystalsObject = GetComponent<ImpSorcererCrystalMovementManager>().originalCrystalsObject;
            crystals = GetComponent<ImpSorcererCrystalMovementManager>().crystals;
            rigidBodies = GetComponent<ImpSorcererCrystalMovementManager>().rigidBodies;
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

            if (cachedTargetHurtBox)
                lastKnownPosition = cachedTargetHurtBox.transform.position;

            if (isTelegraphing)
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
                if (Vector3.SqrMagnitude(lastKnownPosition - transform.position) <= 0.0001f)
                {
                    isTelegraphing = true;
                    velocity = Vector3.zero;
                }
            }
        }
        public void FixedUpdateServer()
        {
            if (isTelegraphing)
            {
                attackTimer += Time.fixedDeltaTime;
                if (attackTimer >= attackInterval)
                {
                    ownerBodyObject.GetComponent<ImpSorcererCrystalController>().movementType = ImpSorcererCrystalController.MovementType.BasicFollowing;
                    GetComponent<ImpSorcererCrystalMovementManager>().enabled = true;
                    ResetStats();
                    enabled = false;
                }
                else
                {
                    if (attackTimer >= attackInterval * attackTime && attacked == false)
                        DoAttack();
                }
            }
            if (!ownerBodyObject)
                Destroy(gameObject);
        }
        private void UpdateMotion()
        {
            Vector3 desiredPosition = GetDesiredPosition();
            angle = (angle + rotationAngularVelocity * Mathf.Deg2Rad * Time.fixedDeltaTime) % (2f * Mathf.PI);
            for (int i = 1; i < 4; i++)
            {
                rigidBodies[i - 1].MovePosition(transform.position + 2f * new Vector3(Mathf.Cos(angle * i), 0f, Mathf.Sin(angle * i)));
            }
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, damping, 14f);
        }
        private void UpdateRotation()
        {
            for (int i = 0; i < 3; i++)
            {
                crystals[i].forward = Vector3.RotateTowards(crystals[i].forward, lastKnownPosition - crystals[i].position, 180f * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
            }
            transform.rotation = Quaternion.AngleAxis(rotationAngularVelocity * Time.deltaTime, Vector3.up) * transform.rotation;
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
        private void ResetStats()
        {
            attackTimer = 0f;
            isTelegraphing = false;
            attacked = false;
            hasDoneEffect = false;
            angle = 2.0944f;
            velocity = Vector3.zero;
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
            //sets the crystal parent object position/rotation to the original without changing the position of the other crystals
            for (int i = 0; i < 3; i++)
                crystals[i].SetParent(originalCrystalsObject.transform, true);
            transform.position = originalCrystalsObject.transform.position;
            transform.rotation = originalCrystalsObject.transform.rotation;
            for (int i = 0; i < 3; i++)
                crystals[i].SetParent(transform, true);
        }
    }

    /*public class ImpSorcererCrystalAttackManager : MonoBehaviour
    {
        public static GameObject effectPrefab;
        public GameObject ownerBodyObject;
        public GameObject targetBodyObject;

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
        private bool isTelegraphing = false;
        private bool attacked = false;
        private bool hasDoneEffect = false;
        private float angle = 2.0944f;
        private Vector3 velocity = Vector3.zero;
        private Vector3 lastKnownPosition;
        private GameObject cachedTargetBodyObject;
        private HurtBox cachedTargetHurtBox;
        private GameObject originalCrystalsObject;
        private Transform[] crystals = new Transform[3];
        private Rigidbody[] rigidBodies = new Rigidbody[3];


        private void Start()
        {
            originalCrystalsObject = GetComponent<ImpSorcererCrystalMovementManager>().originalCrystalsObject;
            crystals = GetComponent<ImpSorcererCrystalMovementManager>().crystals;
            rigidBodies = GetComponent<ImpSorcererCrystalMovementManager>().rigidBodies;

            //sets the crystal parent object position/rotation to the original without changing the position of the other crystals
            for (int i = 0; i < 3; i++)
                crystals[i].SetParent(originalCrystalsObject.transform, true);
            transform.position = originalCrystalsObject.transform.position;
            transform.rotation = originalCrystalsObject.transform.rotation;
            for (int i = 0; i < 3; i++)
                crystals[i].SetParent(transform, true);
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

            if (cachedTargetHurtBox)
                lastKnownPosition = cachedTargetHurtBox.transform.position;
            if (isTelegraphing)
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
                if ((targetBodyObject.GetComponent<CharacterBody>().corePosition - transform.position).sqrMagnitude <= 0.01f)
                {
                    isTelegraphing = true;
                    velocity = Vector3.zero;
                }
            }
        }
        public void FixedUpdateServer()
        {
            if (isTelegraphing)
            {
                attackTimer += Time.fixedDeltaTime;
                if (attackTimer >= attackInterval)
                {
                    originalCrystalsObject.GetComponent<ImpSorcererCrystalController>().movementType = ImpSorcererCrystalController.MovementType.BasicFollowing;
                    GetComponent<ImpSorcererCrystalMovementManager>().enabled = true;
                    ResetStats();
                    enabled = false;
                }
                else
                {
                    if (attackTimer >= attackInterval * attackTime && attacked == false)
                        DoAttack();
                }
            }
            if (!ownerBodyObject)
                Destroy(gameObject);
        }
        private void UpdateMotion()
        {
            Vector3 desiredPosition = GetDesiredPosition();
            angle = (angle + rotationAngularVelocity * Mathf.Deg2Rad * Time.fixedDeltaTime) % (2f * Mathf.PI);
            for (int i = 1; i < 4; i++)
            {

                rigidBodies[i - 1].MovePosition(transform.position + 2f * new Vector3(Mathf.Cos(angle * i), 0f, Mathf.Sin(angle * i)));
            }
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, damping, 14f);
        }
        private void UpdateRotation()
        {
            for (int i = 0; i < 3; i++)
            {
                rigidBodies[i].MoveRotation(Quaternion.LookRotation(Vector3.RotateTowards(crystals[i].forward, lastKnownPosition - crystals[i].position, 180f * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f)));
            }
            transform.rotation = Quaternion.AngleAxis(rotationAngularVelocity * Time.deltaTime, Vector3.up) * transform.rotation;
        }

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
        private void ResetStats()
        {
            attackTimer = 0f;
            attackTime = 0.75f;
            isTelegraphing = false;
            attacked = false;
            hasDoneEffect = false;
            angle = 2.0944f;
            velocity = Vector3.zero;
        }
        private void OnTargetChanged()
        {
            cachedTargetHurtBox = (cachedTargetBodyObject ? cachedTargetBodyObject.GetComponent<CharacterBody>().mainHurtBox : null);
        }
    }*/

    public class ImpSorcererCrystalMovementManager : MonoBehaviour
    {
        public GameObject originalCrystalsObject;
        public float rotationAngularVelocity = 30f;
        public float acceleration = 20f;
        public float damping = 0.3f;
        public bool enableSpringMotion = false;
        private Vector3 velocity = Vector3.zero;
        public Transform[] crystals = new Transform[3];
        public Rigidbody[] rigidBodies = new Rigidbody[3];
        public Transform[] originalCrystals = new Transform[3];

        private void Start()
        {
            for (int i = 0; i < 3; i++)
            {
                var offset = gameObject.transform.Find("Offset" + i);
                crystals[i] = offset;
                rigidBodies[i] = offset.GetComponent<Rigidbody>();
                originalCrystals[i] = originalCrystalsObject.transform.Find("Offset" + i);
            }
        }
        private void Update()
        {
            if (!originalCrystalsObject)
                Destroy(gameObject);
        }
        public void FixedUpdate()
        {
            //This is some janky ass code that makes it so this game object always stays with the original one while keeping its children at their position
            /*transform.DetachChildren();
            transform.position = originalCrystalsObject.transform.position;
            transform.rotation = originalCrystalsObject.transform.rotation;
            foreach (Transform crystal in crystals)
                crystal.SetParent(transform, true);*/

            UpdateMotion();
            UpdateRotation();
        }
        private void UpdateMotion()
        {
            var desiredPosition = (originalCrystalsObject.transform.position - gameObject.transform.position) / 1.2f + gameObject.transform.position;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, damping, 14f);
            for (int i = 0; i < 3; i++)
            {
                //crystals[i].localPosition = Vector3.SmoothDamp(crystals[i].localPosition, originalCrystalsObject.transform.Find("Offset" + i).localPosition, ref velocityOffsets[i], damping, 14f);
                var position = (originalCrystals[i].position - rigidBodies[i].position) / 1.2f + rigidBodies[i].position;
                rigidBodies[i].MovePosition(position);
            }
        }
        private void UpdateRotation()
        {
            gameObject.transform.forward = Vector3.RotateTowards(gameObject.transform.forward, originalCrystalsObject.transform.forward, 30f * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
            for (int i = 0; i < 3; i++)
            {
                //crystals[i].forward = Vector3.RotateTowards(crystals[i].forward, originalCrystalsObject.transform.Find("Offset" + i).forward, 180f * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
                rigidBodies[i].MoveRotation(originalCrystals[i].rotation);
            }
        }
    }
}


namespace MoreMonsters.States.ImpSorcererStates
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
                bullseyeSearch.filterByLoS = true;
                bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
                bullseyeSearch.RefreshCandidates();
                HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
                if (hurtBox)
                {
                    gameObject.GetComponent<ImpSorcererCrystalController>().SetTarget(hurtBox.healthComponent.body.gameObject, attackSpeedStat, duration, damageStat * damageCoefficient);
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