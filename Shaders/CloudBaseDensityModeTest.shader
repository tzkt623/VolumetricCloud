﻿Shader "Tezcat/CloudBaseDensityModeTest"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Off

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
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
			};

			//return xyz=LightEnergy w=Transmittance
			float calculateFinalColor(in float3 rayOrg, in float3 rayDir, in float3 lightDir, in float depth, in float2 uv)
			{
				float dst_to_begin_pos;
				float cloud_thickness;

				if (!calculateCloudThickness(rayOrg, rayDir, dst_to_begin_pos, cloud_thickness))
				{
					return 0.0f;
				}

				float transmittance = 1;
				float light_total_energy = 0;

				float random_offset = 0;
				if (_BlueNoiseIntensity > 1)
				{
					random_offset = _BlueNoiseTex2D.SampleLevel(sampler_BlueNoiseTex2D, squareUV(uv * 3), 0) * _BlueNoiseIntensity;
				}

				float step_thickness = cloud_thickness / _StepCount;
				float total_thickness = random_offset + step_thickness;

				float3 begin_pos = rayOrg + rayDir * (dst_to_begin_pos + random_offset);
				float3 step_dir_length = rayDir * step_thickness;

				float total_density = 0;

				while (total_thickness <= cloud_thickness)
				{
					if (total_thickness + dst_to_begin_pos > depth)
					{
						break;
					}

					total_density += 0.01f;
					if (total_density >= 1)
					{
						break;
					}
					
					total_thickness += step_thickness;
				}

				return total_density * _ShapeDensityStrength;
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.viewDir = calculateViewDir(v.uv);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float3 ray_o;
				float3 ray_dir;
				float3 light_dir;
				float linear_eye_depth;
				calulateRayMarchDatas(i.uv, i.viewDir
					, ray_o, ray_dir, light_dir, linear_eye_depth);

				float col_params = calculateFinalColor(ray_o, ray_dir, light_dir, linear_eye_depth, i.uv);

				float3 col = tex2D(_ScreenTex, i.uv);
				float3 cloud_col = _LightColor0 * col_params;

				col = col * (1 - col_params) + cloud_col;

				return float4(col, 1);
			}
			ENDCG
		}
	}
}