using tezcat.Framework.Utility;
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
        public int mWeatherResolution = 512;
        public ComputeShader mWeatherComputeShader;

        public Texture2D mWeatherTexture2D;
        public Texture2D mHeightTypeTex2D;
        [Range(0.0f, 1.0f)]
        public float mCoverageRate = 1.0f;
        [Range(0.0f, 1.0f)]
        public float mAnvilRate = 0.0f;
        [Min(0.0001f)]
        public float mWeatherScale = 0.0f;

        [Space()]
        public bool mUpdateGenerator = false;
        [Range(0.0f, 1.0f)]
        public float mThreshold = 0.5f;
        public CoveragePerlinMap mCvgPerlinMap;
        public CoverageWorleyMap mCvgWorleyMap;
        public TypeIndexMap mTypeMap;
        public HeightTypeMap mHeightTypeMap;

        [Header("Viewer")]
        public Renderer mWeatherMapViewer;
        public Renderer mHeightTypeViewer;

        RenderTexture mRealTimeWeatherTex2D;

        int mKernel;

        public void init()
        {
            PerlinNoiseData.initSharedData();

            mRealTimeWeatherTex2D = new RenderTexture(mWeatherResolution, mWeatherResolution, 0, GraphicsFormat.R16G16B16A16_SFloat)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                enableRandomWrite = true,
            };
            mRealTimeWeatherTex2D.Create();

            mKernel = mWeatherComputeShader.FindKernel("main2D");
            mWeatherComputeShader.SetInt("inResolution", mWeatherResolution);
            mWeatherComputeShader.SetTexture(mKernel, "outWeatherTex2D", mRealTimeWeatherTex2D);
            mWeatherComputeShader.SetFloat("inThreshold", mThreshold);

            mCvgWorleyMap.init(mWeatherResolution, mKernel, mWeatherComputeShader);
            mCvgPerlinMap.init(2, mKernel, mWeatherComputeShader);
            mTypeMap.init(2, mKernel, mWeatherComputeShader);

            mWeatherComputeShader.Dispatch(mKernel, mWeatherResolution / 8, mWeatherResolution / 8, 1);

            mWeatherMapViewer.material.SetTexture("_WeatherTex2D", mRealTimeWeatherTex2D);

            mHeightTypeMap.calculate();
            mHeightTypeViewer.material.mainTexture = mHeightTypeMap.mTex;
        }

        public override void sendToGPU(Material material)
        {
            material.SetTexture("_WeatherTex2D", mRealTimeWeatherTex2D);
            material.SetTexture("_HeightTypeTex2D", mHeightTypeTex2D);
            material.SetFloat("_CoverageRate", mCoverageRate);
            material.SetFloat("_AnvilRate", mAnvilRate);
            material.SetFloat("_WeatherScale", mWeatherScale);

            if (mUpdateGenerator)
            {
                mWeatherComputeShader.SetFloat("inThreshold", mThreshold);
                mCvgWorleyMap.sendToGPU(mKernel, mWeatherComputeShader);
                mCvgPerlinMap.sendToGPU(mKernel, mWeatherComputeShader);
                mTypeMap.sendToGPU(mKernel, mWeatherComputeShader);

                mWeatherComputeShader.Dispatch(mKernel, mWeatherResolution / 8, mWeatherResolution / 8, 1);
            }
        }
    }
}
