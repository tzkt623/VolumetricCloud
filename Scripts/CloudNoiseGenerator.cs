using System.Collections;
using System.Collections.Generic;
using tezcat.Framework.Utility;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace tezcat.Framework.Exp
{
    public class CloudNoiseGenerator : MonoBehaviour
    {
        [System.Serializable]
        public class PerlinData : PerlinNoiseData
        {
            public PerlinData()
            {
                mMarkName = "Cloud";
            }
        }

        [System.Serializable]
        public class WorleyGroupData : WorleyNoiseGroupData3D
        {
            public WorleyGroupData()
            {
                mMarkName = "Cloud";
            }
        }

        public event System.Action<RenderTexture> onTextureCreated;
        public RenderTexture shapeTexture => mShapeRenderTexture;
        public RenderTexture perlinNoiseTexture => mShapePerlinNoiseTexture;

        public ComputeShader mComputeShader;
        public DetailNoise mDetailNoise;

        public bool mUpdate = false;
        public int mResolution = 64;
        public PerlinData mPerlinNoise;
        public WorleyGroupData mWorleyNoise;

        [Header("Viewer")]
        public Shader mViewerShader;
        public Renderer[] mRenderers;
        public Renderer mPerlinNosieRenderer;

        private int mCSKernel;
        private RenderTexture mShapeRenderTexture = null;
        private RenderTexture mShapePerlinNoiseTexture = null;

        const int mCSThreadZ = 2;

        private void Start()
        {
            this.intiData();
        }

        private void OnDestroy()
        {
            mShapeRenderTexture?.Release();
        }

        private void intiData()
        {
            PerlinNoiseData.initSharedData();

            mDetailNoise.onTextureCreated += onDetailNoiseTextureCreated;

            mCSKernel = mComputeShader.FindKernel("main3D");
            mComputeShader.SetInt("inResolution", mResolution);

            mPerlinNoise.init(3, mCSKernel, mComputeShader);
            mWorleyNoise.init(mResolution, mCSKernel, mComputeShader);

            mShapeRenderTexture = new RenderTexture(mResolution, mResolution, 0, GraphicsFormat.R16G16B16A16_SFloat)
            {
                name = "Shape",
                volumeDepth = mResolution,
                dimension = TextureDimension.Tex3D,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                enableRandomWrite = true,
                useMipMap = true,
                autoGenerateMips = true
            };
            mShapeRenderTexture.Create();
            mComputeShader.SetTexture(mCSKernel, "outShapeTex3D", mShapeRenderTexture);

            mShapePerlinNoiseTexture = new RenderTexture(mResolution, mResolution, 0, GraphicsFormat.R16_SFloat)
            {
                name = "Perlin",
                volumeDepth = mResolution,
                dimension = TextureDimension.Tex3D,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                enableRandomWrite = true,
            };
            mShapePerlinNoiseTexture.Create();
            mComputeShader.SetTexture(mCSKernel, "outShapePerlinNoiseTex3D", mShapePerlinNoiseTexture);
            onTextureCreated?.Invoke(mShapeRenderTexture);

            foreach (var item in mRenderers)
            {
                item.material.SetTexture("_ShapeTex3D", mShapeRenderTexture);

                if (mDetailNoise.renderTexture != null)
                {
                    item.material.SetTexture("_DetailTex3D", mDetailNoise.renderTexture);
                }
            }

            mPerlinNosieRenderer.material.SetTexture("_ShapeTex3D", mShapePerlinNoiseTexture);

            mComputeShader.Dispatch(mCSKernel, mResolution / 8, mResolution / 8, mResolution / mCSThreadZ);
        }

        private void onDetailNoiseTextureCreated(RenderTexture tex)
        {
            foreach (var item in mRenderers)
            {
                item.material.SetTexture("_DetailTex3D", tex);
            }
        }

        private void Update()
        {
            if (mUpdate)
            {
                mPerlinNoise.sendToGPU(mCSKernel, mComputeShader);
                mWorleyNoise.sendToGPU(mCSKernel, mComputeShader);

                mComputeShader.Dispatch(mCSKernel, mResolution / 8, mResolution / 8, mResolution / mCSThreadZ);
            }

            for (int i = 0; i < mRenderers.Length; i++)
            {
                mRenderers[i].material.SetInt("_NoiseType", i);
            }
        }
    }
}