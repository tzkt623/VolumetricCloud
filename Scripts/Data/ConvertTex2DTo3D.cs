using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class ConvertTex2DTo3D
    {
        public ComputeShader mShader;
        public Texture2D mTexture;
        public Vector2Int mCellCount;

        public Shader mViewerShader;
        public Renderer mViewer;

        RenderTexture mRenderTexture = null;
        public RenderTexture renderTexture => mRenderTexture;

        public RenderTexture create()
        {
            mRenderTexture?.Release();

            int width = mTexture.width / mCellCount.x;
            int height = mTexture.height / mCellCount.y;
            int depth = mCellCount.x * mCellCount.y;

            mRenderTexture = new RenderTexture(width, height, 0, GraphicsFormat.R16_UNorm)
            {
                dimension = TextureDimension.Tex3D,
                volumeDepth = depth,
                enableRandomWrite = true
            };
            mRenderTexture.Create();

            int kernel = mShader.FindKernel("main");

            mShader.SetInt("inCellCountX", mCellCount.x);
            mShader.SetInt("inCellCountY", mCellCount.y);
            mShader.SetInt("inTexWidth", mTexture.width);
            mShader.SetInt("inTexHeight", mTexture.height);

            mShader.SetTexture(kernel, "inTex2D", mTexture);
            mShader.SetTexture(kernel, "outTex3D", mRenderTexture);
            mShader.Dispatch(kernel, mTexture.width / 1, mTexture.height / 1, 1);

            mViewer.material = new Material(mViewerShader);
            mViewer.material.SetTexture("_Tex3D", mRenderTexture);

            return mRenderTexture;
        }
    }
}