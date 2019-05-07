using UnityEngine;
using TUNING;
using System.Collections.Generic;

namespace rlane
{
    class AlarmConfig : IBuildingConfig
    {
        public const string ID = "Alarm";

        public override BuildingDef CreateBuildingDef()
        {
            int width = 1;
            int height = 2;
            string anim = "saltlamp_kanim";
            int hitpoints = 10;
            float construction_time = 10f;
            float[] construction_mass = BUILDINGS.CONSTRUCTION_MASS_KG.TIER1;
            float melting_point = 800f;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, width, height, anim, hitpoints, construction_time, construction_mass, MATERIALS.REFINED_METALS, melting_point, BuildLocationRule.OnFloor, BUILDINGS.DECOR.NONE, NOISE_POLLUTION.NOISY.TIER6);
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = 10f;
            buildingDef.ViewMode = OverlayModes.Logic.ID;
            buildingDef.AudioCategory = "Metal";
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<LoopingSounds>();
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            GeneratedBuildings.RegisterLogicPorts(go, LogicOperationalController.INPUT_PORTS_0_0);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            GeneratedBuildings.RegisterLogicPorts(go, LogicOperationalController.INPUT_PORTS_0_0);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            GeneratedBuildings.RegisterLogicPorts(go, LogicOperationalController.INPUT_PORTS_0_0);
            go.GetComponent<KPrefabID>().prefabInitFn += gameObject => new AlarmStateMachine.Instance(gameObject.GetComponent<KPrefabID>()).StartSM();
            go.AddOrGetDef<LightController.Def>();
            go.AddOrGet<Alarm>();
        }
    }

    class Alarm : KMonoBehaviour
    {
        protected override void OnSpawn()
        {
            Light2D light2D = this.FindOrAddComponent<Light2D>();
            light2D.overlayColour = LIGHT2D.FLOORLAMP_OVERLAYCOLOR;
            light2D.Color = ColorForElement(GetComponent<PrimaryElement>().Element);
            light2D.Range = 3f;
            light2D.Angle = 0f;
            light2D.Direction = LIGHT2D.LIGHTBUG_DIRECTION;
            light2D.Offset = LIGHT2D.FLOORLAMP_OFFSET;
            light2D.shape = LightShape.Circle;
            light2D.drawOverlay = true;
            light2D.Lux = 1800;
        }

        public Color ColorForElement(Element element)
        {
            if (element.id == SimHashes.Iron)
            {
                return new Color(10, 0, 0, 1);
            }
            else if (element.id == SimHashes.Copper)
            {
                return new Color(0, 10, 0, 1);
            }
            else if (element.id == SimHashes.Gold)
            {
                return new Color(10, 9.2f, 0.15f, 1);
            }
            else if (element.id == SimHashes.Tungsten)
            {
                return new Color(0, 0, 10, 1);
            }
            else if (element.id == SimHashes.Steel)
            {
                return new Color(10, 10, 10, 1);
            }
            else if (element.id == SimHashes.Niobium)
            {
                return new Color(10, 0, 10, 1);
            }
            else if (element.id == SimHashes.TempConductorSolid)
            {
                return new Color(10, 5, 0, 1);
            }
            else
            {
                return Color.clear;
            }
        }
    }

    public class AlarmStateMachine : GameStateMachine<AlarmStateMachine, AlarmStateMachine.Instance>
    {
        public State Off;
        public State On;

        public static StatusItem alarm_status_item = MakeStatusItem();

        public static StatusItem MakeStatusItem()
        {
            StatusItem status_item = new StatusItem("Alarm", "BUILDING", string.Empty, StatusItem.IconType.Exclamation, NotificationType.BadMinor, allow_multiples: false, OverlayModes.None.ID);
            status_item.AddNotification();
            return status_item;
        }

        public override void InitializeStates(out BaseState defaultState)
        {
            defaultState = Off;

            Off
                .PlayAnim("off")
                .EventTransition(GameHashes.OperationalChanged, On, smi => smi.GetComponent<Operational>().IsOperational);
            On
                .PlayAnim("on")
                .ToggleLoopingSound(GlobalAssets.GetSound("YellowAlert_LP"))
                .Enter("SetActive", smi =>
                {
                    smi.GetComponent<Operational>().SetActive(true, false);
                })
                .ToggleStatusItem(alarm_status_item)
                .EventTransition(GameHashes.OperationalChanged, Off, smi => !smi.GetComponent<Operational>().IsOperational);
        }

        public new class Instance : GameInstance
        {
            public Instance(IStateMachineTarget master) : base(master) { }
        }
    }
}