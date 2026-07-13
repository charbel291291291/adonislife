using System.Collections.Generic;
using AdonisLife.World.Authored;
using AdonisLife.World.Buildings;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.UrbanCell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdonisBuildingSetup
{
    private const string ScenePath = "Assets/Project/World/Scenes/ProceduralCity_Prototype.unity";
    private const string MaterialFolder = "Assets/Project/World/Materials/Buildings";
    private const string SettingsPath = "Assets/Project/World/Configs/ProceduralCitySettings.asset";

    private const string CityRootName = "_ProceduralCityPrototype";
    private const string BuildingsRootName = "_Buildings";

    private const float BlockPadTopY = 0.10f;
    private const float EntranceMarkerHeight = 2.2f;

    [MenuItem("Adonis Life/World/Add Buildings To Procedural City")]
    public static void AddBuildings()
    {
        ProceduralCitySettingsSO settingsAsset = AssetDatabase.LoadAssetAtPath<ProceduralCitySettingsSO>(SettingsPath);
        if (settingsAsset == null)
        {
            Debug.LogError($"Building generation aborted: settings asset not found at '{SettingsPath}'.");
            return;
        }

        if (!settingsAsset.IsValid(out string settingsError))
        {
            Debug.LogError($"Building generation aborted: {settingsError}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"Building generation aborted: could not open scene '{ScenePath}'.");
            return;
        }

        GameObject cityRoot = GameObject.Find(CityRootName);
        if (cityRoot == null)
        {
            Debug.LogError($"Building generation aborted: '{CityRootName}' root not found in scene.");
            return;
        }

        if (cityRoot.transform.Find(BuildingsRootName) != null)
        {
            Debug.LogWarning($"'{BuildingsRootName}' already exists in '{ScenePath}'. Skipping generation.");
            return;
        }

        CityGenerationSettings settings = settingsAsset.ToGenerationSettings();

        System.IO.Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();
        Dictionary<BuildingType, Material> materials = CreateMaterials();
        Material doorMaterial = GetOrCreateMaterial("Entrance_Material", new Color(0.12f, 0.10f, 0.08f));

        GameObject buildingsRoot = new GameObject(BuildingsRootName);
        buildingsRoot.transform.SetParent(cityRoot.transform);

        int created = 0;
        foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
        {
            GameObject cellGroup = new GameObject($"Buildings_C{cell.X}_{cell.Z}");
            cellGroup.transform.SetParent(buildingsRoot.transform);

            foreach (DevelopmentBlockQuadrant quadrant in
                (DevelopmentBlockQuadrant[])System.Enum.GetValues(typeof(DevelopmentBlockQuadrant)))
            {
                List<BuildingSpec> specs = BuildingBlockPlanner.PlanBlock(cell, quadrant, settings);
                for (int i = 0; i < specs.Count; i++)
                {
                    BuildingSpec spec = specs[i];
                    string name = $"Building_{spec.Type}_{quadrant}_{i}";
                    CreateBox(name, cellGroup.transform, materials[spec.Type],
                        spec.Footprint, BlockPadTopY + spec.Height, spec.Height);

                    float markerHeight = Mathf.Min(EntranceMarkerHeight, spec.Height);
                    CreateBox($"{name}_Entrance", cellGroup.transform, doorMaterial,
                        spec.EntranceMarker, BlockPadTopY + markerHeight, markerHeight);
                    created++;
                }
            }
        }

        int expected = BuildingBlockPlanner.PlanCity(settings).Count;
        if (created != expected)
        {
            Debug.LogError($"Building validation failed: created {created} buildings, expected {expected}.");
            return;
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Buildings added to '{ScenePath}': {created} buildings across {settings.CellsX}x{settings.CellsZ} cells.");
    }

    private static Dictionary<BuildingType, Material> CreateMaterials()
    {
        return new Dictionary<BuildingType, Material>
        {
            { BuildingType.Residential, GetOrCreateMaterial("Building_Residential_Material", new Color(0.78f, 0.71f, 0.58f)) },
            { BuildingType.Commercial, GetOrCreateMaterial("Building_Commercial_Material", new Color(0.45f, 0.58f, 0.68f)) },
            { BuildingType.Industrial, GetOrCreateMaterial("Building_Industrial_Material", new Color(0.52f, 0.48f, 0.45f)) },
            { BuildingType.Government, GetOrCreateMaterial("Building_Government_Material", new Color(0.80f, 0.78f, 0.72f)) },
            { BuildingType.Hospital, GetOrCreateMaterial("Building_Hospital_Material", new Color(0.90f, 0.90f, 0.92f)) },
            { BuildingType.School, GetOrCreateMaterial("Building_School_Material", new Color(0.75f, 0.55f, 0.35f)) },
            { BuildingType.Police, GetOrCreateMaterial("Building_Police_Material", new Color(0.25f, 0.35f, 0.60f)) },
            { BuildingType.FireStation, GetOrCreateMaterial("Building_FireStation_Material", new Color(0.72f, 0.22f, 0.18f)) },
            { BuildingType.Hotel, GetOrCreateMaterial("Building_Hotel_Material", new Color(0.60f, 0.50f, 0.70f)) },
            { BuildingType.Restaurant, GetOrCreateMaterial("Building_Restaurant_Material", new Color(0.85f, 0.60f, 0.30f)) },
            { BuildingType.GasStation, GetOrCreateMaterial("Building_GasStation_Material", new Color(0.30f, 0.65f, 0.55f)) },
            { BuildingType.Parking, GetOrCreateMaterial("Building_Parking_Material", new Color(0.35f, 0.35f, 0.35f)) }
        };
    }

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void CreateBox(string name, Transform parent, Material material, CellRect rect, float topY, float thickness)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localScale = new Vector3(rect.Width, thickness, rect.Depth);
        go.transform.position = new Vector3(rect.CenterX, topY - thickness / 2f, rect.CenterZ);
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
}
