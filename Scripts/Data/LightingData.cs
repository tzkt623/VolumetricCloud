using NaughtyAttributes;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class LightingData : BaseData
    {
        public Light mLight;

        [Min(0.0f)]
        public float mLightStepLength = 50;

        public Color mCloudColorLight;
        public Color mCloudColorBlack;

        public Color mCloudColorLight2;
        public Color mCloudColorBlack2;

        [AllowNesting, MinValue(0.0f), MaxValue(1.0f)]
        public Vector2 mColorOffset;

        [Range(0.0f, 1.0f)]
        public float mDarknessThreshold = 0.5f;
        public float mCloudAbsorption = 1.0f;
        public float mLightAbsorption = 1.0f;

        [Space()]
        [Range(1, 3)]
        public int mMultipleScatteringOctave = 1;
        [Range(0.0f, 1.0f)]
        public float mScatterForwardFactor = 0.3f;
        [Range(0.0f, 1.0f)]
        public float mScatterBackFactor = 0.1f;
        [Range(0.0f, 1.0f)]
        public float mScatterBlendFactor = 0.5f;
        [Min(0.0f)]
        public float mScatterExtra;

        [Space()]
        [Min(0.0f)]
        public float mAmbientStrength = 1.0f;
        [Min(0.0f)]
        public float mSunStrength = 1.0f;
        [Min(0.0f)]
        public float mBrightness = 1.0f;


        [Header("Factors")]
        public float mCloudAbsorptionFactor = 0.001f;
        public float mLightAbsorptionFactor = 0.001f;

        public override void initIDs()
        {
            this.generateIDs("_LightData01"
                , "_LightData02"
                , "_PhaseParams"
                , "_CloudColorLight"
                , "_CloudColorBlack"
                , "_CloudColorLight2"
                , "_CloudColorBlack2");
        }

        public override void sendToGPU(Material material)
        {
            float angle = Mathf.Abs(mLight.transform.rotation.eulerAngles.x - 90.0f) / 90.0f;
            //material.SetInt("_MSOctave", mMultipleScatteringOctave);


            material.SetVector(mIDs[0], new Vector4(mLightAbsorption * mLightAbsorptionFactor, mLightStepLength, mDarknessThreshold, 0));

            material.SetVector(mIDs[1], new Vector4(mCloudAbsorption * mCloudAbsorptionFactor
                , mColorOffset.x
                , mColorOffset.y
                , angle));

            material.SetVector(mIDs[2], new Vector4(mScatterForwardFactor, -mScatterBackFactor, mScatterBlendFactor, mScatterExtra));

            material.SetColor(mIDs[3], mCloudColorLight);
            material.SetColor(mIDs[4], mCloudColorBlack);

            material.SetColor(mIDs[5], mCloudColorLight2);
            material.SetColor(mIDs[6], mCloudColorBlack2);

            //material.SetFloat("_Brightness", mBrightness);
            //material.SetVector("_EnergyStrength", new Vector3(mAmbientStrength, mSunStrength, mBrightness));
        }
    }
}