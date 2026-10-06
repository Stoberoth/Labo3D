// Dissolution URP, écrite en HLSL. _Dissolve va de 0 (intact) à 1 (disparu).
// Le pass d'ombre utilise le même seuil, sinon l'objet dissous garderait
// une ombre pleine.
Shader "Labo3D/Dissolve"
{
    Properties
    {
        _BaseColor ("Couleur", Color) = (0.75, 0.32, 0.18, 1)
        _Dissolve ("Dissolution", Range(0, 1)) = 0
        _EdgeWidth ("Largeur du liseré", Range(0.001, 0.3)) = 0.06
        [HDR] _EdgeColor ("Couleur du liseré", Color) = (1, 0.55, 0.1, 1)
        _NoiseScale ("Échelle du bruit", Float) = 3.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "DissolveForward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "DissolveInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS);
                output.positionCS = position.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.positionWS = position.positionWS;
                output.normalWS = normal.normalWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float cut = Labo3D_DissolveCut(input.positionOS);
                clip(cut);

                float edge = 1.0 - saturate(cut / max(_EdgeWidth, 1e-4));

                // Éclairage directionnel minimal : la normale module la couleur
                // de base, le liseré reste émissif pour rester lisible.
                float3 normalWS = normalize(input.normalWS);
                float3 lightDir = normalize(float3(0.4, 0.85, 0.3));
                float ndotl = saturate(dot(normalWS, lightDir));
                float3 lit = _BaseColor.rgb * (0.25 + ndotl * 0.75);
                float3 color = lerp(lit, _EdgeColor.rgb, edge);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DissolveShadow"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma target 3.0
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "DissolveInput.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                ShadowVaryings output;
                output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS)));
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                clip(Labo3D_DissolveCut(input.positionOS));
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
