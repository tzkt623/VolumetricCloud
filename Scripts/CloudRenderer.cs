using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
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

        public enum ShaderIndex
        {
            Error = 0,
            Level01,
            Level02,
            Level03,
            Level04,
            Level05,
        }

        public enum CameraPos
        {
            UnderHorizonLine = -1,
            UnderCloud = 0,
            InCloud = 1,
            OutCloud= 2
        }

        [Header("Shader")]
        public ShaderIndex mShaderIndex;
        public Shader[] mShaders;
        ShaderIndex mCurrentShaderIndex = ShaderIndex.Error;

        [Header("Data")]
        public BoxCloud mBoxCloud;
        public PlanetCloud mPlanetCloud;
        public HorizonLineCloud mHorizonLineCloud;
        public CloudArea mDrawArea;

        [Header("Shape")]
        public CloudNoiseGPU mWorleyNoise;
        public DetailNoise mDetailNoise;
        public Shader mShader;
        public Texture2D mWeatherTexture2D;
        [Min(10)]
        public float mStepCount = 50;
        [Min(1)]
        public float mShapeStepLength = 50;
        [Min(0.0f)]
        public float mShapeScale = 0.1f;
        [Min(0.01f)]
        public float mShapeDensityStrength = 1.0f;
        [Min(0.0f)]
        public float mDetailScale = 0.1f;
        [Min(0.0f)]
        public float mDetailDensityStrength = 1.0f;
        [Range(0.0f, 1.0f)]
        public float mDensityThreshold = 0.0f;
        [Min(0.0f)]
        public float mEdgeLength = 1.0f;
        [Range(0.0f, 1.0f)]
        public float mCoverageRate = 1.0f;

        [Header("Lighting")]
        [Min(0.0f)]
        public float mLightStepLength = 10;
        public Color mCloudColorLight;
        public Color mCloudColorBlack;
        [Range(0.0f, 1.0f)]
        public float mDarknessThreshold = 0.5f;
        [Min(0.0f)]
        public float mCloudAbsorption = 1;
        [Min(0.0f)]
        public float mLightAbsorption = 0.5f;
        [Min(0.0f)]
        public float mBrightness = 1.0f;
        [Min(0.0f)]
        public float mForwardScattering;
        [Tooltip("For Phase Function")]
        public Vector4 mEneryParams;

        [Header("Motion")]
        public Vector3 mCloudOffset;
        public Vector3 mCloudSpeed;
        public Vector3 mShapeSpeedScale;
        public Vector3 mDetailSpeedScale;

        [Header("Filter")]
        public Texture2D mBlueNoise;
        [Min(0.0f)]
        public float mBlueNoiseIntensity;

        [Header("BilateralBlur")]
        public bool mEnableBilateralBlur;
        public Shader mBlurShader;
        [Tooltip("x:SpatialWeight y:TonalWeight z:BlurRadius")]
        public Vector3 mBlurParams;


        RenderTexture mShapeTexture;
        RenderTexture mDetailTexture;
        Material mMaterial = null;
        Material mBlurMaterial = null;

        RenderTexture mBlurRenderTexture;

        void Start()
        {
            if (mWorleyNoise.shapeTexture != null)
            {
                this.onShapeTextureCreated(mWorleyNoise.shapeTexture);
            }
            mWorleyNoise.onTextureCreated += onShapeTextureCreated;

            if (mDetailNoise.renderTexture != null)
            {
                this.onDetailTextureCreated(mDetailNoise.renderTexture);
            }
            mDetailNoise.onTextureCreated += onDetailTextureCreated;
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

        public void rendering(RenderTexture source, RenderTexture destination)
        {
            if (mShapeTexture.dimension != TextureDimension.Tex3D)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (mMaterial == null)
            {
                mCurrentShaderIndex = ShaderIndex.Error;
                mMaterial = new Material(mShaders[(int)mCurrentShaderIndex]);
            }

            if(mCurrentShaderIndex != mShaderIndex)
            {
                mCurrentShaderIndex = mShaderIndex;
                mMaterial.shader = mShaders[(int)mCurrentShaderIndex];
            }


            //-----------------------------------
            //
            //  Data
            //
            mMaterial.SetTexture("_ScreenTex", source);
            mMaterial.SetTexture("_ShapeTex3D", mShapeTexture);
            mMaterial.SetTexture("_DetailTex3D", mDetailTexture);
            mMaterial.SetTexture("_WeatherTex2D", mWeatherTexture2D);

            //-----------------------------------
            //
            //  Shape
            //
            mMaterial.SetFloat("_StepCount", mStepCount);
            mMaterial.SetFloat("_ShapeStepLength", mShapeStepLength);
            mMaterial.SetFloat("_ShapeScale", mShapeScale);
            mMaterial.SetFloat("_DetailScale", mDetailScale);
            mMaterial.SetFloat("_EdgeLength", mEdgeLength);
            mMaterial.SetFloat("_CoverageRate", mCoverageRate);
            mMaterial.SetFloat("_ShapeDensityStrength", mShapeDensityStrength);
            mMaterial.SetFloat("_DetailDensityStrength", mDetailDensityStrength);

            //-----------------------------------
            //
            //  Light
            //
            mMaterial.SetFloat("_LightStepLength", mLightStepLength);
            mMaterial.SetColor("_CloudColorLight", mCloudColorLight);
            mMaterial.SetColor("_CloudColorBlack", mCloudColorBlack);
            mMaterial.SetFloat("_CloudAbsorption", mCloudAbsorption);
            mMaterial.SetFloat("_ForwardScatteringScale", mForwardScattering);
            mMaterial.SetFloat("_DensityThreshold", mDensityThreshold);
            mMaterial.SetFloat("_DarknessThreshold", mDarknessThreshold);
            mMaterial.SetFloat("_LightAbsorption", mLightAbsorption);
            mMaterial.SetVector("_EnergyParams", mEneryParams);
            mMaterial.SetFloat("_Brightness", mBrightness);

            //-----------------------------------
            //
            //  Motion
            //
            mMaterial.SetVector("_CloudOffset", mCloudOffset);
            mMaterial.SetVector("_CloudSpeed", mCloudSpeed);
            mMaterial.SetVector("_ShapeSpeedScale", mShapeSpeedScale);
            mMaterial.SetVector("_DetailSpeedScale", mDetailSpeedScale);

            //------------------------------------
            //
            //  Filter
            //
            mMaterial.SetTexture("_BlueNoiseTex2D", mBlueNoise);
            mMaterial.SetFloat("_BlueNoiseIntensity", mBlueNoiseIntensity);

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

            if (mEnableBilateralBlur)
            {
                if (mBlurMaterial == null)
                {
                    mBlurMaterial = new Material(mBlurShader);
                    mBlurRenderTexture = new RenderTexture(source);
                    mBlurRenderTexture.Create();
                }
                Graphics.Blit(source, mBlurRenderTexture, mMaterial);
                mBlurMaterial.SetTexture("_MainTex", mBlurRenderTexture);
                mBlurMaterial.SetVector("_BlurParams", mBlurParams);
                Graphics.Blit(mBlurRenderTexture, destination, mBlurMaterial);
            }
            else
            {
                Graphics.Blit(source, destination, mMaterial);
            }

            //Screen.SetResolution(full_resolution.width, full_resolution.height, true);
        }

        private void OnDestroy()
        {
            mWorleyNoise.onTextureCreated -= this.onShapeTextureCreated;
            mDetailNoise.onTextureCreated -= this.onDetailTextureCreated;

            mBlurRenderTexture?.Release();
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
                mMaterial.SetInt("_ViewPosition", (int)CameraPos.UnderHorizonLine);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(this.mDrawArea == CloudArea.Planet)
            {
                if(mCloudOffset.x > 360.0f)
                {
                    mCloudOffset.x -= 360.0f;
                }
                else if(mCloudOffset.x < 0.0f)
                {
                    mCloudOffset.x += 360.0f;
                }

                if (mCloudOffset.y > 360.0f)
                {
                    mCloudOffset.y -= 360.0f;
                }
                else if (mCloudOffset.y < 0.0f)
                {
                    mCloudOffset.y += 360.0f;
                }

                if (mCloudOffset.z > 360.0f)
                {
                    mCloudOffset.z -= 360.0f;
                }
                else if (mCloudOffset.z < 0.0f)
                {
                    mCloudOffset.z += 360.0f;
                }

                mCloudOffset += new Vector3(mCloudSpeed.z, mCloudSpeed.x, mCloudSpeed.y);
            }
            else
            {
                mCloudOffset += mCloudSpeed * Time.deltaTime;
            }


            //Debug.Log(mBoxCollider.bounds.min);
            //Debug.Log(mBoxCollider.bounds.max);
        }
    }
}