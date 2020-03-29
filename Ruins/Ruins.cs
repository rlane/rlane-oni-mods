using System.Collections.Generic;
using TemplateClasses;
using UnityEngine;

namespace Ruins
{
    public class Ruins
    {
        public static TemplateContainer MakeRuins(TemplateContainer input)
        {
            var rng = new System.Random();
            var template = new TemplateContainer();
            template.info = input.info;
            foreach (var building in input.buildings)
            {
                if (rng.NextDouble() < 0.5)
                {
                    template.buildings.Add(building);
                }
            }
            return template;
        }

        public static TemplateContainer GetBuildings()
        {
            var template = new TemplateContainer();
            HashSet<GameObject> _excludeEntities = new HashSet<GameObject>();
            for (int i = 0; i < Components.BuildingCompletes.Count; i++)
            {
                BuildingComplete buildingComplete = Components.BuildingCompletes[i];
                if (_excludeEntities.Contains(buildingComplete.gameObject))
                {
                    continue;
                }
                Grid.CellToXY(Grid.PosToCell(buildingComplete), out int x, out int y);
                {
                    Orientation rotation = Orientation.Neutral;
                    var rotatable = buildingComplete.gameObject.GetComponent<Rotatable>();
                    if (rotatable != null)
                    {
                        rotation = rotatable.GetOrientation();
                    }
                    var primary_element = buildingComplete.GetComponent<PrimaryElement>();
                    if (primary_element == null)
                    {
                        continue;
                    }
                    Prefab prefab = new Prefab(buildingComplete.PrefabID().Name, Prefab.Type.Building, x, y, primary_element.ElementID, primary_element.Temperature, 0f, null, 0, rotation);
                    template.buildings.Add(prefab);
                    _excludeEntities.Add(buildingComplete.gameObject);
                }
            }
            // TODO power/gas/liquid/logic connections
            return template;
        }
    }
}
