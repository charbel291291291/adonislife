using System;
using UnityEngine;

namespace AdonisLife.World.Streaming
{
    /// <summary>
    /// Represents a high-precision coordinate in world space.
    /// </summary>
    [Serializable]
    public struct WorldCoordinate : IEquatable<WorldCoordinate>
    {
        [SerializeField]
        private Vector3 _value;

        /// <summary>
        /// Gets the raw Vector3 representation of this world coordinate.
        /// </summary>
        public Vector3 Value => _value;

        /// <summary>
        /// Gets the X component of the coordinate.
        /// </summary>
        public float X => _value.x;

        /// <summary>
        /// Gets the Y component of the coordinate.
        /// </summary>
        public float Y => _value.y;

        /// <summary>
        /// Gets the Z component of the coordinate.
        /// </summary>
        public float Z => _value.z;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldCoordinate"/> struct.
        /// </summary>
        /// <param name="position">The raw world position.</param>
        public WorldCoordinate(Vector3 position)
        {
            _value = position;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldCoordinate"/> struct.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        public WorldCoordinate(float x, float y, float z)
        {
            _value = new Vector3(x, y, z);
        }

        /// <summary>
        /// Calculates the distance to another world coordinate on the horizontal (X-Z) plane.
        /// </summary>
        /// <param name="other">The other coordinate.</param>
        /// <returns>The distance in world units.</returns>
        public float Distance2D(WorldCoordinate other)
        {
            float dx = X - other.X;
            float dz = Z - other.Z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Calculates the 3D Euclidean distance to another world coordinate.
        /// </summary>
        /// <param name="other">The other coordinate.</param>
        /// <returns>The distance in world units.</returns>
        public float Distance3D(WorldCoordinate other)
        {
            return Vector3.Distance(_value, other._value);
        }

        /// <inheritdoc />
        public bool Equals(WorldCoordinate other)
        {
            return _value.Equals(other._value);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is WorldCoordinate other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(WorldCoordinate left, WorldCoordinate right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(WorldCoordinate left, WorldCoordinate right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Implicit conversion from Vector3 to WorldCoordinate.
        /// </summary>
        public static implicit operator WorldCoordinate(Vector3 vec) => new WorldCoordinate(vec);

        /// <summary>
        /// Implicit conversion from WorldCoordinate to Vector3.
        /// </summary>
        public static implicit operator Vector3(WorldCoordinate coord) => coord._value;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"World({X:F2}, {Y:F2}, {Z:F2})";
        }
    }

    /// <summary>
    /// Represents a discrete 2D coordinate of a chunk in the world grid.
    /// </summary>
    [Serializable]
    public struct ChunkCoordinate : IEquatable<ChunkCoordinate>
    {
        [SerializeField]
        private int _x;

        [SerializeField]
        private int _y;

        /// <summary>
        /// Gets the X coordinate in the chunk grid.
        /// </summary>
        public int X => _x;

        /// <summary>
        /// Gets the Y coordinate in the chunk grid.
        /// </summary>
        public int Y => _y;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkCoordinate"/> struct.
        /// </summary>
        /// <param name="x">The grid X coordinate.</param>
        /// <param name="y">The grid Y coordinate.</param>
        public ChunkCoordinate(int x, int y)
        {
            _x = x;
            _y = y;
        }

        /// <inheritdoc />
        public bool Equals(ChunkCoordinate other)
        {
            return _x == other._x && _y == other._y;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ChunkCoordinate other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (_x * 397) ^ _y;
            }
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(ChunkCoordinate left, ChunkCoordinate right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(ChunkCoordinate left, ChunkCoordinate right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Implicit conversion from Vector2Int to ChunkCoordinate.
        /// </summary>
        public static implicit operator ChunkCoordinate(Vector2Int vec) => new ChunkCoordinate(vec.x, vec.y);

        /// <summary>
        /// Implicit conversion from ChunkCoordinate to Vector2Int.
        /// </summary>
        public static implicit operator Vector2Int(ChunkCoordinate coord) => new Vector2Int(coord._x, coord._y);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Chunk({_x}, {_y})";
        }
    }

    /// <summary>
    /// Represents a discrete coordinate of a parcel within a specific chunk,
    /// supporting both local and global representations.
    /// </summary>
    [Serializable]
    public struct ParcelCoordinate : IEquatable<ParcelCoordinate>
    {
        [SerializeField]
        private ChunkCoordinate _chunk;

        [SerializeField]
        private int _localX;

        [SerializeField]
        private int _localY;

        /// <summary>
        /// Gets the parent chunk coordinate.
        /// </summary>
        public ChunkCoordinate Chunk => _chunk;

        /// <summary>
        /// Gets the local X coordinate of the parcel within the chunk.
        /// </summary>
        public int LocalX => _localX;

        /// <summary>
        /// Gets the local Y coordinate of the parcel within the chunk.
        /// </summary>
        public int LocalY => _localY;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParcelCoordinate"/> struct.
        /// </summary>
        /// <param name="chunk">The coordinate of the chunk containing this parcel.</param>
        /// <param name="localX">The local grid X within the chunk.</param>
        /// <param name="localY">The local grid Y within the chunk.</param>
        public ParcelCoordinate(ChunkCoordinate chunk, int localX, int localY)
        {
            _chunk = chunk;
            _localX = localX;
            _localY = localY;
        }

        /// <summary>
        /// Calculates the global coordinates of this parcel, assuming a fixed grid size of parcels per chunk.
        /// </summary>
        /// <param name="parcelsPerChunk">The number of parcels along one axis of a chunk.</param>
        /// <returns>A tuple containing the global grid X and Y coordinates.</returns>
        public (int globalX, int globalY) GetGlobalCoordinates(int parcelsPerChunk)
        {
            return (_chunk.X * parcelsPerChunk + _localX, _chunk.Y * parcelsPerChunk + _localY);
        }

        /// <inheritdoc />
        public bool Equals(ParcelCoordinate other)
        {
            return _chunk.Equals(other._chunk) && _localX == other._localX && _localY == other._localY;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ParcelCoordinate other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = _chunk.GetHashCode();
                hashCode = (hashCode * 397) ^ _localX;
                hashCode = (hashCode * 397) ^ _localY;
                return hashCode;
            }
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(ParcelCoordinate left, ParcelCoordinate right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(ParcelCoordinate left, ParcelCoordinate right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Parcel(Chunk: {_chunk}, Local: ({_localX}, {_localY}))";
        }
    }
}