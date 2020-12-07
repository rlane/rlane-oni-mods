// TODO: Play "on" animation.
// TODO: Add a kill counter.

using System.Collections.Generic;
using TUNING;
using UnityEngine;
using System.Linq;
using FMOD.Studio;
using KSerialization;

namespace rlane
{
    public class MeteorDefenseLaserConfig : IBuildingConfig
    {
        public const string ID = "MeteorDefenseLaser";
        private const int RANGE = 40;
        private const float LASER_ELECTRICITY_CONSUMPTION = 10e3f;
        private const float LASER_HEAT_PRODUCTION = LASER_ELECTRICITY_CONSUMPTION * 1000;
        private const float ELECTRICITY_STORAGE_SECONDS = 3;

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef("MeteorDefenseLaser", 2, 2, "auto_miner_kanim", 10, 10f, BUILDINGS.CONSTRUCTION_MASS_KG.TIER3, MATERIALS.REFINED_METALS, 1600f, BuildLocationRule.OnFoundationRotatable, BUILDINGS.DECOR.PENALTY.TIER2, NOISE_POLLUTION.NOISY.TIER0);
            buildingDef.Floodable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = 1000f;
            buildingDef.ExhaustKilowattsWhenActive = 0f;
            buildingDef.SelfHeatKilowattsWhenActive = 10f;
            buildingDef.PermittedRotations = PermittedRotations.R360;
            buildingDef.Entombable = true;
            buildingDef.Floodable = true;
            buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.SolidConveyorIDs, "AutoMiner");
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<Operational>();
            go.AddOrGet<LoopingSounds>();
            go.AddOrGet<EnergyConsumerSelfSustaining>();
            go.AddOrGet<KSelectable>();
            go.AddOrGet<LogicOperationalController>();
            var defender = go.AddOrGet<MeteorDefenseLaser>();
            defender.range = RANGE;
            defender.laser_heat_production = LASER_HEAT_PRODUCTION;
            defender.laser_electricity_consumption = LASER_ELECTRICITY_CONSUMPTION;
            defender.electricity_capacity = LASER_ELECTRICITY_CONSUMPTION * ELECTRICITY_STORAGE_SECONDS;
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            AddVisualizer(go, movable: true);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            AddVisualizer(go, movable: false);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            AddVisualizer(go, movable: false);
        }

        private static void AddVisualizer(GameObject prefab, bool movable)
        {
            StationaryChoreRangeVisualizer stationaryChoreRangeVisualizer = prefab.AddOrGet<StationaryChoreRangeVisualizer>();
            stationaryChoreRangeVisualizer.x = -RANGE;
            stationaryChoreRangeVisualizer.y = 0;
            stationaryChoreRangeVisualizer.width = RANGE * 2;
            stationaryChoreRangeVisualizer.height = RANGE;
            stationaryChoreRangeVisualizer.vision_offset = new CellOffset(0, 1);
            stationaryChoreRangeVisualizer.movable = movable;
            stationaryChoreRangeVisualizer.blocking_tile_visible = false;
            KPrefabID component = prefab.GetComponent<KPrefabID>();
            component.instantiateFn += delegate (GameObject go)
            {
                go.GetComponent<StationaryChoreRangeVisualizer>().blocking_cb = (int cell) => Grid.VisibleBlockingCB(cell) || (Vector2.Distance(go.transform.position, Grid.CellToPosCCC(cell, Grid.SceneLayer.NoLayer)) > RANGE);
            };
        }
    }

    public class CometTracker
    {
        public HashSet<Comet> comets = new HashSet<Comet>();

        public void Add(Comet comet)
        {
            comets.Add(comet);
        }
        public void Remove(Comet comet)
        {
            comets.Remove(comet);
        }

        public Comet GetClosestComet(Vector2 position, float range)
        {
            Comet result = null;
            float mindist = range;
            foreach (var comet in comets)
            {
                var dist = Vector2.Distance(comet.transform.position, position);
                if (dist < mindist)
                {
                    result = comet;
                    mindist = dist;
                }
            }
            return result;
        }
    }

    public class TrackedComet : KMonoBehaviour
    {
        protected override void OnSpawn()
        {
            Debug.Assert(MeteorDefenseLaser.comet_tracker != null);
            Debug.Assert(this.GetComponent<Comet>() != null);
            MeteorDefenseLaser.comet_tracker.Add(this.GetComponent<Comet>());
        }

        protected override void OnCleanUp()
        {
            Debug.Assert(MeteorDefenseLaser.comet_tracker != null);
            Debug.Assert(this.GetComponent<Comet>() != null);
            MeteorDefenseLaser.comet_tracker.Remove(this.GetComponent<Comet>());
        }
    }

    public class MeteorDefenseLaser : KMonoBehaviour, ISim33ms
    {
        public static CometTracker comet_tracker = new CometTracker();
        public static StatusItem charge_status;
        public static StatusItem kills_status;

        [MyCmpGet]
        private Rotatable rotatable;

        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private KSelectable selectable;

        [MyCmpReq]
        private EnergyConsumerSelfSustaining energyConsumer;

        public float range;
        public bool firing = false;
        public float laser_heat_production;
        public float laser_electricity_consumption;
        private KBatchedAnimController arm_anim_ctrl;
        public GameObject arm_go;
        private KAnimLink link;
        public GameObject[] beam_segs = new GameObject[20];
        const float beam_seg_length = 2.0f;
        private float arm_rot = 90f;
        private float turn_rate = 360f;
        public float electricity_capacity;
        [Serialize]
        private float electricity_available;
        private string rotateSound = "AutoMiner_rotate";
        private bool rotate_sound_playing = false;
        private LoopingSounds looping_sounds;
        public float overkill_time = 0.2f;
        public float overkill_time_left;
        public Color beam_tint = new Color(1.0f, 0.5f, 0.5f, 1.0f);
        public Comet target = null;
        [Serialize]
        public int kills = 0;

        static Vector3 Vec3To2D(Vector3 v)
        {
            v.z = 0;
            return v;
        }
        public static StatusItem MakeChargeStatusItem()
        {
            var s = new StatusItem("LaserStoredCharge", "BUILDING", string.Empty, StatusItem.IconType.Info, NotificationType.Neutral, allow_multiples: false, OverlayModes.None.ID);
            s.resolveStringCallback = delegate (string str, object data)
            {
                MeteorDefenseLaser obj = (MeteorDefenseLaser)data;
                if (obj != null)
                {
                    str = string.Format(str, GameUtil.GetFormattedRoundedJoules(obj.electricity_available), GameUtil.GetFormattedRoundedJoules(obj.electricity_capacity));
                }
                return str;
            };
            return s;
        }

        public static StatusItem MakeKillsStatusItem()
        {
            var s = new StatusItem("LaserKills", "BUILDING", string.Empty, StatusItem.IconType.Info, NotificationType.Neutral, allow_multiples: false, OverlayModes.None.ID);
            s.resolveStringCallback = delegate (string str, object data)
            {
                MeteorDefenseLaser obj = (MeteorDefenseLaser)data;
                if (obj != null)
                {
                    str = string.Format(str, obj.kills);
                }
                return str;
            };
            return s;
        }

        public void InitializeStatusItems()
        {
            if (charge_status == null)
            {
                charge_status = MakeChargeStatusItem();
            }
            if (kills_status == null)
            {
                kills_status = MakeKillsStatusItem();
            }
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            InitializeStatusItems();
        }

        protected override void OnSpawn()
        {
            KBatchedAnimController component = GetComponent<KBatchedAnimController>();
            component.TintColour = new Color(0.5f, 0.5f, 1.0f, 1.0f);
            string name = component.name + ".gun";
            arm_go = new GameObject(name);
            arm_go.SetActive(value: false);
            arm_go.transform.parent = component.transform;
            looping_sounds = arm_go.AddComponent<LoopingSounds>();
            rotateSound = GlobalAssets.GetSound(rotateSound);
            KPrefabID kPrefabID = arm_go.AddComponent<KPrefabID>();
            kPrefabID.PrefabTag = new Tag(name);
            arm_anim_ctrl = arm_go.AddComponent<KBatchedAnimController>();
            arm_anim_ctrl.AnimFiles = new KAnimFile[1] { component.AnimFiles[0] };
            arm_anim_ctrl.initialAnim = "gun";
            arm_anim_ctrl.isMovable = true;
            arm_anim_ctrl.sceneLayer = Grid.SceneLayer.TransferArm;
            arm_anim_ctrl.TintColour = new Color(0.5f, 0.5f, 1.0f, 1.0f);
            component.SetSymbolVisiblity("gun_target", is_visible: false);
            bool symbolVisible;
            Vector4 column = component.GetSymbolTransform(new HashedString("gun_target"), out symbolVisible).GetColumn(3);
            Vector3 position = column;
            position.z = Grid.GetLayerZ(Grid.SceneLayer.TransferArm);
            arm_go.transform.SetPosition(position);
            arm_go.SetActive(value: true);
            link = new KAnimLink(component, arm_anim_ctrl);
            SetupBeam();
            RotateArm(rotatable.GetRotatedOffset(Quaternion.Euler(0f, 0f, -arm_rot) * Vector3.up), warp: true, 0f);
            energyConsumer.UpdatePoweredStatus();
            operational.SetActive(true);
            selectable.AddStatusItem(charge_status, this);
            selectable.AddStatusItem(kills_status, this);
        }

        public void SetupBeam()
        {
            for (int i = 0; i < beam_segs.Length; i++)
            {
                var beam_seg = new GameObject(arm_anim_ctrl.name + ".beam.seg");
                beam_seg.transform.SetParent(arm_go.transform);
                beam_seg.transform.localPosition = new Vector3(0.0f, beam_seg_length * i, 0.0f);
                beam_seg.transform.localRotation = Quaternion.Euler(0f, 0f, 90);
                beam_seg.SetActive(value: false);
                var beam_anim_ctrl = beam_seg.AddComponent<KBatchedAnimController>();
                beam_anim_ctrl.AnimFiles = new KAnimFile[1] { Assets.GetAnim("laser_kanim") };
                beam_anim_ctrl.TintColour = beam_tint;
                beam_seg.AddComponent<LoopingSounds>();
                beam_seg.SetActive(true);
                beam_segs[i] = beam_seg;
            }
        }

        public void Sim33ms(float dt)
        {
            ChargeCapacitor(dt);
            var comet = comet_tracker.GetClosestComet(transform.position, range);
            if (comet != null && operational.IsOperational && AimAt(comet, dt) && HasEnoughElectricity(dt))
            {
                FireAt(comet, dt);
                overkill_time_left = overkill_time;
                if (!firing || (target != comet))
                {
                    firing = true;
                    target = comet;
                    arm_anim_ctrl.Play("gun_digging", KAnim.PlayMode.Loop);
                    foreach (var beam_seg in beam_segs)
                    {
                        var beam_anim_ctrl = beam_seg.GetComponent<KBatchedAnimController>();
                        beam_anim_ctrl.enabled = true;
                        beam_anim_ctrl.TintColour = beam_tint;
                        beam_anim_ctrl.Play("idle", KAnim.PlayMode.Loop);
                    }
                }
            }
            else if (overkill_time_left <= dt)
            {
                if (firing)
                {
                    arm_anim_ctrl.Play("gun", KAnim.PlayMode.Loop);
                    foreach (var beam_seg in beam_segs)
                    {
                        var beam_anim_ctrl = beam_seg.GetComponent<KBatchedAnimController>();
                        beam_anim_ctrl.Stop();
                        beam_anim_ctrl.enabled = false;
                    }
                    firing = false;
                }
            }
            else
            {
                if (overkill_time_left == overkill_time)
                {
                    arm_anim_ctrl.Play("gun", KAnim.PlayMode.Loop);
                }
                overkill_time_left -= dt;
                foreach (var beam_seg in beam_segs)
                {
                    var beam_anim_ctrl = beam_seg.GetComponent<KBatchedAnimController>();
                    beam_anim_ctrl.TintColour = beam_tint * new Color(1, 1, 1, (overkill_time_left + 0.1f) / (overkill_time + 0.1f));
                }
            }
        }

        public bool HasEnoughElectricity(float dt)
        {
            return electricity_available > laser_electricity_consumption * dt;
        }

        public void ChargeCapacitor(float dt)
        {
            if (operational.IsOperational && energyConsumer.IsExternallyPowered && electricity_available < electricity_capacity)
            {
                operational.SetActive(true);
                electricity_available = Mathf.Min(electricity_capacity, electricity_available + energyConsumer.WattsUsed * dt);
            }
            else
            {
                operational.SetActive(false);
            }
            energyConsumer.UpdatePoweredStatus();
            energyConsumer.SetSustained(HasEnoughElectricity(dt));
        }

        private void FireAt(Comet comet, float dt)
        {
            float electricity_used = laser_electricity_consumption * dt;
            if (electricity_available < electricity_used)
            {
                return;
            }
            electricity_available -= electricity_used;

            var primary_element = comet.gameObject.GetComponent<PrimaryElement>();
            var heat_energy = laser_heat_production * dt;
            if (primary_element.Mass > 100)
            {
                // HACK: Rock comets are hundreds of times more massive than iron comets. Even with the electricity to heat multiplier we couldn't affect them. Apply heat based on mass (Rock Comets are between 4000kg and 7200kg)
                heat_energy *= primary_element.Mass / 10;
            }
            // Use half the heat to ablate the meteor and the rest to warm it.
            var burn_heat_energy = 0.5f * heat_energy;
            var warm_heat_energy = heat_energy - burn_heat_energy;
            primary_element.Temperature += GameUtil.CalculateTemperatureChange(primary_element.Element.specificHeatCapacity, primary_element.Mass, warm_heat_energy / 1000/*KJ*/);
            var mass_removed = burn_heat_energy / (1000 * primary_element.Element.specificHeatCapacity * (primary_element.Element.highTemp - primary_element.Temperature));

            if (primary_element.Temperature > primary_element.Element.highTemp || primary_element.Mass <= mass_removed)
            {
                ShowExplosion(comet);
                // Drop Resources! :D - If there is a better way to do this, I'm not surprised but I sure couldn't find it (outseeker)
                PrimaryElement component = comet.GetComponent<PrimaryElement>();
                component.Mass = primary_element.Mass;
                component.Temperature = primary_element.Temperature;
                Element element = component.Element;
                Substance substance = element.substance;
                GameObject go = substance.SpawnResource(comet.transform.GetPosition(), component.Mass, component.Temperature, byte.MaxValue, 0);
                //Debug.Log("Spawned resource from destroyed comet");
                Util.KDestroyGameObject(comet.gameObject);
                kills += 1;
            }
            else
            {
                primary_element.SetMassTemperature(primary_element.Mass - mass_removed, primary_element.Temperature);
                ShowDamageFx(comet.transform.position);
            }

            float sqrMagnitude = (Vec3To2D(comet.transform.position) - Vec3To2D(transform.position)).sqrMagnitude;
            arm_anim_ctrl.GetBatchInstanceData().SetClipRadius(transform.position.x, transform.position.y, sqrMagnitude, do_clip: true);
            foreach (var beam_seg in beam_segs)
            {
                beam_seg.GetComponent<KBatchedAnimController>().GetBatchInstanceData().SetClipRadius(arm_anim_ctrl.transform.position.x, arm_anim_ctrl.transform.position.y, sqrMagnitude, do_clip: true);
            }
        }

        private bool AimAt(Comet comet, float dt)
        {
            var arm_offset = rotatable.GetRotatedOffset(new Vector3(0, 1, 0));
            Vector3 target_dir = Vector3.Normalize(Vec3To2D(comet.transform.position - transform.position - arm_offset));
            RotateArm(target_dir, warp: false, dt);

            int x, y, cx, cy;
            Grid.CellToXY(Grid.PosToCell(base.transform.gameObject), out x, out y);
            Grid.CellToXY(Grid.PosToCell(comet.transform.gameObject), out cx, out cy);
            var los = Grid.TestLineOfSight(x, y, cx, cy, Grid.VisibleBlockingCB);

            float target_angle = MathUtil.AngleSigned(Vector3.up, target_dir, Vector3.forward);
            var rotated = Mathf.Approximately(MathUtil.Wrap(-180f, 180f, target_angle - arm_rot), 0f);

            return los && rotated;
        }

        private void RotateArm(Vector3 target_dir, bool warp, float dt)
        {
            float target_angle = MathUtil.AngleSigned(Vector3.up, target_dir, Vector3.forward);
            float delta_angle = MathUtil.Wrap(-180f, 180f, target_angle - arm_rot);
            if (!warp)
            {
                delta_angle = Mathf.Clamp(delta_angle, -turn_rate * dt, turn_rate * dt);
            }
            arm_rot += delta_angle;
            arm_rot = MathUtil.Wrap(-180f, 180f, arm_rot);
            arm_go.transform.rotation = Quaternion.Euler(0f, 0f, arm_rot);
            if (Mathf.Approximately(target_angle, arm_rot))
            {
                StopRotateSound();
            }
            else
            {
                StartRotateSound();
            }
        }

        private void StartRotateSound()
        {
            if (!rotate_sound_playing)
            {
                looping_sounds.StartSound(rotateSound);
                rotate_sound_playing = true;
            }
        }

        private void StopRotateSound()
        {
            if (rotate_sound_playing)
            {
                looping_sounds.StopSound(rotateSound);
                rotate_sound_playing = false;
            }
        }

        public void ShowExplosion(Comet comet)
        {
            var position = comet.transform.GetPosition();
            string sound = GlobalAssets.GetSound(comet.impactSound);
            if (CameraController.Instance.IsAudibleSound(position, sound))
            {
                EventInstance instance = KFMOD.BeginOneShot(sound, position);
                instance.setParameterByName("userVolume_SFX", KPlayerPrefs.GetFloat("Volume_SFX"));
                KFMOD.EndOneShot(instance);
            }
            var fx_position = position;
            fx_position.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront2);
            var cell = Grid.PosToCell(fx_position);
            if (SafeCell(cell))
            {
                Game.Instance.SpawnFX(comet.explosionEffectHash, fx_position, 0f);
            }
        }

        public void ShowDamageFx(Vector3 position)
        {
            position.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront2);
            var cell = Grid.PosToCell(position);
            if (SafeCell(cell))
            {
                // if comet type is iron or rock, show appropriate FX. The lil' dust ones just asplode straight away so no FX for them (Comet Name is IronComet or RockComet for all comets, FX names apparently changed since initial naming and don't match up with comet names anymore)
                //Debug.Log("Comet Name: " + comet.GetComponent<PrimaryElement>().name);
                if (comet.GetComponent<PrimaryElement>().name is "IronComet")
                {
                    Game.Instance.SpawnFX(SpawnFXHashes.MeteorImpactMetal, position, 0f);

                } else if (comet.GetComponent<PrimaryElement>().name is "RockComet") 
                {
                    Game.Instance.SpawnFX(SpawnFXHashes.MeteorImpactDirt, position, 0f);
                }
                // Alternatively or additionally, show the original spawkly effect used (might make a config option)
                // Game.Instance.SpawnFX(SpawnFXHashes.BuildingSpark, position, 0f);
            }
        }

        // HACK: FX can cause crashes if spawned near the edge of the grid.
        public bool SafeCell(int cell)
        {
            int x, y;
            Grid.CellToXY(cell, out x, out y);
            const int margin = 3;
            return ValidCell(cell) && y < Grid.HeightInCells - margin && y > margin && x < Grid.WidthInCells - margin && x > margin;
        }

        public bool ValidCell(int cell)
        {
            return cell >= 0 && cell < Grid.CellCount;
        }
    }
}
