using UnityEngine;
using TUNING;

class HeatingElementConfig : IBuildingConfig
{
    public const string ID = "HeatingElement";

    public override BuildingDef CreateBuildingDef()
    {
        int width = 1;
        int height = 1;
        string anim = "gas_germs_sensor_kanim";
        int hitpoints = 30;
        float construction_time = 30f;
        float[] construction_mass = BUILDINGS.CONSTRUCTION_MASS_KG.TIER4;
        float melting_point = 3200f;
        BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, width, height, anim, hitpoints, construction_time, construction_mass, MATERIALS.ALL_METALS, melting_point, BuildLocationRule.Anywhere, BUILDINGS.DECOR.PENALTY.TIER1, NOISE_POLLUTION.NONE);
        buildingDef.RequiresPowerInput = true;
        buildingDef.Floodable = false;
        buildingDef.EnergyConsumptionWhenActive = 240f;
        buildingDef.ExhaustKilowattsWhenActive = 0f;
        buildingDef.SelfHeatKilowattsWhenActive = 1016f;
        buildingDef.ViewMode = OverlayModes.Power.ID;
        buildingDef.AudioCategory = "SolidMetal";
        buildingDef.Overheatable = false;
        return buildingDef;
    }

    public override void DoPostConfigureComplete(GameObject go)
    {
        //BuildingTemplates.DoPostConfigure(go);
        go.GetComponent<KPrefabID>().prefabInitFn += gameObject => new HeatingElementStateMachine.Instance(gameObject.GetComponent<KPrefabID>()).StartSM();
    }
}

public class HeatingElementStateMachine : GameStateMachine<HeatingElementStateMachine, HeatingElementStateMachine.Instance>
{
    public State Off;
    public State On;

    private static readonly HashedString[] ON_ANIMS = new HashedString[2] { "on_pre", "on_loop" };
    private static readonly HashedString[] OFF_ANIMS = new HashedString[2] { "on_pst", "off" };

    public override void InitializeStates(out BaseState defaultState)
    {
        defaultState = Off;

        Off
            .Enter("SetInactive", smi => smi.GetComponent<KBatchedAnimController>().Play(OFF_ANIMS))
            .EventTransition(GameHashes.OperationalChanged, On, smi => smi.GetComponent<Operational>().IsOperational);
        On
            .Enter("SetActive", smi => {
                smi.GetComponent<KBatchedAnimController>().Play(ON_ANIMS, KAnim.PlayMode.Loop);
                smi.GetComponent<Operational>().SetActive(true, false);
            })
            .EventTransition(GameHashes.OperationalChanged, Off, smi => !smi.GetComponent<Operational>().IsOperational);
    }

    public new class Instance : GameInstance
    {
        public Instance(IStateMachineTarget master) : base(master) { }
    }
}