using System.Collections;
using System.Collections.Generic;
using tezcat.Framework.Exp;
using UnityEngine;

public class TestDeepSeek : MonoBehaviour
{
    public CloudNoiseGenerator cloudNoiseGenerator;
    public MeshRenderer mRenderer;

    void Start()
    {
        mRenderer.material.SetTexture("_NoiseTex3D", cloudNoiseGenerator.perlinNoiseTexture);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
