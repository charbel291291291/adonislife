using AdonisLife.Gameplay;
using AdonisLife.World.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdonisGameplaySetup
{
    private const string ScenePath = "Assets/Project/World/Scenes/ProceduralCity_Prototype.unity";
    private const string GameplayRootName = "_Gameplay";

    private const float SpawnX = 375f;
    private const float SpawnZ = 100f;
    private const float PlayerHeight = 1.8f;

    [MenuItem("Adonis Life/World/Add Gameplay Systems")]
    public static void AddGameplaySystems()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject cityRoot = GameObject.Find("_ProceduralCityPrototype");
        if (cityRoot == null)
        {
            Debug.LogError("Gameplay setup aborted: city root not found.");
            return;
        }

        if (cityRoot.transform.Find(GameplayRootName) != null)
        {
            Debug.LogWarning("Gameplay systems already exist. Skipping.");
            return;
        }

        GameObject gameplayRoot = new GameObject(GameplayRootName);
        gameplayRoot.transform.SetParent(cityRoot.transform);

        // Player: capsule visual on a CharacterController, spawned on a sidewalk.
        GameObject player = new GameObject("Player");
        player.transform.SetParent(gameplayRoot.transform);
        player.transform.position = new Vector3(SpawnX, PlayerHeight / 2f + 0.2f, SpawnZ);

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = PlayerHeight;
        characterController.radius = 0.35f;
        characterController.center = Vector3.zero;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(player.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(0.7f, PlayerHeight / 2f, 0.7f);
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        player.AddComponent<PlayerController>();
        player.AddComponent<Interactor>();

        // Player camera rig.
        GameObject cameraObject = new GameObject("PlayerCamera");
        cameraObject.transform.SetParent(gameplayRoot.transform);
        cameraObject.transform.position = player.transform.position + new Vector3(0f, 4f, -8f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.farClipPlane = 2000f;
        camera.depth = 10f;

        ThirdPersonCamera follow = cameraObject.AddComponent<ThirdPersonCamera>();
        var followSerialized = new SerializedObject(follow);
        followSerialized.FindProperty("_target").objectReferenceValue = player.transform;
        followSerialized.ApplyModifiedPropertiesWithoutUndo();

        // Service hub wired to the player and the world clock.
        GameObject services = new GameObject("GameplayServices");
        services.transform.SetParent(gameplayRoot.transform);
        GameplayServices hub = services.AddComponent<GameplayServices>();

        var hubSerialized = new SerializedObject(hub);
        hubSerialized.FindProperty("_player").objectReferenceValue = player.transform;
        hubSerialized.FindProperty("_dayNight").objectReferenceValue = Object.FindFirstObjectByType<DayNightCycle>();
        hubSerialized.FindProperty("_weather").objectReferenceValue = Object.FindFirstObjectByType<WeatherSystem>();
        hubSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Gameplay systems added to '{ScenePath}'.");
    }
}
