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
        public override GameObject bodyPrefab => PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/ImpBody"), "ImpSorcererBody");
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


        public override void CreatePrefab()
        {
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

            PrimarySetup();
        }

        public override void RegisterStates()
        {
            LoadoutAPI.AddSkill(typeof(FireSpines));
        }

        private void PrimarySetup()
        {
            SkillLocator component = bodyPrefab.GetComponent<SkillLocator>();

            LanguageAPI.Add("IMPSORCERER_PRIMARY_VOIDCLUSTER_NAME", "Void Cluster");
            LanguageAPI.Add("IMPSORCERER_PRIMARY_VOIDCLUSTER_DESCRIPTION", "");

            // set up your primary skill def here!

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(FireSpines));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 10f;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Frozen;
            mySkillDef.isBullets = false;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = true;
            mySkillDef.noSprint = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = null;
            mySkillDef.skillDescriptionToken = "IMPSORCERER_PRIMARY_VOIDCLUSTER_DESCRIPTION";
            mySkillDef.skillName = "IMPSORCERER_PRIMARY_VOIDCLUSTER_NAME";
            mySkillDef.skillNameToken = "IMPSORCERER_PRIMARY_VOIDCLUSTER_NAME";

            LoadoutAPI.AddSkillDef(mySkillDef);

            component.primary = bodyPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.primary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.primary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };

        }

        private void AddProjectiles()
        {
            GameObject projectilePrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/projectiles/CrocoSpit.prefab"), "ImpSorcererVoidClusterBomb", true);
            GameObject spikePrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/projectiles/ImpVoidspikeProjectile.prefab"), "ImpSorcererVoidClusterSpikes", true);
            // add ghost version here

            var projectileController = projectilePrefab.GetComponent<ProjectileController>();
            var simpleComponent = projectilePrefab.GetComponent<ProjectileSimple>();
            var damageComponent = projectilePrefab.GetComponent<ProjectileDamage>();
            var impactComponent = projectilePrefab.GetComponent<ProjectileImpactExplosion>();

            projectileController.allowPrediction = true;
            projectileController.shouldPlaySounds = true;


            simpleComponent.updateAfterFiring = true;
            simpleComponent.velocity = 13f;
            simpleComponent.lifetime = 8f;

            damageComponent.damageType = DamageType.BleedOnHit;


            impactComponent.lifetime = simpleComponent.lifetime;
            impactComponent.impactEffect = Resources.Load<GameObject>("prefabs/effects/ImpVoidspikeExplosion.prefab");
            impactComponent.destroyOnEnemy = false;
            impactComponent.destroyOnWorld = true;
            impactComponent.projectileHealthComponent = Resources.Load<GameObject>("prefabs/projectiles/VagrantTrackingBomb.prefab").GetComponent<ProjectileImpactExplosion>().projectileHealthComponent;
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
                //base.PlayAnimation("Gesture, Additive", "DoubleSlash", "DoubleSlash.playbackRate", duration);
                base.PlayAnimation("Gesture, Override", "DoubleSlash", "DoubleSlash.playbackRate", duration);
            }
            if (base.isAuthority)
            {
                ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, this.damageStat * damageCoefficient, 0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1f);
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
