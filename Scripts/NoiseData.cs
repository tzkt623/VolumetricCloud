using UnityEngine;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class WorleyNoiseData
    {
        public bool filp;
        [Min(32)]
        public int resolution;
        [Min(1)]
        public int gridCount;
        [Min(1)]
        public int gridLength;
        [Min(1.0f)]
        public float frequency;
        public Vector3 offset;
    }

    [System.Serializable]
    public class PerlinNoiseData
    {
        [Range(1, 10)]
        public int octave = 4;
        [Min(1.0f)]
        public float frequency = 1.0f;
        [Range(2.0f, 4.0f)]
        public float lacunarity = 2.0f;
        [Range(0.0f, 1.0f)]
        public float persistence = 0.5f;
        public Vector3 offset;
    }
}