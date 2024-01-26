using UnityEngine;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class BlurData : BaseData
    {
        Material mBlurMaterial = null;
        RenderTexture mBlurRenderTexture;

        public bool mEnableBilateralBlur;
        public Shader mBlurShader;
        [Tooltip("x:SpatialWeight y:TonalWeight z:BlurRadius")]
        public Vector3 mBlurParams;

        public override void sendToGPU(Material material, RenderTexture source, RenderTexture destination)
        {
            if (mBlurMaterial == null)
            {
                mBlurMaterial = new Material(mBlurShader);
                mBlurRenderTexture = new RenderTexture(source);
                mBlurRenderTexture.Create();
            }
            Graphics.Blit(source, mBlurRenderTexture, material);
            mBlurMaterial.SetTexture("_MainTex", mBlurRenderTexture);
            mBlurMaterial.SetVector("_BlurParams", mBlurParams);
            Graphics.Blit(mBlurRenderTexture, destination, mBlurMaterial);
        }

        public override void close()
        {
            mBlurRenderTexture?.Release();
        }
    }
}
