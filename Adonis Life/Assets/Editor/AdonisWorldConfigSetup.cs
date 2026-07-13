using System.IO;
using AdonisLife.World.Authored;
using UnityEditor;
using UnityEngine;

public static class AdonisWorldConfigSetup
{
    private const string ConfigParentFolder = "Assets/Project/World";
    private const string ConfigFolderName = "Configs";
    private const string ConfigFolder = ConfigParentFolder + "/" + ConfigFolderName;
    private const string ConfigAssetPath = ConfigFolder + "/WorldConfig.asset";

    [MenuItem("Adonis Life/World/Create Default World Config")]
    public static void CreateDefaultWorldConfig()
    {
        if (File.Exists(ConfigAssetPath))
        {
            Debug.LogWarning($"WorldConfig asset already exists at '{ConfigAssetPath}'. Skipping creation.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(ConfigFolder))
        {
            AssetDatabase.CreateFolder(ConfigParentFolder, ConfigFolderName);
        }

        WorldConfigSO config = ScriptableObject.CreateInstance<WorldConfigSO>();

        if (!config.IsValid(out string validationError))
        {
            Debug.LogError($"WorldConfig failed validation and was not saved: {validationError}");
            Object.DestroyImmediate(config);
            return;
        }

        AssetDatabase.CreateAsset(config, ConfigAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"WorldConfig asset created at '{ConfigAssetPath}'.");
        Debug.Log($"Calculated grid: {config.ChunkCountX} x {config.ChunkCountZ} chunks.");
    }
}
