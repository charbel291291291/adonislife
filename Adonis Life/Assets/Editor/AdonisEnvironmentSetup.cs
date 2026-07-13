using System.Collections.Generic;
using AdonisLife.World.Authored;
using AdonisLife.World.Environment;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.Terrain;
using AdonisLife.World.UrbanCell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class AdonisEnvironmentSetup
{
    private const string CityScenePath = "Assets/Project/World/Scenes/ProceduralCity_Prototype.unity";
    private const string TerrainScenePath = "Assets/Project/World/Scenes/Terrain_Prototype.unity";
    private const string MaterialFolder = "Assets/Project/World/Materials/Environment";
    private const string WaterMaterialPath = "Assets/Project/World/Materials/Terrain/Water_Material.mat";
    private const string CitySettingsPath = "Assets/Project/World/Configs/ProceduralCitySettings.asset";
    private const string TerrainSettingsPath = "Assets/Project/World/Configs/TerrainSettings.asset";

    private const string GreenRootName = "_Environment_Green";
    private const string NatureRootName = "_Nature";

    private const float GrassTopY = 0.02f;
    private const float GrassThickness = 0.05f;
    private const float ParkTopY = 0.02f;
    private const float ParkThickness = 0.05f;
    private const float PlaygroundTopY = 0.06f;
    private const float PlaygroundThickness = 0.04f;
    private const float TrunkHeight = 2.5f;
    private const float TrunkWidth = 0.3f;
    private const float CanopySize = 2.2f;

    [MenuItem("Adonis Life/World/Add Environment")]
    public static void AddEnvironment()
    {
        System.IO.Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();

        if (!AddCityEnvironment())
        {
            return;
        }

        if (!AddTerrainNature())
        {
            return;
        }

        MakeWaterTransparent();
        Debug.Log("Environment added to city and terrain scenes.");
    }

    private static bool AddCityEnvironment()
    {
        ProceduralCitySettingsSO settingsAsset = AssetDatabase.LoadAssetAtPath<ProceduralCitySettingsSO>(CitySettingsPath);
        if (settingsAsset == null || !settingsAsset.IsValid(out _))
        {
            Debug.LogError("City environment aborted: settings asset missing or invalid.");
            return false;
        }

        Scene scene = EditorSceneManager.OpenScene(CityScenePath, OpenSceneMode.Single);
        GameObject cityRoot = GameObject.Find("_ProceduralCityPrototype");
        if (cityRoot == null)
        {
            Debug.LogError("City environment aborted: city root not found.");
            return false;
        }

        if (cityRoot.transform.Find(GreenRootName) != null)
        {
            Debug.LogWarning("City environment already exists. Skipping.");
            return true;
        }

        CityGenerationSettings settings = settingsAsset.ToGenerationSettings();

        Material trunkMaterial = GetOrCreateMaterial("TreeTrunk_Material", new Color(0.38f, 0.26f, 0.15f));
        Material canopyMaterial = GetOrCreateMaterial("TreeCanopy_Material", new Color(0.20f, 0.45f, 0.18f));
        Material grassMaterial = GetOrCreateMaterial("Grass_Material", new Color(0.32f, 0.52f, 0.25f));
        Material parkMaterial = GetOrCreateMaterial("Park_Material", new Color(0.28f, 0.55f, 0.28f));
        Material playgroundMaterial = GetOrCreateMaterial("Playground_Material", new Color(0.85f, 0.65f, 0.25f));

        GameObject root = new GameObject(GreenRootName);
        root.transform.SetParent(cityRoot.transform);
        Transform trees = CreateGroup("_StreetTrees", root.transform);
        Transform grass = CreateGroup("_GrassStrips", root.transform);
        Transform parks = CreateGroup("_Parks", root.transform);
        Transform playgrounds = CreateGroup("_Playgrounds", root.transform);

        foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
        {
            string suffix = $"C{cell.X}_{cell.Z}";

            List<Vector2> treePositions = EnvironmentLayout.GetStreetTreePositions(cell, settings);
            for (int i = 0; i < treePositions.Count; i++)
            {
                CreateTree($"StreetTree_{suffix}_{i}", trees, trunkMaterial, canopyMaterial,
                    new Vector3(treePositions[i].x, 0f, treePositions[i].y), 1f);
            }

            List<CellRect> grassStrips = EnvironmentLayout.GetGrassStripRects(cell, settings);
            for (int i = 0; i < grassStrips.Count; i++)
            {
                CreateBox($"Grass_{suffix}_{i}", grass, grassMaterial, grassStrips[i], GrassTopY, GrassThickness);
            }
        }

        List<CellRect> parkRects = EnvironmentLayout.GetParkRects(settings);
        for (int i = 0; i < parkRects.Count; i++)
        {
            CreateBox($"Park_{i}", parks, parkMaterial, parkRects[i], ParkTopY, ParkThickness);
        }

        List<CellRect> pads = EnvironmentLayout.GetPlaygroundPads(settings);
        for (int i = 0; i < pads.Count; i++)
        {
            CreateBox($"Playground_{i}", playgrounds, playgroundMaterial, pads[i], PlaygroundTopY, PlaygroundThickness);
        }

        AttachEnvironmentSystems(cityRoot.transform);

        EditorSceneManager.SaveScene(scene, CityScenePath);
        return true;
    }

    private static void AttachEnvironmentSystems(Transform cityRoot)
    {
        Transform environment = cityRoot.Find("_Environment");
        if (environment == null)
        {
            return;
        }

        GameObject systems = new GameObject("EnvironmentSystems");
        systems.transform.SetParent(environment);

        DayNightCycle dayNight = systems.AddComponent<DayNightCycle>();
        Transform sun = environment.Find("Directional Light");
        if (sun != null)
        {
            var serialized = new SerializedObject(dayNight);
            serialized.FindProperty("_sunLight").objectReferenceValue = sun.GetComponent<Light>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        systems.AddComponent<WeatherSystem>();
    }

    private static bool AddTerrainNature()
    {
        TerrainSettingsSO settingsAsset = AssetDatabase.LoadAssetAtPath<TerrainSettingsSO>(TerrainSettingsPath);
        if (settingsAsset == null || !settingsAsset.IsValid(out _))
        {
            Debug.LogError("Terrain nature aborted: settings asset missing or invalid.");
            return false;
        }

        Scene scene = EditorSceneManager.OpenScene(TerrainScenePath, OpenSceneMode.Single);
        GameObject terrainRoot = GameObject.Find("_TerrainPrototype");
        if (terrainRoot == null)
        {
            Debug.LogError("Terrain nature aborted: terrain root not found.");
            return false;
        }

        if (terrainRoot.transform.Find(NatureRootName) != null)
        {
            Debug.LogWarning("Terrain nature already exists. Skipping.");
            return true;
        }

        TerrainGenerationSettings terrain = settingsAsset.ToGenerationSettings();

        Material trunkMaterial = GetOrCreateMaterial("TreeTrunk_Material", new Color(0.38f, 0.26f, 0.15f));
        Material canopyMaterial = GetOrCreateMaterial("TreeCanopy_Material", new Color(0.20f, 0.45f, 0.18f));
        Material rockMaterial = GetOrCreateMaterial("Rock_Material", new Color(0.42f, 0.41f, 0.40f));

        GameObject root = new GameObject(NatureRootName);
        root.transform.SetParent(terrainRoot.transform);

        List<NatureInstance> instances = EnvironmentLayout.GetNatureInstances(terrain);
        for (int i = 0; i < instances.Count; i++)
        {
            NatureInstance instance = instances[i];
            var position = new Vector3(instance.Position.x, instance.GroundHeight, instance.Position.y);

            if (instance.Type == NatureType.Tree)
            {
                CreateTree($"Tree_{i}", root.transform, trunkMaterial, canopyMaterial, position, instance.Scale);
            }
            else
            {
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = $"Rock_{i}";
                rock.transform.SetParent(root.transform);
                rock.transform.localScale = new Vector3(instance.Scale, instance.Scale * 0.7f, instance.Scale);
                rock.transform.position = position + Vector3.up * instance.Scale * 0.25f;
                rock.transform.rotation = Quaternion.Euler(0f, instance.Scale * 137f, 0f);
                rock.GetComponent<MeshRenderer>().sharedMaterial = rockMaterial;
            }
        }

        EditorSceneManager.SaveScene(scene, TerrainScenePath);
        return true;
    }

    private static void MakeWaterTransparent()
    {
        Material water = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);
        if (water == null)
        {
            Debug.LogWarning("Water material not found; skipping transparency setup.");
            return;
        }

        water.SetFloat("_Surface", 1f);
        water.SetOverrideTag("RenderType", "Transparent");
        water.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        water.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        water.SetFloat("_ZWrite", 0f);
        water.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        water.renderQueue = (int)RenderQueue.Transparent;

        Color color = water.color;
        color.a = 0.65f;
        water.color = color;

        EditorUtility.SetDirty(water);
        AssetDatabase.SaveAssets();
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent);
        return group.transform;
    }

    private static void CreateTree(
        string name, Transform parent, Material trunkMaterial, Material canopyMaterial, Vector3 basePosition, float scale)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(parent);
        tree.transform.position = basePosition;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform);
        trunk.transform.localScale = new Vector3(TrunkWidth * scale, TrunkHeight * scale, TrunkWidth * scale);
        trunk.transform.localPosition = new Vector3(0f, TrunkHeight * scale / 2f, 0f);
        trunk.GetComponent<MeshRenderer>().sharedMaterial = trunkMaterial;

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canopy.name = "Canopy";
        canopy.transform.SetParent(tree.transform);
        canopy.transform.localScale = new Vector3(CanopySize * scale, CanopySize * scale, CanopySize * scale);
        canopy.transform.localPosition = new Vector3(0f, (TrunkHeight + CanopySize / 2f) * scale, 0f);
        canopy.GetComponent<MeshRenderer>().sharedMaterial = canopyMaterial;
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
