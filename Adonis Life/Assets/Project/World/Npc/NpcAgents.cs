using System.Collections.Generic;
using UnityEngine;

namespace AdonisLife.World.Npc
{
    /// <summary>
    /// Runtime pedestrian: walks a looping waypoint route on the sidewalk network using the
    /// pure <see cref="WaypointFollower"/> math.
    /// </summary>
    public class PedestrianAgent : MonoBehaviour
    {
        [SerializeField] private float _walkSpeed = 1.4f;

        private readonly List<Vector2> _waypoints = new List<Vector2>();
        private int _targetIndex;
        private float _walkHeight;

        public void Initialize(IEnumerable<Vector2> waypoints, float walkHeight)
        {
            _waypoints.Clear();
            _waypoints.AddRange(waypoints);
            _targetIndex = 0;
            _walkHeight = walkHeight;
        }

        private void Update()
        {
            if (_waypoints.Count == 0)
            {
                return;
            }

            var position = new Vector2(transform.position.x, transform.position.z);
            (Vector2 next, int index) = WaypointFollower.Step(
                position, _targetIndex, _waypoints, _walkSpeed, Time.deltaTime, loop: true);
            _targetIndex = index;

            Vector2 direction = next - position;
            transform.position = new Vector3(next.x, _walkHeight, next.y);
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));
            }
        }
    }

    /// <summary>
    /// Runtime vehicle: drives its lane start-to-end and wraps back to the start, using the
    /// pure <see cref="WaypointFollower"/> math. Lane-to-lane routing arrives with traffic AI.
    /// </summary>
    public class VehicleAgent : MonoBehaviour
    {
        [SerializeField] private float _driveSpeed = 8f;

        private readonly List<Vector2> _route = new List<Vector2>(2);
        private int _targetIndex;
        private float _rideHeight;

        public void Initialize(Vector2 laneStart, Vector2 laneEnd, float rideHeight)
        {
            _route.Clear();
            _route.Add(laneEnd);
            _route.Add(laneStart);
            _targetIndex = 0;
            _rideHeight = rideHeight;
        }

        private void Update()
        {
            if (_route.Count == 0)
            {
                return;
            }

            var position = new Vector2(transform.position.x, transform.position.z);
            (Vector2 next, int index) = WaypointFollower.Step(
                position, _targetIndex, _route, _driveSpeed, Time.deltaTime, loop: false);

            // Wrap: on reaching the lane end, teleport back to the lane start.
            if (index == _route.Count - 1 && Vector2.Distance(next, _route[0]) < WaypointFollower.ArrivalEpsilon)
            {
                next = _route[1];
                index = 0;
            }

            _targetIndex = index;

            Vector2 direction = _route[0] - next;
            transform.position = new Vector3(next.x, _rideHeight, next.y);
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));
            }
        }
    }
}
