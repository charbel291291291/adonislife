using NUnit.Framework;
using UnityEngine;
using AdonisLife.World.Runtime;
using System.Collections.Generic;

namespace AdonisLife.World.Tests.Editor
{
    public class WorldStateSerializationTests
    {
        [Test]
        public void WorldState_Serialization_RoundTrip_Succeeds()
        {
            // 1. Arrange: Create a fully nested WorldState with sample data
            WorldState originalState = new WorldState
            {
                worldConfigId = "city_of_adonis_01",
                timeOfDay = 14.5,
                globalEconomyIndex = 1.05f
            };

            DistrictState district = new DistrictState
            {
                districtId = "d_downtown",
                currentTaxRate = 0.08f,
                dynamicPopularityIndex = 1.2f,
                currentSecurityLevel = 0.95f
            };
            originalState.districtStates.Add(district);

            ChunkState chunk = new ChunkState
            {
                chunkId = "c_0_0",
                coordinateX = 0,
                coordinateY = 0,
                isLoaded = true,
                trackedEntityIds = new List<string> { "entity_car_1", "entity_ped_2" }
            };

            ParcelState parcel = new ParcelState
            {
                parcelId = "p_101",
                ownerPlayerId = "player_adonis_007",
                marketValue = 1250000.50,
                isPowerGridConnected = true
            };

            BuildingLotState buildingLot = new BuildingLotState
            {
                buildingLotId = "lot_bld_42",
                structuralHealth = 100.0f,
                hasFireDamage = false,
                currentOccupantIds = new List<string> { "char_bob", "char_alice" }
            };

            InteriorLotState interior = new InteriorLotState
            {
                interiorId = "apt_3b",
                lesseePlayerId = "player_adonis_007",
                tenantProfileId = "tenant_id_99",
                isDoorLocked = true
            };
            buildingLot.interiorStates.Add(interior);
            parcel.buildingLotState = buildingLot;
            chunk.parcelStates.Add(parcel);

            RegionState spawnRegion = new RegionState
            {
                regionId = "reg_spawn_dt",
                regionType = RegionType.Spawn,
                isStateActive = true,
                currentEntityCount = 12,
                lastSpawnTimestamp = 1623847200L
            };
            chunk.regionStates.Add(spawnRegion);

            originalState.chunkStates.Add(chunk);

            // 2. Act: Serialize to JSON using Unity's JsonUtility
            string json = JsonUtility.ToJson(originalState, true);
            Debug.Log("[Test] Serialized JSON:\n" + json);

            // 3. Deserialize back to WorldState
            WorldState deserializedState = JsonUtility.FromJson<WorldState>(json);

            // 4. Assert: Verify all properties match exactly
            Assert.IsNotNull(deserializedState);
            Assert.AreEqual(originalState.worldConfigId, deserializedState.worldConfigId);
            Assert.AreEqual(originalState.timeOfDay, deserializedState.timeOfDay);
            Assert.AreEqual(originalState.globalEconomyIndex, deserializedState.globalEconomyIndex);

            Assert.AreEqual(originalState.districtStates.Count, deserializedState.districtStates.Count);
            Assert.AreEqual(originalState.districtStates[0].districtId, deserializedState.districtStates[0].districtId);
            Assert.AreEqual(originalState.districtStates[0].currentTaxRate, deserializedState.districtStates[0].currentTaxRate);

            Assert.AreEqual(originalState.chunkStates.Count, deserializedState.chunkStates.Count);
            Assert.AreEqual(originalState.chunkStates[0].chunkId, deserializedState.chunkStates[0].chunkId);
            Assert.AreEqual(originalState.chunkStates[0].coordinateX, deserializedState.chunkStates[0].coordinateX);
            Assert.AreEqual(originalState.chunkStates[0].coordinateY, deserializedState.chunkStates[0].coordinateY);
            Assert.AreEqual(originalState.chunkStates[0].isLoaded, deserializedState.chunkStates[0].isLoaded);

            Assert.AreEqual(originalState.chunkStates[0].parcelStates.Count, deserializedState.chunkStates[0].parcelStates.Count);
            Assert.AreEqual(originalState.chunkStates[0].parcelStates[0].parcelId, deserializedState.chunkStates[0].parcelStates[0].parcelId);
            Assert.AreEqual(originalState.chunkStates[0].parcelStates[0].marketValue, deserializedState.chunkStates[0].parcelStates[0].marketValue);

            Assert.AreEqual(originalState.chunkStates[0].parcelStates[0].buildingLotState.buildingLotId, deserializedState.chunkStates[0].parcelStates[0].buildingLotState.buildingLotId);
            Assert.AreEqual(originalState.chunkStates[0].parcelStates[0].buildingLotState.interiorStates.Count, deserializedState.chunkStates[0].parcelStates[0].buildingLotState.interiorStates.Count);
            Assert.AreEqual(originalState.chunkStates[0].parcelStates[0].buildingLotState.interiorStates[0].interiorId, deserializedState.chunkStates[0].parcelStates[0].buildingLotState.interiorStates[0].interiorId);

            Assert.AreEqual(originalState.chunkStates[0].regionStates.Count, deserializedState.chunkStates[0].regionStates.Count);
            Assert.AreEqual(originalState.chunkStates[0].regionStates[0].regionId, deserializedState.chunkStates[0].regionStates[0].regionId);
            Assert.AreEqual(originalState.chunkStates[0].regionStates[0].regionType, deserializedState.chunkStates[0].regionStates[0].regionType);
            Assert.AreEqual(originalState.chunkStates[0].regionStates[0].currentEntityCount, deserializedState.chunkStates[0].regionStates[0].currentEntityCount);
        }
    }
}