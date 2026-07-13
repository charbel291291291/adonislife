using System.Collections.Generic;
using AdonisLife.World.Authored;
using AdonisLife.World.Traffic;
using UnityEngine;

namespace AdonisLife.World.Traffic
{
    /// <summary>
    /// Runtime vehicle spawn system. On start it builds the lane graph from the city settings,
    /// asks <see cref="VehicleSpawnModel"/> for a deterministic spawn plan, and instantiates
    /// placeholder vehicles oriented along their lanes. Movement AI arrives with the NPC phase.
    /// </summary>
    public class VehicleSpawner : MonoBehaviour
    {
        [SerializeField] private ProceduralCitySettingsSO _citySettings;
        [SerializeField] private int _vehicleCount = 40;
        [SerializeField] private float _minSpacingMeters = 15f;
        [SerializeField] private int _seed = 1234;

        private static readonly Vector3 VehicleSize = new Vector3(1.8f, 1.5f, 4.5f);
        private const float RoadTopY = 0.05f;

        private readonly List<GameObject> _vehicles = new List<GameObject>();

        public int SpawnedVehicleCount => _vehicles.Count;

        private void Start()
        {
            if (_citySettings == null || !_citySettings.IsValid(out _))
            {
                Debug.LogError("VehicleSpawner: city settings missing or invalid.");
                return;
            }

            RoadGraph graph = RoadGraphBuilder.Build(_citySettings.ToGenerationSettings());
            List<Lane> lanes = LaneGraphBuilder.Build(graph, _citySettings.ToGenerationSettings());
            List<VehicleSpawn> plan = VehicleSpawnModel.PlanSpawns(lanes, _vehicleCount, _minSpacingMeters, _seed);

            foreach (VehicleSpawn spawn in plan)
            {
                Lane lane = lanes[spawn.LaneIndex];
                Vector2 position = lane.GetPoint(spawn.T);
                Vector2 direction = (lane.End - lane.Start).normalized;

                GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vehicle.name = $"Vehicle_{_vehicles.Count}";
                vehicle.transform.SetParent(transform);
                vehicle.transform.localScale = VehicleSize;
                vehicle.transform.position = new Vector3(position.x, RoadTopY + VehicleSize.y / 2f, position.y);
                vehicle.transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));
                _vehicles.Add(vehicle);
            }
        }
    }
}
