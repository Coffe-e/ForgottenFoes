using BepInEx;
using EntityStates;
using EntityStates.MoreMonsters.ImpSorcerer;
using KinematicCharacterController;
using MoreMonsters.Utils;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Navigation;
using RoR2.Projectile;
using RoR2.Skills;
using RoR2.CharacterAI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
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

        GameObject projectilePrefab;
        GameObject spikePrefab;
        SkillDef skillDefSecondary;
        public override void CreatePrefab()
        {
            bodyPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/ImpBody"), "ImpSorcererBody");
            var wispPrefab = Resources.Load<GameObject>("Prefabs/CharacterBodies/WispBody");

            var entitystatemachine = bodyPrefab.GetComponents<EntityStateMachine>();
            EntityStateMachine Fuckingshit = null;
            foreach(EntityStateMachine stateMachine in entitystatemachine)
            {
                if (stateMachine.customName == "body")
                    Fuckingshit = stateMachine;
            }
            var temp = Fuckingshit.initialStateType;
            //UnityEngine.Object.Destroy(bodyPrefab.GetComponent<EntityStateMachine>());

            var entitystatemachine1 = bodyPrefab.GetComponents<EntityStateMachine>();
            EntityStateMachine Fuckingass = null;
            foreach (EntityStateMachine stateMachine in entitystatemachine1)
            {
                if (stateMachine.customName == "body")
                    Fuckingass = stateMachine;
            }
            Fuckingshit = Fuckingass;
            Fuckingshit.initialStateType = temp;



            //GameObject model = CreateModel(bodyPrefab);
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
        }

        public override void RegisterStates()
        {
            LoadoutAPI.AddSkill(typeof(FireSpines));
        }

        public override void CreateMaster()
        {
            masterPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterMasters/ImpMaster"), "ImpSorcererMaster");
            CharacterMaster cm = masterPrefab.GetComponent<CharacterMaster>();
            cm.bodyPrefab = bodyPrefab;

            Component[] toDelete = masterPrefab.GetComponents<AISkillDriver>();
            foreach (AISkillDriver asd in toDelete)
            {
                UnityEngine.Object.Destroy(asd);
            }

            AISkillDriver ass = masterPrefab.AddComponent<AISkillDriver>();
            ass.skillSlot = SkillSlot.Secondary;
            ass.requiredSkill = skillDefSecondary;
            ass.requireSkillReady = false;
            ass.requireEquipmentReady = false;
            ass.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            ass.minDistance = 35f;
            ass.maxDistance = float.PositiveInfinity;
            ass.selectionRequiresTargetLoS = true;
            ass.activationRequiresTargetLoS = true;
            ass.activationRequiresAimConfirmation = true;
            ass.movementType = AISkillDriver.MovementType.StrafeMovetarget;
            ass.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            ass.ignoreNodeGraph = false;
            ass.driverUpdateTimerOverride = 2f;
            ass.noRepeat = false;
            ass.shouldSprint = false;
            ass.shouldFireEquipment = false;
            ass.shouldTapButton = false;

        }

        private void SecondarySetup()
        {
            SkillLocator component = bodyPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("IMPSORCERER_SECONDARY_VOIDCLUSTER_NAME", "Void Cluster");
            LanguageAPI.Add("IMPSORCERER_SECONDARY_VOIDCLUSTER_DESCRIPTION", "");

            // set up your secondary skill def here!

            skillDefSecondary = ScriptableObject.CreateInstance<SkillDef>();
            skillDefSecondary.activationState = new SerializableEntityStateType(typeof(FireSpines));
            skillDefSecondary.activationStateMachineName = "Weapon";
            skillDefSecondary.baseMaxStock = 1;
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

        private void AddProjectiles()
        {
            projectilePrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/CrocoSpit"), "ImpSorcererVoidClusterBomb", true);
            spikePrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/ImpVoidspikeProjectile"), "ImpSorcererVoidClusterSpikes", true);
            // add ghost version here

            var projectileController = projectilePrefab.GetComponent<ProjectileController>();
            var simpleComponent = projectilePrefab.GetComponent<ProjectileSimple>();
            var damageComponent = projectilePrefab.GetComponent<ProjectileDamage>();
            var impactComponent = projectilePrefab.GetComponent<ProjectileImpactExplosion>();

            projectileController.allowPrediction = true;
            projectileController.shouldPlaySounds = true;



            simpleComponent.updateAfterFiring = true;
            simpleComponent.velocity = 13f;
            simpleComponent.lifetime = 6f;

            damageComponent.damageType = DamageType.BleedOnHit;


            impactComponent.lifetime = simpleComponent.lifetime;
            impactComponent.impactEffect = Resources.Load<GameObject>("prefabs/effects/ImpVoidspikeExplosion");
            impactComponent.destroyOnEnemy = false;
            impactComponent.destroyOnWorld = true;
            impactComponent.projectileHealthComponent = Resources.Load<GameObject>("prefabs/projectiles/VagrantTrackingBomb").GetComponent<ProjectileImpactExplosion>().projectileHealthComponent;
            impactComponent.blastRadius = 2f;
            // These make it so the children always shoot downwards. Angle range still needs to be found!
            impactComponent.transformSpace = ProjectileImpactExplosion.TransformSpace.World;
            Vector3 desiredDirection = new Vector3(0f, 0f, 0f);
            impactComponent.maxAngleOffset = desiredDirection - new Vector3(30f, 30f, 30f);
            impactComponent.minAngleOffset = desiredDirection;


            /* These need to be added once sound is done
            impactComponent.offsetForLifeTimeExpiredSound; //Subtracts this number from the lifetime to make the sound play early
            impactComponent.explosionSoundString;
            impactComponent.lifetimeExpiredSound;*/

            //Do voidspike mods here

            impactComponent.childrenCount = 5;
            impactComponent.childrenProjectilePrefab = spikePrefab;
            impactComponent.fireChildren = true;
            ProjectileCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(projectilePrefab);
                list.Add(spikePrefab);
            };
            FireSpines.projectilePrefab = projectilePrefab;
        }

    }
}

namespace EntityStates.MoreMonsters.ImpSorcerer
{

    public class FireSpines : BaseState
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
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }



    }


}
