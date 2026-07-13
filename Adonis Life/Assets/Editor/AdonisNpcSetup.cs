using AdonisLife.World.Authored;
using AdonisLife.World.Npc;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdonisNpcSetup
{
    private const string ScenePath = "Assets/Project/World/Scenes/ProceduralCity_Prototype.unity";
    private const string SettingsPath = "Assets/Project/World/Configs/ProceduralCitySettings.asset";
    private const string NpcRootName = "_Npc";

    [MenuItem("Adonis Life/World/Add NPC Systems")]
    public static void AddNpcSystems()
    {
        ProceduralCitySettingsSO settingsAsset = AssetDatabase.LoadAssetAtPath<ProceduralCitySettingsSO>(SettingsPath);
        if (settingsAsset == null || !settingsAsset.IsValid(out _))
        {
            Debug.LogError("NPC setup aborted: settings asset missing or invalid.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject cityRoot = GameObject.Find("_ProceduralCityPrototype");
        if (cityRoot == null)
        {
            Debug.LogError("NPC setup aborted: city root not found.");
            return;
        }

        if (cityRoot.transform.Find(NpcRootName) != null)
        {
            Debug.LogWarning("NPC systems already exist. Skipping.");
            return;
        }

        GameObject npcRoot = new GameObject(NpcRootName);
        npcRoot.transform.SetParent(cityRoot.transform);

        NpcSpawnManager manager = npcRoot.AddComponent<NpcSpawnManager>();
        var serialized = new SerializedObject(manager);
        serialized.FindProperty("_citySettings").objectReferenceValue = settingsAsset;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"NPC systems added to '{ScenePath}'.");
    }
}
