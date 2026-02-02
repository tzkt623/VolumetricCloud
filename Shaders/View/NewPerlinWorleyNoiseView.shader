Shader"Tezcat/NewPerlinWorleyNoiseView"
{
	Properties
	{
		_Channel("Channel", Int) = 3
		_Coverage("Coverage", Range(0, 1)) = 0.8
		_FBMFactor("FBMFactor", Range(0, 1)) = 0.8
		_Clip("Clip", Range(0, 100)) = 0.5
	}
	SubShader
	{
		Tags { "RenderType" = " Opaque" }
		LOD 100

		ZWrite On
		ZTest LEqual
		//Cull Off

		Pass
		{
			CGPROGRAM
			#pragma enable_d3d11_debug_symbols
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "../Head/Function.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float3 uv3 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler3D _PerlinWorleyNoiseTex3D;
			int _Channel = 4;
			float _Coverage = 0.8;
			float _FBMFactor = 0.8;
			float _Clip = 0.8;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv3 = v.vertex.xyz + 0.5f;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				//mipmap有bug
				float4 noise = tex3D(_PerlinWorleyNoiseTex3D, i.uv3 + float3(0, 0, _Clip * 0.01f));
	//noise = pow(noise, 2.0f);
				switch(_Channel)
				{
					case 0:
						return float4(noise.rrr, 1.0f);
					case 1:
						return float4(noise.ggg, 1.0f);
					case 2:
						return float4(noise.bbb, 1.0f);
					case 3:
						return float4(noise.aaa, 1.0f);
					case 4:
						float fbm = dot(noise.gba, float3(0.625, 0.25, 0.125));
						float v = saturate(remap(noise.r, 0.0, 1.0, fbm * _FBMFactor, 1.0));
						v = remap(v, 1 - _Coverage, 1.0, 0.0, 1.0);
						return float4(v, v, v, 1.0f);
				}
				
				return float4(1.0f, 0.0f, 1.0f, 1.0f);
			}
			ENDCG
		}
	}
}
