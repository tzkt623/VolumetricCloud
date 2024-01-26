using UnityEngine;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class ShapeData : BaseData
    {
        public CloudNoiseGenerator mWorleyNoise;
        public DetailNoise mDetailNoise;

        [Min(10)]
        public float mStepCount = 50;
        [Min(1)]
        public float mShapeStepLength = 50;
        [Min(0.0f)]
        public float mShapeScale = 0.1f;
        [Min(0.0f)]
        public float mShapeDensityStrength = 1.0f;
        [Min(0.0f)]
        public float mDetailScale = 0.1f;
        [Min(0.0f)]
        public float mDetailDensityStrength = 1.0f;

        [Range(0.0f, 1.0f)]
        public float mDensityScale = 1.0f;
        [Range(0.0f, 1.0f)]
        public float mDensityThreshold = 0.0f;

        public override void sendToGPU(Material material)
        {
            material.SetFloat("_StepCount", mStepCount);
            material.SetFloat("_ShapeStepLength", mShapeStepLength);
            material.SetFloat("_ShapeScale", mShapeScale * 0.00001f);
            material.SetFloat("_DetailScale", mDetailScale * 0.00001f);
            material.SetFloat("_DensityScale", mDensityScale);

            const float modifier = 1000.0f;
            //mShapeDensityStrength = Mathf.Min(mShapeDensityStrength, modifier);
            //mDetailDensityStrength = Mathf.Min(mDetailDensityStrength, modifier);
            material.SetFloat("_ShapeDensityStrength", mShapeDensityStrength / modifier);
            material.SetFloat("_DetailDensityStrength", mDetailDensityStrength / modifier);
            material.SetFloat("_DensityThreshold", mDensityThreshold);
        }

        public override void update()
        {

        }
    }
}
