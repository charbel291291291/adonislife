using System.Collections.Generic;
using AdonisLife.World.Authored;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.Terrain;
using AdonisLife.World.UrbanCell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdonisInfrastructureSetup
{
    private const string CityScenePath = "Assets/Project/World/Scenes/ProceduralCity_Prototype.unity";
    private const string TerrainScenePath = "Assets/Project/World/Scenes/Terrain_Prototype.unity";
    private const string MaterialFolder = "Assets/Project/World/Materials/Infrastructure";
    private const string CitySettingsPath = "Assets/Project/World/Configs/ProceduralCitySettings.asset";
    private const string TerrainSettingsPath = "Assets/Project/World/Configs/TerrainSettings.asset";

    private const string InfrastructureRootName = "_Infrastructure";

    private const float ConduitTopY = 0.045f;
    private const float ConduitThickness = 0.03f;
    private const float ManholeTopY = 0.07f;
    private const float ManholeThickness = 0.02f;
    private const float WireTopY = 7.9f;
    private const float WireThickness = 0.05f;
    private const float ServicePathTopY = 0.02f;
    private const float ServicePathThickness = 0.05f;
    private const float DeckThickness = 0.6f;
    private const float TransformerHeight = 1.2f;
    private const float ValveHeight = 0.6f;
    private const float CabinetHeight = 1.5f;
    private const float ManholeVentHeight = 0.4f;
    private const float EquipmentSize = 1f;

    [MenuItem("Adonis Life/World/Add Infrastructure")]
    public static void AddInfrastructure()
    {
        System.IO.Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();

        if (!AddCityInfrastructure())
        {
            return;
        }

        if (!AddTerrainInfrastructure())
        {
            return;
        }

        Debug.Log("Infrastructure added to city and terrain scenes.");
    }

    private static bool AddCityInfrastructure()
    {
        ProceduralCitySettingsSO settingsAsset = AssetDatabase.LoadAssetAtPath<ProceduralCitySettingsSO>(CitySettingsPath);
        if (settingsAsset == null || !settingsAsset.IsValid(out _))
        {
            Debug.LogError("City infrastructure aborted: settings asset missing or invalid.");
            return false;
        }

        Scene scene = EditorSceneManager.OpenScene(CityScenePath, OpenSceneMode.Single);
        GameObject cityRoot = GameObject.Find("_ProceduralCityPrototype");
        if (cityRoot == null)
        {
            Debug.LogError("City infrastructure aborted: city root not found.");
            return false;
        }

        if (cityRoot.transform.Find(InfrastructureRootName) != null)
        {
            Debug.LogWarning("City infrastructure already exists. Skipping.");
            return true;
        }

        CityGenerationSettings settings = settingsAsset.ToGenerationSettings();

        Material electricityMaterial = GetOrCreateMaterial("Conduit_Electricity_Material", new Color(0.85f, 0.75f, 0.15f));
        Material waterMaterial = GetOrCreateMaterial("Conduit_Water_Material", new Color(0.20f, 0.45f, 0.80f));
        Material sewageMaterial = GetOrCreateMaterial("Conduit_Sewage_Material", new Color(0.45f, 0.32f, 0.20f));
        Material internetMaterial = GetOrCreateMaterial("Conduit_Internet_Material", new Color(0.20f, 0.75f, 0.75f));
        Material manholeMaterial = GetOrCreateMaterial("Manhole_Material", new Color(0.15f, 0.15f, 0.15f));
        Material equipmentMaterial = GetOrCreateMaterial("UtilityEquipment_Material", new Color(0.50f, 0.55f, 0.50f));
        Material wireMaterial = GetOrCreateMaterial("LightingWire_Material", new Color(0.08f, 0.08f, 0.08f));
        Material servicePathMaterial = GetOrCreateMaterial("ServicePath_Material", new Color(0.55f, 0.50f, 0.42f));

        var conduitMaterials = new Dictionary<UtilityNetwork, Material>
        {
            { UtilityNetwork.Electricity, electricityMaterial },
            { UtilityNetwork.Water, waterMaterial },
            { UtilityNetwork.Sewage, sewageMaterial },
            { UtilityNetwork.Internet, internetMaterial }
        };

        GameObject root = new GameObject(InfrastructureRootName);
        root.transform.SetParent(cityRoot.transform);
        Transform conduits = CreateGroup("_UtilityConduits", root.transform);
        Transform manholes = CreateGroup("_Manholes", root.transform);
        Transform equipment = CreateGroup("_UtilityEquipment", root.transform);
        Transform lighting = CreateGroup("_LightingCircuit", root.transform);
        Transform servicePaths = CreateGroup("_ServicePaths", root.transform);

        foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
        {
            string suffix = $"C{cell.X}_{cell.Z}";

            foreach (UtilityNetwork network in (UtilityNetwork[])System.Enum.GetValues(typeof(UtilityNetwork)))
            {
                List<CellRect> rects = InfrastructureLayout.GetConduitRects(cell, settings, network);
                for (int i = 0; i < rects.Count; i++)
                {
                    CreateBox($"Conduit_{network}_{suffix}_{i}", conduits, conduitMaterials[network],
                        rects[i], ConduitTopY, ConduitThickness);
                }

                Vector2 position = InfrastructureLayout.GetCornerEquipmentPosition(cell, settings, network);
                float height = GetEquipmentHeight(network);
                CreateBoxAt($"Equipment_{network}_{suffix}", equipment, equipmentMaterial,
                    position, EquipmentSize, height);
            }

            List<Vector2> manholePositions = InfrastructureLayout.GetManholePositions(cell, settings);
            for (int i = 0; i < manholePositions.Count; i++)
            {
                var rect = new CellRect(
                    manholePositions[i].x - InfrastructureLayout.ManholeSize / 2f,
                    manholePositions[i].x + InfrastructureLayout.ManholeSize / 2f,
                    manholePositions[i].y - InfrastructureLayout.ManholeSize / 2f,
                    manholePositions[i].y + InfrastructureLayout.ManholeSize / 2f);
                CreateBox($"Manhole_{suffix}_{i}", manholes, manholeMaterial, rect, ManholeTopY, ManholeThickness);
            }

            List<CellRect> wires = InfrastructureLayout.GetLightingCircuitRects(cell, settings);
            for (int i = 0; i < wires.Count; i++)
            {
                CreateBox($"LightingWire_{suffix}_{i}", lighting, wireMaterial, wires[i], WireTopY, WireThickness);
            }

            foreach (DevelopmentBlockQuadrant quadrant in
                (DevelopmentBlockQuadrant[])System.Enum.GetValues(typeof(DevelopmentBlockQuadrant)))
            {
                List<CellRect> paths = InfrastructureLayout.GetServicePathRects(cell, settings, quadrant);
                for (int i = 0; i < paths.Count; i++)
                {
                    CreateBox($"ServicePath_{quadrant}_{suffix}_{i}", servicePaths, servicePathMaterial,
                        paths[i], ServicePathTopY, ServicePathThickness);
                }
            }
        }

        EditorSceneManager.SaveScene(scene, CityScenePath);
        return true;
    }

    private static bool AddTerrainInfrastructure()
    {
        TerrainSettingsSO settingsAsset = AssetDatabase.LoadAssetAtPath<TerrainSettingsSO>(TerrainSettingsPath);
        if (settingsAsset == null || !settingsAsset.IsValid(out _))
        {
            Debug.LogError("Terrain infrastructure aborted: settings asset missing or invalid.");
            return false;
        }

        Scene scene = EditorSceneManager.OpenScene(TerrainScenePath, OpenSceneMode.Single);
        GameObject terrainRoot = GameObject.Find("_TerrainPrototype");
        if (terrainRoot == null)
        {
            Debug.LogError("Terrain infrastructure aborted: terrain root not found.");
            return false;
        }

        if (terrainRoot.transform.Find(InfrastructureRootName) != null)
        {
            Debug.LogWarning("Terrain infrastructure already exists. Skipping.");
            return true;
        }

        TerrainGenerationSettings terrain = settingsAsset.ToGenerationSettings();
        Material deckMaterial = GetOrCreateMaterial("BridgeDeck_Material", new Color(0.62f, 0.62f, 0.60f));
        Material tunnelMaterial = GetOrCreateMaterial("Tunnel_Material", new Color(0.20f, 0.18f, 0.16f));

        GameObject root = new GameObject(InfrastructureRootName);
        root.transform.SetParent(terrainRoot.transform);

        (CellRect deckFootprint, float deckTopY) = InfrastructureLayout.GetBridgeDeck(terrain);
        CreateBox("Bridge_RiverCrossing", root.transform, deckMaterial, deckFootprint, deckTopY, DeckThickness);

        (CellRect tunnelFootprint, float floorY, float tunnelHeight) = InfrastructureLayout.GetTunnelSegment(terrain);
        CreateBox("Tunnel_CliffCrossing", root.transform, tunnelMaterial, tunnelFootprint, floorY + tunnelHeight, tunnelHeight);

        EditorSceneManager.SaveScene(scene, TerrainScenePath);
        return true;
    }

    private static float GetEquipmentHeight(UtilityNetwork network)
    {
        switch (network)
        {
            case UtilityNetwork.Electricity: return TransformerHeight;
            case UtilityNetwork.Water: return ValveHeight;
            case UtilityNetwork.Internet: return CabinetHeight;
            case UtilityNetwork.Sewage: return ManholeVentHeight;
            default:
                throw new System.ArgumentOutOfRangeException(nameof(network), network, null);
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

    private static void CreateBox(string name, Transform parent, Material material, CellRect rect, float topY, float thickness)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localScale = new Vector3(rect.Width, thickness, rect.Depth);
        go.transform.position = new Vector3(rect.CenterX, topY - thickness / 2f, rect.CenterZ);
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void CreateBoxAt(string name, Transform parent, Material material, Vector2 position, float size, float height)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localScale = new Vector3(size, height, size);
        go.transform.position = new Vector3(position.x, height / 2f, position.y);
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
}
