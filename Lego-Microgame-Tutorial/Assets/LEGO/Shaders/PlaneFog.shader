Shader "Unlit/PlaneFog"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _FogTint("Fog Tint", Float) = 0.5
        _FogParam("Max Depth, Multiplier, Offset, Speed", Vector) = (1,1,0.5, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent-1"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 screenPos : TEXCOORD0;
                float depth : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _CameraDepthTexture;
            float4 _Color;
            float4 _FogParam;
            float _FogTint;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos =  ComputeScreenPos(o.vertex);
                COMPUTE_EYEDEPTH(o.depth);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = (tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                depth = LinearEyeDepth(depth) - i.depth;
                
                depth += _FogParam.z;
                
                depth = smoothstep(0.0f, _FogParam.x, depth) * _FogParam.y;
                depth = saturate(depth);
    
                fixed4 col = float4(_Color.xyz * lerp(1.0f, 0.0f, _FogTint),depth);
                
                return col;
            }
            ENDCG
        }
    }
}
