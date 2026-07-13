using UnityEngine;

namespace AdonisLife.Gameplay
{
    /// <summary>Pure third-person camera positioning math.</summary>
    public static class CameraFollowModel
    {
        /// <summary>
        /// Camera position on an orbit around the target: yaw around Y, pitch tilting down,
        /// at the given distance, looking toward the target.
        /// </summary>
        public static Vector3 ComputePosition(Vector3 targetPosition, float yawDegrees, float pitchDegrees, float distance)
        {
            Quaternion orbit = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            return targetPosition - orbit * Vector3.forward * distance;
        }
    }

    /// <summary>
    /// Runtime follow camera: orbits behind its target with smoothing, using
    /// <see cref="CameraFollowModel"/> for the placement math.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _distance = 8f;
        [SerializeField] private float _pitchDegrees = 25f;
        [SerializeField] private float _heightOffset = 1.6f;
        [SerializeField] private float _smoothing = 6f;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 focus = _target.position + Vector3.up * _heightOffset;
            Vector3 desired = CameraFollowModel.ComputePosition(
                focus, _target.eulerAngles.y, _pitchDegrees, _distance);

            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-_smoothing * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(focus - transform.position);
        }
    }
}
