using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace tezcat.Framework.Exp
{
    #region Map
    [System.Serializable]
    public class CoveragePerlinMap : PerlinNoiseData
    {
        public CoveragePerlinMap()
        {
            mMarkName = "CvgMap";
        }
    }

    [System.Serializable]
    public class CoverageWorleyMap : WorleyNoiseData2D
    {
        public CoverageWorleyMap()
        {
            mMarkName = "CvgMap";
        }
    }

    [System.Serializable]
    public class TypeIndexMap : PerlinNoiseData
    {
        public TypeIndexMap()
        {
            mMarkName = "TypeIndexMap";
        }
    }

    [System.Serializable]
    public class HeightTypeMap
    {
        public AnimationCurve mCurveX;
        public AnimationCurve mCurveY;

        public AnimationCurve mStratus;
        public AnimationCurve mStratusHeight;

        public AnimationCurve mStratocumulus;
        public AnimationCurve mCumulus;
        public AnimationCurve mCumulonimbus1;
        public AnimationCurve mCumulonimbus2;

        [HideInInspector]
        public Texture2D mTex;

        public void calculate()
        {
            mTex = new Texture2D(64, 64, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None);

            for (int y = 0; y < 64; y++)
            {
                float y_rate = y / 63.0f;
                var yv = mCurveY.Evaluate(y_rate);
                for (int x = 0; x < 64; x++)
                {
                    float x_rate = x / 63.0f;

                    var xv = mCurveX.Evaluate(x_rate);

                    var lp = mStratus.Evaluate(x_rate)
                        * mStratus.Evaluate(x_rate)
                        * mStratocumulus.Evaluate(x_rate)
                        * mCumulonimbus2.Evaluate(x_rate)
                        * y_rate;

                    var sample = xv * yv;

                    mTex.SetPixel(x, y, new Color(lp, lp, lp));
                }
            }

            mTex.Apply();
        }
    }
    #endregion

    [System.Serializable]
    public class WeatherMapData : BaseData
    {
        [Min(0.0001f)]
        public float mWeatherScale = 1.0f;
        [Range(0, 1)]
        public float mCoverage = 0.5f;
        [Header("Motion")]
        public Vector2 mWeatherOffset;
        public Vector2 mWeatherSpeed;

        private RenderTexture mTexture = null;
        public Material material { get; set; }

        public void setRenderTexture(RenderTexture texture)
        {
            mTexture = texture;
        }

        public override void initIDs()
        {
            this.generateIDs("_WeatherTex2D", "_WeatherData001", "_WeatherData002");
        }

        public override void sendToGPU(Material material)
        {
            mWeatherOffset += mWeatherSpeed * Time.deltaTime;

            material.SetTexture(mIDs[0], mTexture);
            material.SetVector(mIDs[1], new Vector4(mWeatherScale, mCoverage));
            material.SetVector(mIDs[2], new Vector4(mWeatherOffset.x, mWeatherOffset.y, mWeatherSpeed.x, mWeatherSpeed.y));
        }
    }

    [System.Serializable]
    public class WeatherMap : PerlinNoise2D
    {
        public float mThreshold = 0.5f;

        public override void generate()
        {
            mTexture?.Release();
            mTexture = new RenderTexture(mResolution, mResolution, 0, GraphicsFormat.R16G16B16A16_SFloat)
            {
                name = "PerlinNoise2D",
                dimension = TextureDimension.Tex2D,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false,
            };
            mTexture.Create();
            this.emitEvtTextureCreated();

            mShader.SetTexture(mKernel, "outPerlinNoiseTex2D", mTexture);
            mShader.SetBuffer(mKernel, "inPerlinHashArray", PerlineNoiseSharedData.getHashArrayBuffer());
            mShader.SetBuffer(mKernel, "inPerlinGradients2D", PerlineNoiseSharedData.getGradients2DBuffer());
        }

        public override void sendToGPU()
        {
            mPerlinData1[0] = mResolution;
            mPerlinData1[4] = mOctave;
            mPerlinData1[8] = mFrequency;
            mPerlinData1[12] = mLacunarity;
            mPerlinData2.x = mOffset.x;
            mPerlinData2.y = mOffset.y;
            mPerlinData2.z = mThreshold;
            mPerlinData2.w = mPersistence;

            mShader.SetInts(mIDs[0], mPerlinData1);
            mShader.SetVector(mIDs[1], mPerlinData2);
            mShader.Dispatch(mKernel, mResolution / 8, mResolution / 8, 1);
        }
    }
}
