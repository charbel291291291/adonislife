using AdonisLife.World.ProceduralCity;
using UnityEngine;

namespace AdonisLife.World.Authored
{
    /// <summary>
    /// Authored, configurable parameters for the procedural city generator prototype.
    /// </summary>
    [CreateAssetMenu(fileName = "ProceduralCitySettings", menuName = "AdonisLife/World/ProceduralCitySettings")]
    public class ProceduralCitySettingsSO : ScriptableObject
    {
        [SerializeField] private int _cellsX = 3;
        public int CellsX => _cellsX;

        [SerializeField] private int _cellsZ = 3;
        public int CellsZ => _cellsZ;

        [SerializeField] private float _cellSize = 250f;
        public float CellSize => _cellSize;

        [SerializeField] private float _mainRoadWidth = 20f;
        public float MainRoadWidth => _mainRoadWidth;

        [SerializeField] private float _secondaryRoadWidth = 14f;
        public float SecondaryRoadWidth => _secondaryRoadWidth;

        [SerializeField] private float _sidewalkWidth = 4f;
        public float SidewalkWidth => _sidewalkWidth;

        [SerializeField] private float _blockInset = 2f;
        public float BlockInset => _blockInset;

        [SerializeField] private int _seed;
        public int Seed => _seed;

        public CityGenerationSettings ToGenerationSettings()
        {
            return new CityGenerationSettings(
                _cellsX, _cellsZ, _cellSize, _mainRoadWidth, _secondaryRoadWidth, _sidewalkWidth, _blockInset, _seed);
        }

        public bool IsValid(out string validationError)
        {
            if (_cellsX <= 0 || _cellsZ <= 0)
            {
                validationError = $"Grid dimensions must be positive. Current grid: {_cellsX} x {_cellsZ}.";
                return false;
            }

            if (_cellSize <= 0f)
            {
                validationError = $"Cell size must be positive. Current cell size: {_cellSize}.";
                return false;
            }

            if (_mainRoadWidth <= 0f || _secondaryRoadWidth <= 0f)
            {
                validationError = "Road widths must be positive.";
                return false;
            }

            if (_sidewalkWidth < 0f || _blockInset < 0f)
            {
                validationError = "Sidewalk width and block inset must not be negative.";
                return false;
            }

            float mainBand = _mainRoadWidth + (2f * _sidewalkWidth);
            float secondaryBand = _secondaryRoadWidth + (2f * _sidewalkWidth);

            if (mainBand >= _cellSize || secondaryBand >= _cellSize)
            {
                validationError = $"Road and sidewalk widths ({mainBand} x {secondaryBand}) leave no room for development blocks within a {_cellSize}m cell.";
                return false;
            }

            if ((mainBand / 2f) + _blockInset >= _cellSize / 2f || (secondaryBand / 2f) + _blockInset >= _cellSize / 2f)
            {
                validationError = "Block inset is too large for the remaining development block area.";
                return false;
            }

            validationError = null;
            return true;
        }
    }
}
