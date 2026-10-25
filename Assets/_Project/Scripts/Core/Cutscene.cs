using System.Collections;
using UnityEngine;

namespace N89.Core
{
    // Движения для катсцен: персонажи шагают (ноги анимирует LegsAnimator — по смещению), дёргаются от
    // отдачи и попаданий, падают; стоп-кадр и замедление. actor — корень персонажа, body — повёрнутый спрайт.
    public static class Cutscene
    {
        // Идёт к точке: разгон, ровный шаг, торможение; корпус поворачивается по ходу (если faceMove)
        public static IEnumerator Walk(Transform actor, Transform body, Vector2 to, float speed, bool faceMove = true)
        {
            Vector2 from = actor.position;
            float distance = Vector2.Distance(from, to);
            if (distance < 0.01f) yield break;
            Rigidbody2D rb = actor.GetComponent<Rigidbody2D>();
            float duration = distance / speed + 0.2f;
            Vector2 dir = (to - from) / distance;
            Quaternion facing = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (actor == null) yield break;
                SetPosition(actor, rb, Vector2.Lerp(from, to, EaseWalk(t / duration)));
                if (faceMove && body != null) body.rotation = Quaternion.RotateTowards(body.rotation, facing, 540f * Time.deltaTime);
                yield return null;
            }
            SetPosition(actor, rb, to);
        }

        // Плавный поворот корпуса к точке
        public static IEnumerator Turn(Transform body, Vector2 lookAt, float duration)
        {
            Vector2 dir = lookAt - (Vector2)body.position;
            Quaternion from = body.rotation;
            Quaternion to = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (body == null) yield break;
                body.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            body.rotation = to;
        }

        // Отдача: корпус дёргается назад по линии ствола и возвращается
        public static IEnumerator Recoil(Transform body, float distance = 0.12f, float duration = 0.14f)
        {
            Vector3 rest = body.localPosition;
            Vector3 back = body.parent != null ? body.parent.InverseTransformDirection(-body.right) * distance : -body.right * distance;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                body.localPosition = rest + back * (k < 0.25f ? k / 0.25f : 1f - (k - 0.25f) / 0.75f);
                yield return null;
            }
            body.localPosition = rest;
        }

        // Попадание: отбрасывает по направлению выстрела (быстро, с торможением), корпус дёргается в сторону
        public static IEnumerator Stagger(Transform actor, Transform body, Vector2 dir, float distance = 0.3f, float duration = 0.22f)
        {
            Rigidbody2D rb = actor.GetComponent<Rigidbody2D>();
            Vector2 from = actor.position;
            Vector2 to = from + dir.normalized * distance;
            Quaternion rest = body != null ? body.rotation : Quaternion.identity;
            float twist = Random.Range(18f, 30f) * (Random.value < 0.5f ? -1f : 1f);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                SetPosition(actor, rb, Vector2.Lerp(from, to, 1f - (1f - k) * (1f - k)));
                if (body != null) body.rotation = rest * Quaternion.Euler(0f, 0f, twist * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            SetPosition(actor, rb, to);
            if (body != null) body.rotation = rest;
        }

        // Падение: тело заваливается по направлению dir (на середине — спрайт лежащего), проезжает по полу
        // и «шлёпается» (сплющивание и отскок масштаба)
        public static IEnumerator Fall(Transform actor, SpriteRenderer body, Sprite lying, Vector2 dir, float slide = 0.35f, float duration = 0.35f)
        {
            Rigidbody2D rb = actor.GetComponent<Rigidbody2D>();
            Transform t0 = body.transform;
            Vector3 scale = t0.localScale;
            Vector2 from = actor.position;
            Vector2 to = from + dir.normalized * slide;
            float target = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion start = t0.rotation;
            bool swapped = false;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                if (!swapped && k >= 0.45f && lying != null)
                {
                    body.sprite = lying;
                    swapped = true;
                }
                t0.rotation = Quaternion.Slerp(start, Quaternion.Euler(0f, 0f, target), k * k);
                // Пока падает — чуть «выше» (крупнее), у пола — сплющивается
                float lift = Mathf.Sin(k * Mathf.PI) * 0.12f;
                t0.localScale = new Vector3(scale.x * (1f + lift), scale.y * (1f + lift), scale.z);
                SetPosition(actor, rb, Vector2.Lerp(from, to, 1f - (1f - k) * (1f - k)));
                yield return null;
            }
            if (lying != null) body.sprite = lying;
            t0.rotation = Quaternion.Euler(0f, 0f, target);
            for (float t = 0f; t < 0.12f; t += Time.deltaTime)
            {
                float k = t / 0.12f;
                t0.localScale = new Vector3(scale.x * (1f + 0.08f * (1f - k)), scale.y * (1f - 0.1f * (1f - k)), scale.z);
                yield return null;
            }
            t0.localScale = scale;
            SetPosition(actor, rb, to);
        }

        // Короткий выпад к точке и назад: жест в разговоре, рывок к оружию
        public static IEnumerator Lunge(Transform body, Vector2 dir, float distance = 0.15f, float duration = 0.25f)
        {
            Vector3 rest = body.localPosition;
            Vector3 offset = body.parent != null ? body.parent.InverseTransformDirection(dir.normalized) * distance : (Vector3)(dir.normalized * distance);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                body.localPosition = rest + offset * Mathf.Sin(t / duration * Mathf.PI);
                yield return null;
            }
            body.localPosition = rest;
        }

        // Предмет вылетает по дуге (на середине — крупнее, «в воздухе»), крутясь, и падает
        public static IEnumerator Toss(Transform item, Vector2 to, float spin, float duration = 0.45f)
        {
            Vector2 from = item.position;
            Vector3 scale = item.localScale;
            float angle = item.eulerAngles.z;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                item.position = Vector2.Lerp(from, to, 1f - (1f - k) * (1f - k));
                item.localScale = scale * (1f + 0.4f * Mathf.Sin(k * Mathf.PI));
                item.rotation = Quaternion.Euler(0f, 0f, angle + spin * k);
                yield return null;
            }
            item.position = to;
            item.localScale = scale;
        }

        // Стоп-кадр: мир замирает на миг (удар, попадание) и продолжает с той же скоростью
        public static IEnumerator HitStop(float duration)
        {
            float scale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = scale;
        }

        // Замедление на время (реальное), потом обратно к нормальной скорости
        public static IEnumerator SlowMo(float scale, float duration)
        {
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }

        private static void SetPosition(Transform actor, Rigidbody2D rb, Vector2 position)
        {
            if (rb != null) rb.position = position;
            actor.position = position;
        }

        // Шаг: плавный разгон и торможение, середина пути — почти с постоянной скоростью
        private static float EaseWalk(float k)
        {
            k = Mathf.Clamp01(k);
            float smooth = k * k * (3f - 2f * k);
            return Mathf.Lerp(k, smooth, 0.6f);
        }
    }
}
