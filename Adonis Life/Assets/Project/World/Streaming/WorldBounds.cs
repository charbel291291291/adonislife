using System;
using UnityEngine;

namespace AdonisLife.World.Streaming
{
    /// <summary>
    /// Represents the bounding limits of the streaming world.
    /// Used to validate coordinates and constrain streaming operations within valid region bounds.
    /// </summary>
    [Serializable]
    public struct WorldBounds : IEquatable<WorldBounds>
    {
        [SerializeField]
        private Vector3 _min;

        [SerializeField]
        private Vector3 _max;

        /// <summary>
        /// Gets the minimum corner of the world bounds in world space.
        /// </summary>
        public Vector3 Min => _min;

        /// <summary>
        /// Gets the maximum corner of the world bounds in world space.
        /// </summary>
        public Vector3 Max => _max;

        /// <summary>
        /// Gets the size of the world bounds along each axis.
        /// </summary>
        public Vector3 Size => _max - _min;

        /// <summary>
        /// Gets the center position of the world bounds.
        /// </summary>
        public Vector3 Center => _min + (_max - _min) * 0.5f;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldBounds"/> struct.
        /// </summary>
        /// <param name="min">The minimum corner of the bounds.</param>
        /// <param name="max">The maximum corner of the bounds.</param>
        /// <exception cref="ArgumentException">Thrown if any component of min is greater than max.</exception>
        public WorldBounds(Vector3 min, Vector3 max)
        {
            if (min.x > max.x || min.y > max.y || min.z > max.z)
            {
                throw new ArgumentException("Min corner components must be less than or equal to Max corner components.");
            }

            _min = min;
            _max = max;
        }

        /// <summary>
        /// Checks if a world coordinate is inside the boundary limits.
        /// </summary>
        /// <param name="coordinate">The world coordinate to test.</param>
        /// <param name="includeHeight">Whether to validate height (Y axis) or only horizontal bounds (X and Z axis).</param>
        /// <returns>True if inside; otherwise, false.</returns>
        public bool Contains(WorldCoordinate coordinate, bool includeHeight = false)
        {
            if (coordinate.X < _min.x || coordinate.X > _max.x) return false;
            if (coordinate.Z < _min.z || coordinate.Z > _max.z) return false;
            if (includeHeight)
            {
                if (coordinate.Y < _min.y || coordinate.Y > _max.y) return false;
            }
            return true;
        }

        /// <summary>
        /// Clamps a world coordinate to stay within the bounds of the world.
        /// </summary>
        /// <param name="coordinate">The original coordinate.</param>
        /// <returns>The clamped WorldCoordinate.</returns>
        public WorldCoordinate Clamp(WorldCoordinate coordinate)
        {
            float clampedX = Mathf.Clamp(coordinate.X, _min.x, _max.x);
            float clampedY = Mathf.Clamp(coordinate.Y, _min.y, _max.y);
            float clampedZ = Mathf.Clamp(coordinate.Z, _min.z, _max.z);
            return new WorldCoordinate(clampedX, clampedY, clampedZ);
        }

        /// <inheritdoc/>
        public bool Equals(WorldBounds other)
        {
            return _min.Equals(other._min) && _max.Equals(other._max);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is WorldBounds other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (_min.GetHashCode() * 397) ^ _max.GetHashCode();
            }
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(WorldBounds left, WorldBounds right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(WorldBounds left, WorldBounds right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Bounds(Min: {_min}, Max: {_max})";
        }
    }
}