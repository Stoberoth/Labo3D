// Hologramme URP, écrit en HLSL. Unlit volontairement : le rim (fresnel)
// et les scanlines suffisent à lire l'effet. Blend additif, pas d'écriture
// de profondeur, donc le volume se superpose au décor.
Shader "Labo3D/Holographic"
{
    Properties
    {
        [HDR] _BaseColor ("Couleur", Color) = (0.15, 0.85, 1, 1)
        _RimPower ("Puissance du rim", Range(0.5, 8)) = 2.5
        _ScanSpeed ("Vitesse des scanlines", Float) = 1.5
        _ScanScale ("Densité des scanlines", Float) = 10
        _Alpha ("Transparence", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HolographicForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _RimPower;
                float _ScanSpeed;
                float _ScanScale;
                float _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = normal.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(position.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float facing = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - facing, _RimPower);

                float stripe = frac(input.positionWS.y * _ScanScale - _Time.y * _ScanSpeed);
                float scan = smoothstep(0.82, 0.9, stripe);

                float3 color = _BaseColor.rgb * (0.2 + fresnel * 1.8) + scan * _BaseColor.rgb;
                float alpha = saturate(_Alpha * (0.2 + fresnel) + scan * 0.7);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
