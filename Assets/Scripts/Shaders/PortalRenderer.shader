﻿Shader "Botania/PortalRenderer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD0;
			};
			sampler2D _MainTex;
			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}
			fixed4 frag(v2f IN) : SV_Target
			{
				float2 uv = IN.screenPos.xy / IN.screenPos.w;
				fixed4 col = tex2D(_MainTex, uv);
				return col;
			}
			ENDCG
		}
	}
}
