using UnityEngine;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class MotionData : BaseData
    {
        public Vector3 mCloudOffset;
        public Vector3 mCloudSpeed;

        [Space()]
        public Vector3 mShapeSpeedScale;
        public Vector3 mDetailSpeedScale;

        [Space()]
        public Vector3 mWindDirection;

        [Space()]
        public Vector2 mWeatherOffset;
        public Vector2 mWeatherSpeed;

        public override void sendToGPU(Material material)
        {
            material.SetVector("_WeatherOffset", mWeatherOffset);
            material.SetVector("_CloudOffset", mCloudOffset);
            material.SetVector("_CloudSpeed", mCloudSpeed);
            material.SetVector("_ShapeSpeedScale", mShapeSpeedScale);
            material.SetVector("_DetailSpeedScale", mDetailSpeedScale);
            material.SetVector("_WindDirection", mWindDirection);
        }

        public void update(CloudRenderer.CloudArea cloudArea)
        {
            mWeatherOffset += mWeatherSpeed * Time.deltaTime;

            if (cloudArea == CloudRenderer.CloudArea.Planet)
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
        }
    }
}
