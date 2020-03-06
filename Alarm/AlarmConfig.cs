using UnityEngine;
using TUNING;
using System.Collections.Generic;
using KSerialization;

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
            buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<LoopingSounds>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            Light2D light2D = go.AddOrGet<Light2D>();
            light2D.overlayColour = LIGHT2D.FLOORLAMP_OVERLAYCOLOR;
            light2D.Range = 3f;
            light2D.Angle = 0f;
            light2D.Direction = LIGHT2D.LIGHTBUG_DIRECTION;
            light2D.Offset = LIGHT2D.FLOORLAMP_OFFSET;
            light2D.shape = LightShape.Circle;
            light2D.drawOverlay = true;
            go.AddOrGetDef<LightController.Def>();
            go.AddOrGet<Alarm>();
            go.AddOrGet<AlarmBrightnessSlider>();
        }
    }

    class Alarm : StateMachineComponent<Alarm.StatesInstance>
    {
        public static StatusItem alarm_status_item = null;

        protected override void OnSpawn()
        {
            base.smi.StartSM();
            Light2D light2D = GetComponent<Light2D>();
            light2D.Color = ColorForElement(GetComponent<PrimaryElement>().Element, GetComponent<AlarmBrightnessSlider>().brightness);
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            InitializeStatusItems();
        }

        public static void InitializeStatusItems()
        {
            if (alarm_status_item == null)
            {
                alarm_status_item = new StatusItem("Alarm", "BUILDING", string.Empty, StatusItem.IconType.Exclamation, NotificationType.BadMinor, allow_multiples: false, OverlayModes.None.ID);
                alarm_status_item.AddNotification();
            }
        }

        public static Color ColorForElement(Element element, float brightness)
        {
            if (element.id == SimHashes.Iron)
            {
                return new Color(10 * brightness, 0, 0, 1);
            }
            else if (element.id == SimHashes.Copper)
            {
                return new Color(0, 10 * brightness, 0, 1);
            }
            else if (element.id == SimHashes.Gold)
            {
                return new Color(10 * brightness, 9.2f * brightness, 0.15f * brightness, 1);
            }
            else if (element.id == SimHashes.Tungsten)
            {
                return new Color(0, 0, 10 * brightness, 1);
            }
            else if (element.id == SimHashes.Steel)
            {
                return new Color(10 * brightness, 10 * brightness, 10 * brightness, 1);
            }
            else if (element.id == SimHashes.Niobium)
            {
                return new Color(10 * brightness, 0, 10 * brightness, 1);
            }
            else if (element.id == SimHashes.TempConductorSolid)
            {
                return new Color(10 * brightness, 5 * brightness, 0, 1);
            }
            else if (element.id == SimHashes.Lead)
            {
                return new Color(5 * brightness, 5 * brightness, 6 * brightness, 1);
            }
            else if (element.id == SimHashes.Aluminum)
            {
                return new Color(10 * brightness, 10 * brightness, 10 * brightness, 1);
            }
            else
            {
                return Color.clear;
            }
        }

        public void SetBrightness(float brightness)
        {
            this.GetComponent<Light2D>().Color = ColorForElement(GetComponent<PrimaryElement>().Element, brightness);
        }

        public void SetName(string name)
        {
            KSelectable component = GetComponent<KSelectable>();
            base.name = name;
            if (component != null)
            {
                component.SetName(name);
            }
            base.gameObject.name = name;
            NameDisplayScreen.Instance.UpdateName(base.gameObject);
        }

        public class StatesInstance : GameStateMachine<States, StatesInstance, Alarm, object>.GameInstance
        {
            public StatesInstance(Alarm master)
                : base(master)
            {
            }
        }

        public class States : GameStateMachine<States, StatesInstance, Alarm, object>
        {
            public State Off;
            public State On;

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
        }
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    class AlarmBrightnessSlider : KMonoBehaviour, ISingleSliderControl, ISliderControl
    {
        [Serialize]
        public float brightness = 0.5f;

        public string SliderTitleKey => "STRINGS.UI.UISIDESCREENS.ALARM.TITLE";

        public string SliderUnits => STRINGS.UI.UNITSUFFIXES.PERCENT;

        public int SliderDecimalPlaces(int index)
        {
            return 0;
        }

        public float GetSliderMin(int index)
        {
            return 0;
        }

        public float GetSliderMax(int index)
        {
            return 100;
        }

        public float GetSliderValue(int index)
        {
            return brightness * 100;
        }

        public void SetSliderValue(float percent, int index)
        {
            brightness = percent / 100;
            GetComponent<Alarm>().SetBrightness(brightness);
        }

        public string GetSliderTooltipKey(int index)
        {
            return "STRINGS.UI.UISIDESCREENS.ALARM.TOOLTIP";
        }

        public string GetSliderTooltip()
        {
            return Strings.Get("STRINGS.UI.UISIDESCREENS.ALARM.TOOLTIP");
        }
    }
}