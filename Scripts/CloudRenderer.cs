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
            Planet
        }

        public enum ShaderIndex
        {
            Cloud = 0,
            Level01,
            Level02,
            Level03,
            Level04,
        }

        [Header("Shader")]
        public ShaderIndex mShaderIndex;
        public Shader[] mShaders;
        ShaderIndex mCurrentShaderIndex = ShaderIndex.Cloud;

        [Header("Data")]
        public PlanetCloud mPlanetCloud;
        public BoxCloud mBoxCloud;
        public CloudArea drawArea;

        [Header("Shape")]
        public CloudNoiseGPU mWorleyNoise;
        public DetailNoise mDetailNoise;
        public Shader mShader;
        public Texture2D mWeatherTexture2D;
        [Min(1.0f)]
        public float mStepThickness = 50;
        [Min(0.01f)]
        public float mShapeDensityStrength = 1.0f;
        [Range(0.0f, 1.0f)]
        public float mDensityThreshold = 0.0f;
        [Min(0.0f)]
        public float mShapeScale = 0.1f;
        [Min(0.0f)]
        public float mDetailDensityStrength = 1.0f;
        [Min(0.0f)]
        public float mEdgeLength = 1.0f;
        [Range(0.0f, 1.0f)]
        public float mCoverageRate = 1.0f;

        [Header("Lighting")]
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
        public float mForwardScatteringScale;
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

        // Start is called before the first frame update
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
                mCurrentShaderIndex = ShaderIndex.Cloud;
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
            mMaterial.SetFloat("_StepThickness", mStepThickness);
            mMaterial.SetFloat("_ShapeScale", mShapeScale);
            mMaterial.SetFloat("_EdgeLength", mEdgeLength);
            mMaterial.SetFloat("_CoverageRate", 1 - mCoverageRate);
            mMaterial.SetFloat("_ShapeDensityStrength", mShapeDensityStrength);
            mMaterial.SetFloat("_DetailDensityStrength", mDetailDensityStrength);

            //-----------------------------------
            //
            //  Light
            //
            mMaterial.SetColor("_CloudColorLight", mCloudColorLight);
            mMaterial.SetColor("_CloudColorBlack", mCloudColorBlack);
            mMaterial.SetFloat("_CloudAbsorption", mCloudAbsorption);
            mMaterial.SetFloat("_ForwardScatteringScale", mForwardScatteringScale);
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
            mMaterial.SetInt("_DrawPlanetArea", (int)this.drawArea);
            switch (this.drawArea)
            {
                case CloudArea.Box:
                    {
                        mMaterial.SetVector("_BoxMin", mBoxCloud.min);
                        mMaterial.SetVector("_BoxMax", mBoxCloud.max);
                    }
                    break;
                case CloudArea.Planet:
                    {
                        mMaterial.SetVector("_PlanetData", mPlanetCloud.planetData);
                        mMaterial.SetVector("_PlanetCloudThickness", mPlanetCloud.cloudThickness);

                        var cam = SceneView.GetAllSceneCameras()[0];
                        var camera_height = (cam.transform.position - mPlanetCloud.position).magnitude;

                        ///在云层上
                        if (camera_height > mPlanetCloud.outerRadius)
                        {
                            mMaterial.SetInt("_ViewPosition", 2);
                        }
                        ///在云层中
                        else if (camera_height > mPlanetCloud.innerRadius)
                        {
                            mMaterial.SetInt("_ViewPosition", 1);
                        }
                        ///在云层下
                        else if (camera_height > mPlanetCloud.planetRadius)
                        {
                            mMaterial.SetInt("_ViewPosition", 0);
                        }
                        else
                        {
                            //GG
                            mMaterial.SetInt("_ViewPosition", -1);
                        }
                    }
                    break;
                default:
                    break;
            }


            //----------------------------------------------
            //
            //  BilateralBlur
            //
            //
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

        // Update is called once per frame
        void Update()
        {
            mCloudOffset += mCloudSpeed * Time.deltaTime;


            //Debug.Log(mBoxCollider.bounds.min);
            //Debug.Log(mBoxCollider.bounds.max);
        }
    }
}