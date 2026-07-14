using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click generation pipeline: runs every world-building step in dependency order. Each
/// step is idempotent (it skips itself when its output already exists), so the pipeline can be
/// re-run safely on a fresh clone or an existing project.
/// </summary>
public static class AdonisPipeline
{
    [MenuItem("Adonis Life/World/Generate Full World Pipeline")]
    public static void GenerateFullWorld()
    {
        AdonisProceduralCitySetup.CreateProceduralCityPrototype();
        AdonisTerrainSetup.CreateTerrainPrototype();
        AdonisRoadDetailSetup.AddRoadDetails();
        AdonisBuildingSetup.AddBuildings();
        AdonisInfrastructureSetup.AddInfrastructure();
        AdonisEnvironmentSetup.AddEnvironment();
        AdonisTrafficSetup.AddTrafficSystems();
        AdonisNpcSetup.AddNpcSystems();
        AdonisGameplaySetup.AddGameplaySystems();
        AdonisDevTools.AddPerformanceOverlay();
        AdonisOptimizationSetup.RunOptimizationPass();

        Debug.Log("Full world pipeline finished.");
    }
}
