using System;

namespace AdonisLife.World.ProceduralCity
{
    /// <summary>
    /// Zero-based (X, Z) index of one urban cell within the procedural city grid.
    /// </summary>
    public readonly struct CellCoordinate2D : IEquatable<CellCoordinate2D>
    {
        public readonly int X;
        public readonly int Z;

        public CellCoordinate2D(int x, int z)
        {
            X = x;
            Z = z;
        }

        public bool Equals(CellCoordinate2D other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is CellCoordinate2D other && Equals(other);
        public override int GetHashCode() => unchecked((X * 397) ^ Z);
        public override string ToString() => $"Cell({X}, {Z})";
    }
}
