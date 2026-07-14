using System.Collections.Generic;
using AdonisLife.World.Authored;
using AdonisLife.World.Buildings;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.Streaming;
using AdonisLife.World.Terrain;
using AdonisLife.World.Tools;
using AdonisLife.World.Traffic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor developer tools: world debugger, streaming debugger, chunk viewer, and the
/// city-statistics / generation-profiler commands.
/// </summary>
public static class AdonisDevTools
{
    private const string CitySettingsPath = "Assets/Project/World/Configs/ProceduralCitySettings.asset";
    private const string TerrainSettingsPath = "Assets/Project/World/Configs/TerrainSettings.asset";
    private const string WorldConfigPath = "Assets/Project/World/Configs/WorldConfig.asset";

    [MenuItem("Adonis Life/Tools/World Debugger")]
    public static void OpenWorldDebugger()
    {
        EditorWindow.GetWindow<WorldDebuggerWindow>("World Debugger").Show();
    }

    [MenuItem("Adonis Life/Tools/Streaming Debugger")]
    public static void OpenStreamingDebugger()
    {
        EditorWindow.GetWindow<StreamingDebuggerWindow>("Streaming Debugger").Show();
    }

    [MenuItem("Adonis Life/Tools/Chunk Viewer")]
    public static void OpenChunkViewer()
    {
        EditorWindow.GetWindow<ChunkViewerWindow>("Chunk Viewer").Show();
    }

    [MenuItem("Adonis Life/Tools/Add Performance Overlay To City")]
    public static void AddPerformanceOverlay()
    {
        const string scenePath = "Assets/Project/World/Scenes/ProceduralCity_Prototype.unity";
        UnityEngine.SceneManagement.Scene scene =
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        GameObject cityRoot = GameObject.Find("_ProceduralCityPrototype");
        if (cityRoot == null)
        {
            Debug.LogError("Performance overlay aborted: city root not found.");
            return;
        }

        Transform existing = cityRoot.transform.Find("_DevTools");
        if (existing != null)
        {
            Debug.LogWarning("Performance overlay already exists. Skipping.");
            return;
        }

        GameObject devTools = new GameObject("_DevTools");
        devTools.transform.SetParent(cityRoot.transform);
        devTools.AddComponent<PerformanceOverlay>();

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("Performance overlay added to the city scene.");
    }

    [MenuItem("Adonis Life/Tools/Log City Statistics")]
    public static void LogCityStatistics()
    {
        ProceduralCitySettingsSO settings = AssetDatabase.LoadAssetAtPath<ProceduralCitySettingsSO>(CitySettingsPath);
        if (settings == null)
        {
            Debug.LogError("City statistics aborted: settings asset not found.");
            return;
        }

        if (!settings.IsValid(out string error))
        {
            Debug.LogError($"City statistics aborted: {error}");
            return;
        }

        Debug.Log(CityStatistics.Format(CityStatistics.Compute(settings.ToGenerationSettings())));
    }

    [MenuItem("Adonis Life/Tools/Profile Generation")]
    public static void ProfileGeneration()
    {
        ProceduralCitySettingsSO citySettings = AssetDatabase.LoadAssetAtPath<ProceduralCitySettingsSO>(CitySettingsPath);
        TerrainSettingsSO terrainSettings = AssetDatabase.LoadAssetAtPath<TerrainSettingsSO>(TerrainSettingsPath);
        if (citySettings == null || terrainSettings == null)
        {
            Debug.LogError("Generation profiling aborted: settings assets not found.");
            return;
        }

        CityGenerationSettings city = citySettings.ToGenerationSettings();
        TerrainGenerationSettings terrain = terrainSettings.ToGenerationSettings();
        var profiler = new GenerationProfiler();

        using (profiler.Section("Building plan"))
        {
            BuildingBlockPlanner.PlanCity(city);
        }

        using (profiler.Section("Road + lane graphs"))
        {
            LaneGraphBuilder.Build(RoadGraphBuilder.Build(city), city);
        }

        using (profiler.Section("Terrain chunk (sequential)"))
        {
            TerrainHeightField.GenerateChunkHeights01(0, 0, terrain);
        }

        using (profiler.Section("Terrain chunk (Burst parallel)"))
        {
            TerrainHeightFieldParallel.GenerateChunkHeights01(0, 0, terrain);
        }

        using (profiler.Section("Nature placement"))
        {
            EnvironmentLayout.GetNatureInstances(terrain);
        }

        Debug.Log(profiler.FormatReport());
    }

    /// <summary>Shows the authored configuration assets and validates them in one place.</summary>
    private class WorldDebuggerWindow : EditorWindow
    {
        private Vector2 _scroll;

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Authored Configuration", EditorStyles.boldLabel);

            DrawAsset<WorldConfigSO>("World Config", WorldConfigPath, config => config.IsValid(out string e) ? null : e);
            DrawAsset<ProceduralCitySettingsSO>("City Settings", CitySettingsPath, s => s.IsValid(out string e) ? null : e);
            DrawAsset<TerrainSettingsSO>("Terrain Settings", TerrainSettingsPath, s => s.IsValid(out string e) ? null : e);

            EditorGUILayout.Space();
            if (GUILayout.Button("Log City Statistics"))
            {
                LogCityStatistics();
            }

            if (GUILayout.Button("Profile Generation"))
            {
                ProfileGeneration();
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawAsset<T>(string label, string path, System.Func<T, string> validate) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(label, asset, typeof(T), false);
            if (asset == null)
            {
                EditorGUILayout.LabelField("MISSING", GUILayout.Width(80f));
            }
            else
            {
                string error = validate(asset);
                EditorGUILayout.LabelField(error == null ? "OK" : $"INVALID: {error}", GUILayout.Width(240f));
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>Drives a live WorldStreamer with a movable observer, entirely in edit mode.</summary>
    private class StreamingDebuggerWindow : EditorWindow
    {
        private WorldStreamer _streamer;
        private float _observerX = 125f;
        private float _observerZ = 125f;
        private float _loadRadius = 300f;
        private float _unloadRadius = 450f;
        private Vector2 _scroll;

        private void OnGUI()
        {
            _loadRadius = EditorGUILayout.Slider("Load Radius", _loadRadius, 100f, 1000f);
            _unloadRadius = EditorGUILayout.Slider("Unload Radius", _unloadRadius, _loadRadius + 50f, 1500f);
            _observerX = EditorGUILayout.Slider("Observer X", _observerX, -1000f, 1750f);
            _observerZ = EditorGUILayout.Slider("Observer Z", _observerZ, -1000f, 1750f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Streamer"))
            {
                var grid = new WorldGrid(250f, 5,
                    new WorldBounds(new Vector3(-1000f, 0f, -1000f), new Vector3(1750f, 100f, 1750f)));
                _streamer = new WorldStreamer(
                    grid, new PlaceholderChunkLoader(), new PlaceholderChunkUnloader(),
                    _loadRadius, _unloadRadius, 64, 128);
                _streamer.RegisterObserver("editor", new WorldCoordinate(_observerX, 0f, _observerZ));
            }

            using (new EditorGUI.DisabledScope(_streamer == null))
            {
                if (GUILayout.Button("Tick"))
                {
                    _streamer.UpdateObserver("editor", new WorldCoordinate(_observerX, 0f, _observerZ));
                    _streamer.Tick(0.016f);
                }
            }

            EditorGUILayout.EndHorizontal();

            if (_streamer == null)
            {
                EditorGUILayout.HelpBox("Reset the streamer to begin.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Loaded chunks: {_streamer.LoadedChunkCount}", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach ((ChunkCoordinate coordinate, ChunkLifecycleState state, float distance)
                in _streamer.GetChunkSnapshot())
            {
                EditorGUILayout.LabelField($"({coordinate.X}, {coordinate.Y})  {state}  {distance:F0} m");
            }

            EditorGUILayout.EndScrollView();
        }
    }

    /// <summary>Grid view of the city's cells colored by zoning, with per-cell details on click.</summary>
    private class ChunkViewerWindow : EditorWindow
    {
        private void OnGUI()
        {
            ProceduralCitySettingsSO settingsAsset =
                AssetDatabase.LoadAssetAtPath<ProceduralCitySettingsSO>(CitySettingsPath);
            if (settingsAsset == null || !settingsAsset.IsValid(out _))
            {
                EditorGUILayout.HelpBox("City settings asset missing or invalid.", MessageType.Error);
                return;
            }

            CityGenerationSettings settings = settingsAsset.ToGenerationSettings();
            EditorGUILayout.LabelField("City cells (click for details)", EditorStyles.boldLabel);

            // Draw north row first so the layout matches world orientation.
            for (int z = settings.CellsZ - 1; z >= 0; z--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < settings.CellsX; x++)
                {
                    var cell = new CellCoordinate2D(x, z);
                    BlockZone zone = BuildingBlockPlanner.GetZone(cell, settings);

                    Color previous = GUI.backgroundColor;
                    GUI.backgroundColor = GetZoneColor(zone);
                    if (GUILayout.Button($"C{x}_{z}\n{zone}", GUILayout.Height(48f)))
                    {
                        LogCellDetails(cell, settings);
                    }

                    GUI.backgroundColor = previous;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private static void LogCellDetails(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            var counts = new Dictionary<BuildingType, int>();
            foreach (DevelopmentBlockQuadrant quadrant in
                (DevelopmentBlockQuadrant[])System.Enum.GetValues(typeof(DevelopmentBlockQuadrant)))
            {
                foreach (BuildingSpec spec in BuildingBlockPlanner.PlanBlock(cell, quadrant, settings))
                {
                    counts[spec.Type] = counts.TryGetValue(spec.Type, out int count) ? count + 1 : 1;
                }
            }

            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"Cell {cell}: zone {BuildingBlockPlanner.GetZone(cell, settings)}");
            foreach (KeyValuePair<BuildingType, int> entry in counts)
            {
                builder.AppendLine($"  {entry.Key}: {entry.Value}");
            }

            Debug.Log(builder.ToString());
        }

        private static Color GetZoneColor(BlockZone zone)
        {
            switch (zone)
            {
                case BlockZone.Commercial: return new Color(0.5f, 0.7f, 1f);
                case BlockZone.Industrial: return new Color(0.8f, 0.7f, 0.4f);
                case BlockZone.Civic: return new Color(0.9f, 0.6f, 0.6f);
                default: return new Color(0.6f, 0.9f, 0.6f);
            }
        }
    }
}
