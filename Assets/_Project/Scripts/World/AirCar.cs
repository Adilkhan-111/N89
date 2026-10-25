using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace N89.World
{
    // Летающая машина Рэя: стоит, пока он не сядет; TakeOff — опоры вспыхивают, машина поднимается
    // (растёт, тень уходит в сторону) и, разгоняясь, улетает за кадр. Посадку героя делает сценарий.
    public class AirCar : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Transform _shadow;
        [SerializeField] private Light2D[] _thrusters;

        // Ближайшая к герою дверь — сбоку от купола
        public Vector2 DoorFor(Vector2 hero)
        {
            float side = Vector2.Dot(hero - (Vector2)transform.position, transform.up) >= 0f ? 0.8f : -0.8f;
            return (Vector2)transform.position + (Vector2)transform.up * side;
        }

        public void DisableColliders()
        {
            foreach (Collider2D col in GetComponentsInChildren<Collider2D>()) col.enabled = false;
        }

        public IEnumerator TakeOff()
        {
            DisableColliders();
            N89.Core.Sfx.Play(N89.Core.Sfx.CarStart, transform.position);
            Transform body = _body.transform;
            int order = _body.sortingOrder;
            _body.sortingOrder = 60;
            Vector3 shadowFrom = _shadow.localPosition;

            for (float t = 0f; t < 1.3f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 1.3f);
                foreach (Light2D thruster in _thrusters) thruster.intensity = k * Random.Range(1.2f, 1.8f);
                body.localScale = Vector3.one * (1f + 0.15f * k);
                body.localPosition = new Vector3(0f, 0.04f * Mathf.Sin(t * 9f), 0f);
                _shadow.localPosition = shadowFrom + new Vector3(0.3f, -0.45f, 0f) * k;
                _shadow.localScale = Vector3.one * (1f - 0.15f * k);
                yield return null;
            }
            yield return new WaitForSeconds(0.3f);

            // Разгон вперёд и вверх
            N89.Core.Sfx.Play(N89.Core.Sfx.CarFly, transform.position);
            float speed = 0f;
            for (float t = 0f; t < 2.2f; t += Time.deltaTime)
            {
                speed += 24f * Time.deltaTime;
                transform.position += transform.right * (speed * Time.deltaTime);
                body.localScale = Vector3.one * (1.15f + 0.3f * Mathf.Clamp01(t / 2.2f));
                _shadow.localPosition = shadowFrom + new Vector3(0.3f, -0.45f, 0f) * (1f + t);
                foreach (Light2D thruster in _thrusters) thruster.intensity = Random.Range(1.6f, 2.4f);
                yield return null;
            }
            _body.sortingOrder = order;
        }
    }
}
