using System.Collections.Generic;
using AdonisLife.World.Authored;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.Traffic;
using AdonisLife.World.UrbanCell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdonisTrafficSetup
{
    private const string ScenePath = "Assets/Project/World/Scenes/ProceduralCity_Prototype.unity";
    private const string MaterialFolder = "Assets/Project/World/Materials/Infrastructure";
    private const string SettingsPath = "Assets/Project/World/Configs/ProceduralCitySettings.asset";

    private const string TrafficRootName = "_Traffic";

    private const float PoleHeight = 6f;
    private const float PoleWidth = 0.15f;
    private const float LampSize = 0.35f;
    private const float TopLampY = 5.6f;
    private const float CornerPull = 0.8f;

    [MenuItem("Adonis Life/World/Add Traffic Systems")]
    public static void AddTrafficSystems()
    {
        ProceduralCitySettingsSO settingsAsset = AssetDatabase.LoadAssetAtPath<ProceduralCitySettingsSO>(SettingsPath);
        if (settingsAsset == null || !settingsAsset.IsValid(out _))
        {
            Debug.LogError("Traffic setup aborted: settings asset missing or invalid.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject cityRoot = GameObject.Find("_ProceduralCityPrototype");
        if (cityRoot == null)
        {
            Debug.LogError("Traffic setup aborted: city root not found.");
            return;
        }

        if (cityRoot.transform.Find(TrafficRootName) != null)
        {
            Debug.LogWarning("Traffic systems already exist. Skipping.");
            return;
        }

        CityGenerationSettings settings = settingsAsset.ToGenerationSettings();

        Material poleMaterial = GetOrCreateMaterial("Pole_Material", new Color(0.25f, 0.25f, 0.27f));
        Material lampOffMaterial = GetOrCreateMaterial("LampHousing_Material", new Color(0.10f, 0.10f, 0.10f));

        GameObject trafficRoot = new GameObject(TrafficRootName);
        trafficRoot.transform.SetParent(cityRoot.transform);
        Transform lightsGroup = CreateGroup("_TrafficLights", trafficRoot.transform);

        foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
        {
            CellRect intersection = RoadDetailLayout.GetIntersectionRect(cell, settings);
            var intersectionCenter = new Vector2(intersection.CenterX, intersection.CenterZ);

            GameObject intersectionGroup = new GameObject($"Intersection_C{cell.X}_{cell.Z}");
            intersectionGroup.transform.SetParent(lightsGroup);
            intersectionGroup.AddComponent<TrafficLightController>();

            List<CellRect> pads = RoadDetailLayout.GetCornerCurbRects(cell, settings);
            for (int i = 0; i < pads.Count; i++)
            {
                // Pads 0 and 3 (SW/NE) control the north-south axis; 1 and 2 the east-west axis.
                string axisPrefix = (i == 0 || i == 3)
                    ? TrafficLightController.NorthSouthLampPrefix
                    : TrafficLightController.EastWestLampPrefix;
                Vector2 position = GetPolePosition(pads[i], intersectionCenter);
                CreateTrafficLightPost($"TrafficLight_{i}", intersectionGroup.transform,
                    poleMaterial, lampOffMaterial, position, axisPrefix);
            }
        }

        GameObject systems = new GameObject("TrafficSystems");
        systems.transform.SetParent(trafficRoot.transform);
        VehicleSpawner spawner = systems.AddComponent<VehicleSpawner>();
        var serialized = new SerializedObject(spawner);
        serialized.FindProperty("_citySettings").objectReferenceValue = settingsAsset;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Traffic systems added to '{ScenePath}'.");
    }

    private static Vector2 GetPolePosition(CellRect pad, Vector2 intersectionCenter)
    {
        float x = pad.CenterX < intersectionCenter.x ? pad.XMax - CornerPull : pad.XMin + CornerPull;
        float z = pad.CenterZ < intersectionCenter.y ? pad.ZMax - CornerPull : pad.ZMin + CornerPull;
        return new Vector2(x, z);
    }

    private static void CreateTrafficLightPost(
        string name, Transform parent, Material poleMaterial, Material lampMaterial,
        Vector2 position, string lampPrefix)
    {
        GameObject post = new GameObject(name);
        post.transform.SetParent(parent);
        post.transform.position = new Vector3(position.x, 0f, position.y);

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.name = "Pole";
        pole.transform.SetParent(post.transform);
        pole.transform.localScale = new Vector3(PoleWidth, PoleHeight, PoleWidth);
        pole.transform.localPosition = new Vector3(0f, PoleHeight / 2f, 0f);
        pole.GetComponent<MeshRenderer>().sharedMaterial = poleMaterial;

        string[] lampNames = { "Red", "Yellow", "Green" };
        for (int i = 0; i < lampNames.Length; i++)
        {
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lamp.name = $"{lampPrefix}{lampNames[i]}";
            lamp.transform.SetParent(post.transform);
            lamp.transform.localScale = new Vector3(LampSize, LampSize, LampSize);
            lamp.transform.localPosition = new Vector3(0f, TopLampY - i * (LampSize + 0.1f), 0f);
            lamp.GetComponent<MeshRenderer>().sharedMaterial = lampMaterial;
        }
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent);
        return group.transform;
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
}
