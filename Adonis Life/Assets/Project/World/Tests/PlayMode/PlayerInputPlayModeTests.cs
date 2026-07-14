using System.Collections;
using System.Collections.Generic;
using AdonisLife.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AdonisLife.World.Tests.PlayMode
{
    /// <summary>
    /// PlayMode coverage for the Input System migration: player movement and camera follow
    /// driven through the injectable input source, exercising the real Update/LateUpdate paths.
    /// </summary>
    public class PlayerInputPlayModeTests
    {
        private class ScriptedInputSource : IPlayerInputSource
        {
            public Vector2 MoveInput { get; set; }
            public bool Sprint { get; set; }
        }

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }

            _spawned.Clear();
        }

        private GameObject CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "TestGround";
            ground.transform.localScale = new Vector3(200f, 1f, 200f);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            _spawned.Add(ground);
            return ground;
        }

        private (GameObject player, PlayerController controller, ScriptedInputSource input) CreatePlayer(Vector3 position)
        {
            var player = new GameObject("TestPlayer");
            player.transform.position = position;
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;

            PlayerController controller = player.AddComponent<PlayerController>();
            var input = new ScriptedInputSource();
            controller.InputSource = input;

            _spawned.Add(player);
            return (player, controller, input);
        }

        [UnityTest]
        public IEnumerator Player_MovesForwardFromInput()
        {
            CreateGround();
            (GameObject player, PlayerController _, ScriptedInputSource input) = CreatePlayer(new Vector3(0f, 1f, 0f));

            input.MoveInput = new Vector2(0f, 1f);
            float startZ = player.transform.position.z;

            yield return new WaitForSeconds(0.5f);

            Assert.Greater(player.transform.position.z - startZ, 0.5f,
                "Forward input did not move the player forward.");
            Assert.AreEqual(0f, player.transform.position.x, 0.05f,
                "Forward-only input drifted sideways.");
        }

        [UnityTest]
        public IEnumerator Player_SprintsFasterThanWalking()
        {
            CreateGround();
            (GameObject walker, PlayerController _, ScriptedInputSource walkInput) =
                CreatePlayer(new Vector3(-5f, 1f, 0f));
            (GameObject sprinter, PlayerController _, ScriptedInputSource sprintInput) =
                CreatePlayer(new Vector3(5f, 1f, 0f));

            walkInput.MoveInput = new Vector2(0f, 1f);
            sprintInput.MoveInput = new Vector2(0f, 1f);
            sprintInput.Sprint = true;

            float walkerStartZ = walker.transform.position.z;
            float sprinterStartZ = sprinter.transform.position.z;

            yield return new WaitForSeconds(0.5f);

            float walked = walker.transform.position.z - walkerStartZ;
            float sprinted = sprinter.transform.position.z - sprinterStartZ;
            Assert.Greater(sprinted, walked * 1.3f,
                $"Sprinting ({sprinted:F2}m) was not clearly faster than walking ({walked:F2}m).");
        }

        [UnityTest]
        public IEnumerator Player_TurnsWithHorizontalInput()
        {
            CreateGround();
            (GameObject player, PlayerController _, ScriptedInputSource input) = CreatePlayer(new Vector3(0f, 1f, 0f));

            input.MoveInput = new Vector2(1f, 0f);
            float startYaw = player.transform.eulerAngles.y;

            yield return new WaitForSeconds(0.5f);

            float turned = Mathf.DeltaAngle(startYaw, player.transform.eulerAngles.y);
            Assert.Greater(turned, 5f, "Horizontal input did not turn the player.");
        }

        [UnityTest]
        public IEnumerator Camera_FollowsBehindMovingPlayer()
        {
            CreateGround();
            (GameObject player, PlayerController _, ScriptedInputSource input) = CreatePlayer(new Vector3(0f, 1f, 0f));

            var cameraObject = new GameObject("TestCamera");
            cameraObject.transform.position = player.transform.position + new Vector3(0f, 4f, -8f);
            ThirdPersonCamera follow = cameraObject.AddComponent<ThirdPersonCamera>();
            follow.SetTarget(player.transform);
            _spawned.Add(cameraObject);

            input.MoveInput = new Vector2(0f, 1f);

            yield return new WaitForSeconds(1f);

            Vector3 cameraPosition = cameraObject.transform.position;
            Vector3 playerPosition = player.transform.position;

            Assert.Less(cameraPosition.z, playerPosition.z, "Camera is not behind the forward-moving player.");
            Assert.Greater(cameraPosition.y, playerPosition.y, "Camera did not stay above the player.");

            float distance = Vector3.Distance(cameraPosition, playerPosition);
            Assert.Less(distance, 12f, "Camera fell too far behind its target.");
            Assert.Greater(distance, 4f, "Camera collapsed onto its target.");

            Vector3 toPlayer = (playerPosition + Vector3.up * 1.6f - cameraPosition).normalized;
            float alignment = Vector3.Dot(cameraObject.transform.forward, toPlayer);
            Assert.Greater(alignment, 0.95f, "Camera is not looking at the player.");
        }
    }
}
