using System.IO;
using AdonisLife.World.Authored;
using AdonisLife.World.UrbanCell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdonisUrbanCellSetup
{
    private const string SceneFolder = "Assets/Project/World/Scenes";
    private const string ScenePath = SceneFolder + "/CityBase_Prototype.unity";
    private const string MaterialFolder = "Assets/Project/World/Materials/UrbanPrototype";
    private const string WorldConfigPath = "Assets/Project/World/Configs/WorldConfig.asset";

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
        "_Ground", "_Roads", "_Sidewalks", "_DevelopmentBlocks", "_References", "_Environment"
    };

    [MenuItem("Adonis Life/World/Create Urban Base Cell Prototype")]
    public static void CreateUrbanBaseCellPrototype()
    {
        if (File.Exists(ScenePath))
        {
            Debug.LogWarning($"Urban cell prototype scene already exists at '{ScenePath}'. Skipping creation.");
            return;
        }

        if (!ValidateWorldConfig(out string worldConfigError))
        {
            Debug.LogError($"Urban cell prototype creation aborted: {worldConfigError}");
            return;
        }

        Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();

        Material groundMaterial = GetOrCreateMaterial("UrbanGround_Material", new Color(0.55f, 0.55f, 0.55f));
        Material roadMaterial = GetOrCreateMaterial("Road_Material", new Color(0.12f, 0.12f, 0.12f));
        Material sidewalkMaterial = GetOrCreateMaterial("Sidewalk_Material", new Color(0.75f, 0.72f, 0.68f));
        Material blockMaterial = GetOrCreateMaterial("DevelopmentBlock_Material", new Color(0.35f, 0.55f, 0.35f));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("_UrbanCellPrototype");
        GameObject groundRoot = new GameObject("_Ground");
        GameObject roadsRoot = new GameObject("_Roads");
        GameObject sidewalksRoot = new GameObject("_Sidewalks");
        GameObject blocksRoot = new GameObject("_DevelopmentBlocks");
        GameObject referencesRoot = new GameObject("_References");
        GameObject environmentRoot = new GameObject("_Environment");

        groundRoot.transform.SetParent(root.transform);
        roadsRoot.transform.SetParent(root.transform);
        sidewalksRoot.transform.SetParent(root.transform);
        blocksRoot.transform.SetParent(root.transform);
        referencesRoot.transform.SetParent(root.transform);
        environmentRoot.transform.SetParent(root.transform);

        CreateGround(groundRoot.transform, groundMaterial);
        CreateRoads(roadsRoot.transform, roadMaterial);
        CreateSidewalks(sidewalksRoot.transform, sidewalkMaterial);
        CreateDevelopmentBlocks(blocksRoot.transform, blockMaterial);
        CreateScaleReferences(referencesRoot.transform);
        CreateCameraAndLight(environmentRoot.transform);

        if (!ValidateScene(root.transform, out string sceneError))
        {
            Debug.LogError($"Urban cell prototype validation failed: {sceneError}");
            return;
        }

        Directory.CreateDirectory(SceneFolder);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Urban base cell prototype created at '{ScenePath}'.");
    }

    private static bool ValidateWorldConfig(out string error)
    {
        WorldConfigSO config = AssetDatabase.LoadAssetAtPath<WorldConfigSO>(WorldConfigPath);
        if (config == null)
        {
            error = $"WorldConfig asset not found at '{WorldConfigPath}'.";
            return false;
        }

        if (!config.IsValid(out string validationError))
        {
            error = $"WorldConfig failed validation: {validationError}";
            return false;
        }

        if (!Mathf.Approximately(config.ChunkSize, UrbanCellLayout.CellSize))
        {
            error = $"WorldConfig chunk size ({config.ChunkSize}) does not match the urban cell size ({UrbanCellLayout.CellSize}).";
            return false;
        }

        error = null;
        return true;
    }

    private static bool ValidateScene(Transform root, out string error)
    {
        foreach (string childName in RequiredHierarchyChildren)
        {
            if (root.Find(childName) == null)
            {
                error = $"Required hierarchy object '{childName}' is missing.";
                return false;
            }
        }

        CellRect groundRect = new CellRect(0f, UrbanCellLayout.CellSize, 0f, UrbanCellLayout.CellSize);
        if (!Mathf.Approximately(groundRect.Width, 250f) || !Mathf.Approximately(groundRect.Depth, 250f))
        {
            error = "Ground dimensions do not equal 250 x 250.";
            return false;
        }

        CellRect mainRoad = UrbanCellLayout.GetMainRoadRect();
        if (!Mathf.Approximately(mainRoad.Depth, UrbanCellLayout.MainRoadWidth))
        {
            error = "Main road width mismatch.";
            return false;
        }

        CellRect secondaryRoad = UrbanCellLayout.GetSecondaryRoadRect();
        if (!Mathf.Approximately(secondaryRoad.Width, UrbanCellLayout.SecondaryRoadWidth))
        {
            error = "Secondary road width mismatch.";
            return false;
        }

        error = null;
        return true;
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

    private static void CreateGround(Transform parent, Material material)
    {
        CellRect groundRect = new CellRect(0f, UrbanCellLayout.CellSize, 0f, UrbanCellLayout.CellSize);
        CreateBox("Ground_Base", parent, material, groundRect, GroundTopY, GroundThickness);
    }

    private static void CreateRoads(Transform parent, Material material)
    {
        CreateBox("MainAvenue", parent, material, UrbanCellLayout.GetMainRoadRect(), RoadTopY, RoadThickness);
        CreateBox("SecondaryRoad", parent, material, UrbanCellLayout.GetSecondaryRoadRect(), RoadTopY, RoadThickness);
    }

    private static void CreateSidewalks(Transform parent, Material material)
    {
        CreateBox("Sidewalk_MainAvenue_North", parent, material, UrbanCellLayout.GetMainSidewalkRect(north: true), SidewalkTopY, SidewalkThickness);
        CreateBox("Sidewalk_MainAvenue_South", parent, material, UrbanCellLayout.GetMainSidewalkRect(north: false), SidewalkTopY, SidewalkThickness);
        CreateBox("Sidewalk_SecondaryRoad_West_South", parent, material, UrbanCellLayout.GetSecondarySidewalkRect(east: false, north: false), SidewalkTopY, SidewalkThickness);
        CreateBox("Sidewalk_SecondaryRoad_West_North", parent, material, UrbanCellLayout.GetSecondarySidewalkRect(east: false, north: true), SidewalkTopY, SidewalkThickness);
        CreateBox("Sidewalk_SecondaryRoad_East_South", parent, material, UrbanCellLayout.GetSecondarySidewalkRect(east: true, north: false), SidewalkTopY, SidewalkThickness);
        CreateBox("Sidewalk_SecondaryRoad_East_North", parent, material, UrbanCellLayout.GetSecondarySidewalkRect(east: true, north: true), SidewalkTopY, SidewalkThickness);
    }

    private static void CreateDevelopmentBlocks(Transform parent, Material material)
    {
        CreateBox("Block_NW", parent, material, UrbanCellLayout.GetBlockRect(UrbanCellBlock.NW), BlockTopY, BlockThickness);
        CreateBox("Block_NE", parent, material, UrbanCellLayout.GetBlockRect(UrbanCellBlock.NE), BlockTopY, BlockThickness);
        CreateBox("Block_SW", parent, material, UrbanCellLayout.GetBlockRect(UrbanCellBlock.SW), BlockTopY, BlockThickness);
        CreateBox("Block_SE", parent, material, UrbanCellLayout.GetBlockRect(UrbanCellBlock.SE), BlockTopY, BlockThickness);
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

    private static void CreateCameraAndLight(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent);
        cameraObject.transform.position = new Vector3(UrbanCellLayout.CellCenter, 220f, -60f);
        cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.farClipPlane = 1000f;

        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(parent);
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light directionalLight = lightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 1.2f;
    }
}
