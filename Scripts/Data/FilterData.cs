using UnityEngine;

namespace tezcat.Framework.Exp
{
    [System.Serializable]
    public class FilterData : BaseData
    {
        public Texture2D mBlueNoise;
        [Min(0.0f)]
        public float mBlueNoiseIntensity;

        public override void sendToGPU(Material material)
        {
            material.SetTexture("_BlueNoiseTex2D", mBlueNoise);
            material.SetFloat("_BlueNoiseIntensity", mBlueNoiseIntensity);
        }

        public override void update()
        {

        }
    }
}
