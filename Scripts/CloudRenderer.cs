using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace tezcat.Framework.Exp
{
    public class CloudRenderer
        : MonoBehaviour
        , IPostRenderer
    {
        public enum CloudArea
        {
            Box,
            Planet,
            HorizonLine
        }

        public enum CameraPos
        {
            UnderGround = -1,
            UnderCloud = 0,
            InCloud = 1,
            OutCloud = 2
        }

        [HideInInspector, SerializeField]
        int mCurrnetIndex = -1;
        [HideInInspector, SerializeField]
        int mSetIndex = -1;
        [HideInInspector]
        public List<Shader> mShaderList = new List<Shader>();

        [Header("Data")]
        public BoxCloud mBoxCloud;
        public PlanetCloud mPlanetCloud;
        public HorizonLineCloud mHorizonLineCloud;
        [Header("Draw Area")]
        public CloudArea mDrawArea;

        public ShapeData mShapeData;
        public WeatherMapData mWeatherMapData;
        public LightingData mLightingData;
        public MotionData mMotionData;
        public FilterData mFilterData;
        public BlurData mBlurData;

        RenderTexture mShapeTexture;
        RenderTexture mDetailTexture;
        Material mMaterial = null;


        void Start()
        {
            if (mShapeData.mWorleyNoise.shapeTexture != null)
            {
                this.onShapeTextureCreated(mShapeData.mWorleyNoise.shapeTexture);
            }
            mShapeData.mWorleyNoise.onTextureCreated += onShapeTextureCreated;

            if (mShapeData.mDetailNoise.renderTexture != null)
            {
                this.onDetailTextureCreated(mShapeData.mDetailNoise.renderTexture);
            }
            mShapeData.mDetailNoise.onTextureCreated += onDetailTextureCreated;

            mWeatherMapData.init();
        }

        private void onShapeTextureCreated(RenderTexture obj)
        {
            mShapeTexture = obj;
            if (mShapeTexture.dimension == TextureDimension.Tex3D)
            {
                mMaterial?.SetTexture("_ShapeTex3D", mShapeTexture);
            }
        }

        private void onDetailTextureCreated(RenderTexture obj)
        {
            mDetailTexture = obj;
            if (mDetailTexture.dimension == TextureDimension.Tex3D)
            {
                mMaterial?.SetTexture("_DetailTex3D", mDetailTexture);
            }
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
            if (mShapeTexture.dimension != TextureDimension.Tex3D)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (mMaterial == null)
            {
                if(mCurrnetIndex < 0)
                {
                    mCurrnetIndex = mShaderList.Count - 1;
                }

                if (mCurrnetIndex < 0)
                {
                    Graphics.Blit(source, destination);
                    return;
                }

                mMaterial = new Material(mShaderList[mCurrnetIndex]);
            }

            //-----------------------------------
            //
            //  Data
            //
            mMaterial.SetTexture("_ScreenTex", source);
            mMaterial.SetTexture("_ShapeTex3D", mShapeTexture);
            mMaterial.SetTexture("_DetailTex3D", mDetailTexture);

            //-----------------------------------
            //
            //  Shape
            //
            mShapeData.sendToGPU(mMaterial);

            //-----------------------------------
            //
            //  Weather
            //
            mWeatherMapData.sendToGPU(mMaterial);

            //-----------------------------------
            //
            //  Light
            //
            mLightingData.sendToGPU(mMaterial);

            //-----------------------------------
            //
            //  Motion
            //
            mMotionData.sendToGPU(mMaterial);

            //------------------------------------
            //
            //  Filter
            //
            mFilterData.sendToGPU(mMaterial);

            //------------------------------------
            //
            //  Area
            //
            mMaterial.SetInt("_DrawAreaIndex", (int)mDrawArea);
            switch (this.mDrawArea)
            {
                case CloudArea.Box:
                    {
                        mMaterial.SetVector("_BoxMin", mBoxCloud.min);
                        mMaterial.SetVector("_BoxMax", mBoxCloud.max);
                    }
                    break;
                case CloudArea.Planet:
                    {
                        this.sendSphereAreaData(mPlanetCloud);
                    }
                    break;
                case CloudArea.HorizonLine:
                    {
                        this.sendSphereAreaData(mHorizonLineCloud);
                    }
                    break;
                default:
                    break;
            }


            //----------------------------------------------
            //
            //  BilateralBlur
            //
            //var full_resolution = Screen.currentResolution;
            //Screen.SetResolution(full_resolution.width / 2, full_resolution.height / 2, true);

            if (mBlurData.mEnableBilateralBlur)
            {
                mBlurData.sendToGPU(mMaterial, source, destination);
            }
            else
            {
                Graphics.Blit(source, destination, mMaterial);
            }

            //Screen.SetResolution(full_resolution.width, full_resolution.height, true);
        }

        private void OnDestroy()
        {
            mShapeData.mWorleyNoise.onTextureCreated -= this.onShapeTextureCreated;
            mShapeData.mDetailNoise.onTextureCreated -= this.onDetailTextureCreated;

            mBlurData?.close();

            mShapeTexture?.Release();
            mDetailTexture?.Release();
        }

        private float min(Vector3 vec)
        {
            return Mathf.Min(vec.x, Mathf.Min(vec.y, vec.z));
        }

        private void sendSphereAreaData(SphereArea sphereArea)
        {
            mMaterial.SetVector("_PlanetData", sphereArea.planetData);
            mMaterial.SetVector("_PlanetCloudThickness", sphereArea.cloudThickness);

            var cam = SceneView.GetAllSceneCameras()[0];
            mMaterial.SetVector("_CameraUp", cam.transform.up);
            var camera_height = (cam.transform.position - sphereArea.planetCenter).magnitude;

            ///在云层上
            if (camera_height > sphereArea.outerRadius)
            {
                mMaterial.SetInt("_ViewPosition", (int)CameraPos.OutCloud);
            }
            ///在云层中
            else if (camera_height > sphereArea.innerRadius)
            {
                mMaterial.SetInt("_ViewPosition", (int)CameraPos.InCloud);
            }
            ///在云层下
            else if (camera_height > sphereArea.planetRadius)
            {
                mMaterial.SetInt("_ViewPosition", (int)CameraPos.UnderCloud);
            }
            else
            {
                //GG
                mMaterial.SetInt("_ViewPosition", (int)CameraPos.UnderGround);
            }
        }

        // Update is called once per frame
        void Update()
        {
            mMotionData.update(mDrawArea);


            //Debug.Log(mBoxCollider.bounds.min);
            //Debug.Log(mBoxCollider.bounds.max);
        }
    }
}