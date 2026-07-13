using System.Collections.Generic;
using UnityEngine;

namespace AdonisLife.Gameplay
{
    /// <summary>Anything the player can interact with.</summary>
    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(GameObject interactor);
    }

    /// <summary>Pure interaction target selection.</summary>
    public static class InteractionModel
    {
        /// <summary>
        /// Index of the nearest candidate within range, or -1 if none qualifies. Ties resolve
        /// to the lowest index for determinism.
        /// </summary>
        public static int SelectNearest(Vector3 origin, IReadOnlyList<Vector3> candidatePositions, float maxRange)
        {
            int selected = -1;
            float best = maxRange;

            for (int i = 0; i < candidatePositions.Count; i++)
            {
                float distance = Vector3.Distance(origin, candidatePositions[i]);
                if (distance < best)
                {
                    best = distance;
                    selected = i;
                }
            }

            return selected;
        }
    }

    /// <summary>
    /// Runtime interactor: finds the nearest <see cref="IInteractable"/> in range each frame
    /// and triggers it on the interact key.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private float _range = 3f;
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        public IInteractable Current { get; private set; }

        private void Update()
        {
            Current = FindNearest();
            if (Current != null && Input.GetKeyDown(_interactKey))
            {
                Current.Interact(gameObject);
            }
        }

        private IInteractable FindNearest()
        {
            var interactables = new List<IInteractable>();
            var positions = new List<Vector3>();

            foreach (Collider collider in Physics.OverlapSphere(transform.position, _range))
            {
                if (collider.TryGetComponent(out IInteractable interactable))
                {
                    interactables.Add(interactable);
                    positions.Add(collider.transform.position);
                }
            }

            int index = InteractionModel.SelectNearest(transform.position, positions, _range);
            return index >= 0 ? interactables[index] : null;
        }
    }
}
