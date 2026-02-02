using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class CloudNoise
    {
        private PerlinNoise3D mPerlinNoiseData;
        private WorleyNoise2 mWorleyNoiseData;
        private WorleyNoise2 mDetailNoiseData;
        private WeatherMap mWeatherNoiseData;

        public ComputeShader mCombinePerlinWorley;
        private RenderTexture mPerlinWorleyTex3D;
        public RenderTexture mPerlinWorleyTex3D1;

        public RenderTexture perlinWorleyTex3D => mPerlinWorleyTex3D;
        public RenderTexture detailTex3D => mDetailNoiseData.texture;
        public RenderTexture weathMapTex2D => mWeatherNoiseData.texture;

        public event Action<RenderTexture> evtTextureCreated;

        private int mKernelCombine;

        public void init(PerlinNoise3D perlinNoiseData, WorleyNoise2 worleyNoiseData, WorleyNoise2 detailNoiseData, WeatherMap weatherNoiseData)
        {
            mPerlinNoiseData = perlinNoiseData;
            mWorleyNoiseData = worleyNoiseData;
            mDetailNoiseData = detailNoiseData;
            mWeatherNoiseData = weatherNoiseData;
            this.generate();
        }

        public void generate()
        {
            mPerlinWorleyTex3D?.Release();
            mPerlinWorleyTex3D = new RenderTexture(mPerlinNoiseData.mResolution, mPerlinNoiseData.mResolution, 0, GraphicsFormat.R32G32B32A32_SFloat)
            {
                volumeDepth = mPerlinNoiseData.mResolution,
                dimension = TextureDimension.Tex3D,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false
            };
            mPerlinWorleyTex3D.Create();
            evtTextureCreated?.Invoke(mPerlinWorleyTex3D);

            mKernelCombine = mCombinePerlinWorley.FindKernel("CSMain");
            mCombinePerlinWorley.SetTexture(mKernelCombine, "inPerlinNoiseTex3D", mPerlinNoiseData.texture);
            mCombinePerlinWorley.SetTexture(mKernelCombine, "inWorleyNoiseTex3D", mWorleyNoiseData.texture);
            mCombinePerlinWorley.SetTexture(mKernelCombine, "outPerlinWorleyNoiseTex3D", mPerlinWorleyTex3D);

            mPerlinWorleyTex3D1 = mPerlinWorleyTex3D;
        }

        public void makePerlin()
        {
            mPerlinNoiseData.sendToGPU();
        }

        public void makeWorley()
        {
            mWorleyNoiseData.sendToGPU();
        }

        public void makeDetail()
        {
            mDetailNoiseData.sendToGPU();
        }

        public void makeCloudNoise()
        {
            this.makePerlin();
            this.makeWorley();
            this.makeDetail();
            mCombinePerlinWorley.Dispatch(mKernelCombine, mPerlinNoiseData.mResolution / 8, mPerlinNoiseData.mResolution / 8, mPerlinNoiseData.mResolution / 2);
        }
    }
}