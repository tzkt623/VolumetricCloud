using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace tezcat.Framework.Exp
{
    public class DetailNoise : MonoBehaviour
    {
        public ComputeShader mComputeShader;
        public Shader mViewerShader;
        public int mResolution = 32;
        public int mGridCount = 4;
        [Min(1.0f)]
        public float mFrequency = 1.0f;
        public Vector3 mOffset;
        public bool mFlip = false;
        int mGridLength = 4;


        public event System.Action<RenderTexture> onTextureCreated;
        public RenderTexture renderTexture => mDetailRenderTexture;

        private Vector3Int[][] mMarkPointArray3D32;
        private ComputeBuffer[] mBuffers;
        private RenderTexture mDetailRenderTexture;
        private int mCSKernel;

        Material mMaterial = null;

        public Renderer[] mViewers;

        void initSamplePoint(int index, int gridCount)
        {
            var array = new Vector3Int[gridCount * gridCount * gridCount];

            int gridLength = mResolution / gridCount;

            for (int z = 0; z < gridCount; z++)
            {
                int begin_z = z * gridLength;
                int z_offset = z * gridCount * gridCount;

                for (int y = 0; y < gridCount; y++)
                {
                    int begin_y = y * gridLength;
                    int y_offset = y * gridCount;

                    for (int x = 0; x < gridCount; x++)
                    {
                        int begin_x = x * gridLength;

                        var pos_x = Random.Range(begin_x, begin_x + gridLength);
                        var pos_y = Random.Range(begin_y, begin_y + gridLength);
                        var pos_z = Random.Range(begin_z, begin_z + gridLength);

                        array[x + y_offset + z_offset] = new Vector3Int(pos_x, pos_y, pos_z);
                    }
                }
            }

            mMarkPointArray3D32[index] = array;
        }

        ComputeBuffer createBuffer(int kernel, string name, int gridCount, Vector3Int[] array)
        {
            var buffer = new ComputeBuffer(gridCount * gridCount * gridCount, sizeof(int) * 3);
            buffer.SetData(array);
            mComputeShader.SetBuffer(kernel, name, buffer);

            return buffer;
        }

        void Start()
        {
            mGridLength = mResolution / mGridCount;

            if (mMaterial == null)
            {
                mMaterial = new Material(mViewerShader);
            }

            mBuffers = new ComputeBuffer[3];
            mMarkPointArray3D32 = new Vector3Int[3][];

            mDetailRenderTexture = new RenderTexture(mResolution, mResolution, 0, GraphicsFormat.R16G16B16A16_UNorm)
            {
                name = "Detail",
                volumeDepth = mResolution,
                dimension = TextureDimension.Tex3D,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                enableRandomWrite = true,
            };
            mDetailRenderTexture.Create();
            onTextureCreated?.Invoke(mDetailRenderTexture);


            mCSKernel = mComputeShader.FindKernel("main3D");
            mComputeShader.SetTexture(mCSKernel, "outDetailTex3D", mDetailRenderTexture);

            mComputeShader.SetInt("inGridLength", mGridLength);
            mComputeShader.SetInt("inGridCount", mGridCount);
            mComputeShader.SetInt("inResolution", mResolution);
            mComputeShader.SetFloat("inGridRate", 1.0f / mGridCount);
            mComputeShader.SetVector("inOffset", mOffset);
            mComputeShader.SetBool("inFlip", mFlip);

            for (int i = 0; i < 3; i++)
            {
                int count = (int)Mathf.Pow(2, i + 1);
                this.initSamplePoint(i, count);
                mBuffers[i] = this.createBuffer(mCSKernel, $"inSamplerPoint{i}", count, mMarkPointArray3D32[i]);
            }

            for (int i = 0; i < mViewers.Length; i++)
            {
                mViewers[i].material = new Material(mViewerShader);
                mViewers[i].material.SetInt("_DetailLevel", i);
                mViewers[i].material.SetTexture("_DetailTex3D", mDetailRenderTexture);
            }

            mComputeShader.Dispatch(mCSKernel, mResolution / 8, mResolution / 8, mResolution / 2);
        }

        private void OnDestroy()
        {
            foreach (var item in mBuffers)
            {
                item?.Release();
            }
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}