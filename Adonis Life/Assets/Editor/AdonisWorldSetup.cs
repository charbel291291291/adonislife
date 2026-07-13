using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdonisWorldSetup
{
    public static void CreateWorldPrototype()
    {
        const string sceneFolder = "Assets/Project/World/Scenes";
        const string materialFolder = "Assets/Project/World/Materials";
        const string scenePath = sceneFolder + "/World_Prototype.unity";

        Directory.CreateDirectory(sceneFolder);
        Directory.CreateDirectory(materialFolder);

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        GameObject worldRoot = new GameObject("_World");
        GameObject districtsRoot = new GameObject("_Districts");
        GameObject environmentRoot = new GameObject("_Environment");
        GameObject systemsRoot = new GameObject("_Systems");

        districtsRoot.transform.SetParent(worldRoot.transform);
        environmentRoot.transform.SetParent(worldRoot.transform);
        systemsRoot.transform.SetParent(worldRoot.transform);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Adonis_Ground";
        ground.transform.SetParent(environmentRoot.transform);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(200f, 1f, 200f);

        string[] districtNames =
        {
            "D01_Historic_Center",
            "D02_Marina",
            "D03_Beach",
            "D04_Business",
            "D05_Residential_East",
            "D06_Residential_West",
            "D07_University",
            "D08_Tech_Park",
            "D09_Hills",
            "D10_Nature_Reserve"
        };

        Vector3[] positions =
        {
            new Vector3(-600f, 1f, -200f),
            new Vector3(-600f, 1f, -600f),
            new Vector3(200f, 1f, -600f),
            new Vector3(200f, 1f, -200f),
            new Vector3(600f, 1f, 200f),
            new Vector3(-200f, 1f, 200f),
            new Vector3(-600f, 1f, 200f),
            new Vector3(200f, 1f, 200f),
            new Vector3(600f, 1f, 600f),
            new Vector3(-200f, 1f, 600f)
        };

        Color[] colors =
        {
            new Color(0.70f, 0.45f, 0.25f),
            new Color(0.10f, 0.45f, 0.80f),
            new Color(0.90f, 0.75f, 0.35f),
            new Color(0.35f, 0.35f, 0.45f),
            new Color(0.75f, 0.55f, 0.45f),
            new Color(0.65f, 0.45f, 0.55f),
            new Color(0.45f, 0.35f, 0.75f),
            new Color(0.20f, 0.65f, 0.75f),
            new Color(0.45f, 0.60f, 0.30f),
            new Color(0.15f, 0.50f, 0.20f)
        };

        for (int i = 0; i < districtNames.Length; i++)
        {
            GameObject district = GameObject.CreatePrimitive(PrimitiveType.Cube);
            district.name = districtNames[i];
            district.transform.SetParent(districtsRoot.transform);
            district.transform.position = positions[i];
            district.transform.localScale = new Vector3(380f, 2f, 380f);

            Material material = new Material(
                Shader.Find("Universal Render Pipeline/Lit")
            );

            material.name = districtNames[i] + "_Material";
            material.color = colors[i];

            string materialPath =
                materialFolder + "/" + material.name + ".mat";

            AssetDatabase.CreateAsset(material, materialPath);
            district.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(environmentRoot.transform);
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light directionalLight = lightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 1.2f;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 1700f, -1500f);
        cameraObject.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;

        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("World_Prototype created successfully.");
        EditorApplication.Exit(0);
    }
}
