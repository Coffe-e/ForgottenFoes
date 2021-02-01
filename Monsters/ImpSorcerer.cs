using BepInEx;
using EntityStates;
using KinematicCharacterController;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreMonsters;
using MoreMonsters.EntityStates.ImpSorcerer;
using MoreMonsters.Utils;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
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
using UnityEngine.Animations;


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
        SkillDef skillDefPrimary;
        SkillDef skillDefSecondary;

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

        ///<summary>Because this was designed in a completely **FUCKING STUPID** WAY AND TAKES THE Z OFFETS FOR BOTH Y AND Z OF THE VECTOR, this injects some code to make it take the y offset</summary>
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
            {
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
                cpt1.characterDirection = cpt0;
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
            }



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
            SecondarySetup();
        }
        public override void RegisterStates()
        {
            LoadoutAPI.AddSkill(typeof(FireSpines));
            LoadoutAPI.AddSkill(typeof(Fly));
        }

        public override void CreateMaster()
        {
            masterPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterMasters/ImpMaster"), "ImpSorcererMaster");
            CharacterMaster cm = masterPrefab.GetComponent<CharacterMaster>();
            cm.bodyPrefab = bodyPrefab;

            var baseAI = masterPrefab.GetComponent<BaseAI>();
            baseAI.minDistanceFromEnemy = 55f;
            baseAI.stateMachine = masterPrefab.GetComponent<EntityStateMachine>();
            baseAI.enemyAttentionDuration = 7f;

            Component[] toDelete = masterPrefab.GetComponents<AISkillDriver>();
            foreach (AISkillDriver asd in toDelete)
            {
                switch (asd.customName)
                {
                    case "StrafeBecausePrimaryIsntReady":
                        asd.customName = "FleeBecauseCantAttack";
                        asd.maxDistance = 55f;
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
            skillDefSecondary.activationState = new SerializableEntityStateType(typeof(FireSpines));
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


        private void AddProjectiles()
        {
            projectilePrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/VagrantTrackingBomb"), "ImpSorcererVoidClusterBomb", true);
            spikePrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/ImpVoidspikeProjectile"), "ImpSorcererVoidClusterSpikes", true);
            // add ghost version here
            //adds the halo
            var haloPrefab = PrefabAPI.InstantiateClone(Assets.mainAssetBundle.LoadAsset<GameObject>("ImpSorcererHalo"), "ImpSorcererHalo", false);
            var material = Assets.mainAssetBundle.LoadAsset<Material>("ImpMat");
            haloPrefab.transform.SetParent(projectilePrefab.transform);
            haloPrefab.transform.localPosition = Vector3.zero;
           //haloPrefab.transform.scale *= 3f;
            var cone = haloPrefab.transform.Find("cone").gameObject;
            var haloMeshRenderer = cone.GetComponent<MeshRenderer>();
            haloMeshRenderer.material = spikePrefab.transform.Find("ImpactEffect").Find("AreaIndicator").GetComponent<MeshRenderer>().material;
            haloMeshRenderer.sharedMaterial = spikePrefab.transform.Find("ImpactEffect").Find("AreaIndicator").GetComponent<MeshRenderer>().material;
            //var indicatorOrientator = haloPrefab.AddComponent<ProjectileIndicatorOrientator>();


            //var haloCurve = cone.AddComponent<ObjectScaleCurve>();

            //haloCurve = spikePrefab.transform.Find("ImpactEffect").Find("AreaIndicator").GetComponent<ObjectScaleCurve>();

            UnityEngine.Object.Destroy(projectilePrefab.GetComponent<ProjectileDirectionalTargetFinder>());
            UnityEngine.Object.Destroy(projectilePrefab.GetComponent<ProjectileSteerTowardTarget>());

            var projectileController = projectilePrefab.GetComponent<ProjectileController>();
            var simpleComponent = projectilePrefab.GetComponent<ProjectileSimple>();
            var damageComponent = projectilePrefab.GetComponent<ProjectileDamage>();
            var targetingComponent = projectilePrefab.AddComponent<ProjectileSphereTargetFinder>();
            var steerComponent = projectilePrefab.AddComponent<ProjectileSteerAboveTarget>();
            var impactComponent = projectilePrefab.GetComponent<ProjectileImpactExplosion>();

            projectileController.allowPrediction = true;
            projectileController.shouldPlaySounds = true;

            simpleComponent.updateAfterFiring = true;
            simpleComponent.velocity = 13f;
            simpleComponent.lifetime = 99f;

            damageComponent.damageType = DamageType.BleedOnHit;

            targetingComponent.lookRange = 400f;
            targetingComponent.onlySearchIfNoTarget = true;
            targetingComponent.allowTargetLoss = false;
            targetingComponent.flierAltitudeTolerance = float.PositiveInfinity;

            steerComponent.rotationSpeed = 720f;
            steerComponent.yAxisOnly = false;

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

            /* These need to be added once sound is done
            impactComponent.offsetForLifeTimeExpiredSound; //Subtracts this number from the lifetime to make the sound play early
            impactComponent.explosionSoundString;
            impactComponent.lifetimeExpiredSound;*/

            //Do voidspike mods here

            impactComponent.childrenCount = 8;
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



    public class ProjectileSteerAboveTarget : MonoBehaviour
    {
        public bool yAxisOnly;
        public float rotationSpeed;
        private new Transform transform;
        private ProjectileTargetComponent targetComponent;
        private ProjectileSimple projectileSimple;
        private Transform transformHalo;
        private static Quaternion down;
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
            transformHalo = transform.Find("ImpSorcererHalo");
            MoreMonsters._logger.LogWarning(transformHalo.rotation);
            down = new Quaternion(0f, 0f, 0f, 1f);
        }

        // Token: 0x06002700 RID: 9984 RVA: 0x000A3934 File Offset: 0x000A1B34
        private void FixedUpdate()
        {
            if (targetComponent.target)
            {
                Vector3 vector = targetComponent.target.transform.position + new Vector3(0f, 10f) - transform.position;
                if (Mathf.Abs(vector.y) < 1f)
                    vector.y = 0f;
                if (vector != Vector3.zero)
                    transform.forward = Vector3.RotateTowards(transform.forward, vector, rotationSpeed * 0.0174532924f * Time.fixedDeltaTime, 0f);
                if (Mathf.Abs(vector.x) < 1f && Mathf.Abs(vector.z) < 1f && projectileSimple.velocity > 0.25f && vector.y > -1f)
                    projectileSimple.velocity -= 0.25f;
                else
                if (projectileSimple.velocity < 13f)
                    projectileSimple.velocity += 1f;
                if (projectileSimple.velocity > 13f)
                    projectileSimple.velocity = 13f;
            }
            //transformHalo.rotation = new Quaternion(0f, transformHalo.rotation.y, transformHalo.rotation.z, transformHalo.rotation.w);
            //transformHalo.position = transform.position;
        }

    }

    public class ProjectileIndicatorOrientator : MonoBehaviour
    {
        public Quaternion original;
        private void FixedUpdate()
        {
            gameObject.transform.eulerAngles = new Vector3(0f, 0f, 0f);
        }
    }
}




namespace MoreMonsters.EntityStates.ImpSorcerer
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
            if (base.fixedAge >= duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }
    }

    public class Poop : BaseSkillState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            //animator = base.GetModelAnimator();
            /*if (animator)
            {
                flyOverrideLayer = animator.GetLayerIndex("FlyOverride");
            }*/
            if (base.modelLocator)
            {
                base.modelLocator.normalizeToFloor = false;
            }
            //Util.PlaySound(enterSoundString, base.gameObject);
        }

        public override void Update()
        {
            base.Update();
            /*if (animator)
            {
                animator.SetLayerWeight(flyOverrideLayer, Util.Remap(Mathf.Clamp01(base.age / mecanimTransitionDuration), 0f, 1f, 1f - flyOverrideMecanimLayerWeight, flyOverrideMecanimLayerWeight));
            }*/
        }

        public override void OnExit()
        {
            if (base.characterMotor)
            {
                base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
            }
            base.OnExit();
        }

        public float mecanimTransitionDuration;
        public float flyOverrideMecanimLayerWeight;
        public float movementSpeedMultiplier;
        public string enterSoundString;
        protected Animator animator;
        protected int flyOverrideLayer;
    }


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


}
