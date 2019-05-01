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