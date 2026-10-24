using System.Collections;
using N89.Combat;
using N89.Enemies;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace N89.Hacking
{
    // «Короткое замыкание» на человеке: импланты коротит, он застывает в судорогах на пару секунд
    // (тряска, цифровые помехи, разряды), потом приходит в себя и сразу идёт к игроку. Не убивает.
    // Добавляется на врага в момент взлома (HackableDevice).
    public class Malfunction : MonoBehaviour
    {
        private const int ArcCount = 3;
        private const int ArcPoints = 7;

        private static readonly Color Cyan = new Color(0.3f, 1f, 1f);
        private static readonly Color Magenta = new Color(1f, 0.2f, 0.75f);

        public bool IsActive { get; private set; }

        public void Begin(float duration, SpriteRenderer body, Material glitchMaterial, GameObject sparksFx)
        {
            if (!IsActive)
            {
                StartCoroutine(Run(duration, body, glitchMaterial, sparksFx));
            }
        }

        private IEnumerator Run(float duration, SpriteRenderer body, Material glitchMaterial, GameObject sparksFx)
        {
            IsActive = true;
            Health health = GetComponent<Health>();
            EnemyAI ai = GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.enabled = false;
            }

            Transform bodyT = body.transform;
            Vector3 basePos = bodyT.localPosition;
            Quaternion baseRot = bodyT.localRotation;
            Vector3 baseScale = bodyT.localScale;
            Color baseColor = body.color;
            Material material = glitchMaterial != null ? glitchMaterial : body.sharedMaterial;

            // Хроматическая аберрация: красная и голубая копии спрайта, разъезжаются по горизонтали
            SpriteRenderer ghostRed = Ghost("GlitchRed", body, material, new Color(1f, 0.15f, 0.3f, 0.6f));
            SpriteRenderer ghostCyan = Ghost("GlitchCyan", body, material, new Color(0.2f, 1f, 1f, 0.6f));

            // Электрические дуги вокруг тела
            var arcs = new LineRenderer[ArcCount];
            for (int i = 0; i < ArcCount; i++)
            {
                arcs[i] = Arc(material, body.sortingOrder + 2);
            }

            Light2D light = GetComponentInChildren<Light2D>();
            Color lightColor = light != null ? light.color : Color.white;
            float lightIntensity = light != null ? light.intensity : 0f;

            float time = 0f;
            float nextArcs = 0f;
            float nextSparks = 0.5f;
            // Реальное время: замедление при сканировании (TAB) не растягивает стан
            while (time < duration && health != null && !health.IsDead)
            {
                time += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(time / duration);
                // Изредка — сильный «срыв» картинки
                bool spike = Random.value < 0.12f + 0.15f * k;

                float shake = (0.04f + 0.08f * k) * (spike ? 2.5f : 1f);
                bodyT.localPosition = basePos + (Vector3)(Random.insideUnitCircle * shake);
                bodyT.localRotation = baseRot * Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f) * (0.5f + k));
                bodyT.localScale = spike
                    ? Vector3.Scale(baseScale, new Vector3(Random.Range(1.15f, 1.45f), Random.Range(0.6f, 0.85f), 1f))
                    : baseScale;

                body.enabled = Random.value > 0.07f;
                body.color = spike ? Color.white : Random.value < 0.35f ? Cyan : baseColor;

                float split = spike ? Random.Range(0.15f, 0.3f) : Random.Range(0.03f, 0.1f);
                PlaceGhost(ghostRed, bodyT, -split);
                PlaceGhost(ghostCyan, bodyT, split);

                if (time >= nextArcs)
                {
                    nextArcs = time + 0.05f;
                    foreach (LineRenderer arc in arcs)
                    {
                        RebuildArc(arc, bodyT.position, k);
                    }
                }

                if (light != null)
                {
                    light.color = Random.value < 0.5f ? Cyan : Magenta;
                    light.intensity = Random.Range(0.3f, 1.8f + k);
                }

                if (sparksFx != null && time >= nextSparks)
                {
                    nextSparks = time + 0.6f;
                    Instantiate(sparksFx, bodyT.position + (Vector3)(Random.insideUnitCircle * 0.3f), Quaternion.identity);
                }

                yield return null;
            }

            // Убили пулей во время судорог — объект уже уничтожается, прибирать нечего
            if (health == null || health.IsDead)
            {
                yield break;
            }

            Destroy(ghostRed.gameObject);
            Destroy(ghostCyan.gameObject);
            foreach (LineRenderer arc in arcs)
            {
                Destroy(arc.gameObject);
            }

            bodyT.localPosition = basePos;
            bodyT.localRotation = baseRot;
            bodyT.localScale = baseScale;
            body.enabled = true;
            body.color = baseColor;
            if (light != null)
            {
                light.color = lightColor;
                light.intensity = lightIntensity;
            }

            IsActive = false;
            // Очнулся — знает, кто его взломал, и сразу идёт к игроку
            if (ai != null)
            {
                ai.enabled = true;
                ai.Provoke();
            }
        }

        private static SpriteRenderer Ghost(string name, SpriteRenderer body, Material material, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(body.transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = body.sprite;
            sr.sharedMaterial = material;
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder + 1;
            sr.color = color;
            return sr;
        }

        private static void PlaceGhost(SpriteRenderer ghost, Transform body, float offset)
        {
            // Сдвиг по экранной горизонтали, независимо от поворота тела
            ghost.transform.position = body.position + new Vector3(offset, Random.Range(-0.02f, 0.02f), 0f);
            ghost.enabled = Random.value > 0.1f;
        }

        private LineRenderer Arc(Material material, int order)
        {
            var go = new GameObject("GlitchArc");
            go.transform.SetParent(transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = ArcPoints;
            line.widthMultiplier = 0.035f;
            line.numCapVertices = 0;
            line.sortingOrder = order;
            line.startColor = Color.white;
            line.endColor = Cyan;
            return line;
        }

        // Ломаная молния от одной точки у тела к другой
        private static void RebuildArc(LineRenderer arc, Vector2 center, float intensity)
        {
            arc.enabled = Random.value < 0.55f + 0.4f * intensity;
            if (!arc.enabled)
            {
                return;
            }

            float a = Random.Range(0f, Mathf.PI * 2f);
            float b = a + Random.Range(1f, 2.5f);
            Vector2 from = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(0.25f, 0.5f);
            Vector2 to = center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * Random.Range(0.3f, 0.7f);
            for (int i = 0; i < ArcPoints; i++)
            {
                float t = i / (float)(ArcPoints - 1);
                Vector2 point = Vector2.Lerp(from, to, t);
                if (i > 0 && i < ArcPoints - 1)
                {
                    point += Random.insideUnitCircle * 0.12f;
                }

                arc.SetPosition(i, point);
            }
        }
    }
}
