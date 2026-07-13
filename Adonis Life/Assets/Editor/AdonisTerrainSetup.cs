using System.IO;
using AdonisLife.World.Authored;
using AdonisLife.World.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdonisTerrainSetup
{
    private const string SceneFolder = "Assets/Project/World/Scenes";
    private const string ScenePath = SceneFolder + "/Terrain_Prototype.unity";
    private const string GeneratedFolder = "Assets/Project/World/Terrain/Generated";
    private const string MaterialFolder = "Assets/Project/World/Materials/Terrain";
    private const string SettingsPath = "Assets/Project/World/Configs/TerrainSettings.asset";

    private const float WaterThickness = 0.4f;
    private const int LayerTextureSize = 4;
    private const float LayerTileSize = 12f;

    [MenuItem("Adonis Life/World/Create Terrain Prototype")]
    public static void CreateTerrainPrototype()
    {
        if (File.Exists(ScenePath))
        {
            Debug.LogWarning($"Terrain prototype scene already exists at '{ScenePath}'. Skipping creation.");
            return;
        }

        TerrainSettingsSO settingsAsset = GetOrCreateSettings();
        if (!settingsAsset.IsValid(out string settingsError))
        {
            Debug.LogError($"Terrain prototype creation aborted: {settingsError}");
            return;
        }

        TerrainGenerationSettings settings = settingsAsset.ToGenerationSettings();

        Directory.CreateDirectory(GeneratedFolder);
        Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();

        TerrainLayer sandLayer = GetOrCreateTerrainLayer("Sand", new Color(0.87f, 0.78f, 0.55f));
        TerrainLayer grassLayer = GetOrCreateTerrainLayer("Grass", new Color(0.35f, 0.55f, 0.30f));
        TerrainLayer rockLayer = GetOrCreateTerrainLayer("Rock", new Color(0.45f, 0.44f, 0.42f));
        TerrainLayer[] layers = { sandLayer, grassLayer, rockLayer };

        Material waterMaterial = GetOrCreateMaterial("Water_Material", new Color(0.15f, 0.35f, 0.55f));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("_TerrainPrototype");
        GameObject chunksRoot = new GameObject("_TerrainChunks");
        GameObject waterRoot = new GameObject("_Water");
        GameObject environmentRoot = new GameObject("_Environment");
        chunksRoot.transform.SetParent(root.transform);
        waterRoot.transform.SetParent(root.transform);
        environmentRoot.transform.SetParent(root.transform);

        UnityEngine.Terrain[,] terrains = new UnityEngine.Terrain[settings.ChunksX, settings.ChunksZ];
        for (int cz = 0; cz < settings.ChunksZ; cz++)
        {
            for (int cx = 0; cx < settings.ChunksX; cx++)
            {
                terrains[cx, cz] = CreateTerrainChunk(cx, cz, settings, layers, chunksRoot.transform);
            }
        }

        ConnectNeighbors(terrains, settings);
        CreateWater(waterRoot.transform, waterMaterial, settings);
        CreateCameraAndLight(environmentRoot.transform, settings);

        if (!ValidateScene(root.transform, settings, out string sceneError))
        {
            Debug.LogError($"Terrain prototype validation failed: {sceneError}");
            return;
        }

        Directory.CreateDirectory(SceneFolder);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Terrain prototype created at '{ScenePath}': {settings.ChunksX}x{settings.ChunksZ} chunks, " +
                  $"{settings.TotalWidth}x{settings.TotalDepth}m starting at ({settings.OriginX}, {settings.OriginZ}).");
    }

    private static TerrainSettingsSO GetOrCreateSettings()
    {
        TerrainSettingsSO existing = AssetDatabase.LoadAssetAtPath<TerrainSettingsSO>(SettingsPath);
        if (existing != null)
        {
            return existing;
        }

        TerrainSettingsSO settings = ScriptableObject.CreateInstance<TerrainSettingsSO>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        return settings;
    }

    private static UnityEngine.Terrain CreateTerrainChunk(
        int chunkX, int chunkZ, TerrainGenerationSettings settings, TerrainLayer[] layers, Transform parent)
    {
        string dataPath = $"{GeneratedFolder}/TerrainData_C{chunkX}_{chunkZ}.asset";
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
        if (data == null)
        {
            data = new TerrainData();
            AssetDatabase.CreateAsset(data, dataPath);
        }

        data.heightmapResolution = settings.HeightmapResolution;
        data.size = new Vector3(settings.ChunkSize, settings.MaxHeight, settings.ChunkSize);
        data.SetHeights(0, 0, TerrainHeightField.GenerateChunkHeights01(chunkX, chunkZ, settings));

        data.terrainLayers = layers;
        int alphaRes = settings.HeightmapResolution - 1;
        data.alphamapResolution = alphaRes;
        float[,,] alphas = new float[alphaRes, alphaRes, 3];
        float step = settings.ChunkSize / alphaRes;
        float originX = settings.OriginX + chunkX * settings.ChunkSize;
        float originZ = settings.OriginZ + chunkZ * settings.ChunkSize;
        for (int iz = 0; iz < alphaRes; iz++)
        {
            for (int ix = 0; ix < alphaRes; ix++)
            {
                Vector3 weights = TerrainHeightField.GetSplatWeights(
                    originX + (ix + 0.5f) * step, originZ + (iz + 0.5f) * step, settings);
                alphas[iz, ix, 0] = weights.x;
                alphas[iz, ix, 1] = weights.y;
                alphas[iz, ix, 2] = weights.z;
            }
        }

        data.SetAlphamaps(0, 0, alphas);
        EditorUtility.SetDirty(data);

        GameObject terrainObject = UnityEngine.Terrain.CreateTerrainGameObject(data);
        terrainObject.name = $"TerrainChunk_C{chunkX}_{chunkZ}";
        terrainObject.transform.SetParent(parent);
        terrainObject.transform.position = new Vector3(originX, 0f, originZ);
        return terrainObject.GetComponent<UnityEngine.Terrain>();
    }

    private static void ConnectNeighbors(UnityEngine.Terrain[,] terrains, TerrainGenerationSettings settings)
    {
        for (int cz = 0; cz < settings.ChunksZ; cz++)
        {
            for (int cx = 0; cx < settings.ChunksX; cx++)
            {
                UnityEngine.Terrain left = cx > 0 ? terrains[cx - 1, cz] : null;
                UnityEngine.Terrain right = cx < settings.ChunksX - 1 ? terrains[cx + 1, cz] : null;
                UnityEngine.Terrain bottom = cz > 0 ? terrains[cx, cz - 1] : null;
                UnityEngine.Terrain top = cz < settings.ChunksZ - 1 ? terrains[cx, cz + 1] : null;
                terrains[cx, cz].SetNeighbors(left, top, right, bottom);
            }
        }
    }

    private static void CreateWater(Transform parent, Material material, TerrainGenerationSettings settings)
    {
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
        water.name = "Sea_Surface";
        water.transform.SetParent(parent);
        water.transform.localScale = new Vector3(settings.TotalWidth, WaterThickness, settings.TotalDepth);
        water.transform.position = new Vector3(
            settings.OriginX + settings.TotalWidth / 2f,
            settings.SeaLevel - WaterThickness / 2f,
            settings.OriginZ + settings.TotalDepth / 2f);
        water.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void CreateCameraAndLight(Transform parent, TerrainGenerationSettings settings)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent);
        cameraObject.transform.position = new Vector3(
            settings.OriginX + settings.TotalWidth / 2f,
            settings.MaxHeight * 12f,
            settings.OriginZ - settings.TotalDepth * 0.3f);
        cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.farClipPlane = 3000f;

        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(parent);
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light directionalLight = lightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 1.2f;
    }

    private static bool ValidateScene(Transform root, TerrainGenerationSettings settings, out string error)
    {
        Transform chunks = root.Find("_TerrainChunks");
        if (chunks == null || root.Find("_Water") == null || root.Find("_Environment") == null)
        {
            error = "Required hierarchy objects are missing.";
            return false;
        }

        int expected = settings.ChunksX * settings.ChunksZ;
        if (chunks.childCount != expected)
        {
            error = $"Expected {expected} terrain chunks, found {chunks.childCount}.";
            return false;
        }

        foreach (Transform chunk in chunks)
        {
            UnityEngine.Terrain terrain = chunk.GetComponent<UnityEngine.Terrain>();
            if (terrain == null || terrain.terrainData == null)
            {
                error = $"Chunk '{chunk.name}' has no terrain data.";
                return false;
            }

            if (terrain.terrainData.heightmapResolution != settings.HeightmapResolution)
            {
                error = $"Chunk '{chunk.name}' has wrong heightmap resolution.";
                return false;
            }

            if (terrain.terrainData.terrainLayers.Length != 3)
            {
                error = $"Chunk '{chunk.name}' does not have the three splat layers.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static TerrainLayer GetOrCreateTerrainLayer(string name, Color color)
    {
        string layerPath = $"{GeneratedFolder}/{name}_Layer.terrainlayer";
        TerrainLayer existing = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        if (existing != null)
        {
            return existing;
        }

        string texturePath = $"{GeneratedFolder}/{name}_Texture.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            texture = new Texture2D(LayerTextureSize, LayerTextureSize, TextureFormat.RGBA32, false) { name = $"{name}_Texture" };
            Color[] pixels = new Color[LayerTextureSize * LayerTextureSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, texturePath);
        }

        TerrainLayer layer = new TerrainLayer
        {
            name = $"{name}_Layer",
            diffuseTexture = texture,
            tileSize = new Vector2(LayerTileSize, LayerTileSize)
        };
        AssetDatabase.CreateAsset(layer, layerPath);
        return layer;
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
