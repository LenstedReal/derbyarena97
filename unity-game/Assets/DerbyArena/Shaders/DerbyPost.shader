// DERBY ARENA post-processing for the Built-in render pipeline (OnRenderImage).
// Passes: 0 bloom prefilter, 1 downsample, 2 additive upsample, 3 composite + grade, 4 grade only.
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

    half3 Grade(half3 c, float2 uv)
    {
        c *= _Exposure;
        half3 t = saturate((c * (2.51 * c + 0.03)) / (c * (2.43 * c + 0.59) + 0.14));
        c = lerp(saturate(c), t, _Tonemap);
        half l = dot(c, half3(0.2126, 0.7152, 0.0722));
        c = lerp(half3(l, l, l), c, _Saturation);
        c = (c - 0.5) * _Contrast + 0.5;
        c = c * _Gain.rgb + _Lift.rgb;
        float2 d = uv - 0.5;
        c *= saturate(1.0 - dot(d, d) * _Vignette);
        return saturate(c);
    }

    half4 FragComposite(v2f_img i) : SV_Target
    {
        half3 c = tex2D(_MainTex, i.uv).rgb;
        c += tex2D(_BloomTex, i.uv).rgb * _BloomIntensity * _BloomTint.rgb;
        return half4(Grade(c, i.uv), 1.0);
    }

    half4 FragGrade(v2f_img i) : SV_Target
    {
        return half4(Grade(tex2D(_MainTex, i.uv).rgb, i.uv), 1.0);
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
            #pragma vertex vert_img
            #pragma fragment FragComposite
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragGrade
            ENDCG
        }
    }
    FallBack Off
}
