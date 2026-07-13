using System.Collections.Generic;
using AdonisLife.World.Buildings;
using AdonisLife.World.ProceduralCity;
using UnityEngine;

namespace AdonisLife.World.Traffic
{
    /// <summary>A parking lot connected to the road network at its cell's intersection node.</summary>
    public readonly struct ParkingNode
    {
        public readonly Vector2 Position;
        public readonly CellCoordinate2D Cell;
        public readonly int RoadNodeId;

        public ParkingNode(Vector2 position, CellCoordinate2D cell, int roadNodeId)
        {
            Position = position;
            Cell = cell;
            RoadNodeId = roadNodeId;
        }
    }

    /// <summary>
    /// Builds the parking graph from the building plan: every planned parking lot becomes a
    /// parking node linked to the intersection of the cell it sits in.
    /// </summary>
    public static class ParkingGraphBuilder
    {
        public static List<ParkingNode> Build(CityGenerationSettings settings)
        {
            var nodes = new List<ParkingNode>();

            foreach (CellCoordinate2D cell in ProceduralCityLayout.GetAllCells(settings))
            {
                int roadNodeId = RoadGraphBuilder.GetNodeId(cell, settings);
                foreach (DevelopmentBlockQuadrant quadrant in
                    (DevelopmentBlockQuadrant[])System.Enum.GetValues(typeof(DevelopmentBlockQuadrant)))
                {
                    foreach (BuildingSpec spec in BuildingBlockPlanner.PlanBlock(cell, quadrant, settings))
                    {
                        if (spec.Type != BuildingType.Parking)
                        {
                            continue;
                        }

                        nodes.Add(new ParkingNode(
                            new Vector2(spec.Footprint.CenterX, spec.Footprint.CenterZ),
                            cell,
                            roadNodeId));
                    }
                }
            }

            return nodes;
        }
    }
}
