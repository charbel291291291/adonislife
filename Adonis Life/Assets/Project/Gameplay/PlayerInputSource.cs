using UnityEngine;
using UnityEngine.InputSystem;

namespace AdonisLife.Gameplay
{
    /// <summary>
    /// Abstraction over player input so gameplay components never read devices directly:
    /// production uses <see cref="KeyboardInputSource"/>, tests inject scripted sources.
    /// </summary>
    public interface IPlayerInputSource
    {
        /// <summary>Movement input: x turns, y moves forward. Each axis in [-1, 1].</summary>
        Vector2 MoveInput { get; }

        /// <summary>True while the sprint modifier is held.</summary>
        bool Sprint { get; }
    }

    /// <summary>
    /// Default Unity Input System implementation reading WASD/arrow keys and Left Shift from
    /// the current keyboard. Safe when no keyboard is present (returns neutral input).
    /// </summary>
    public class KeyboardInputSource : IPlayerInputSource
    {
        public Vector2 MoveInput
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard == null)
                {
                    return Vector2.zero;
                }

                float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                          (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                          (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
                return new Vector2(x, y);
            }
        }

        public bool Sprint
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                return keyboard != null && keyboard.leftShiftKey.isPressed;
            }
        }
    }
}
