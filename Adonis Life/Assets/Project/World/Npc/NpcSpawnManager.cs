using System.Collections.Generic;
using AdonisLife.World.Authored;
using AdonisLife.World.ProceduralCity;
using AdonisLife.World.Traffic;
using AdonisLife.World.UrbanCell;
using UnityEngine;

namespace AdonisLife.World.Npc
{
    /// <summary>
    /// Runtime NPC spawn manager: places pedestrians on sidewalks (weighted by crowd zones)
    /// walking loops around their cell's intersection corners, and vehicles on traffic lanes.
    /// All placement comes from the deterministic planning models.
    /// </summary>
    public class NpcSpawnManager : MonoBehaviour
    {
        [SerializeField] private ProceduralCitySettingsSO _citySettings;
        [SerializeField] private int _pedestrianCount = 60;
        [SerializeField] private int _vehicleCount = 30;
        [SerializeField] private float _vehicleMinSpacing = 15f;
        [SerializeField] private int _seed = 1234;

        private const float SidewalkTopY = 0.20f;
        private const float PedestrianHeight = 0.9f;
        private const float RoadTopY = 0.05f;
        private static readonly Vector3 VehicleSize = new Vector3(1.8f, 1.5f, 4.5f);

        public int SpawnedPedestrians { get; private set; }
        public int SpawnedVehicles { get; private set; }

        private void Start()
        {
            if (_citySettings == null || !_citySettings.IsValid(out _))
            {
                Debug.LogError("NpcSpawnManager: city settings missing or invalid.");
                return;
            }

            CityGenerationSettings settings = _citySettings.ToGenerationSettings();
            SpawnPedestrians(settings);
            SpawnVehicles(settings);
        }

        private void SpawnPedestrians(CityGenerationSettings settings)
        {
            var pedestriansRoot = new GameObject("_Pedestrians").transform;
            pedestriansRoot.SetParent(transform);

            foreach (PedestrianSpawn spawn in PedestrianSpawnModel.PlanSpawns(settings, _pedestrianCount, _seed))
            {
                GameObject pedestrian = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                pedestrian.name = $"Pedestrian_{SpawnedPedestrians}";
                pedestrian.transform.SetParent(pedestriansRoot);
                pedestrian.transform.localScale = new Vector3(0.5f, PedestrianHeight, 0.5f);
                pedestrian.transform.position = new Vector3(
                    spawn.Position.x, SidewalkTopY + PedestrianHeight, spawn.Position.y);

                PedestrianAgent agent = pedestrian.AddComponent<PedestrianAgent>();
                agent.Initialize(GetCornerLoop(spawn.Cell, settings), SidewalkTopY + PedestrianHeight);
                SpawnedPedestrians++;
            }
        }

        private void SpawnVehicles(CityGenerationSettings settings)
        {
            var vehiclesRoot = new GameObject("_Vehicles").transform;
            vehiclesRoot.SetParent(transform);

            RoadGraph graph = RoadGraphBuilder.Build(settings);
            List<Lane> lanes = LaneGraphBuilder.Build(graph, settings);

            foreach (VehicleSpawn spawn in VehicleSpawnModel.PlanSpawns(lanes, _vehicleCount, _vehicleMinSpacing, _seed))
            {
                Lane lane = lanes[spawn.LaneIndex];
                Vector2 position = lane.GetPoint(spawn.T);

                GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vehicle.name = $"Vehicle_{SpawnedVehicles}";
                vehicle.transform.SetParent(vehiclesRoot);
                vehicle.transform.localScale = VehicleSize;
                vehicle.transform.position = new Vector3(
                    position.x, RoadTopY + VehicleSize.y / 2f, position.y);

                VehicleAgent agent = vehicle.AddComponent<VehicleAgent>();
                agent.Initialize(lane.Start, lane.End, RoadTopY + VehicleSize.y / 2f);
                SpawnedVehicles++;
            }
        }

        private static List<Vector2> GetCornerLoop(CellCoordinate2D cell, CityGenerationSettings settings)
        {
            List<CellRect> pads = RoadDetailLayout.GetCornerCurbRects(cell, settings);
            // Walk order SW -> SE -> NE -> NW forms a loop around the intersection.
            return new List<Vector2>
            {
                new Vector2(pads[0].CenterX, pads[0].CenterZ),
                new Vector2(pads[1].CenterX, pads[1].CenterZ),
                new Vector2(pads[3].CenterX, pads[3].CenterZ),
                new Vector2(pads[2].CenterX, pads[2].CenterZ)
            };
        }
    }
}
