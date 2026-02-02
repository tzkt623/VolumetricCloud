Shader "Tezcat/NewWorleyNoiseView"
{
	Properties
	{
		_Channel("Channel", Int) = 0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		ZWrite On
		ZTest LEqual

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

			sampler3D _WorleyNoiseTex3D;
			int _Channel;

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
				float4 noise = tex3D(_WorleyNoiseTex3D, i.uv3);
				//noise = pow(noise, 2.0f);

				switch(_Channel)
				{
					case 0:
			noise.rgb = noise.rrr;
						break;
					case 1:
			noise.rgb = noise.ggg;
						break;
					case 2:
			noise.rgb = noise.bbb;
						break;
					case 3:
			noise.rgb = noise.aaa;
						break;
				}
				
				return float4(noise.rgb, 1.0f);
}
			ENDCG
		}
	}
}
