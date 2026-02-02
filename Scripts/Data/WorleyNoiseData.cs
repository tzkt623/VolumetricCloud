using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace tezcat.Framework.Exp
{
    public abstract class WorleyNoiseData
    {
        public bool mFilp;
        [Min(1.0f)]
        public float mFrequency;

        public Vector3 mOffset;

        protected int mGridLength;
        protected string mMarkName;

        public virtual void close() { }
    }

    public abstract class WorleyNoiseSingleData : WorleyNoiseData
    {
        [Min(1)]
        public int mGridCount;
    }

    [System.Serializable]
    public class WorleyNoiseData2D : WorleyNoiseSingleData
    {
        private Vector2Int[] mMarkPointArray2D;
        private ComputeBuffer mComputeBuffer;

        public void init(int resolution, int kernel, ComputeShader shader)
        {
            mGridLength = resolution / mGridCount;

            mMarkPointArray2D = new Vector2Int[mGridCount * mGridCount];
            for (int y = 0; y < mGridCount; y++)
            {
                for (int x = 0; x < mGridCount; x++)
                {
                    int begin_x = x * mGridLength;
                    int begin_y = y * mGridLength;

                    var pos_x = UnityEngine.Random.Range(begin_x, begin_x + mGridLength);
                    var pos_y = UnityEngine.Random.Range(begin_y, begin_y + mGridLength);

                    mMarkPointArray2D[x + y * mGridCount] = new Vector2Int(pos_x, pos_y);
                }
            }

            mComputeBuffer = new ComputeBuffer(mGridCount * mGridCount, sizeof(int) * 2);
            mComputeBuffer.SetData(mMarkPointArray2D);
            shader.SetBuffer(kernel, $"in{mMarkName}WLMarkPoints", mComputeBuffer);

            shader.SetInt($"in{mMarkName}WLGridCount", mGridCount);
            shader.SetBool($"in{mMarkName}WLFlip", mFilp);
            shader.SetFloat($"in{mMarkName}WLFrequency", mFrequency);
        }

        public virtual void sendToGPU(int kernel, ComputeShader shader)
        {
            shader.SetBool($"in{mMarkName}WLFlip", mFilp);
            shader.SetFloat($"in{mMarkName}WLFrequency", mFrequency);
        }

        public override void close()
        {
            mComputeBuffer.Release();
        }
    }

    [System.Serializable]
    public class WorleyNoiseData3D : WorleyNoiseSingleData
    {
        private ComputeBuffer mComputeBuffer;

        public void init(int resolution, int kernel, ComputeShader shader)
        {
            mGridLength = resolution / mGridCount;
            var array = new Vector3Int[mGridCount * mGridCount * mGridCount];

            for (int z = 0; z < mGridCount; z++)
            {
                int begin_z = z * mGridLength;
                int z_offset = z * mGridCount * mGridCount;

                for (int y = 0; y < mGridCount; y++)
                {
                    int begin_y = y * mGridLength;
                    int y_offset = y * mGridCount;

                    for (int x = 0; x < mGridCount; x++)
                    {
                        int begin_x = x * mGridLength;

                        var pos_x = UnityEngine.Random.Range(begin_x, begin_x + mGridLength);
                        var pos_y = UnityEngine.Random.Range(begin_y, begin_y + mGridLength);
                        var pos_z = UnityEngine.Random.Range(begin_z, begin_z + mGridLength);

                        array[x + y_offset + z_offset] = new Vector3Int(pos_x, pos_y, pos_z);
                    }
                }
            }

            mComputeBuffer = new ComputeBuffer(mGridCount * mGridCount * mGridCount, sizeof(int) * 3);
            mComputeBuffer.SetData(array);
            shader.SetBuffer(kernel, $"in{mMarkName}WLMarkPoints", mComputeBuffer);

            shader.SetInt($"in{mMarkName}WLGridCount", mGridCount);

            shader.SetBool($"in{mMarkName}WLFlip", mFilp);
            shader.SetFloat($"in{mMarkName}WLFrequency", mFrequency);
        }

        public virtual void sendToGPU(int kernel, ComputeShader shader)
        {
            shader.SetBool($"in{mMarkName}WLFlip", mFilp);
            shader.SetFloat($"in{mMarkName}WLFrequency", mFrequency);
        }

        public override void close()
        {
            mComputeBuffer.Release();
        }
    }

    public abstract class WorleyNoiseGroupData : WorleyNoiseData
    {
        public int[] mGridCounts;
    }

    [System.Serializable]
    public class WorleyNoiseGroupData3D : WorleyNoiseGroupData
    {
        private ComputeBuffer[] mComputeBuffer;
        public ComputeBuffer mGridCountBuffer;

        public void init(int resolution, int kernel, ComputeShader shader)
        {
            mComputeBuffer = new ComputeBuffer[mGridCounts.Length];

            for (int i = 0; i < mGridCounts.Length; i++)
            {
                this.calculateSamplePoint3D(i, resolution, mGridCounts[i], kernel, shader);
            }

            shader.SetBool($"in{mMarkName}WLFlip", mFilp);
            shader.SetFloat($"in{mMarkName}WLFrequency", mFrequency);

            //Count Buffer
            mGridCountBuffer = new ComputeBuffer(mGridCounts.Length, sizeof(int));
            mGridCountBuffer.SetData(mGridCounts);

            shader.SetBuffer(kernel, $"in{mMarkName}WLGroupGridCount", mGridCountBuffer);
        }

        public virtual void sendToGPU(int kernel, ComputeShader shader)
        {
            shader.SetBool($"in{mMarkName}WLFlip", mFilp);
            shader.SetFloat($"in{mMarkName}WLFrequency", mFrequency);
        }

        protected void calculateSamplePoint3D(int index, int resolution, int gridCount, int kernel, ComputeShader shader)
        {
            var grid_length = resolution / gridCount;
            var array = new Vector3Int[gridCount * gridCount * gridCount];

            for (int z = 0; z < gridCount; z++)
            {
                int begin_z = z * grid_length;
                int z_offset = z * gridCount * gridCount;

                for (int y = 0; y < gridCount; y++)
                {
                    int begin_y = y * grid_length;
                    int y_offset = y * gridCount;

                    for (int x = 0; x < gridCount; x++)
                    {
                        int begin_x = x * grid_length;

                        var pos_x = UnityEngine.Random.Range(begin_x, begin_x + grid_length);
                        var pos_y = UnityEngine.Random.Range(begin_y, begin_y + grid_length);
                        var pos_z = UnityEngine.Random.Range(begin_z, begin_z + grid_length);

                        array[x + y_offset + z_offset] = new Vector3Int(pos_x, pos_y, pos_z);
                    }
                }
            }

            var buffer = new ComputeBuffer(gridCount * gridCount * gridCount, sizeof(int) * 3);
            buffer.SetData(array);
            shader.SetBuffer(kernel, $"in{mMarkName}WLMarkPoints3D{index}", buffer);

            mComputeBuffer[index] = buffer;
        }

        public override void close()
        {
            foreach (var item in mComputeBuffer)
            {
                item?.Release();
            }

            mGridCountBuffer?.Release();
        }
    }

    public static class ShaderExtension
    {
        public static int[] generateIDs(params string[] names)
        {
            int[] ids = new int[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                ids[i] = Shader.PropertyToID(names[i]);
            }

            return ids;
        }
    }

    [System.Serializable]
    public class WorleyNoise2 : BaseData
    {
        [SerializeField]
        private ComputeShader mShader;
        [SerializeField]
        private bool mFilp = false;
        [SerializeField]
        private int mResolution = 128;
        [SerializeField]
        private int mCellCount = 4;
        [SerializeField]
        private Vector3 mOffset;
        [SerializeField]
        private Vector3 mPowFactor;
        [SerializeField]
        private float mAmplitudeFactor = 0.5f;

        RenderTexture mTexture = null;
        int mKernel = -1;

        public RenderTexture texture { get { return mTexture; } }
        public event Action<RenderTexture> evtTextureCreated;

        public override void init()
        {
            base.init();
            mKernel = mShader.FindKernel("CSMain");
            this.generate();
        }

        public override void initIDs()
        {
            this.generateIDs("outWorleyNoiseTex3D",
                "inResolution",
                "inFlip",
                "inCellCount",
                "inOffset1",
                "inPowFactor");
        }

        public void generate()
        {
            mTexture?.Release();
            mTexture = new RenderTexture(mResolution, mResolution, 0, GraphicsFormat.R16G16B16A16_SFloat)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = mResolution,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false
            };
            mTexture.Create();
            evtTextureCreated?.Invoke(mTexture);

            mShader.SetTexture(mKernel, mIDs[0], mTexture);
            mShader.SetInt(mIDs[1], mResolution);
        }

        public override void sendToGPU()
        {
            mShader.SetBool(mIDs[2], mFilp);
            mShader.SetFloat(mIDs[3], mCellCount);
            mShader.SetVector(mIDs[4], new Vector4(mOffset.x, mOffset.y, mOffset.z, mAmplitudeFactor));
            mShader.SetVector(mIDs[5], new Vector4((int)mPowFactor.x, (int)mPowFactor.y, (int)mPowFactor.z, 0.0f));
            mShader.Dispatch(mKernel, mResolution / 8, mResolution / 8, mResolution / 2);
        }
    }
}