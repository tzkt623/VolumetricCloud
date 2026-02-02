using NaughtyAttributes;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class ShapeData : BaseData
    {
        [MinValue(10), MaxValue(128), AllowNesting]
        public int mStepCount = 64;
        [Min(0.0f), Tooltip("Set 0 to use StepCount")]
        public float mStepLength = 50;

        public float mShapeScale = 1000.0f;
        public float mShapeDensityStrength = 1000.0f;
        public float mDensityScale = 1000.0f;
        public float mDensityThreshold = 0.0f;

        public float[] mMixFactors;

        [Header("Motion")]
        public Vector3 mCloudOffset;
        public Vector3 mCloudSpeed;

        [Header("Scale"), AllowNesting]
        public float mShapeScaleFactor = 0.001f;
        public float mShapeStrengthScaleFactor = 0.001f;
        public float mDensityScaleFactor = 0.001f;
        public float mThresholdScaleFactor = 0.001f;

        public override void initIDs()
        {
            this.generateIDs("_ShapeData01"
                , "_ShapeData02"
                , "_ShapeMixFactor"
                , "_ShapeData03"
                , "_ShapeData04");
        }

        public void sendToGPU(AreaData areaData, Material material)
        {
            if (areaData.mDrawArea == AreaData.CloudArea.Planet)
            {
                if (mCloudOffset.x > 360.0f)
                {
                    mCloudOffset.x -= 360.0f;
                }
                else if (mCloudOffset.x < 0.0f)
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

            material.SetInt(mIDs[0], mStepCount);
            material.SetVector(mIDs[1], new Vector4(mShapeScale * mShapeScaleFactor
                , mShapeDensityStrength  * mShapeStrengthScaleFactor
                , mDensityThreshold * mThresholdScaleFactor
                , mDensityScale * mDensityScaleFactor));
            material.SetVector(mIDs[2], new Vector4(mMixFactors[0], mMixFactors[1], mMixFactors[2],
                mMixFactors[0] + mMixFactors[1] + mMixFactors[2]));

            material.SetVector(mIDs[3], mCloudOffset);
            material.SetVector(mIDs[4], mCloudSpeed);
        }

        public override void update()
        {

        }
    }

    [System.Serializable]
    public class DetailData : BaseData
    {
        [Range(0.0f, 2000.0f), Tooltip("Modifier 1000 = 1")]
        public float mDetailScale = 1000.0f;
        [Range(0.0f, 1000.0f), Tooltip("Modifier 1000 = 1")]
        public float mDetailDensityStrength = 1000.0f;

        [Header("Scale")]
        public float mScaleFactor = 0.001f;
        public float mDensityFactor = 0.001f;


        public override void initIDs()
        {
            this.generateIDs("_DetailData01");
        }

        public override void sendToGPU(Material material)
        {
            material.SetVector(mIDs[0], new Vector4(mDetailScale * mScaleFactor
                , mDetailDensityStrength * mDensityFactor
                , 0
                , 0));
        }
    }
}
