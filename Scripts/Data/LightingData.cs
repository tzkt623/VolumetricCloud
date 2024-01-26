using UnityEngine;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class LightingData : BaseData
    {
        [Min(0.0f)]
        public float mLightStepLength = 50;

        public Color mCloudColorLight;
        public Color mCloudColorBlack;
        [Range(0.0f, 1.0f)]
        public float mDarknessThreshold = 0.5f;
        [Min(0.0f)]
        public float mCloudAbsorption = 0;
        [Min(0.0f)]
        public float mLightAbsorption = 1.0f;

        [Space()]
        [Range(0.0f, 1.0f)]
        public float mScatterForwardFactor = 0.3f;
        [Range(0.0f, 1.0f)]
        public float mScatterBackFactor = 0.1f;
        [Range(0.0f, 1.0f)]
        public float mScatterBlendFactor = 0.5f;
        [Min(0.0f)]
        public float mScatterExtra;

        [Space()]
        [Range(1, 3)]
        public int mMultipleScatteringOctave = 1;
        [Min(0.0f)]
        public float mAmbientStrength = 1.0f;
        [Min(0.0f)]
        public float mSunStrength = 1.0f;
        [Min(0.0f)]
        public float mBrightness = 1.0f;

        public override void sendToGPU(Material material)
        {
            material.SetInt("_MSOctave", mMultipleScatteringOctave);
            material.SetFloat("_LightStepLength", mLightStepLength);
            material.SetColor("_CloudColorLight", mCloudColorLight);
            material.SetColor("_CloudColorBlack", mCloudColorBlack);
            material.SetFloat("_CloudAbsorption", mCloudAbsorption / 1000.0f);
            material.SetFloat("_DarknessThreshold", mDarknessThreshold);
            material.SetFloat("_LightAbsorption", mLightAbsorption);
            material.SetVector("_PhaseParams", new Vector4(mScatterForwardFactor, -mScatterBackFactor, mScatterBlendFactor, mScatterExtra));
            material.SetFloat("_Brightness", mBrightness);
            material.SetVector("_EnergyStrength", new Vector3(mAmbientStrength, mSunStrength, mBrightness));
        }
    }
}