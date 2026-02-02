Shader "Tezcat/TestDeepSeek" {
    Properties {
        _NoiseTex3D ("Noise Texture 3D", 3D) = "white" {}
        _Coverage ("Cloud Coverage", Range(0, 1)) = 0.5
        _Density ("Cloud Density", Float) = 0.5
        _LightAbsorption ("Light Absorption", Float) = 1.0
        _StepSize ("Raymarch Step Size", Float) = 10.0
        _LightStepSize ("Light Step Size", Float) = 50.0
    }

    SubShader {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            sampler3D _NoiseTex3D;
            float _Coverage;
            float _Density;
            float _LightAbsorption;
            float _StepSize;
            float _LightStepSize;

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            // Henyey-Greenstein相位函数
            float hg(float cosTheta, float g) {
                float g2 = g * g;
                return (1 - g2) / (4 * 3.1415 * pow(1 + g2 - 2 * g * cosTheta, 1.5));
            }

            // 生成云密度
            float sampleCloud(float3 position) {
                // 采样3D噪声
                float noise = tex3Dlod(_NoiseTex3D, float4(position * 0.01, 0)).r;
                
                // 添加更多噪声层增加细节
                float detailNoise = tex3Dlod(_NoiseTex3D, float4(position * 0.05, 0)).r * 0.5;
                noise = saturate(noise * 2.0 - 0.5 + detailNoise);
                
                // 应用覆盖度控制
                return saturate(noise - _Coverage) * _Density;
            }

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 设置光线步进参数
                float3 rayStart = _WorldSpaceCameraPos;
                float3 rayDir = normalize(i.worldPos - _WorldSpaceCameraPos);
                float rayLength = length(i.worldPos - _WorldSpaceCameraPos);
                
                float step = _StepSize;
                int steps = (int)(rayLength / step);
                
                fixed4 finalColor = fixed4(0, 0, 0, 0);
                float transmittance = 1.0;
                
                // 主光线步进循环
                for (int i = 0; i < steps; i++) {
                    float3 currentPos = rayStart + rayDir * step * i;
                    float density = sampleCloud(currentPos);
                    
                    if (density > 0) {
                        // 光照计算
                        float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                        float lightDensity = 0;
                        
                        // 阴影光线步进
                        int lightSteps = 10;
                        for (int j = 0; j < lightSteps; j++) {
                            float3 lightPos = currentPos + lightDir * _LightStepSize * j;
                            lightDensity += sampleCloud(lightPos) * _LightStepSize;
                        }
                        
                        // 计算光照衰减
                        float lightTransmittance = exp(-lightDensity * _LightAbsorption);
                        float phase = hg(dot(rayDir, lightDir), 0.7);
                        
                        // 累积颜色
                        float scattering = density * step * lightTransmittance * phase;
                        finalColor.rgb += scattering * transmittance * _LightColor0.rgb;
                        
                        // 更新透射率
                        transmittance *= exp(-density * step * _LightAbsorption);
                        
                        // 提前终止
                        if (transmittance < 0.01) break;
                    }
                }
                
                // 混合背景颜色
                finalColor.a = 1 - transmittance;
                return finalColor;
            }
            ENDCG
        }
    }
}