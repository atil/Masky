Shader "Unlit/TheShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MousePos ("Mouse Pos", Vector) = (0, 0, 0, 1)
		_Color("Quad color", Color) = (.25, .5, .5, 1)
		_Color2("Other quad color", Color) = (.25, .5, .5, 1)
		_ColorEdge("Edge color", Color) = (.25, .5, .5, 1)
		_HoleRadius ("Hole radius", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MousePos;
            float4 _Color;
            float4 _Color2;
            float4 _ColorEdge;
            float _HoleRadius;

			// hash based 3d value noise
			// function taken from https://www.shadertoy.com/view/XslGRr
			// Created by inigo quilez - iq/2013
			// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
			// ported from GLSL to HLSL

			float hash(float n) { return frac(sin(n)*43758.5453); }

			float myNoise(float3 x)
			{
				float3 p = floor(x);
				float3 f = frac(x);

				f = f*f*(3.0-2.0*f);
				float n = p.x + p.y*57.0 + 113.0*p.z;

				return lerp(lerp(lerp( hash(n+0.0), hash(n+1.0),f.x),
							   lerp( hash(n+57.0), hash(n+58.0),f.x),f.y),
						   lerp(lerp( hash(n+113.0), hash(n+114.0),f.x),
							   lerp( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
			}

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul (unity_ObjectToWorld, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float noiseNorm = (myNoise((i.worldPos.xyz + _Time.xxx * 0.5) * 3) + 1) * 0.5;
                noiseNorm = step(0.7, noiseNorm);
                fixed4 col = lerp(_Color, _Color2, noiseNorm);

                float dist = length(i.worldPos.xy - _MousePos.xy);
                if (dist < _HoleRadius) col.a = 0;
                else if (dist < _HoleRadius + 0.03) col = _ColorEdge;

                return col;
            }
            ENDCG
        }
    }
}
