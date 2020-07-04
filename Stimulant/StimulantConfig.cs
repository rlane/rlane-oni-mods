using System.Collections.Generic;
using Klei.AI;
using UnityEngine;

namespace Stimulant
{
    public class StimulantConfig : IEntityConfig
    {
        public const string ID = "Stimulant";

        public static ComplexRecipe recipe;

        public static readonly MedicineInfo medicineInfo = new MedicineInfo("Stimulant", "Medicine_Stimulant", MedicineInfo.MedicineType.Booster);

        public GameObject CreatePrefab()
        {
            Db.Get().effects.Add(CreateEffect());

            GameObject gameObject = EntityTemplates.CreateLooseEntity("Stimulant", "Stimulant", "Temporarily increases physical ability at the cost of increased metabolism.", 1f, unitMass: true, Assets.GetAnim("pill_2_kanim"), "object", Grid.SceneLayer.Front, EntityTemplates.CollisionShape.RECTANGLE, 0.8f, 0.4f, isPickupable: true);
            EntityTemplates.ExtendEntityToMedicine(gameObject, medicineInfo);
            ComplexRecipe.RecipeElement[] inputs = new ComplexRecipe.RecipeElement[3]
            {
                new ComplexRecipe.RecipeElement(SimHashes.RefinedCarbon.CreateTag(), 10f),
                new ComplexRecipe.RecipeElement(SpiceNutConfig.ID, 1f),
                new ComplexRecipe.RecipeElement("LightBugEgg", 1f)
            };
            ComplexRecipe.RecipeElement[] outputs = new ComplexRecipe.RecipeElement[1]
            {
                new ComplexRecipe.RecipeElement("Stimulant", 10f)
            };
            string id = ComplexRecipeManager.MakeRecipeID("Apothecary", inputs, outputs);
            recipe = new ComplexRecipe(id, inputs, outputs)
            {
                time = 75f,
                description = "Temporarily increases physical ability at the cost of increased metabolism.",
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag> { "Apothecary" },
                sortOrder = 1
            };
            return gameObject;
        }

        private static Effect CreateEffect()
        {
            var effect = new Effect("Medicine_Stimulant", "Stimulant", "Temporarily increases physical ability at the cost of increased metabolism.", 600f, show_in_ui: true, trigger_floating_text: true, is_bad: false);
            var attributes = new List<string> {
                Db.Get().Attributes.Athletics.Id,
                Db.Get().Attributes.Strength.Id,
                Db.Get().Attributes.Digging.Id,
                Db.Get().Attributes.Construction.Id,
            };
            var skill_multiplier = 0.25f;
            var metabolism_multiplier = 0.5f;
            foreach (var attribute in attributes)
            {
                effect.Add(new AttributeModifier(attribute, 2, effect.Name));
                effect.Add(new AttributeModifier(attribute, skill_multiplier, effect.Name, is_multiplier: true));
            }
            var calorieIncrease = metabolism_multiplier * 1000 * (5.0f/3);
            effect.Add(new AttributeModifier(Db.Get().Amounts.Calories.deltaAttribute.Id, -calorieIncrease, effect.Name));
            //effect.Add(new AttributeModifier("CaloriesMax", calorieIncrease * 1000, effect.Name));
            effect.Add(new AttributeModifier("AirConsumptionRate", 0.1f * metabolism_multiplier, effect.Name));
            return effect;
        }

        public void OnPrefabInit(GameObject inst)
        {
        }

        public void OnSpawn(GameObject inst)
        {
        }
    }

}