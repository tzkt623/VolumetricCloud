using System.Collections;
using tezcat.Framework.Utility;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    public class PerlinNoiseCPU2D : PerlinNoise
    {
        Texture2D mTex2D;

        public bool mUpdate = false;

        public enum Type
        {
            Normal,
            Fractal
        }

        public Type mType = Type.Normal;

        // Use this for initialization
        void Start()
        {
            //Debug.Log(5245 & 8);

            mTex2D = new Texture2D(mResolution, mResolution)
            {
                name = "PerlinNoise",
                wrapMode = TextureWrapMode.Repeat,
            };

            mRenderer.material.mainTexture = mTex2D;

            switch (mType)
            {
                case Type.Normal:
                    this.updateColor2D();
                    break;
                case Type.Fractal:
                    this.updateFractalNoise();
                    break;
                default:
                    break;
            }
        }

        private void updateColor2D()
        {
            for (int h = 0; h < mResolution; h++)
            {
                for (int w = 0; w < mResolution; w++)
                {
                    float pos_x = w / (float)(mResolution - 1);
                    float pos_y = h / (float)(mResolution - 1);
                    float color = (TezNoise.perlin2D(new Vector2(pos_x, pos_y), mFrequency) + 1.0f) * 0.5f;

                    //Debug.Log(color);
                    mTex2D.SetPixel(w, h, new Color(color, color, color, 1.0f));
                }
            }

            mTex2D.Apply();
        }

        private void updateFractalNoise()
        {

            for (int h = 0; h < mResolution; h++)
            {
                for (int w = 0; w < mResolution; w++)
                {
                    float pos_x = w / (float)(mResolution - 1);
                    float pos_y = h / (float)(mResolution - 1);

                    float noise = 0;
                    float amplitude = 1.0f;
                    float sum_amplitude = 0;
                    int frequency = mFrequency;

                    for (int i = 0; i < mOctave; i++)
                    {
                        float temp = (TezNoise.perlin2DRepeat(new Vector2(pos_x, pos_y), frequency) + 1.0f) * 0.5f;
                        if(temp > 1.0f)
                        {
                            throw new System.Exception();
                        }
                        noise += amplitude * temp;

                        sum_amplitude += amplitude;
                        frequency *= mLacunarity;
                        amplitude *= mPersistence;
                    }

                    noise /= sum_amplitude;

                    //Debug.Log(color);
                    mTex2D.SetPixel(w, h, new Color(noise, noise, noise, 1.0f));
                }
            }

            mTex2D.Apply();
        }

        // Update is called once per frame
        void Update()
        {
            if(mUpdate)
            {
                mUpdate = false;
                switch (mType)
                {
                    case Type.Normal:
                        this.updateColor2D();
                        break;
                    case Type.Fractal:
                        this.updateFractalNoise();
                        break;
                    default:
                        break;
                }

            }
        }
    }
}