using UnityEngine;

namespace N89.Core
{
    // Простейший чекпоинт между перезагрузками сцены: после смерти сцена грузится заново
    // и сценарий проверяет, с какой точки продолжать.
    public static class Checkpoint
    {
        public static string Current { get; private set; }

        public static void Set(string id) => Current = id;

        public static bool Is(string id) => Current == id;

        public static void Clear() => Current = null;

        // В проекте выключена перезагрузка домена при входе в Play Mode — сбрасываем вручную
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Current = null;
    }
}
