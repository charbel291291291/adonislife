using System.Collections.Generic;
using System.IO;
using AdonisLife.World.Authored;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.UrbanCell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdonisProceduralCitySetup
{
    private const string SceneFolder = "Assets/Project/World/Scenes";
    private const string ScenePath = SceneFolder + "/ProceduralCity_Prototype.unity";
    private const string MaterialFolder = "Assets/Project/World/Materials/UrbanPrototype";
    private const string WorldConfigPath = "Assets/Project/World/Configs/WorldConfig.asset";
    private const string SettingsFolder = "Assets/Project/World/Configs";
    private const string SettingsPath = SettingsFolder + "/ProceduralCitySettings.asset";

    private const float GroundTopY = 0f;
    private const float GroundThickness = 0.2f;
    private const float RoadTopY = 0.05f;
    private const float RoadThickness = 0.1f;
    private const float SidewalkTopY = 0.20f;
    private const float SidewalkThickness = 0.1f;
    private const float BlockTopY = 0.10f;
    private const float BlockThickness = 0.1f;

    private static readonly string[] RequiredHierarchyChildren =
    {
        "_Ground", "_RoadNetwork", "_Sidewalks", "_DevelopmentBlocks", "_References", "_Environment"
    };

    [MenuItem("Adonis Life/World/Create Procedural City Prototype")]
    public static void CreateProceduralCityPrototype()
    {
        if (File.Exists(ScenePath))
        {
            Debug.LogWarning($"Procedural city prototype scene already exists at '{ScenePath}'. Skipping creation.");
            return;
        }

        ProceduralCitySettingsSO settingsAsset = GetOrCreateSettings();
        if (!settingsAsset.IsValid(out string settingsError))
        {
            Debug.LogError($"Procedural city prototype creation aborted: {settingsError}");
            return;
        }

        if (!ValidateWorldConfigIntegration(settingsAsset, out string worldConfigError))
        {
            Debug.LogError($"Procedural city prototype creation aborted: {worldConfigError}");
            return;
        }

        if (!TryLoadMaterials(out Material groundMaterial, out Material roadMaterial, out Material sidewalkMaterial, out Material blockMaterial, out string materialError))
        {
            Debug.LogError($"Procedural city prototype creation aborted: {materialError}");
            return;
        }

        CityGenerationSettings settings = settingsAsset.ToGenerationSettings();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("_ProceduralCityPrototype");
        GameObject groundRoot = new GameObject("_Ground");
        GameObject roadNetworkRoot = new GameObject("_RoadNetwork");
        GameObject sidewalksRoot = new GameObject("_Sidewalks");
        GameObject blocksRoot = new GameObject("_DevelopmentBlocks");
        GameObject referencesRoot = new GameObject("_References");
        GameObject environmentRoot = new GameObject("_Environment");

        groundRoot.transform.SetParent(root.transform);
        roadNetworkRoot.transform.SetParent(root.transform);
        sidewalksRoot.transform.SetParent(root.transform);
        blocksRoot.transform.SetParent(root.transform);
        referencesRoot.transform.SetParent(root.transform);
        environmentRoot.transform.SetParent(root.transform);

        CreateGround(groundRoot.transform, groundMaterial, settings);

        List<CellRect> allRoadRects = new List<CellRect>();
        List<CellRect> allBlockRects = new List<CellRect>();

        foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
        {
            CreateRoadsForCell(roadNetworkRoot.transform, roadMaterial, cell, settings, allRoadRects);
            CreateSidewalksForCell(sidewalksRoot.transform, sidewalkMaterial, cell, settings);
            CreateDevelopmentBlocksForCell(blocksRoot.transform, blockMaterial, cell, settings, allBlockRects);
        }

        CreateScaleReferences(referencesRoot.transform);
        CreateCameraAndLight(environmentRoot.transform, settings);

        if (!ValidateGeneratedRoads(allRoadRects, out string roadError))
        {
            Debug.LogError($"Procedural city prototype validation failed: {roadError}");
            return;
        }

        if (!ValidateGeneratedBlocks(allBlockRects, out string blockError))
        {
            Debug.LogError($"Procedural city prototype validation failed: {blockError}");
            return;
        }

        if (!ValidateScene(root.transform, settings, out string sceneError))
        {
            Debug.LogError($"Procedural city prototype validation failed: {sceneError}");
            return;
        }

        Directory.CreateDirectory(SceneFolder);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Vector2 dimensions = ProceduralCityLayout.GetCityDimensions(settings);
        Debug.Log($"Procedural city prototype created at '{ScenePath}': {settings.CellsX}x{settings.CellsZ} cells, {dimensions.x}x{dimensions.y}m footprint.");
    }

    private static ProceduralCitySettingsSO GetOrCreateSettings()
    {
        ProceduralCitySettingsSO existing = AssetDatabase.LoadAssetAtPath<ProceduralCitySettingsSO>(SettingsPath);
        if (existing != null)
        {
            return existing;
        }

        Directory.CreateDirectory(SettingsFolder);
        ProceduralCitySettingsSO settings = ScriptableObject.CreateInstance<ProceduralCitySettingsSO>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        return settings;
    }

    private static bool ValidateWorldConfigIntegration(ProceduralCitySettingsSO settingsAsset, out string error)
    {
        WorldConfigSO config = AssetDatabase.LoadAssetAtPath<WorldConfigSO>(WorldConfigPath);
        if (config == null)
        {
            error = $"WorldConfig asset not found at '{WorldConfigPath}'.";
            return false;
        }

        if (!Mathf.Approximately(config.ChunkSize, settingsAsset.CellSize))
        {
            error = $"WorldConfig chunk size ({config.ChunkSize}) does not match the procedural city cell size ({settingsAsset.CellSize}).";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryLoadMaterials(out Material ground, out Material road, out Material sidewalk, out Material block, out string error)
    {
        ground = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/UrbanGround_Material.mat");
        road = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Road_Material.mat");
        sidewalk = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Sidewalk_Material.mat");
        block = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/DevelopmentBlock_Material.mat");

        if (ground == null || road == null || sidewalk == null || block == null)
        {
            error = $"Required Urban Prototype materials not found in '{MaterialFolder}'. Run the Urban Base Cell Prototype milestone first.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool ValidateGeneratedRoads(List<CellRect> roads, out string error)
    {
        for (int i = 0; i < roads.Count; i++)
        {
            for (int j = i + 1; j < roads.Count; j++)
            {
                if (roads[i].Equals(roads[j]))
                {
                    error = $"Duplicate road segment detected at indices {i} and {j}.";
                    return false;
                }
            }
        }

        error = null;
        return true;
    }

    private static bool ValidateGeneratedBlocks(List<CellRect> blocks, out string error)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i].Width <= 0f || blocks[i].Depth <= 0f)
            {
                error = $"Development block at index {i} has an invalid (non-positive) dimension.";
                return false;
            }

            for (int j = i + 1; j < blocks.Count; j++)
            {
                if (blocks[i].Overlaps(blocks[j]))
                {
                    error = $"Development blocks at indices {i} and {j} overlap.";
                    return false;
                }
            }
        }

        error = null;
        return true;
    }

    private static bool ValidateScene(Transform root, CityGenerationSettings settings, out string error)
    {
        foreach (string childName in RequiredHierarchyChildren)
        {
            if (root.Find(childName) == null)
            {
                error = $"Required hierarchy object '{childName}' is missing.";
                return false;
            }
        }

        int expectedCellCount = settings.CellsX * settings.CellsZ;
        int actualRoadNetworkChildCount = root.Find("_RoadNetwork").childCount;
        if (actualRoadNetworkChildCount != expectedCellCount * 2)
        {
            error = $"Expected {expectedCellCount * 2} road segments, found {actualRoadNetworkChildCount}.";
            return false;
        }

        int actualBlockChildCount = root.Find("_DevelopmentBlocks").childCount;
        if (actualBlockChildCount != expectedCellCount * 4)
        {
            error = $"Expected {expectedCellCount * 4} development blocks, found {actualBlockChildCount}.";
            return false;
        }

        error = null;
        return true;
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

    private static void CreateGround(Transform parent, Material material, CityGenerationSettings settings)
    {
        Vector2 dimensions = ProceduralCityLayout.GetCityDimensions(settings);
        CellRect groundRect = new CellRect(0f, dimensions.x, 0f, dimensions.y);
        CreateBox("Ground_Base", parent, material, groundRect, GroundTopY, GroundThickness);
    }

    private static void CreateRoadsForCell(Transform parent, Material material, CellCoordinate2D cell, CityGenerationSettings settings, List<CellRect> allRoadRects)
    {
        CellRect mainRoad = ProceduralCityLayout.GetMainRoadRect(cell, settings);
        CellRect secondaryRoad = ProceduralCityLayout.GetSecondaryRoadRect(cell, settings);

        CreateBox($"MainAvenue_C{cell.X}_{cell.Z}", parent, material, mainRoad, RoadTopY, RoadThickness);
        CreateBox($"SecondaryRoad_C{cell.X}_{cell.Z}", parent, material, secondaryRoad, RoadTopY, RoadThickness);

        allRoadRects.Add(mainRoad);
        allRoadRects.Add(secondaryRoad);
    }

    private static void CreateSidewalksForCell(Transform parent, Material material, CellCoordinate2D cell, CityGenerationSettings settings)
    {
        string suffix = $"C{cell.X}_{cell.Z}";
        CreateBox($"Sidewalk_MainAvenue_North_{suffix}", parent, material, ProceduralCityLayout.GetMainSidewalkRect(cell, settings, north: true), SidewalkTopY, SidewalkThickness);
        CreateBox($"Sidewalk_MainAvenue_South_{suffix}", parent, material, ProceduralCityLayout.GetMainSidewalkRect(cell, settings, north: false), SidewalkTopY, SidewalkThickness);
        CreateBox($"Sidewalk_SecondaryRoad_West_South_{suffix}", parent, material, ProceduralCityLayout.GetSecondarySidewalkRect(cell, settings, east: false, north: false), SidewalkTopY, SidewalkThickness);
        CreateBox($"Sidewalk_SecondaryRoad_West_North_{suffix}", parent, material, ProceduralCityLayout.GetSecondarySidewalkRect(cell, settings, east: false, north: true), SidewalkTopY, SidewalkThickness);
        CreateBox($"Sidewalk_SecondaryRoad_East_South_{suffix}", parent, material, ProceduralCityLayout.GetSecondarySidewalkRect(cell, settings, east: true, north: false), SidewalkTopY, SidewalkThickness);
        CreateBox($"Sidewalk_SecondaryRoad_East_North_{suffix}", parent, material, ProceduralCityLayout.GetSecondarySidewalkRect(cell, settings, east: true, north: true), SidewalkTopY, SidewalkThickness);
    }

    private static void CreateDevelopmentBlocksForCell(Transform parent, Material material, CellCoordinate2D cell, CityGenerationSettings settings, List<CellRect> allBlockRects)
    {
        string suffix = $"C{cell.X}_{cell.Z}";
        foreach (DevelopmentBlockQuadrant quadrant in (DevelopmentBlockQuadrant[])System.Enum.GetValues(typeof(DevelopmentBlockQuadrant)))
        {
            CellRect rect = ProceduralCityLayout.GetBlockRect(cell, settings, quadrant);
            CreateBox($"Block_{quadrant}_{suffix}", parent, material, rect, BlockTopY, BlockThickness);
            allBlockRects.Add(rect);
        }
    }

    private static void CreateScaleReferences(Transform parent)
    {
        GameObject human = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        human.name = "Reference_Human_1_8m";
        human.transform.SetParent(parent);
        human.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
        human.transform.position = new Vector3(20f, 0.9f, 20f);

        GameObject parkingSpace = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parkingSpace.name = "Reference_ParkingSpace_2_5x5m";
        parkingSpace.transform.SetParent(parent);
        parkingSpace.transform.localScale = new Vector3(2.5f, 0.02f, 5f);
        parkingSpace.transform.position = new Vector3(30f, 0.01f, 20f);

        GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vehicle.name = "Reference_Vehicle_4_5m";
        vehicle.transform.SetParent(parent);
        vehicle.transform.localScale = new Vector3(1.8f, 1.5f, 4.5f);
        vehicle.transform.position = new Vector3(40f, 0.75f, 20f);

        GameObject floorHeight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorHeight.name = "Reference_FloorHeight_3m";
        floorHeight.transform.SetParent(parent);
        floorHeight.transform.localScale = new Vector3(0.3f, 3f, 0.3f);
        floorHeight.transform.position = new Vector3(50f, 1.5f, 20f);
    }

    private static void CreateCameraAndLight(Transform parent, CityGenerationSettings settings)
    {
        Vector2 center = ProceduralCityLayout.GetCityCenter(settings);
        Vector2 dimensions = ProceduralCityLayout.GetCityDimensions(settings);
        float height = Mathf.Max(dimensions.x, dimensions.y) * 0.9f;
        float zOffset = -Mathf.Max(dimensions.x, dimensions.y) * 0.25f;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent);
        cameraObject.transform.position = new Vector3(center.x, height, zOffset);
        cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.farClipPlane = 2000f;

        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(parent);
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light directionalLight = lightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 1.2f;
    }
}
