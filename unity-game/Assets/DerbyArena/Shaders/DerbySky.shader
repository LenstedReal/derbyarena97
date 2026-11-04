// DERBY ARENA sky (2.1): per-theme gradient, sun or moon disc with glow, horizon haze, a scrolling cloud
// panorama lit from the sun side, and stars at night. One texture tap per pixel; runs on GLES3-class phones.
Shader "DerbyArena/Sky"
{
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.28, 0.5, 0.9, 1)
        _HorizonColor ("Horizon", Color) = (0.85, 0.82, 0.75, 1)
        _GroundColor ("Ground", Color) = (0.5, 0.45, 0.4, 1)
        _SunColor ("Sun", Color) = (1, 0.95, 0.85, 1)
        _SunDir ("Sun direction", Vector) = (0.3, 0.7, 0.4, 0)
        _SunSize ("Sun size (radians)", Range(0.005, 0.2)) = 0.035
        _SunGlow ("Sun glow", Range(0, 4)) = 1.2
        _Haze ("Horizon haze", Range(0, 3)) = 1.0
        _CloudTex ("Clouds (panorama, R)", 2D) = "black" {}
        _CloudColor ("Cloud lit", Color) = (1, 0.98, 0.95, 1)
        _CloudShadow ("Cloud shadow", Color) = (0.6, 0.62, 0.7, 1)
        _CloudDensity ("Cloud density", Range(0, 2)) = 1.0
        _CloudSpeed ("Cloud speed", Float) = 0.004
        _Stars ("Stars", Range(0, 2)) = 0
        _Exposure ("Exposure", Range(0, 3)) = 1.0
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            half4 _ZenithColor, _HorizonColor, _GroundColor, _SunColor, _CloudColor, _CloudShadow;
            float4 _SunDir;
            half _SunSize, _SunGlow, _Haze, _CloudDensity, _CloudSpeed, _Stars, _Exposure;
            sampler2D _CloudTex;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
                return o;
            }

            // sin-free hash (stable on mobile GPUs)
            float hash13(float3 p3)
            {
                p3 = frac(p3 * 0.1031);
                p3 += dot(p3, p3.zyx + 31.32);
                return frac((p3.x + p3.y) * p3.z);
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float y = d.y;
                float up = saturate(y);

                // gradient: horizon band widens with haze, ground darkens below the horizon
                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(up, 0.6));
                half3 ground = lerp(_HorizonColor.rgb * 0.8, _GroundColor.rgb, saturate(-y * 4.0));
                half3 col = y >= 0.0 ? sky : ground;
                col += _HorizonColor.rgb * exp(-abs(y) * 9.0) * _Haze * 0.18;

                // sun / moon
                float3 s = normalize(_SunDir.xyz);
                float cosang = dot(d, s);
                float disc = smoothstep(cos(_SunSize * 1.3), cos(_SunSize * 0.8), cosang);
                float ca = saturate(cosang);
                float ca8 = ca * ca; ca8 *= ca8; ca8 *= ca8;                  // ca^8
                float ca64 = ca8 * ca8; ca64 *= ca64; ca64 *= ca64;            // ca^64
                float glow = ca64 * ca64 * 0.9 + ca64 * 0.25 + ca8 * ca8 * 0.05;   // tight corona, faint halo
                col += _SunColor.rgb * (disc * 2.5 + glow * _SunGlow);

                // clouds: panorama lookup (u around, v from zenith to horizon), lit toward the sun
                float u = atan2(d.x, d.z) / 6.2831853 + _Time.y * _CloudSpeed;
                float v = 1.0 - pow(up, 0.55);
                float c = tex2D(_CloudTex, float2(u, v)).r;
                float c2 = tex2D(_CloudTex, float2(u, v - 0.035)).r;           // density a little higher up: top-lit shading
                c = saturate(c * _CloudDensity) * saturate(y * 5.0);
                float lit = saturate(0.55 + (c - c2 * _CloudDensity) * 2.5) * saturate(0.35 + 0.65 * (cosang * 0.5 + 0.5));
                half3 cloud = lerp(_CloudShadow.rgb, _CloudColor.rgb, lit);
                cloud += _SunColor.rgb * glow * 0.5;
                col = lerp(col, cloud, c * 0.92);

                // stars: hashed cells on the direction, hidden by clouds and near the horizon
                if (_Stars > 0.001)
                {
                    float3 cell = floor(d * 420.0);
                    float h = hash13(cell);
                    float3 f = frac(d * 420.0) - 0.5;
                    float star = step(0.993, h) * saturate(1.0 - dot(f, f) * 9.0) * (0.4 + 0.6 * frac(h * 91.7));
                    col += star * _Stars * (1.0 - c) * saturate(y * 3.0);
                }
                return half4(col * _Exposure, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Skybox/Procedural"
}
