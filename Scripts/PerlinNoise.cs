using System.Collections;
using UnityEngine;


namespace tezcat.Framework.Exp
{
    public abstract class PerlinNoise : MonoBehaviour
    {
        [Range(2, 256)]
        public int mResolution = 64;
        [Min(1)]
        public int mFrequency = 2;
        [Range(1, 10)]
        public int mOctave = 4;
        [Range(2, 4)]
        public int mLacunarity = 2;
        [Range(0.0f, 1.0f)]
        public float mPersistence = 0.5f;


        public Renderer mRenderer;
    }
}