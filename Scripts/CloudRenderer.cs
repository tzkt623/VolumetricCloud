using System;
using System.Collections.Generic;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    public class CloudRenderer
        : MonoBehaviour
        , IPostRenderer
    {
        [HideInInspector, SerializeField]
        int mCurrnetIndex = -1;
        [HideInInspector, SerializeField]
        int mSetIndex = -1;
        [HideInInspector]
        public List<Shader> mShaderList = new List<Shader>();

        public bool mRefreshShaderIDs = false;

        public NewCloudNoise mNewCloudNoise;

        [Header("Data")]
        public AreaData mAreaData;
        public ShapeData mShapeData;
        public DetailData mDetailData;
        public WeatherMapData mWeatherMapData;
        public LightingData mLightingData;
        public FilterData mFilterData;
        public BlurData mBlurData;
        public ExtensionData mExtensionData;
        CloudRenderData mCloudData = new CloudRenderData();

        [Header("Debug")]
        public DebugUVData mDebugUV;

        RenderTexture mShapeTexture = null;
        RenderTexture mDetailTexture = null;
        Material mMaterial = null;
        int mFrameCount;

        List<Action> mDelayInitList = new List<Action>();

        void Start()
        {
            mCloudData.init();
            mAreaData.init();
            mShapeData.init();
            mDetailData.init();
            mLightingData.init();
            mWeatherMapData.init();
            mFilterData.init();
            mExtensionData.init();

            mNewCloudNoise.mDetailNoise.evtTextureCreated += mCloudData.setDetailNoiseTex3D;
            mNewCloudNoise.mCloudNoise.evtTextureCreated += mCloudData.setPerlinWorleyNoiseTex3D;
            mNewCloudNoise.mWeatherNoise.evtTextureCreated += mWeatherMapData.setRenderTexture;
            mNewCloudNoise.initGPU();

            mDebugUV.init();
        }

        private void evtWeathTextureCreated(RenderTexture texture)
        {
            mDelayInitList.Add(() =>
            {
                mWeatherMapData.setRenderTexture(texture);
            });
        }

        private void evtDetailTextureCreated(RenderTexture obj)
        {
            mDetailTexture = obj;
            mDelayInitList.Add(() =>
            {
                mMaterial.SetTexture("_DetailNoiseTex3D", mDetailTexture);
            });
        }

        private void evtCloudTextureCreated(RenderTexture obj)
        {
            mShapeTexture = obj;
            mDelayInitList.Add(() =>
            {
                mMaterial.SetTexture("_PerlinWorleyNoiseTex3D", mShapeTexture);
            });
        }

        public void setShader(int i)
        {
            mSetIndex = i;
            if (mSetIndex != mCurrnetIndex)
            {
                mCurrnetIndex = mSetIndex;
                if (mCurrnetIndex >= 0 && mMaterial != null)
                {
                    mMaterial.shader = mShaderList[mCurrnetIndex];
                }
            }
        }

        public bool shaderUsing(int i)
        {
            return mCurrnetIndex == i;
        }

        public void rendering(RenderTexture source, RenderTexture destination)
        {
            if (mMaterial == null)
            {
                if (mCurrnetIndex < 0)
                {
                    mCurrnetIndex = mShaderList.Count - 1;
                }

                if (mCurrnetIndex < 0)
                {
                    Graphics.Blit(source, destination);
                    return;
                }

                mMaterial = new Material(mShaderList[mCurrnetIndex]);
                mWeatherMapData.material = mMaterial;
            }

            if (mDelayInitList.Count > 0)
            {
                foreach (var func in mDelayInitList)
                {
                    func();
                }
                mDelayInitList.Clear();
            }

            // Camera.main.projectionMatrix
            mMaterial.SetMatrix("_Matrix_Inv_VP", Camera.main.previousViewProjectionMatrix.inverse);

            mAreaData.sendToGPU(mMaterial);
            mShapeData.sendToGPU(mAreaData, mMaterial);
            mDetailData.sendToGPU(mMaterial);
            mWeatherMapData.sendToGPU(mMaterial);
            mLightingData.sendToGPU(mMaterial);
            mFilterData.sendToGPU(mMaterial);
            mExtensionData.sendToGPU(mMaterial);
            mCloudData.sendToGPU(mMaterial);

            mDebugUV.sendToGPU(mMaterial);
            if (mBlurData.mEnableBilateralBlur)
            {
                mBlurData.sendToGPU(mMaterial, source, destination);
            }
            else
            {
                Graphics.Blit(source, destination, mMaterial);
            }
        }

        private void OnDestroy()
        {
            mBlurData?.close();
            if(mShapeTexture != null)
            {
                mShapeTexture.Release();
            }

            if (mDetailTexture != null)
            {
                mDetailTexture.Release();
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}