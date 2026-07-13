using System;
using AdonisLife.World.Authored;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AdonisLife.World.Tests.Editor
{
    public class WorldConfigValidationTests
    {
        private WorldConfigSO _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<WorldConfigSO>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void DefaultConfiguration_HasWorldSize2000x2000()
        {
            Assert.AreEqual(new Vector2(2000f, 2000f), _config.WorldSize);
        }

        [Test]
        public void DefaultConfiguration_HasChunkSize250()
        {
            Assert.AreEqual(250f, _config.ChunkSize);
        }

        [Test]
        public void DefaultConfiguration_ChunkCountXEquals8()
        {
            Assert.AreEqual(8, _config.ChunkCountX);
        }

        [Test]
        public void DefaultConfiguration_ChunkCountZEquals8()
        {
            Assert.AreEqual(8, _config.ChunkCountZ);
        }

        [Test]
        public void DefaultConfiguration_IsValid()
        {
            bool isValid = _config.IsValid(out string validationError);

            Assert.IsTrue(isValid, validationError);
            Assert.IsNull(validationError);
        }

        [Test]
        public void EmptyId_FailsValidation()
        {
            SetSerializedField("_id", string.Empty);

            bool isValid = _config.IsValid(out string validationError);

            Assert.IsFalse(isValid);
            Assert.IsNotEmpty(validationError);
        }

        [Test]
        public void InvalidWorldDimensions_FailValidation()
        {
            SetSerializedField("_worldSize", Vector2.zero);

            bool isValid = _config.IsValid(out string validationError);

            Assert.IsFalse(isValid);
            Assert.IsNotEmpty(validationError);
        }

        [Test]
        public void InvalidChunkSize_FailsValidation()
        {
            SetSerializedField("_chunkSize", 0f);

            bool isValid = _config.IsValid(out string validationError);

            Assert.IsFalse(isValid);
            Assert.IsNotEmpty(validationError);
        }

        [Test]
        public void NonDivisibleWorldSize_FailsValidation()
        {
            SetSerializedField("_worldSize", new Vector2(2100f, 2000f));

            bool isValid = _config.IsValid(out string validationError);

            Assert.IsFalse(isValid);
            Assert.IsNotEmpty(validationError);
        }

        /// <summary>
        /// Mutates a private serialized field via SerializedObject so tests can exercise
        /// invalid states without adding public setters to WorldConfigSO.
        /// </summary>
        private void SetSerializedField(string fieldName, object value)
        {
            var serializedObject = new SerializedObject(_config);
            SerializedProperty property = serializedObject.FindProperty(fieldName);

            switch (value)
            {
                case string stringValue:
                    property.stringValue = stringValue;
                    break;
                case float floatValue:
                    property.floatValue = floatValue;
                    break;
                case Vector2 vectorValue:
                    property.vector2Value = vectorValue;
                    break;
                default:
                    throw new ArgumentException($"Unsupported value type: {value.GetType()}");
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
