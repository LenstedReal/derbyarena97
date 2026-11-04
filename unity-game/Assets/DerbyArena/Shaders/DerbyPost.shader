// DERBY ARENA post-processing for the Built-in render pipeline (OnRenderImage).
// Passes: 0 bloom prefilter, 1 downsample, 2 additive upsample, 3 composite + grade, 4 grade only.
// 2.1: ACES filmic tone mapping (HDR, half strength), film grain, blast flash, nitro / impact chromatic split.
// (The red damage edges stay in the HUD.)
Shader "Hidden/DerbyArena/Post"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _BloomTex ("Bloom", 2D) = "black" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _BloomTex;
    half _Threshold;
    half _Knee;
    half _BloomIntensity;
    half4 _BloomTint;
    half _Exposure;
    half _Contrast;
    half _Saturation;
    half4 _Lift;
    half4 _Gain;
    half _Vignette;
    half _Tonemap;
    half _Aces;
    half _Grain;
    half _Flash;
    half _Speed;

    // Nitro: red and blue sampled slightly apart along the radius (2 extra taps, only while boosting).
    half3 SampleSource(float2 uv)
    {
        half3 c = tex2D(_MainTex, uv).rgb;
        if (_Speed > 0.001)
        {
            float2 dir = (uv - 0.5) * (_Speed * 0.012);
            c.r = tex2D(_MainTex, uv - dir).r;
            c.b = tex2D(_MainTex, uv + dir).b;
        }
        return c;
    }

    // Narkowicz ACES fit, blended at about half strength by _Aces (full strength crushed dark ground ~40%).
    // computed in float with the input capped: in half precision x*(2.51x) overflows above ~160 (NaN pixels)
    half3 Aces(half3 c)
    {
        float3 x = min((float3)c, 64.0) * 0.9;
        return (half3)saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14));
    }

    half3 Box4(float2 uv, float d)
    {
        float4 o = _MainTex_TexelSize.xyxy * float4(-d, -d, d, d);
        half3 s = tex2D(_MainTex, uv + o.xy).rgb;
        s += tex2D(_MainTex, uv + o.zy).rgb;
        s += tex2D(_MainTex, uv + o.xw).rgb;
        s += tex2D(_MainTex, uv + o.zw).rgb;
        return s * 0.25;
    }

    half4 FragPrefilter(v2f_img i) : SV_Target
    {
        half3 c = Box4(i.uv, 1.0);
        half br = max(c.r, max(c.g, c.b));
        half soft = clamp(br - _Threshold + _Knee, 0.0, 2.0 * _Knee);
        soft = soft * soft / (4.0 * _Knee + 0.0001);
        half contrib = max(soft, br - _Threshold) / max(br, 0.0001);
        return half4(c * contrib, 1.0);
    }

    half4 FragDown(v2f_img i) : SV_Target
    {
        return half4(Box4(i.uv, 1.0), 1.0);
    }

    half4 FragUp(v2f_img i) : SV_Target
    {
        return half4(Box4(i.uv, 0.5), 1.0);
    }

    // Grading runs in gamma (display) space. In 1.3.0 contrast was applied to linear values around 0.5,
    // which clipped everything darker than ~sRGB 0.25 to pure black (ground and obstacles merged).
    // pix: full-precision pixel position (SV_POSITION); uv is half precision on mobile and too coarse for grain
    half3 Grade(half3 c, float2 uv, float2 pix)
    {
        c *= _Exposure;
        half3 e = max(c - 0.75, 0.0);
        half3 shoulder = lerp(c, min(c, 0.75) + e / (1.0 + e * 4.0), _Tonemap);   // soft highlight shoulder (LDR)
        c = lerp(shoulder, Aces(max(c, 0.0)), _Aces);
        half3 g = LinearToGammaSpace(max(c, 0.0));
        half l = dot(g, half3(0.2126, 0.7152, 0.0722));
        g = lerp(half3(l, l, l), g, _Saturation);
        g = (g - 0.5) * _Contrast + 0.5;
        g = g * _Gain.rgb + _Lift.rgb;
        float2 d = uv - 0.5;
        float r2 = dot(d, d);
        g *= 1.0 - saturate(r2 * _Vignette);
        g += _Flash;                                                          // nearby blast: white flash
        // film grain, re-seeded 24 times a second; sin-free float hash (stays stable on mobile GPUs after hours)
        float2 p = floor(pix) + fmod(floor(_Time.y * 24.0), 97.0) * float2(17.0, 59.0);
        float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
        p3 += dot(p3, p3.yzx + 33.33);
        float n = frac((p3.x + p3.y) * p3.z);
        g += (n - 0.5) * _Grain;
        return GammaToLinearSpace(saturate(g));
    }

    half4 FragComposite(v2f_img i) : SV_Target
    {
        half3 c = SampleSource(i.uv);
        c += tex2D(_BloomTex, i.uv).rgb * _BloomIntensity * _BloomTint.rgb;
        return half4(Grade(c, i.uv, i.pos.xy), 1.0);
    }

    half4 FragGrade(v2f_img i) : SV_Target
    {
        return half4(Grade(SampleSource(i.uv), i.uv, i.pos.xy), 1.0);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragPrefilter
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragDown
            ENDCG
        }

        Pass
        {
            Blend One One
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragUp
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment FragComposite
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment FragGrade
            ENDCG
        }
    }
    FallBack Off
}
