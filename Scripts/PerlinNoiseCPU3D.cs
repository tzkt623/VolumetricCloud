using tezcat.Framework.Utility;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    public class PerlinNoiseCPU3D : PerlinNoise
    {
        Texture3D mTex3D;

        public bool mUpdateTex3D = false;
        public bool mUpdatePerlin3D = false;

        public Vector3 mOffset3D = Vector3.zero;

        void Start()
        {

            mTex3D = new Texture3D(mResolution, mResolution, mResolution, TextureFormat.RGBA32, false)
            {
                name = "PerlinNoise3D",
                wrapMode = TextureWrapMode.Repeat,
            };

            mRenderer.material.SetTexture("_ShapeTex3D", mTex3D);
            this.updateFractalNoise();
        }

        private void updateColor3D()
        {
            Color[] colors = new Color[mResolution * mResolution * mResolution];

            for (int z = 0; z < mResolution; z++)
            {
                int offset_z = z * mResolution * mResolution;
                float pos_z = z / (float)(mResolution - 1) - 0.5f;
                for (int y = 0; y < mResolution; y++)
                {
                    int offset_y = y * mResolution;
                    float pos_y = y / (float)(mResolution - 1) - 0.5f;
                    for (int x = 0; x < mResolution; x++)
                    {
                        float pos_x = x / (float)(mResolution - 1) - 0.5f;

                        float perlin = (TezNoise.perlin3DRepeat(new Vector3(pos_x, pos_y, pos_z) + mOffset3D, mFrequency) + 1.0f) * 0.5f;

                        colors[x + offset_y + offset_z] = new Color(perlin, perlin, perlin, 1.0f);
                    }
                }
            }

            mTex3D.SetPixels(colors);
            mTex3D.Apply();
        }

        private void updateFractalNoise()
        {
            Color[] colors = new Color[mResolution * mResolution * mResolution];

            for (int z = 0; z < mResolution; z++)
            {
                int offset_z = z * mResolution * mResolution;
                float pos_z = z / (float)(mResolution - 1) - 0.5f;
                for (int y = 0; y < mResolution; y++)
                {
                    int offset_y = y * mResolution;
                    float pos_y = y / (float)(mResolution - 1) - 0.5f;
                    for (int x = 0; x < mResolution; x++)
                    {
                        float pos_x = x / (float)(mResolution - 1) - 0.5f;

                        float noise = 0;
                        float amplitude = 1.0f;
                        float sum_amplitude = 0.0f;
                        int frequence = mFrequency;

                        for (int i = 0; i < mOctave; i++)
                        {
                            noise += amplitude * (TezNoise.perlin3DRepeat(new Vector3(pos_x, pos_y, pos_z) + mOffset3D, frequence) + 1.0f) * 0.5f;

                            sum_amplitude += amplitude;
                            frequence *= mLacunarity;
                            amplitude *= mPersistence;
                        }


                        noise /= sum_amplitude;

                        colors[x + offset_y + offset_z] = new Color(noise, noise, noise, 1.0f);
                    }
                }
            }

            mTex3D.SetPixels(colors);
            mTex3D.Apply();
        }

        // Update is called once per frame
        void Update()
        {
            if (mUpdateTex3D)
            {
                mUpdateTex3D = false;
                mTex3D = new Texture3D(mResolution, mResolution, mResolution, TextureFormat.RGBA32, false)
                {
                    name = "PerlinNoise3D",
                    wrapMode = TextureWrapMode.Repeat,
                };
                mRenderer.material.SetTexture("_ShapeTex3D", mTex3D);
            }

            if (mUpdatePerlin3D)
            {
                mUpdatePerlin3D = false;
                this.updateFractalNoise();
            }
        }

        private void OnValidate()
        {
        }
    }
}