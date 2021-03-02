Shader "Custom/Grid"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "DisableBatching"="True" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 unscaledWorldRight = normalize(unity_ObjectToWorld._m00_m10_m20_m30);
            float4 unscaledWorldUp = normalize(unity_ObjectToWorld._m01_m11_m21_m31);
            float4 unscaledWorldForward = normalize(unity_ObjectToWorld._m02_m12_m22_m32);
            float4 worldOrigin = unity_ObjectToWorld._m03_m13_m23_m33;
            float4x4 worldToUnscaledObject = float4x4(unscaledWorldRight, unscaledWorldUp, unscaledWorldForward, worldOrigin);

            fixed4 c = tex2D (_MainTex, mul(worldToUnscaledObject, (IN.worldPos - worldOrigin)).xz / 6.4);
            c = lerp(_Color, c, c.a);
            c = lerp(_Color, c, pow(clamp(dot(IN.worldNormal, unscaledWorldUp), 0, 1), 10));
            o.Albedo = c.rgb;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
