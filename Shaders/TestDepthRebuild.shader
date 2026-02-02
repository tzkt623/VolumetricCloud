Shader "Tezcat/DepthRebuild"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Tags
        {
            "LightMode" = "ForwardBase"
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "Head/Function.cginc"
			#include "Head/CloudHead.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float3 viewDir : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
				o.viewDir = calculateViewDir3(o.uv);
                return o;
            }

            sampler2D _Tex1;
            sampler2D _Tex2;
            sampler3D _Tex3D;

            //
            // Description : Array and textureless GLSL 2D/3D/4D simplex 
            //               noise functions.
            //      Author : Ian McEwan, Ashima Arts.
            //  Maintainer : stegu
            //     Lastmod : 20201014 (stegu)
            //     License : Copyright (C) 2011 Ashima Arts. All rights reserved.
            //               Distributed under the MIT License. See LICENSE file.
            //               https://github.com/ashima/webgl-noise
            //               https://github.com/stegu/webgl-noise
            // 

            float3 mod289(float3 x) 
            {
                return x - floor(x * (1.0 / 289.0)) * 289.0;
            }

            float4 mod289(float4 x) 
            {
                return x - floor(x * (1.0 / 289.0)) * 289.0;
            }

            float4 permute(float4 x) 
            {
                return mod289(((x*34.0)+10.0)*x);
            }

            float4 taylorInvSqrt(float4 r)
            {
                return 1.79284291400159 - 0.85373472095314 * r;
            }

            float snoise(float3 v)
            { 
                const float2  C = float2(1.0/6.0, 1.0/3.0) ;
                const float4  D = float4(0.0, 0.5, 1.0, 2.0);

                // First corner
                float3 i  = floor(v + dot(v, C.yyy) );
                float3 x0 =   v - i + dot(i, C.xxx) ;

                // Other corners
                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1.0 - g;
                float3 i1 = min( g.xyz, l.zxy );
                float3 i2 = max( g.xyz, l.zxy );

                //   x0 = x0 - 0.0 + 0.0 * C.xxx;
                //   x1 = x0 - i1  + 1.0 * C.xxx;
                //   x2 = x0 - i2  + 2.0 * C.xxx;
                //   x3 = x0 - 1.0 + 3.0 * C.xxx;
                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
                float3 x3 = x0 - D.yyy;      // -1.0+3.0*C.x = -0.5 = -D.y

                // Permutations
                i = mod289(i); 
                float4 p = permute( permute( permute( 
                            i.z + float4(0.0, i1.z, i2.z, 1.0 ))
                        + i.y + float4(0.0, i1.y, i2.y, 1.0 )) 
                        + i.x + float4(0.0, i1.x, i2.x, 1.0 ));

                // Gradients: 7x7 points over a square, mapped onto an octahedron.
                // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
                float n_ = 0.142857142857; // 1.0/7.0
                float3  ns = n_ * D.wyz - D.xzx;

                float4 j = p - 49.0 * floor(p * ns.z * ns.z);  //  mod(p,7*7)

                float4 x_ = floor(j * ns.z);
                float4 y_ = floor(j - 7.0 * x_ );    // mod(j,N)

                float4 x = x_ *ns.x + ns.yyyy;
                float4 y = y_ *ns.x + ns.yyyy;
                float4 h = 1.0 - abs(x) - abs(y);

                float4 b0 = float4( x.xy, y.xy );
                float4 b1 = float4( x.zw, y.zw );

                //float4 s0 = float4(lessThan(b0,0.0))*2.0 - 1.0;
                //float4 s1 = float4(lessThan(b1,0.0))*2.0 - 1.0;
                float4 s0 = floor(b0)*2.0 + 1.0;
                float4 s1 = floor(b1)*2.0 + 1.0;
                float4 sh = -step(h, float4(0.0,0.0,0.0,0.0));

                float4 a0 = b0.xzyw + s0.xzyw*sh.xxyy ;
                float4 a1 = b1.xzyw + s1.xzyw*sh.zzww ;

                float3 p0 = float3(a0.xy,h.x);
                float3 p1 = float3(a0.zw,h.y);
                float3 p2 = float3(a1.xy,h.z);
                float3 p3 = float3(a1.zw,h.w);

                //Normalise gradients
                float4 norm = taylorInvSqrt(float4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
                p0 *= norm.x;
                p1 *= norm.y;
                p2 *= norm.z;
                p3 *= norm.w;

                // Mix final noise value
                float4 m = max(0.5 - float4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
                m = m * m;
                return 105.0 * dot( m*m, float4( dot(p0,x0), dot(p1,x1), 
                                                dot(p2,x2), dot(p3,x3) ) );
            }

            float sampleDensity(float3 pos, float3 box_size)
            {
                //const float2 scale = float2(1.f, 1.f);
                float2 uv = (pos.xz - _BoxMin.xz) / box_size.xz;
                return tex2D(_Tex1, uv * _ShapeScale).r;
            }

            float sampleDensity(float3 pos)
            {
                //const float3 scale = float3(0.5, 0.5, 0.5);
                //return snoise(pos * _ShapeScale);
                return tex3Dlod(_Tex3D, float4(pos * _ShapeScale, 0)).r;
            }

            float lightEnergy(float3 lightDir, float3 pos)
            {
                float step = 0.5;
                float total_density = 0;
                for(int i = 0; i < 6; i++)
                {
                    //float2 uv = (pos.xz - _BoxMin.xz) / boxSize.xz;
                    pos = pos + (-lightDir) * step;
                    float step_density = sampleDensity(pos);
                    total_density += step_density;
                }

                return exp(-total_density * step);
            }

            float lightEnergy(float3 lightDir, float3 pos, float3 boxSize)
            {
                float step = 0.5;
                pos = pos + (-lightDir) * step;
                float total_density = 0;
                for(int i = 0; i < 6; i++)
                {
                    float step_density = sampleDensity(pos, boxSize);
                    total_density += step_density;
                    pos = pos + (-lightDir) * step;
                }

                return exp(-total_density * step);
            }

            //float HenyeyGreensteinPhaseFunction(float cosLV, float g)
            //{
            //    float g2 = g * g;
            //    return (1 - g2) / (4 * UNITY_PI * pow(1 + g2 - 2 * g * cosLV, 1.5));
            //}

            //float SchlickPhaseFunction(float cosLV, float k)
            //{
            //    float k2 = k * k;
            //    return (1 - k2) / (4 * UNITY_PI * pow(1 - k * cosLV, 2));
            //}

            //float Phase(float cosLV)
            //{
            //    float frontLight = HenyeyGreensteinPhaseFunction(cosLV, _PhaseParams.x);
            //    float backLight = HenyeyGreensteinPhaseFunction(cosLV, _PhaseParams.y);
            //    float phase = lerp(frontLight, backLight, 0.5) * _PhaseParams.z + _PhaseParams.w;
            //    return phase;
            //}

            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
				// 读取深度并线性化
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                //float linearDepth = Linear01Depth(depth);
                float linearDepth = LinearEyeDepth(depth);

                // 计算视图空间坐标
                float3 viewPos = i.viewDir * linearDepth;
                float3 ray_dir = normalize(i.viewDir);
                float3 light_dir = normalize(_WorldSpaceLightPos0.xyz);

                //return float4(i.viewDir, 1.0f);
                //return float4(i.viewDir, 1.0f);
                float3 ray_o;
				//float depth;
                calulateRayMarchDatas(i.uv, i.viewDir, ray_o, ray_dir, light_dir, depth);

                float2 data = rayBoxDst(_BoxMin, _BoxMax, _WorldSpaceCameraPos.xyz, 1.0f / ray_dir);
                float3 box_size = _BoxMax - _BoxMin;
                float3 cloud_color = float3(0.0f, 0.0f, 0.0f);
                float transmittance = 1.0f;
                float phase_value = phaseFunc(dot(-light_dir, -ray_dir), _PhaseParams);
                float travel_thickness = data.x;

                if(data.y > 0)
                {
                    float step_length = data.y / _StepCount;
                    float3 pos = ray_o + ray_dir * data.x;
                    //float3 offset = hash33(pos);
                   // step_length *= (1 + offset.x);
                    for(int iii = 0; iii < _StepCount; iii++)
                    {
                        if(travel_thickness >= linearDepth)
                        {
                            //col.rgb = float3(1.0f, 0.0f, 0.0f);
                            break;
                        }

                        float step_density = sampleDensity(pos);
                        if(step_density > 0.0f)
                        {                 
                            float step_transmittence = exp(-step_density * step_length);
                            transmittance *= step_transmittence;
                            if(transmittance <= 0.01f)
                            {
                                break;
                            }

                            float step_scattering = lightEnergy(light_dir, pos) * phase_value;
                            cloud_color += step_scattering * transmittance * step_length;
                        }

                        pos += ray_dir * step_length;
                        travel_thickness += step_length;
                    }
                }

                //cloud_color.a /= 10.0f;

                //if(transmittance >= 0.1f)
                //{
                //    col.r = 0;
                //    col.g = 1;
                //}

                float3 final_color = col.xyz * transmittance
                    + cloud_color
                    ;

                return float4(final_color.xyz, 0.0f);
            }
            ENDCG
        }
    }
}
