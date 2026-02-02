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
        public RenderTexture shapeTexture
        {
            get
            {
                if (mUseNoise)
                {
                    return mShapeRenderTexture;
                }
                else
                {
                    return mTexture2To3.renderTexture;
                }
            }
        }

        public RenderTexture perlinNoiseTexture => mShapePerlinNoiseTexture;

        public ComputeShader mComputeShader;
        public DetailNoise mDetailNoise;

        public bool mUpdate = false;
        public bool mUseNoise = true;
        public int mResolution = 64;
        public PerlinData mPerlinNoise;
        public WorleyGroupData mWorleyNoise;

        public ConvertTex2DTo3D mTexture2To3;

        private bool mInitNoise = false;
        private bool mInitTexture = false;

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
//             const int MASK = 255;
//             Debug.Log(-348 % MASK);
//             Debug.Log(-348 & MASK);
// 
//             for (int i = 0; i < 200; i++)
//             {
//                 Debug.Log($"Exp[{i}] {Mathf.Exp(-i)}");
//             }

            if (mUseNoise)
            {
                this.intiCloudNoise();
            }
            else
            {
                this.initCloudTexture();
            }
        }

        public void useTexture()
        {
            if (!mInitTexture)
            {
                this.initCloudTexture();
            }
            else
            {
                onTextureCreated?.Invoke(mTexture2To3.renderTexture);
            }
        }

        public void useNoise()
        {
            if (!mInitNoise)
            {
                this.intiCloudNoise();
            }
            else
            {
                onTextureCreated?.Invoke(mShapeRenderTexture);
            }
        }

        private void initCloudTexture()
        {
            if (mInitTexture)
            {
                return;
            }

            mInitTexture = true;
            onTextureCreated?.Invoke(mTexture2To3.create());
        }

        private void OnDestroy()
        {
            mShapeRenderTexture?.Release();
        }

        private void intiCloudNoise()
        {
            if (mInitNoise)
            {
                return;
            }

            mInitNoise = true;

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
                autoGenerateMips = false
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
                enableRandomWrite = true
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
            mShapeRenderTexture.GenerateMips();
        }

        private void evtWorleyTex3DCreated(RenderTexture tex)
        {

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

            if (mUseNoise)
            {
                if (mUpdate)
                {
                    mPerlinNoise.sendToGPU(mCSKernel, mComputeShader);
                    mWorleyNoise.sendToGPU(mCSKernel, mComputeShader);

                    mComputeShader.Dispatch(mCSKernel, mResolution / 8, mResolution / 8, mResolution / mCSThreadZ);
                    mShapeRenderTexture.GenerateMips();
                }

                for (int i = 0; i < mRenderers.Length; i++)
                {
                    mRenderers[i].material.SetInt("_NoiseType", i);
                }
            }
        }
    }
}