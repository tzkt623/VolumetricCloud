using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    public class PlanetCloud : MonoBehaviour
    {
        public Transform markObject;

        public Vector3 position => markObject.position;
        public float planetRadius;
        public Vector2 cloudThickness;

        public float cloudMin => this.cloudThickness.x;
        public float cloudMax => this.cloudThickness.y;

        public float innerRadius => planetRadius + this.cloudThickness.x;
        public float outerRadius => planetRadius + this.cloudThickness.y;

        public Vector4 planetData
        {
            get
            {
                return new Vector4(position.x, position.y, position.z, planetRadius);
            }
        }
    }
}
