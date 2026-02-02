using System;
using tezcat.Framework.Utility;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace tezcat.Framework.Exp
{
    public class PerlineNoiseSharedData
    {
        static ComputeBuffer mGradients2DBuffer;
        static ComputeBuffer mGradients3DBuffer;
        static ComputeBuffer mHashArrayBuffer;
        static bool mInit = false;

        public static void initSharedData()
        {
            if (mInit)
            {
                return;
            }

            mInit = true;
            mGradients2DBuffer = new ComputeBuffer(TezNoise.gradients2D.Length, sizeof(float) * 2);
            mGradients2DBuffer.SetData(TezNoise.gradients2D);
            mGradients3DBuffer = new ComputeBuffer(TezNoise.gradients3D.Length, sizeof(float) * 3);
            mGradients3DBuffer.SetData(TezNoise.gradients3D);
            mHashArrayBuffer = new ComputeBuffer(TezNoise.hashArray.Length, sizeof(int));
            mHashArrayBuffer.SetData(TezNoise.hashArray);
        }

        public static ComputeBuffer getGradients2DBuffer() { return mGradients2DBuffer; }
        public static ComputeBuffer getGradients3DBuffer() { return mGradients3DBuffer; }
        public static ComputeBuffer getHashArrayBuffer() { return mHashArrayBuffer; }
    }

    [System.Serializable]
    public class PerlinNoiseData
    {
        [Range(1, 10)]
        public int mOctave = 4;
        [Min(1.0f)]
        public int mFrequency = 1;
        [Range(2.0f, 4.0f)]
        public int mLacunarity = 2;
        [Range(0.0f, 1.0f)]
        public float mPersistence = 0.5f;
        public Vector3 mOffset;

        static ComputeBuffer mGradients2DBuffer;
        static ComputeBuffer mGradients3DBuffer;
        static ComputeBuffer mHashArrayBuffer;
        static bool mInit = false;

        protected string mMarkName;

        public static void initSharedData()
        {
            if (mInit)
            {
                return;
            }

            mInit = true;

            mGradients2DBuffer = new ComputeBuffer(TezNoise.gradients2D.Length, sizeof(float) * 2);
            mGradients2DBuffer.SetData(TezNoise.gradients2D);

            mGradients3DBuffer = new ComputeBuffer(TezNoise.gradients3D.Length, sizeof(float) * 3);
            mGradients3DBuffer.SetData(TezNoise.gradients3D);

            mHashArrayBuffer = new ComputeBuffer(TezNoise.hashArray.Length, sizeof(int));
            mHashArrayBuffer.SetData(TezNoise.hashArray);
        }

        public virtual void init(int d, int kernel, ComputeShader shader)
        {
            shader.SetBuffer(kernel, $"inPerlinHashArray", mHashArrayBuffer);
            shader.SetBuffer(kernel, $"inPerlinGradients2D", mGradients2DBuffer);
            shader.SetBuffer(kernel, $"inPerlinGradients3D", mGradients3DBuffer);
            this.sendToGPU(kernel, shader);
        }

        public virtual void sendToGPU(int kernel, ComputeShader shader)
        {
            shader.SetInt($"in{mMarkName}PLOctave", mOctave);
            shader.SetInt($"in{mMarkName}PLFrequency", mFrequency);
            shader.SetInt($"in{mMarkName}PLLacunarity", mLacunarity);
            shader.SetFloat($"in{mMarkName}PLPersistence", mPersistence);
            shader.SetVector($"in{mMarkName}PLOffset", mOffset);
        }
    }

    [System.Serializable]
    public abstract class PerlinNoiseBase : BaseData
    {
        [SerializeField]
        protected ComputeShader mShader;
        public int mResolution = 128;
        [Range(1, 10)]
        public int mOctave = 4;
        [Min(1.0f)]
        public int mFrequency = 1;
        [Range(2.0f, 4.0f)]
        public int mLacunarity = 2;
        [Range(0.0f, 1.0f)]
        public float mPersistence = 0.5f;

        protected int mKernel;
        protected int[] mPerlinData1 = new int[4 * 4];
        protected Vector4 mPerlinData2 = new Vector4();

        protected RenderTexture mTexture = null;
        public RenderTexture texture { get { return mTexture; } }
        public event Action<RenderTexture> evtTextureCreated;

        protected void emitEvtTextureCreated()
        {
            evtTextureCreated?.Invoke(mTexture);
        }

        public override void init()
        {
            base.init();
            mKernel = mShader.FindKernel("CSMain");
        }

        public abstract void generate();
    }

    [System.Serializable]
    public class PerlinNoise2D : PerlinNoiseBase
    {
        public Vector2 mOffset;

        public override void init()
        {
            base.init();
            this.generate();
        }

        public override void initIDs()
        {
            this.generateIDs("inPerlinData1", "inPerlinData2");
        }

        public override void generate()
        {
            mTexture?.Release();
            mTexture = new RenderTexture(mResolution, mResolution, 0, GraphicsFormat.R16_SFloat)
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
            mPerlinData2.z = 0;
            mPerlinData2.w = mPersistence;

            mShader.SetInts(mIDs[0], mPerlinData1);
            mShader.SetVector(mIDs[1], mPerlinData2);
            mShader.Dispatch(mKernel, mResolution / 8, mResolution / 8, 1);
        }
    }

    [System.Serializable]
    public class PerlinNoise3D : PerlinNoiseBase
    {
        public Vector3 mOffset;
        int mKernelDebug;
        ComputeBuffer mDebugBuffer;

        public override void init()
        {
            base.init();
            this.generate();
        }

        public override void initIDs()
        {
            this.generateIDs("inPerlinData1", "inPerlinData2");
        }

        public override void generate()
        {
            mTexture?.Release();
            mTexture = new RenderTexture(mResolution, mResolution, 0, GraphicsFormat.R16_SFloat)
            {
                name = "PerlinNoise3D",
                volumeDepth = mResolution,
                dimension = TextureDimension.Tex3D,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false,
            };
            mTexture.Create();
            this.emitEvtTextureCreated();

            mShader.SetTexture(mKernel, "outPerlinNoiseTex3D", mTexture);
            mShader.SetBuffer(mKernel, "inPerlinHashArray", PerlineNoiseSharedData.getHashArrayBuffer());
            mShader.SetBuffer(mKernel, "inPerlinGradients3D", PerlineNoiseSharedData.getGradients3DBuffer());
        }

        public override void sendToGPU()
        {
            mPerlinData1[0] = mResolution;
            mPerlinData1[4] = mOctave;
            mPerlinData1[8] = mFrequency;
            mPerlinData1[12] = mLacunarity;
            mPerlinData2.x = mOffset.x;
            mPerlinData2.y = mOffset.y;
            mPerlinData2.z = mOffset.z;
            mPerlinData2.w = mPersistence;

            mShader.SetInts(mIDs[0], mPerlinData1);
            mShader.SetVector(mIDs[1], mPerlinData2);
            mShader.Dispatch(mKernel, mResolution / 8, mResolution / 8, mResolution / 2);
        }
    }
}