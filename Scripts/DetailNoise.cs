using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace tezcat.Framework.Exp
{
    public class DetailNoise : MonoBehaviour
    {
        [System.Serializable]
        public class WorleyData : WorleyNoiseGroupData3D
        {
            public WorleyData()
            {
                mMarkName = "Detail";
            }
        }

        public ComputeShader mComputeShader;
        public Shader mViewerShader;
        public int mResolution = 32;
        public WorleyData mWorleyData;
        public Renderer[] mViewers;

        public event System.Action<RenderTexture> onTextureCreated;
        public RenderTexture renderTexture => mDetailRenderTexture;
        private RenderTexture mDetailRenderTexture;
        private int mCSKernel;

        private Material mMaterial = null;

        private void Start()
        {
            mDetailRenderTexture = new RenderTexture(mResolution, mResolution, 0, GraphicsFormat.R16G16B16A16_SFloat)
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

            mComputeShader.SetInt("inResolution", mResolution);
            mComputeShader.SetTexture(mCSKernel, "outDetailTex3D", mDetailRenderTexture);
            mWorleyData.init(mResolution, mCSKernel, mComputeShader);


            if (mMaterial == null)
            {
                mMaterial = new Material(mViewerShader);
            }
            mMaterial.SetTexture("_DetailTex3D", mDetailRenderTexture);
            for (int i = 0; i < mViewers.Length; i++)
            {
                mViewers[i].material = mMaterial;
                mViewers[i].material.SetInt("_DetailLevel", i);
            }

            mComputeShader.Dispatch(mCSKernel, mResolution / 8, mResolution / 8, mResolution / 2);
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            mWorleyData.close();
        }
    }
}