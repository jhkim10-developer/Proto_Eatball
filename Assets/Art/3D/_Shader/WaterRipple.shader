Shader "Custom/URP/WaterRipple"
{
    Properties
    {
        [Header(Water Base)]
        _WaterColor ("Water Color", Color) = (0.2, 0.6, 1.0, 0.8)
        _DeepWaterColor ("Deep Water Color", Color) = (0.1, 0.3, 0.8, 1.0)
        _Transparency ("Transparency", Range(0, 1)) = 0.8
        _Depth ("Water Depth", Range(0.1, 5.0)) = 1.0
        
        [Header(Surface Waves)]
        _WaveSpeed ("Wave Speed", Range(0, 3)) = 1.0
        _WaveScale ("Wave Scale", Range(0.1, 5.0)) = 1.0
        _WaveHeight ("Wave Height", Range(0, 0.5)) = 0.1
        _WaveFrequency ("Wave Frequency", Range(1, 10)) = 3.0
        
        [Header(Surface Ripples)]
        _RippleScale ("Ripple Scale", Range(0.5, 10.0)) = 2.0
        _RippleSpeed ("Ripple Speed", Range(0, 5)) = 2.0
        _RippleIntensity ("Ripple Intensity", Range(0, 1)) = 0.3
        
        [Header(Refraction)]
        _RefractionStrength ("Refraction Strength", Range(0, 0.2)) = 0.05
        _RefractionSpeed ("Refraction Speed", Range(0, 3)) = 1.5
        
        [Header(Surface Effects)]
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2.0
        _ReflectionIntensity ("Reflection Intensity", Range(0, 1)) = 0.3
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamIntensity ("Foam Intensity", Range(0, 2)) = 0.5
        
        [Header(Animation)]
        _FlowDirection ("Flow Direction", Vector) = (1, 0, 0, 0)
        _FlowSpeed ("Flow Speed", Range(0, 2)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float3 viewDirWS    : TEXCOORD3;
                float4 screenPos    : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                half4 _WaterColor;
                half4 _DeepWaterColor;
                half _Transparency;
                half _Depth;
                half _WaveSpeed;
                half _WaveScale;
                half _WaveHeight;
                half _WaveFrequency;
                half _RippleScale;
                half _RippleSpeed;
                half _RippleIntensity;
                half _RefractionStrength;
                half _RefractionSpeed;
                half _FresnelPower;
                half _ReflectionIntensity;
                half4 _FoamColor;
                half _FoamIntensity;
                float4 _FlowDirection;
                half _FlowSpeed;
            CBUFFER_END
            
            // 노이즈 함수들
            float hash21(float2 p)
            {
                p = frac(p * float2(233.34, 851.73));
                p += dot(p, p + 23.45);
                return frac(p.x * p.y);
            }
            
            float smoothNoise(float2 uv)
            {
                float2 lv = frac(uv);
                float2 id = floor(uv);
                lv = lv * lv * (3.0 - 2.0 * lv);
                
                float bl = hash21(id);
                float br = hash21(id + float2(1, 0));
                float b = lerp(bl, br, lv.x);
                
                float tl = hash21(id + float2(0, 1));
                float tr = hash21(id + float2(1, 1));
                float t = lerp(tl, tr, lv.x);
                
                return lerp(b, t, lv.y);
            }
            
            // 물결 계산
            float getWaves(float2 uv, float time)
            {
                float2 flowUV = uv + _FlowDirection.xy * time * _FlowSpeed;
                
                // 여러 스케일의 파도 조합
                float waves = 0.0;
                waves += sin(flowUV.x * _WaveFrequency + time * _WaveSpeed) * 0.5;
                waves += sin(flowUV.y * _WaveFrequency * 0.7 + time * _WaveSpeed * 1.3) * 0.3;
                waves += sin((flowUV.x + flowUV.y) * _WaveFrequency * 1.5 + time * _WaveSpeed * 0.8) * 0.2;
                
                return waves * _WaveHeight;
            }
            
            // 잔물결 계산
            float getRipples(float2 uv, float time)
            {
                float2 rippleUV = uv * _RippleScale;
                
                // 다방향 잔물결
                float ripples = 0.0;
                ripples += smoothNoise(rippleUV + time * _RippleSpeed) * 0.5;
                ripples += smoothNoise(rippleUV * 1.5 + time * _RippleSpeed * 1.2 + 100.0) * 0.3;
                ripples += smoothNoise(rippleUV * 2.3 + time * _RippleSpeed * 0.8 + 200.0) * 0.2;
                
                return (ripples - 0.5) * _RippleIntensity;
            }
            
            // 굴절 효과 계산
            float2 getRefraction(float2 uv, float time)
            {
                float2 refractionUV = uv * 3.0;
                
                float2 refraction = float2(
                    smoothNoise(refractionUV + time * _RefractionSpeed) - 0.5,
                    smoothNoise(refractionUV + time * _RefractionSpeed * 1.3 + 150.0) - 0.5
                );
                
                return refraction * _RefractionStrength;
            }
            
            // 거품 효과 (반짝임 제거된 버전)
            float getFoam(float2 uv, float time, float waveHeight)
            {
                // 파도 정점에서만 거품 생성 (더 부드럽게)
                float foam = smoothstep(0.7, 0.9, waveHeight + 0.5);
                
                // 부드러운 거품 텍스처 (반짝임 없음)
                float foamNoise = smoothNoise(uv * 15.0 + time * 1.0);
                foamNoise *= smoothNoise(uv * 8.0 + time * 0.5);
                foamNoise = smoothstep(0.3, 0.7, foamNoise); // 부드러운 경계
                
                return foam * foamNoise * _FoamIntensity * 0.5; // 강도 줄임
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                float time = _TimeParameters.x;
                
                // 버텍스에 물결 적용
                float waves = getWaves(input.uv, time);
                float ripples = getRipples(input.uv, time);
                
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                worldPos.y += waves + ripples;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(TransformWorldToObject(worldPos));
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.positionWS = worldPos;
                output.uv = input.uv;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(worldPos);
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.uv;
                float time = _TimeParameters.x;
                
                // 파도와 잔물결
                float waves = getWaves(uv, time);
                float ripples = getRipples(uv, time);
                float totalWaves = waves + ripples;
                
                // 굴절된 배경 샘플링
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 refraction = getRefraction(uv, time);
                float2 distortedScreenUV = screenUV + refraction;
                
                // 배경 색상 (굴절 효과)
                half3 refractionColor = SampleSceneColor(distortedScreenUV);
                
                // 물 색상 (깊이에 따른 색상 변화)
                float depthFactor = saturate(_Depth * 0.5);
                half3 waterColor = lerp(_WaterColor.rgb, _DeepWaterColor.rgb, depthFactor);
                
                // 노멀 계산 (부드럽게 조정)
                float2 normalOffset = float2(
                    getWaves(uv + float2(0.01, 0), time) - getWaves(uv - float2(0.01, 0), time),
                    getWaves(uv + float2(0, 0.01), time) - getWaves(uv - float2(0, 0.01), time)
                ) * 10.0; // 강도 줄임
                
                float3 normalWS = normalize(input.normalWS + float3(normalOffset.x, 0, normalOffset.y) * 0.5); // 노멀 변화 줄임
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // 프레넬 효과
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                
                // 반사 효과 (간단한 스카이박스 색상)
                half3 reflectionColor = half3(0.5, 0.7, 1.0); // 하늘색
                
                // 거품 효과
                float foam = getFoam(uv, time, totalWaves);
                
                // 최종 색상 조합 (반짝임 제거)
                half3 finalColor = lerp(refractionColor, waterColor, _Transparency);
                finalColor = lerp(finalColor, reflectionColor, fresnel * _ReflectionIntensity * 0.5); // 반사 줄임
                finalColor = lerp(finalColor, _FoamColor.rgb, foam * 0.3); // 거품 강도 줄임
                
                // 부드러운 밝기 변화 (급격한 변화 제거)
                float brightness = 1.0 + totalWaves * 0.1; // 밝기 변화 줄임
                finalColor *= brightness;
                
                // 최종 알파 (부드럽게 조정)
                half alpha = lerp(_Transparency, 1.0, fresnel * 0.2 + foam * 0.1); // 변화량 줄임
                alpha = saturate(alpha); // 범위 제한
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}