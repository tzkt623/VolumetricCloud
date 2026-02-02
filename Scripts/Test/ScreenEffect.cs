using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using tezcat.Framework.Exp;
using UnityEngine;
using NaughtyAttributes;

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
public class ScreenEffect : MonoBehaviour
{
    public Material material;
    public BoxCloud boxCloud;

    public Texture2D texture1;
    public Texture2D texture2;
    public Texture3D perlinNoise3D;

    public int stepCount = 32;
    public float shapeScale = 1.0f;

    [MinValue(0), MaxValue(1)]
    public Vector4 phaseParams;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material != null)
        {
            //对渲染纹理进行处理
            material.SetMatrix("_Matrix_Inv_VP", Camera.main.previousViewProjectionMatrix.inverse);
            material.SetVector("_BoxMin", boxCloud.min);
            material.SetVector("_BoxMax", boxCloud.max);

            material.SetTexture("_Tex1", texture1);
            material.SetTexture("_Tex2", texture2);
            material.SetTexture("_Tex3D", perlinNoise3D);

            material.SetFloat("_ShapeScale", shapeScale);

            material.SetVector("_ShapeData01", new Vector4(stepCount
                , shapeScale
                , 1
                , 1));

            material.SetVector("_PhaseParams", phaseParams);

            Graphics.Blit(source, destination, material);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}
