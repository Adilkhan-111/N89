using N89.Combat;
using UnityEngine;

namespace N89.Enemies
{
    // Турель: поворачивается к цели и стреляет, когда навелась.
    public class Turret : MonoBehaviour
    {
        [SerializeField] private Transform _head;
        [SerializeField] private Weapon _weapon;
        [SerializeField] private float _turnSpeed = 540f;
        [SerializeField] private float _fireAngle = 8f;

        public Transform Target { get; set; }
        public bool Active { get; set; }

        private void Update()
        {
            if (!Active || Target == null)
            {
                return;
            }

            Vector2 toTarget = Target.position - _head.position;
            float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float current = _head.eulerAngles.z;
            float next = Mathf.MoveTowardsAngle(current, targetAngle, _turnSpeed * Time.deltaTime);
            _head.rotation = Quaternion.Euler(0f, 0f, next);

            if (Mathf.Abs(Mathf.DeltaAngle(next, targetAngle)) < _fireAngle)
            {
                _weapon.TryFire(_head.right);
            }
        }
    }
}
