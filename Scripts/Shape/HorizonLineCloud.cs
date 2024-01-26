using UnityEngine;

namespace tezcat.Framework.Exp
{
    public class HorizonLineCloud
        : SphereArea
    {
        public Vector3 horizonLinePosition => markObject.position;
        public Vector3 horizonLineSize => new Vector3(this.planetRadius * 2, 1, this.planetRadius * 2);

        public override Vector3 planetCenter
        {
            get
            {
                var pos = horizonLinePosition;
                return new Vector3(pos.x, pos.y - planetRadius, pos.z);
            }
        }
    }
}
