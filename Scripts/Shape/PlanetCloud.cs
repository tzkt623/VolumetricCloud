using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tezcat.Framework.Exp
{
    public class PlanetCloud
        : SphereArea
    {
        public override Vector3 planetCenter => markObject.position;
    }
}
