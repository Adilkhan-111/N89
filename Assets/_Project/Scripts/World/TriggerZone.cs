using System;
using N89.Player;
using UnityEngine;

namespace N89.World
{
    // Зона-триггер: срабатывает, когда в неё заходит игрок.
    [RequireComponent(typeof(Collider2D))]
    public class TriggerZone : MonoBehaviour
    {
        public bool PlayerInside { get; private set; }
        public bool WasEntered { get; private set; }

        public event Action<TriggerZone> Entered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            PlayerInside = true;
            WasEntered = true;
            Entered?.Invoke(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
            {
                PlayerInside = false;
            }
        }
    }
}
