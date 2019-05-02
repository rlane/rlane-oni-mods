// TODO: Consume energy
// TODO: Draw beam
// TODO: Rotate arm
// TODO: Spawn liquid
// TODO: Take specific heat and mass of meteor into account

using System.Collections.Generic;
using TUNING;
using UnityEngine;
using System.Linq;
using FMOD.Studio;

namespace rlane
{
    public class MeteorDefenseLaserConfig : IBuildingConfig
    {
        public const string ID = "MeteorDefenseLaser";

        private const int RANGE = 20;

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef("MeteorDefenseLaser", 2, 2, "auto_miner_kanim", 10, 10f, BUILDINGS.CONSTRUCTION_MASS_KG.TIER3, MATERIALS.REFINED_METALS, 1600f, BuildLocationRule.OnFoundationRotatable, BUILDINGS.DECOR.PENALTY.TIER2, NOISE_POLLUTION.NOISY.TIER0);
            buildingDef.Floodable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = 120f;
            buildingDef.ExhaustKilowattsWhenActive = 0f;
            buildingDef.SelfHeatKilowattsWhenActive = 2f;
            buildingDef.PermittedRotations = PermittedRotations.R360;
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.SolidConveyorIDs, "AutoMiner");
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<Operational>();
            go.AddOrGet<LoopingSounds>();
            go.AddOrGet<MiningSounds>();
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            GeneratedBuildings.RegisterLogicPorts(go, LogicOperationalController.INPUT_PORTS_0_0);
            AddVisualizer(go, movable: true);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            GeneratedBuildings.RegisterLogicPorts(go, LogicOperationalController.INPUT_PORTS_0_0);
            AddVisualizer(go, movable: false);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            GeneratedBuildings.RegisterLogicPorts(go, LogicOperationalController.INPUT_PORTS_0_0);
            go.AddOrGet<LogicOperationalController>();
            var defender = go.AddOrGet<MeteorDefenseLaser>();
            defender.range = RANGE;
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
                go.GetComponent<StationaryChoreRangeVisualizer>().blocking_cb = (int cell) => Grid.PhysicalBlockingCB(cell) || (Vector2.Distance(go.transform.position, Grid.CellToPosCCC(cell, Grid.SceneLayer.NoLayer)) > RANGE);
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

    public class MeteorDefenseLaser : KMonoBehaviour, ISim33ms
    {
        public static CometTracker comet_tracker = new CometTracker();

        public float range;

        public void Sim33ms(float dt)
        {
            var comet = comet_tracker.GetClosestComet(transform.position, range);
            if (comet != null)
            {
                Debug.Log("RLL defender " + this + " at " + PosMin() + " attacking comet " + comet + " at " + comet.PosMin());
                var primary_element = comet.gameObject.GetComponent<PrimaryElement>();
                primary_element.SetMassTemperature(primary_element.Mass, primary_element.Temperature + dt * 5000);
                ShowDamageFx(comet.transform.position);
                if (primary_element.Temperature > primary_element.Element.highTemp)
                {
                    Debug.Log("RLL melted " + comet + " at " + comet.PosMin());
                    Explode(comet);
                    Util.KDestroyGameObject(comet.gameObject);
                }
            }
        }

        public void Explode(Comet comet)
        {
            var position = comet.transform.GetPosition();
            //var primary_element = comet.gameObject.GetComponent<PrimaryElement>();
            string sound = GlobalAssets.GetSound("Meteor_Large_Impact");
            if (CameraController.Instance.IsAudibleSound(position, sound))
            {
                EventInstance instance = KFMOD.BeginOneShot(sound, position);
                instance.setParameterValue("userVolume_SFX", KPlayerPrefs.GetFloat("Volume_SFX"));
                KFMOD.EndOneShot(instance);
            }
            var fx_position = position;
            fx_position.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront2);
            var cell = Grid.PosToCell(fx_position);
            if (cell >= 0 && cell < Grid.CellCount)
            {
                Game.Instance.SpawnFX(comet.explosionEffectHash, fx_position, 0f);
            }
            else
            {
                Debug.Log("RLL bad cell at " + fx_position);
            }
        }

        public void ShowDamageFx(Vector3 position)
        {
            position.z = Grid.GetLayerZ(Grid.SceneLayer.FXFront2);
            var cell = Grid.PosToCell(position);
            if (cell >= 0 && cell < Grid.CellCount)
            {
                Game.Instance.SpawnFX(SpawnFXHashes.BuildingSpark, position, 0f);
            }
        }
    }
}
