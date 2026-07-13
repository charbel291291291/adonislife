using System.Collections.Generic;
using System.IO;
using AdonisLife.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace AdonisLife.World.Tests.Editor
{
    public class GameplayTests
    {
        [Test]
        public void Movement_NormalizesDiagonals_AndAppliesSprint()
        {
            Vector3 straight = PlayerMovementModel.ComputeMove(
                new Vector2(0f, 1f), 0f, 4f, 2f, sprinting: false, deltaTime: 1f);
            Assert.AreEqual(4f, straight.magnitude, 0.001f);
            Assert.AreEqual(4f, straight.z, 0.001f);

            Vector3 diagonal = PlayerMovementModel.ComputeMove(
                new Vector2(1f, 1f), 0f, 4f, 2f, sprinting: false, deltaTime: 1f);
            Assert.AreEqual(4f, diagonal.magnitude, 0.001f, "Diagonal movement must not be faster.");

            Vector3 sprint = PlayerMovementModel.ComputeMove(
                new Vector2(0f, 1f), 0f, 4f, 2f, sprinting: true, deltaTime: 1f);
            Assert.AreEqual(8f, sprint.magnitude, 0.001f);
        }

        [Test]
        public void Movement_RespectsYaw_AndGravityAccumulates()
        {
            Vector3 east = PlayerMovementModel.ComputeMove(
                new Vector2(0f, 1f), 90f, 4f, 2f, sprinting: false, deltaTime: 1f);
            Assert.AreEqual(4f, east.x, 0.001f);
            Assert.AreEqual(0f, east.z, 0.001f);

            float falling = PlayerMovementModel.ApplyGravity(0f, grounded: false, deltaTime: 0.5f);
            Assert.AreEqual(PlayerMovementModel.Gravity * 0.5f, falling, 0.001f);
            Assert.AreEqual(-1f, PlayerMovementModel.ApplyGravity(-30f, grounded: true, deltaTime: 0.5f));
        }

        [Test]
        public void Camera_OrbitsBehindTheTarget()
        {
            Vector3 position = CameraFollowModel.ComputePosition(Vector3.zero, 0f, 0f, 8f);
            Assert.AreEqual(new Vector3(0f, 0f, -8f), position);

            Vector3 pitched = CameraFollowModel.ComputePosition(Vector3.zero, 0f, 30f, 8f);
            Assert.Greater(pitched.y, 0f, "Pitched camera should rise above the target.");
            Assert.AreEqual(8f, pitched.magnitude, 0.001f);
        }

        [Test]
        public void Interaction_SelectsNearestWithinRange()
        {
            var candidates = new List<Vector3>
            {
                new Vector3(5f, 0f, 0f),
                new Vector3(2f, 0f, 0f),
                new Vector3(2f, 0f, 0f),
                new Vector3(10f, 0f, 0f)
            };

            Assert.AreEqual(1, InteractionModel.SelectNearest(Vector3.zero, candidates, 6f),
                "Nearest candidate (with lowest-index tiebreak) should be selected.");
            Assert.AreEqual(-1, InteractionModel.SelectNearest(Vector3.zero, candidates, 1f),
                "Nothing in range should select -1.");
        }

        [Test]
        public void Inventory_AddsRemovesAndEnforcesCapacity()
        {
            var inventory = new Inventory(2);
            int eventCount = 0;
            inventory.OnChanged += (_, _) => eventCount++;

            Assert.IsTrue(inventory.Add("apple", 3));
            Assert.IsTrue(inventory.Add("key"));
            Assert.IsFalse(inventory.Add("coin"), "A third unique item must not fit in two slots.");
            Assert.AreEqual(3, inventory.GetCount("apple"));

            Assert.IsTrue(inventory.Remove("apple", 2));
            Assert.AreEqual(1, inventory.GetCount("apple"));
            Assert.IsFalse(inventory.Remove("apple", 5), "Removing more than held must fail.");

            Assert.IsTrue(inventory.Remove("key"));
            Assert.AreEqual(1, inventory.UsedSlots);
            Assert.IsTrue(inventory.Add("coin"), "A freed slot should accept a new item.");
            Assert.AreEqual(5, eventCount);
        }

        [Test]
        public void Quests_ProgressAndComplete_WithEvents()
        {
            var log = new QuestLog();
            Quest completed = null;
            log.OnQuestCompleted += quest => completed = quest;

            log.Register(new Quest
            {
                id = "q1",
                title = "Groceries",
                objectives =
                {
                    new QuestObjective { id = "buy", description = "Buy food", requiredCount = 2 },
                    new QuestObjective { id = "home", description = "Return home", requiredCount = 1 }
                }
            });

            Assert.IsFalse(log.AddProgress("q1", "buy"), "Progress before starting must fail.");
            Assert.IsTrue(log.Start("q1"));
            Assert.IsFalse(log.Start("q1"), "Starting twice must fail.");

            Assert.IsTrue(log.AddProgress("q1", "buy", 2));
            Assert.IsNull(completed);
            Assert.IsTrue(log.AddProgress("q1", "home"));
            Assert.IsNotNull(completed);
            Assert.AreEqual(QuestState.Completed, log.Quests["q1"].state);
        }

        [Test]
        public void Missions_EnforceObjectiveOrder()
        {
            var log = new QuestLog();
            log.Register(new Quest
            {
                id = "m1",
                title = "First Day",
                sequential = true,
                objectives =
                {
                    new QuestObjective { id = "step1", requiredCount = 1 },
                    new QuestObjective { id = "step2", requiredCount = 1 }
                }
            });
            log.Start("m1");

            Assert.IsFalse(log.AddProgress("m1", "step2"), "A mission must reject out-of-order progress.");
            Assert.IsTrue(log.AddProgress("m1", "step1"));
            Assert.IsTrue(log.AddProgress("m1", "step2"));
            Assert.AreEqual(QuestState.Completed, log.Quests["m1"].state);
        }

        [Test]
        public void SaveSystem_RoundTripsFullState()
        {
            string path = Path.Combine(Path.GetTempPath(), "adonis_test_save.json");

            try
            {
                var inventory = new Inventory(8);
                inventory.Add("apple", 3);
                inventory.Add("key");

                var log = new QuestLog();
                log.Register(new Quest
                {
                    id = "q1",
                    title = "Groceries",
                    objectives = { new QuestObjective { id = "buy", requiredCount = 2, progress = 1 } }
                });
                log.Start("q1");

                SaveData snapshot = SaveSystem.CreateSnapshot(
                    new Vector3(120f, 0.2f, 340f), 90f, 14.5f, 3, inventory, log);
                SaveSystem.Save(snapshot, path);

                SaveData loaded = SaveSystem.Load(path);
                Assert.IsNotNull(loaded);
                Assert.AreEqual(snapshot.playerPosition, loaded.playerPosition);
                Assert.AreEqual(90f, loaded.playerYawDegrees, 0.001f);
                Assert.AreEqual(14.5f, loaded.timeOfDay, 0.001f);
                Assert.AreEqual(3, loaded.day);

                Assert.AreEqual(2, loaded.items.Count);
                Assert.AreEqual("apple", loaded.items[0].itemId);
                Assert.AreEqual(3, loaded.items[0].count);

                Assert.AreEqual(1, loaded.quests.Count);
                Assert.AreEqual(QuestState.Active, loaded.quests[0].state);
                Assert.AreEqual(1, loaded.quests[0].objectives[0].progress);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void SaveSystem_LoadReturnsNullForMissingFile()
        {
            Assert.IsNull(SaveSystem.Load(Path.Combine(Path.GetTempPath(), "adonis_missing_save.json")));
        }
    }
}
