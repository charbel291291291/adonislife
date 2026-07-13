using System.Collections.Generic;
using AdonisLife.World.Authored;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.UrbanCell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdonisRoadDetailSetup
{
    private const string ScenePath = "Assets/Project/World/Scenes/ProceduralCity_Prototype.unity";
    private const string MaterialFolder = "Assets/Project/World/Materials/UrbanPrototype";
    private const string SettingsPath = "Assets/Project/World/Configs/ProceduralCitySettings.asset";

    private const string CityRootName = "_ProceduralCityPrototype";
    private const string DetailsRootName = "_RoadDetails";

    private const float IntersectionTopY = 0.055f;
    private const float IntersectionThickness = 0.01f;
    private const float MarkingTopY = 0.06f;
    private const float MarkingThickness = 0.02f;
    private const float DrainageTopY = 0.058f;
    private const float DrainageThickness = 0.016f;
    private const float MedianTopY = 0.30f;
    private const float MedianThickness = 0.25f;
    private const float IslandTopY = 0.15f;
    private const float IslandThickness = 0.08f;
    private const float CurbTopY = 0.26f;
    private const float CurbThickness = 0.06f;
    private const float UtilityTopY = 0.03f;
    private const float UtilityThickness = 0.05f;
    private const float StreetLightPoleHeight = 8f;
    private const float StreetLightPoleWidth = 0.2f;
    private const float SignPoleHeight = 2.5f;
    private const float SignPoleWidth = 0.1f;
    private const float SignBoardSize = 0.6f;

    private static readonly string[] DetailGroupNames =
    {
        "_Intersections", "_LaneMarkings", "_Medians", "_PedestrianCrossings", "_TrafficIslands",
        "_Curbs", "_Drainage", "_UtilityStrips", "_StreetLights", "_TrafficSigns"
    };

    [MenuItem("Adonis Life/World/Add Road Details To Procedural City")]
    public static void AddRoadDetails()
    {
        ProceduralCitySettingsSO settingsAsset = AssetDatabase.LoadAssetAtPath<ProceduralCitySettingsSO>(SettingsPath);
        if (settingsAsset == null)
        {
            Debug.LogError($"Road detail generation aborted: settings asset not found at '{SettingsPath}'.");
            return;
        }

        if (!settingsAsset.IsValid(out string settingsError))
        {
            Debug.LogError($"Road detail generation aborted: {settingsError}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"Road detail generation aborted: could not open scene '{ScenePath}'.");
            return;
        }

        GameObject cityRoot = GameObject.Find(CityRootName);
        if (cityRoot == null)
        {
            Debug.LogError($"Road detail generation aborted: '{CityRootName}' root not found in scene.");
            return;
        }

        if (cityRoot.transform.Find(DetailsRootName) != null)
        {
            Debug.LogWarning($"'{DetailsRootName}' already exists in '{ScenePath}'. Skipping generation.");
            return;
        }

        CityGenerationSettings settings = settingsAsset.ToGenerationSettings();

        Material markingMaterial = GetOrCreateMaterial("LaneMarking_Material", new Color(0.92f, 0.92f, 0.90f));
        Material crossingMaterial = GetOrCreateMaterial("Crossing_Material", new Color(0.85f, 0.85f, 0.82f));
        Material intersectionMaterial = GetOrCreateMaterial("Intersection_Material", new Color(0.16f, 0.16f, 0.16f));
        Material medianMaterial = GetOrCreateMaterial("Median_Material", new Color(0.42f, 0.52f, 0.40f));
        Material islandMaterial = GetOrCreateMaterial("TrafficIsland_Material", new Color(0.60f, 0.60f, 0.58f));
        Material curbMaterial = GetOrCreateMaterial("Curb_Material", new Color(0.66f, 0.64f, 0.60f));
        Material drainageMaterial = GetOrCreateMaterial("Drainage_Material", new Color(0.09f, 0.09f, 0.10f));
        Material utilityMaterial = GetOrCreateMaterial("UtilityStrip_Material", new Color(0.30f, 0.30f, 0.36f));
        Material poleMaterial = GetOrCreateMaterial("Pole_Material", new Color(0.25f, 0.25f, 0.27f));
        Material signMaterial = GetOrCreateMaterial("TrafficSign_Material", new Color(0.72f, 0.10f, 0.10f));

        GameObject detailsRoot = new GameObject(DetailsRootName);
        detailsRoot.transform.SetParent(cityRoot.transform);

        Dictionary<string, Transform> groups = new Dictionary<string, Transform>();
        foreach (string groupName in DetailGroupNames)
        {
            GameObject group = new GameObject(groupName);
            group.transform.SetParent(detailsRoot.transform);
            groups[groupName] = group.transform;
        }

        foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
        {
            string suffix = $"C{cell.X}_{cell.Z}";

            CreateBox($"Intersection_{suffix}", groups["_Intersections"], intersectionMaterial,
                RoadDetailLayout.GetIntersectionRect(cell, settings), IntersectionTopY, IntersectionThickness);

            CreateBoxes($"LaneDash_{suffix}", groups["_LaneMarkings"], markingMaterial,
                RoadDetailLayout.GetLaneDashRects(cell, settings), MarkingTopY, MarkingThickness);

            CreateBoxes($"Median_{suffix}", groups["_Medians"], medianMaterial,
                RoadDetailLayout.GetMedianRects(cell, settings), MedianTopY, MedianThickness);

            CreateBoxes($"Crossing_{suffix}", groups["_PedestrianCrossings"], crossingMaterial,
                RoadDetailLayout.GetCrossingRects(cell, settings), MarkingTopY, MarkingThickness);

            CreateBoxes($"RefugeIsland_{suffix}", groups["_TrafficIslands"], islandMaterial,
                RoadDetailLayout.GetRefugeIslandRects(cell, settings), IslandTopY, IslandThickness);

            CreateBoxes($"Curb_{suffix}", groups["_Curbs"], curbMaterial,
                RoadDetailLayout.GetCurbRects(cell, settings), CurbTopY, CurbThickness);

            CreateBoxes($"CornerCurb_{suffix}", groups["_Curbs"], curbMaterial,
                RoadDetailLayout.GetCornerCurbRects(cell, settings), CurbTopY, CurbThickness);

            CreateBoxes($"Drainage_{suffix}", groups["_Drainage"], drainageMaterial,
                RoadDetailLayout.GetDrainageRects(cell, settings), DrainageTopY, DrainageThickness);

            CreateBoxes($"UtilityStrip_{suffix}", groups["_UtilityStrips"], utilityMaterial,
                RoadDetailLayout.GetUtilityStripRects(cell, settings), UtilityTopY, UtilityThickness);

            CreateStreetLights(suffix, groups["_StreetLights"], poleMaterial,
                RoadDetailLayout.GetStreetLightPositions(cell, settings));

            CreateTrafficSigns(suffix, groups["_TrafficSigns"], poleMaterial, signMaterial,
                RoadDetailLayout.GetTrafficSignPositions(cell, settings));
        }

        if (!ValidateDetails(detailsRoot.transform, settings, out string validationError))
        {
            Debug.LogError($"Road detail validation failed: {validationError}");
            return;
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Road details added to '{ScenePath}'.");
    }

    private static bool ValidateDetails(Transform detailsRoot, CityGenerationSettings settings, out string error)
    {
        foreach (string groupName in DetailGroupNames)
        {
            Transform group = detailsRoot.Find(groupName);
            if (group == null)
            {
                error = $"Detail group '{groupName}' is missing.";
                return false;
            }

            if (group.childCount == 0)
            {
                error = $"Detail group '{groupName}' is empty.";
                return false;
            }
        }

        int cellCount = settings.CellsX * settings.CellsZ;
        int intersections = detailsRoot.Find("_Intersections").childCount;
        if (intersections != cellCount)
        {
            error = $"Expected {cellCount} intersection patches, found {intersections}.";
            return false;
        }

        int crossings = detailsRoot.Find("_PedestrianCrossings").childCount;
        if (crossings != cellCount * 4)
        {
            error = $"Expected {cellCount * 4} pedestrian crossings, found {crossings}.";
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

    private static void CreateBoxes(string baseName, Transform parent, Material material, List<CellRect> rects, float topY, float thickness)
    {
        for (int i = 0; i < rects.Count; i++)
        {
            CreateBox($"{baseName}_{i}", parent, material, rects[i], topY, thickness);
        }
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

    private static void CreateStreetLights(string suffix, Transform parent, Material poleMaterial, List<Vector2> positions)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pole.name = $"StreetLight_{suffix}_{i}";
            pole.transform.SetParent(parent);
            pole.transform.localScale = new Vector3(StreetLightPoleWidth, StreetLightPoleHeight, StreetLightPoleWidth);
            pole.transform.position = new Vector3(positions[i].x, StreetLightPoleHeight / 2f, positions[i].y);
            pole.GetComponent<MeshRenderer>().sharedMaterial = poleMaterial;
        }
    }

    private static void CreateTrafficSigns(string suffix, Transform parent, Material poleMaterial, Material signMaterial, List<Vector2> positions)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            GameObject sign = new GameObject($"TrafficSign_{suffix}_{i}");
            sign.transform.SetParent(parent);
            sign.transform.position = new Vector3(positions[i].x, 0f, positions[i].y);

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pole.name = "Pole";
            pole.transform.SetParent(sign.transform);
            pole.transform.localScale = new Vector3(SignPoleWidth, SignPoleHeight, SignPoleWidth);
            pole.transform.localPosition = new Vector3(0f, SignPoleHeight / 2f, 0f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = poleMaterial;

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            board.transform.SetParent(sign.transform);
            board.transform.localScale = new Vector3(SignBoardSize, SignBoardSize, 0.05f);
            board.transform.localPosition = new Vector3(0f, SignPoleHeight + SignBoardSize / 2f, 0f);
            board.GetComponent<MeshRenderer>().sharedMaterial = signMaterial;
        }
    }
}
