using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdonisLife.World.Runtime;
using AdonisLife.World.Streaming;
using NUnit.Framework;
using UnityEngine;

namespace AdonisLife.World.Tests.Editor
{
    public class WorldStreamerTests
    {
        private class RecordingLoader : IChunkLoader
        {
            public readonly List<ChunkCoordinate> Requested = new List<ChunkCoordinate>();

            public Task<ChunkState> LoadChunkAsync(ChunkCoordinate coordinate, CancellationToken cancellationToken)
            {
                Requested.Add(coordinate);
                return new PlaceholderChunkLoader().LoadChunkAsync(coordinate, cancellationToken);
            }
        }

        private class RecordingUnloader : IChunkUnloader
        {
            public readonly List<ChunkCoordinate> Requested = new List<ChunkCoordinate>();

            public Task UnloadChunkAsync(ChunkCoordinate coordinate, ChunkState state, CancellationToken cancellationToken)
            {
                Requested.Add(coordinate);
                return Task.CompletedTask;
            }
        }

        private static WorldGrid DefaultGrid()
        {
            return new WorldGrid(
                chunkSize: 250f,
                parcelsPerChunk: 5,
                bounds: new WorldBounds(new Vector3(-5000f, 0f, -5000f), new Vector3(5000f, 100f, 5000f)));
        }

        private static WorldStreamer CreateStreamer(
            RecordingLoader loader,
            RecordingUnloader unloader,
            float loadRadius = 300f,
            float unloadRadius = 450f,
            int maxConcurrent = 64,
            int maxLoaded = 64)
        {
            return new WorldStreamer(DefaultGrid(), loader, unloader, loadRadius, unloadRadius, maxConcurrent, maxLoaded);
        }

        private static void TickUntilSettled(WorldStreamer streamer, int ticks = 6)
        {
            for (int i = 0; i < ticks; i++)
            {
                streamer.Tick(0.016f);
            }
        }

        [Test]
        public void Constructor_RejectsUnloadRadiusNotExceedingLoadRadius()
        {
            Assert.Throws<ArgumentException>(() =>
                new WorldStreamer(DefaultGrid(), new RecordingLoader(), new RecordingUnloader(), 300f, 300f, 4, 16));
        }

        [Test]
        public void Tick_LoadsChunksWithinLoadRadius()
        {
            var loader = new RecordingLoader();
            WorldStreamer streamer = CreateStreamer(loader, new RecordingUnloader());

            streamer.RegisterObserver("player", new WorldCoordinate(125f, 0f, 125f));
            TickUntilSettled(streamer);

            Assert.IsTrue(streamer.IsChunkLoaded(new ChunkCoordinate(0, 0)), "Observer's own chunk is not loaded.");
            Assert.IsTrue(streamer.IsChunkLoaded(new ChunkCoordinate(1, 0)), "East neighbor within radius is not loaded.");
            Assert.Greater(streamer.LoadedChunkCount, 0);
        }

        [Test]
        public void Tick_DoesNotLoadChunksBeyondLoadRadius()
        {
            var loader = new RecordingLoader();
            WorldStreamer streamer = CreateStreamer(loader, new RecordingUnloader());

            streamer.RegisterObserver("player", new WorldCoordinate(125f, 0f, 125f));
            TickUntilSettled(streamer);

            Assert.IsFalse(streamer.IsChunkLoaded(new ChunkCoordinate(4, 4)),
                "A chunk far beyond the load radius was loaded.");
        }

        [Test]
        public void NearestChunk_IsRequestedFirst()
        {
            var loader = new RecordingLoader();
            WorldStreamer streamer = CreateStreamer(loader, new RecordingUnloader(), maxConcurrent: 1);

            streamer.RegisterObserver("player", new WorldCoordinate(125f, 0f, 125f));
            streamer.Tick(0.016f);

            Assert.AreEqual(new ChunkCoordinate(0, 0), loader.Requested[0],
                "The observer's own chunk was not requested first.");
        }

        [Test]
        public void Hysteresis_KeepsChunkLoadedBetweenRadii()
        {
            var loader = new RecordingLoader();
            var unloader = new RecordingUnloader();
            WorldStreamer streamer = CreateStreamer(loader, unloader, loadRadius: 300f, unloadRadius: 450f);

            streamer.RegisterObserver("player", new WorldCoordinate(125f, 0f, 125f));
            TickUntilSettled(streamer);
            Assert.IsTrue(streamer.IsChunkLoaded(new ChunkCoordinate(0, 0)));

            // Move so the chunk center (125,125) is ~350 away: beyond load, inside unload radius.
            streamer.UpdateObserver("player", new WorldCoordinate(475f, 0f, 125f));
            TickUntilSettled(streamer);
            Assert.IsTrue(streamer.IsChunkLoaded(new ChunkCoordinate(0, 0)),
                "Chunk between load and unload radius was evicted (no hysteresis).");

            // Move so the chunk center is ~500 away: beyond the unload radius.
            streamer.UpdateObserver("player", new WorldCoordinate(625f, 0f, 125f));
            TickUntilSettled(streamer);
            Assert.IsFalse(streamer.IsChunkLoaded(new ChunkCoordinate(0, 0)),
                "Chunk beyond the unload radius was not evicted.");
            Assert.Contains(new ChunkCoordinate(0, 0), unloader.Requested);
        }

        [Test]
        public void MemoryBudget_CapsLoadedChunkCount()
        {
            var loader = new RecordingLoader();
            WorldStreamer streamer = CreateStreamer(loader, new RecordingUnloader(), maxLoaded: 3);

            streamer.RegisterObserver("player", new WorldCoordinate(125f, 0f, 125f));
            TickUntilSettled(streamer, ticks: 10);

            Assert.LessOrEqual(streamer.LoadedChunkCount, 3, "Loaded chunk count exceeded the memory budget.");
            Assert.Greater(streamer.LoadedChunkCount, 0);
        }

        [Test]
        public void UnregisterObserver_UnloadsEverything()
        {
            var loader = new RecordingLoader();
            WorldStreamer streamer = CreateStreamer(loader, new RecordingUnloader());

            streamer.RegisterObserver("player", new WorldCoordinate(125f, 0f, 125f));
            TickUntilSettled(streamer);
            Assert.Greater(streamer.LoadedChunkCount, 0);

            streamer.UnregisterObserver("player");
            TickUntilSettled(streamer);
            Assert.AreEqual(0, streamer.LoadedChunkCount, "Chunks remained loaded with no observers.");
        }

        [Test]
        public void TryGetChunkState_ReturnsLoadedStateWithCorrectIdentity()
        {
            WorldStreamer streamer = CreateStreamer(new RecordingLoader(), new RecordingUnloader());

            streamer.RegisterObserver("player", new WorldCoordinate(125f, 0f, 125f));
            TickUntilSettled(streamer);

            Assert.IsTrue(streamer.TryGetChunkState(new ChunkCoordinate(0, 0), out ChunkState state));
            Assert.AreEqual("C0_0", state.chunkId);
            Assert.AreEqual(0, state.coordinateX);
            Assert.AreEqual(0, state.coordinateY);
            Assert.IsTrue(state.isLoaded);
        }

        [Test]
        public void MovingObserver_ShiftsTheLoadedWindow()
        {
            WorldStreamer streamer = CreateStreamer(new RecordingLoader(), new RecordingUnloader());

            streamer.RegisterObserver("player", new WorldCoordinate(125f, 0f, 125f));
            TickUntilSettled(streamer);
            Assert.IsTrue(streamer.IsChunkLoaded(new ChunkCoordinate(0, 0)));

            streamer.UpdateObserver("player", new WorldCoordinate(2625f, 0f, 125f));
            TickUntilSettled(streamer, ticks: 10);

            Assert.IsFalse(streamer.IsChunkLoaded(new ChunkCoordinate(0, 0)), "Old window chunk still loaded.");
            Assert.IsTrue(streamer.IsChunkLoaded(new ChunkCoordinate(10, 0)), "New window chunk not loaded.");
        }

        [Test]
        public void OutOfBoundsChunks_AreNeverRequested()
        {
            var loader = new RecordingLoader();
            var grid = new WorldGrid(
                chunkSize: 250f,
                parcelsPerChunk: 5,
                bounds: new WorldBounds(new Vector3(0f, 0f, 0f), new Vector3(750f, 100f, 750f)));
            var streamer = new WorldStreamer(grid, loader, new RecordingUnloader(), 300f, 450f, 64, 64);

            streamer.RegisterObserver("player", new WorldCoordinate(10f, 0f, 10f));
            TickUntilSettled(streamer);

            foreach (ChunkCoordinate requested in loader.Requested)
            {
                Assert.IsTrue(grid.IsChunkWithinBounds(requested),
                    $"Out-of-bounds chunk {requested} was requested.");
            }
        }
    }

    public class ChunkLodCalculatorTests
    {
        private static readonly float[] Distances = { 300f, 600f };

        [Test]
        public void LodLevels_MapDistanceRingsCorrectly()
        {
            Assert.AreEqual(0, ChunkLodCalculator.GetLodLevel(0f, Distances));
            Assert.AreEqual(0, ChunkLodCalculator.GetLodLevel(300f, Distances));
            Assert.AreEqual(1, ChunkLodCalculator.GetLodLevel(301f, Distances));
            Assert.AreEqual(1, ChunkLodCalculator.GetLodLevel(600f, Distances));
            Assert.AreEqual(2, ChunkLodCalculator.GetLodLevel(601f, Distances));
            Assert.AreEqual(2, ChunkLodCalculator.GetLodLevel(10000f, Distances));
        }

        [Test]
        public void InvalidInputs_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => ChunkLodCalculator.GetLodLevel(100f, new float[0]));
            Assert.Throws<ArgumentException>(() => ChunkLodCalculator.GetLodLevel(100f, new[] { 600f, 300f }));
            Assert.Throws<ArgumentOutOfRangeException>(() => ChunkLodCalculator.GetLodLevel(-1f, Distances));
        }

        [Test]
        public void LodDistanceValidation_DetectsAscendingSequences()
        {
            Assert.IsTrue(ChunkLodCalculator.AreValidLodDistances(new[] { 100f, 200f, 400f }));
            Assert.IsFalse(ChunkLodCalculator.AreValidLodDistances(new[] { 100f, 100f }));
            Assert.IsFalse(ChunkLodCalculator.AreValidLodDistances(new[] { -50f, 100f }));
            Assert.IsFalse(ChunkLodCalculator.AreValidLodDistances(null));
        }
    }
}
