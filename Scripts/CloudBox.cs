using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace tezcat.Framework.Exp
{
    public class CloudBox : MonoBehaviour
    {
        public bool mDrawCloudBox = false;
        public Transform mCloudBox;

        [Header("Shape")]
        public CloudNoiseGPU mWorleyNoise;
        public DetailNoise mDetailNoise;
        public Shader mShader;
        public Texture2D mWeatherTexture2D;
        [Min(1)]
        public float mStepThickness = 50;
        public float mShapeDensityStrength = 1.0f;
        public float mDetailDensityStrength = 1.0f;
        [Range(0.0f, 1.0f)]
        public float mDensityThreshold = 0.0f;
        [Min(0.0f)]
        public float mCloudScale = 100.0f;

        [Header("Lighting")]
        public Color mCloudColor;
        public Color mCloudColorLight;
        public Color mCloudColorBlack;
        public Vector2 mCloudColorOffset;
        [Min(0.0f)]
        public float mDarknessThreshold = 0.5f;
        [Min(0.0f)]
        public float mCloudAbsorption = 1;
        [Min(0.0f)]
        public float mLightAbsorption = 0.5f;
        public Vector3 mEneryParams;

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
            if (mWorleyNoise.renderTexture != null)
            {
                this.onShapeTextureCreated(mWorleyNoise.renderTexture);
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

        public void renderCloudBox(RenderTexture source, RenderTexture destination)
        {
            if (mShapeTexture.dimension != TextureDimension.Tex3D)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (mMaterial == null)
            {
                mMaterial = new Material(mShader);
            }

            mMaterial.SetTexture("_ScreenTex", source);
            mMaterial.SetTexture("_ShapeTex3D", mShapeTexture);
            mMaterial.SetTexture("_DetailTex3D", mDetailTexture);
            mMaterial.SetTexture("_WeatherTex2D", mWeatherTexture2D);

            mMaterial.SetFloat("_StepThickness", mStepThickness);

            mMaterial.SetFloat("_CloudScale", mCloudScale);
            mMaterial.SetVector("_CloudOffset", mCloudOffset);
            mMaterial.SetFloat("_CloudAbsorption", mCloudAbsorption);
            mMaterial.SetColor("_CloudColor", mCloudColor);
            mMaterial.SetColor("_CloudColorLight", mCloudColorLight);
            mMaterial.SetColor("_CloudColorBlack", mCloudColorBlack);
            mMaterial.SetVector("_CloudColorOffset", mCloudOffset);
            mMaterial.SetVector("_ShapeSpeedScale", mShapeSpeedScale);
            mMaterial.SetVector("_DetailSpeedScale", mDetailSpeedScale);

            mMaterial.SetFloat("_ShapeDensityStrength", mShapeDensityStrength);
            mMaterial.SetFloat("_DetailDensityStrength", mDetailDensityStrength);
            mMaterial.SetFloat("_DensityThreshold", mDensityThreshold);
            mMaterial.SetFloat("_DarknessThreshold", mDarknessThreshold);
            mMaterial.SetFloat("_LightAbsorption", mLightAbsorption);
            mMaterial.SetVector("_EnergyParams", mEneryParams);

            mMaterial.SetTexture("_BlueNoiseTex2D", mBlueNoise);
            mMaterial.SetFloat("_BlueNoiseIntensity", mBlueNoiseIntensity);

            mMaterial.SetVector("_BoxMin", mCloudBox.position - mCloudBox.localScale * 0.5f);
            mMaterial.SetVector("_BoxMax", mCloudBox.position + mCloudBox.localScale * 0.5f);


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

        private void OnDrawGizmos()
        {
            if (mDrawCloudBox)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(this.transform.position, this.transform.localScale);
                Gizmos.color = Color.white;
            }
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