using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public static class AdonisOptimizationSetup
{
    private const string CityScenePath = "Assets/Project/World/Scenes/ProceduralCity_Prototype.unity";
    private const string TerrainScenePath = "Assets/Project/World/Scenes/Terrain_Prototype.unity";
    private const string MaterialsRootFolder = "Assets/Project/World/Materials";

    private const float LodCullScreenHeight = 0.015f;

    private static readonly string[] CityStaticGroups =
    {
        "_Ground", "_RoadNetwork", "_Sidewalks", "_DevelopmentBlocks", "_RoadDetails",
        "_Buildings", "_Infrastructure", "_Environment_Green", "_References"
    };

    private static readonly string[] CityLodGroups = { "_Buildings", "_Environment_Green" };

    [MenuItem("Adonis Life/World/Run Optimization Pass")]
    public static void RunOptimizationPass()
    {
        int instancedMaterials = EnableGpuInstancing();
        (int lodGroups, int staticObjects) = OptimizeCityScene();
        int terrainLodGroups = OptimizeTerrainScene();

        if (instancedMaterials == 0 || lodGroups == 0 || staticObjects == 0)
        {
            Debug.LogError(
                $"Optimization validation failed: materials={instancedMaterials}, lodGroups={lodGroups}, staticObjects={staticObjects}.");
            return;
        }

        ReportSceneStatistics();
        Debug.Log($"Optimization pass complete: {instancedMaterials} materials instanced, " +
                  $"{lodGroups} city + {terrainLodGroups} terrain LOD groups, {staticObjects} static objects.");
    }

    private static int EnableGpuInstancing()
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialsRootFolder }))
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material == null)
            {
                continue;
            }

            if (!material.enableInstancing)
            {
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
            }

            count++;
        }

        AssetDatabase.SaveAssets();
        return count;
    }

    private static (int lodGroups, int staticObjects) OptimizeCityScene()
    {
        Scene scene = EditorSceneManager.OpenScene(CityScenePath, OpenSceneMode.Single);
        GameObject cityRoot = GameObject.Find("_ProceduralCityPrototype");
        if (cityRoot == null)
        {
            Debug.LogError("Optimization aborted: city root not found.");
            return (0, 0);
        }

        int lodGroups = 0;
        foreach (string groupName in CityLodGroups)
        {
            Transform group = cityRoot.transform.Find(groupName);
            if (group != null)
            {
                lodGroups += AddCullingLodGroups(group);
            }
        }

        int staticObjects = 0;
        foreach (string groupName in CityStaticGroups)
        {
            Transform group = cityRoot.transform.Find(groupName);
            if (group != null)
            {
                staticObjects += MarkStaticRecursive(group);
            }
        }

        EditorSceneManager.SaveScene(scene, CityScenePath);
        return (lodGroups, staticObjects);
    }

    private static int OptimizeTerrainScene()
    {
        Scene scene = EditorSceneManager.OpenScene(TerrainScenePath, OpenSceneMode.Single);
        GameObject terrainRoot = GameObject.Find("_TerrainPrototype");
        if (terrainRoot == null)
        {
            Debug.LogError("Optimization aborted: terrain root not found.");
            return 0;
        }

        int lodGroups = 0;
        Transform nature = terrainRoot.transform.Find("_Nature");
        if (nature != null)
        {
            lodGroups = AddCullingLodGroups(nature);
        }

        Transform infrastructure = terrainRoot.transform.Find("_Infrastructure");
        if (infrastructure != null)
        {
            MarkStaticRecursive(infrastructure);
        }

        EditorSceneManager.SaveScene(scene, TerrainScenePath);
        return lodGroups;
    }

    /// <summary>
    /// Adds a single-level LODGroup (render, then cull at a small screen height) to every
    /// individual object under a group. Container nodes (names starting with '_' or a
    /// per-cell "Buildings_" prefix) are descended into, never given groups themselves; any
    /// LODGroup mistakenly sitting on a container is removed first.
    /// </summary>
    private static int AddCullingLodGroups(Transform group)
    {
        int added = 0;
        foreach (Transform child in GetAllDescendants(group))
        {
            bool isContainer = child.name.StartsWith("_") || child.name.StartsWith("Buildings_");
            if (isContainer)
            {
                LODGroup misplaced = child.GetComponent<LODGroup>();
                if (misplaced != null)
                {
                    Object.DestroyImmediate(misplaced);
                }

                continue;
            }

            Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0 || child.GetComponent<LODGroup>() != null ||
                child.GetComponentInParent<LODGroup>() != null)
            {
                continue;
            }

            LODGroup lodGroup = child.gameObject.AddComponent<LODGroup>();
            lodGroup.SetLODs(new[] { new LOD(LodCullScreenHeight, renderers) });
            lodGroup.RecalculateBounds();
            added++;
        }

        return added;
    }

    private static int MarkStaticRecursive(Transform root)
    {
        const StaticEditorFlags flags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic;

        int count = 0;
        foreach (Transform child in GetAllDescendants(root))
        {
            GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
            count++;
        }

        GameObjectUtility.SetStaticEditorFlags(root.gameObject, flags);
        return count + 1;
    }

    private static IEnumerable<Transform> GetAllDescendants(Transform root)
    {
        foreach (Transform child in root)
        {
            yield return child;
            foreach (Transform grandchild in GetAllDescendants(child))
            {
                yield return grandchild;
            }
        }
    }

    [MenuItem("Adonis Life/World/Report Scene Statistics")]
    public static void ReportSceneStatistics()
    {
        Scene scene = EditorSceneManager.OpenScene(CityScenePath, OpenSceneMode.Single);

        int objectCount = 0;
        int rendererCount = 0;
        int lodGroupCount = 0;
        long vertexCount = 0;
        var uniqueMaterials = new HashSet<Material>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                objectCount++;
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                rendererCount++;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        uniqueMaterials.Add(material);
                    }
                }

                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    vertexCount += meshFilter.sharedMesh.vertexCount;
                }
            }

            lodGroupCount += root.GetComponentsInChildren<LODGroup>(true).Length;
        }

        long totalAllocated = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        long totalReserved = Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);

        Debug.Log(
            "Scene statistics for ProceduralCity_Prototype: " +
            $"objects={objectCount}, renderers={rendererCount}, lodGroups={lodGroupCount}, " +
            $"vertices={vertexCount}, uniqueMaterials={uniqueMaterials.Count}, " +
            $"allocatedMemoryMB={totalAllocated}, reservedMemoryMB={totalReserved}.");
    }
}
