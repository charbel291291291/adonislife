using System.Collections.Generic;
using System.IO;
using AdonisLife.World.Authored;
using AdonisLife.World.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Project-wide validation suite: checks authored assets, generated scenes, and optimization
/// state in one pass. Runs from the menu or in CI batch mode (exits nonzero on failure).
/// </summary>
public static class AdonisValidationSuite
{
    private const string WorldConfigPath = "Assets/Project/World/Configs/WorldConfig.asset";
    private const string CitySettingsPath = "Assets/Project/World/Configs/ProceduralCitySettings.asset";
    private const string TerrainSettingsPath = "Assets/Project/World/Configs/TerrainSettings.asset";
    private const string CityScenePath = "Assets/Project/World/Scenes/ProceduralCity_Prototype.unity";
    private const string MaterialsRootFolder = "Assets/Project/World/Materials";

    private static readonly string[] RequiredScenes =
    {
        "Assets/Project/World/Scenes/World_Prototype.unity",
        "Assets/Project/World/Scenes/CityBase_Prototype.unity",
        CityScenePath,
        "Assets/Project/World/Scenes/Terrain_Prototype.unity"
    };

    private static readonly string[] RequiredCityGroups =
    {
        "_Ground", "_RoadNetwork", "_Sidewalks", "_DevelopmentBlocks", "_References", "_Environment",
        "_RoadDetails", "_Buildings", "_Infrastructure", "_Environment_Green", "_Traffic", "_Npc",
        "_Gameplay", "_DevTools"
    };

    [MenuItem("Adonis Life/Tools/Run Validation Suite")]
    public static void RunAll()
    {
        var failures = new List<string>();

        ValidateAuthoredAssets(failures);
        ValidateScenes(failures);
        ValidateCityHierarchy(failures);
        ValidateMaterials(failures);
        ValidateStatistics(failures);

        if (failures.Count == 0)
        {
            Debug.Log("Validation suite PASSED: all checks succeeded.");
            return;
        }

        foreach (string failure in failures)
        {
            Debug.LogError($"Validation FAILED: {failure}");
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateAuthoredAssets(List<string> failures)
    {
        WorldConfigSO worldConfig = AssetDatabase.LoadAssetAtPath<WorldConfigSO>(WorldConfigPath);
        if (worldConfig == null)
        {
            failures.Add($"WorldConfig missing at '{WorldConfigPath}'.");
        }
        else if (!worldConfig.IsValid(out string worldError))
        {
            failures.Add($"WorldConfig invalid: {worldError}");
        }

        ProceduralCitySettingsSO city = AssetDatabase.LoadAssetAtPath<ProceduralCitySettingsSO>(CitySettingsPath);
        if (city == null)
        {
            failures.Add($"City settings missing at '{CitySettingsPath}'.");
        }
        else if (!city.IsValid(out string cityError))
        {
            failures.Add($"City settings invalid: {cityError}");
        }

        TerrainSettingsSO terrain = AssetDatabase.LoadAssetAtPath<TerrainSettingsSO>(TerrainSettingsPath);
        if (terrain == null)
        {
            failures.Add($"Terrain settings missing at '{TerrainSettingsPath}'.");
        }
        else if (!terrain.IsValid(out string terrainError))
        {
            failures.Add($"Terrain settings invalid: {terrainError}");
        }
    }

    private static void ValidateScenes(List<string> failures)
    {
        foreach (string scenePath in RequiredScenes)
        {
            if (!File.Exists(scenePath))
            {
                failures.Add($"Required scene missing: '{scenePath}'.");
            }
        }
    }

    private static void ValidateCityHierarchy(List<string> failures)
    {
        if (!File.Exists(CityScenePath))
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(CityScenePath, OpenSceneMode.Single);
        GameObject cityRoot = GameObject.Find("_ProceduralCityPrototype");
        if (cityRoot == null)
        {
            failures.Add("City scene has no '_ProceduralCityPrototype' root.");
            return;
        }

        foreach (string groupName in RequiredCityGroups)
        {
            if (cityRoot.transform.Find(groupName) == null)
            {
                failures.Add($"City scene is missing group '{groupName}'.");
            }
        }
    }

    private static void ValidateMaterials(List<string> failures)
    {
        int total = 0;
        int missingInstancing = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialsRootFolder }))
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material == null)
            {
                continue;
            }

            total++;
            if (!material.enableInstancing)
            {
                missingInstancing++;
            }
        }

        if (total == 0)
        {
            failures.Add("No materials found under the materials root.");
        }

        if (missingInstancing > 0)
        {
            failures.Add($"{missingInstancing} materials lack GPU instancing (run the optimization pass).");
        }
    }

    private static void ValidateStatistics(List<string> failures)
    {
        ProceduralCitySettingsSO city = AssetDatabase.LoadAssetAtPath<ProceduralCitySettingsSO>(CitySettingsPath);
        if (city == null || !city.IsValid(out _))
        {
            return;
        }

        CityStatisticsReport report = CityStatistics.Compute(city.ToGenerationSettings());
        if (report.BuildingCount <= 0 || report.RoadSegmentCount <= 0)
        {
            failures.Add("City statistics report empty generation output.");
        }
    }
}
