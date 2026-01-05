Shader "Custom/SimpleArrowMoving"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        _ArrowDirection ("Arrow Direction", Vector) = (-1, 0, 0, 0)
        _MoveSpeed ("Move Speed", Range(0, 5)) = 1.0
        _TileScale ("Tile Scale", Range(0.1, 10)) = 1.0
        
        [Header(Emission)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission Texture", 2D) = "white" {}
        _EmissionTileScale ("Emission Tile Scale", Range(0.1, 10)) = 1.0
        [Toggle] _UseEmissionMovement ("Move Emission with Arrow", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _EmissionMap_ST;
                half4 _BaseColor;
                float4 _ArrowDirection;
                float _MoveSpeed;
                float _TileScale;
                half4 _EmissionColor;
                float _EmissionTileScale;
                float _UseEmissionMovement;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = normalInput.normalWS;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // 화살표 방향으로 움직이는 UV 계산
                float2 arrowDir = normalize(_ArrowDirection.xy);
                float timeOffset = _Time.y * _MoveSpeed;
                
                // 메인 텍스처 UV를 화살표 방향으로 이동
                float2 animatedUV = input.uv * _TileScale;
                animatedUV += arrowDir * timeOffset;
                
                // 이미션 텍스처 UV 계산
                float2 emissionUV = TRANSFORM_TEX(input.uv, _EmissionMap) * _EmissionTileScale;
                if (_UseEmissionMovement > 0.5)
                {
                    // 이미션도 같이 움직임
                    emissionUV += arrowDir * timeOffset;
                }
                
                // 텍스처 샘플링
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, animatedUV);
                col *= _BaseColor;
                
                // 이미션 텍스처 샘플링
                half4 emissionTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, emissionUV);
                half3 emission = emissionTex.rgb * _EmissionColor.rgb;
                
                // 간단한 라이팅
                Light mainLight = GetMainLight();
                half3 lightColor = mainLight.color;
                half3 lightDir = mainLight.direction;
                
                half NdotL = saturate(dot(input.normalWS, lightDir));
                half3 lighting = lightColor * NdotL + unity_AmbientSky.rgb;
                
                col.rgb *= lighting;
                col.rgb += emission; // 이미션 추가
                
                // 포그 적용
                col.rgb = MixFog(col.rgb, input.fogFactor);
                
                return col;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}