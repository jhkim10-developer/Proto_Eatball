Shader "Custom/URP/OilSplash_SimpleIntersection"
{
    Properties
    {
        [Header(Main Settings)]
        _BaseColor ("Base Color", Color) = (1, 0.95, 0.8, 0.7)
        _EmissionStrength ("Emission Strength", Range(0, 2)) = 0.3
        
        [Header(Animation)]
        _AnimationSpeed ("Animation Speed", Range(0, 5)) = 1.0
        _SplashIntensity ("Splash Intensity", Range(0, 2)) = 1.0
        
        [Header(Pattern)]
        _CellDensity ("Oil Bubble Density", Range(5, 50)) = 20.0
        _Temperature ("Temperature", Range(0, 1)) = 0.5
        _PatternSoftness ("Pattern Softness", Range(0.1, 1.0)) = 0.5
        _SmallBubbleOpacity ("Small Bubble Opacity", Range(0, 1)) = 0.6
        _ParticleOpacity ("Particle Opacity", Range(0, 1)) = 0.4
        
        [Header(Pattern Sizes)]
        _SmallBubbleSize ("Small Bubble Size", Range(0.5, 3.0)) = 1.0
        _ParticleSize ("Particle Size", Range(0.5, 5.0)) = 1.0
        _PopSize ("Pop Effect Size", Range(0.5, 3.0)) = 1.0
        _MicroTextureSize ("Micro Texture Size", Range(0.5, 4.0)) = 1.0
        
        [Header(Edge Effect)]
        _EdgeColor ("Edge Color", Color) = (1, 1, 1, 1)
        _EdgeIntensity ("Edge Intensity", Range(0, 3)) = 1.0
        _EdgeWidth ("Edge Width", Range(0.01, 0.3)) = 0.1
        _EdgePulse ("Edge Pulse Speed", Range(0, 5)) = 2.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
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
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _EmissionStrength;
                float _AnimationSpeed;
                float _SplashIntensity;
                float _CellDensity;
                float _Temperature;
                float _PatternSoftness;
                float _SmallBubbleOpacity;
                float _ParticleOpacity;
                float _SmallBubbleSize;
                float _ParticleSize;
                float _PopSize;
                float _MicroTextureSize;
                float4 _EdgeColor;
                float _EdgeIntensity;
                float _EdgeWidth;
                float _EdgePulse;
            CBUFFER_END
            
            float hash21(float2 p)
            {
                p = frac(p * float2(233.34, 851.73));
                p += dot(p, p + 23.45);
                return frac(p.x * p.y);
            }
            
            float2 hash22(float2 p)
            {
                float n = sin(dot(p, float2(41, 289)));
                return frac(float2(2097152, 262144) * n);
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
            
            float voronoi(float2 uv)
            {
                float2 gv = frac(uv) - 0.5;
                float2 id = floor(uv);
                float minDist = 1.0;
                
                for(int y = -1; y <= 1; y++)
                {
                    for(int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y);
                        float2 n = hash22(id + offset);
                        float pulseSpeed = 2.0 + n.x * 3.0;
                        float pulse = sin(_TimeParameters.x * pulseSpeed + n.y * 6.28) * 0.3 + 0.7;
                        float2 p = offset + n * pulse;
                        float d = length(gv - p);
                        minDist = min(d, minDist);
                    }
                }
                
                return minDist;
            }
            
            float bubblePattern(float2 uv, float time)
            {
                // 기본 방울 패턴 (여러 스케일) - 부드러움 조절 가능
                float bubbles1 = voronoi(uv * _CellDensity + time * 0.1);
                float bubbles2 = voronoi(uv * _CellDensity * 0.7 + time * 0.15);
                float bubbles3 = voronoi(uv * _CellDensity * 1.3 + time * 0.08);
                
                // 부드러움 정도 조절
                float softness = _PatternSoftness;
                bubbles1 = 1.0 - smoothstep(0.0, 0.3 * softness, bubbles1);
                bubbles2 = 1.0 - smoothstep(0.0, 0.4 * softness, bubbles2);
                bubbles3 = 1.0 - smoothstep(0.0, 0.2 * softness, bubbles3);
                
                float baseBubbles = max(max(bubbles1, bubbles2 * 0.7), bubbles3 * 0.5);
                
                // 팝핑 효과 (크기 조절 가능)
                float popScale = 25.0 / _PopSize; // 사이즈가 클수록 스케일은 작아짐
                float popNoise = smoothNoise(uv * popScale + time * 2.0);
                float pops = smoothstep(0.8, 1.0, popNoise) * (sin(time * 10.0 + popNoise * 20.0) * 0.5 + 0.5);
                pops *= _SplashIntensity * 0.8;
                
                // 작은 기포들 (크기 조절 가능)
                float smallScale1 = 40.0 / _SmallBubbleSize;
                float smallScale2 = 60.0 / _SmallBubbleSize;
                
                float smallBubbles1 = smoothNoise(uv * smallScale1 + time * 0.8);
                float smallBubbles2 = smoothNoise(uv * smallScale2 + time * 1.2 + 100.0);
                
                // 더 부드러운 경계
                smallBubbles1 = smoothstep(0.55, 0.75, smallBubbles1) * _SmallBubbleOpacity;
                smallBubbles2 = smoothstep(0.65, 0.85, smallBubbles2) * _SmallBubbleOpacity * 0.8;
                
                // 버져나가는 파티클 효과 (크기 조절 가능)
                float particleScale = 80.0 / _ParticleSize;
                float particles = smoothNoise(uv * particleScale + time * 3.0);
                particles = smoothstep(0.75, 0.95, particles) * (sin(time * 15.0 + particles * 30.0) * 0.3 + 0.4);
                particles *= _ParticleOpacity * _SplashIntensity;
                
                // 추가 미세 텍스처 (크기 조절 가능)
                float microScale = 120.0 / _MicroTextureSize;
                float microTexture = smoothNoise(uv * microScale + time * 1.5);
                microTexture = smoothstep(0.7, 1.0, microTexture) * _ParticleOpacity * 0.3;
                
                // 중간 크기 불규칙 패턴 추가 (새로운 레이어)
                float mediumScale = 30.0 / (_SmallBubbleSize * 0.7 + _ParticleSize * 0.3);
                float mediumPattern = smoothNoise(uv * mediumScale + time * 1.8);
                mediumPattern = smoothstep(0.6, 0.9, mediumPattern) * _SmallBubbleOpacity * 0.6;
                
                // 큰 불규칙 패턴 (배경 텍스처)
                float largeScale = 15.0 / _PopSize;
                float largePattern = smoothNoise(uv * largeScale + time * 0.5);
                largePattern = smoothstep(0.4, 0.8, largePattern) * _ParticleOpacity * 0.4;
                
                // 모든 효과 조합
                return baseBubbles + pops + smallBubbles1 + smallBubbles2 + particles + microTexture + mediumPattern + largePattern;
            }
            
            float getEdgeGlow(float2 uv, float time)
            {
                // UV 가장자리로부터의 거리
                float2 center = abs(uv - 0.5);
                float distFromEdge = 0.5 - max(center.x, center.y);
                
                // 가장자리 근처에서 글로우 효과
                float edge = 1.0 - smoothstep(0.0, _EdgeWidth, distFromEdge);
                
                // 시간 기반 펄스 효과
                float pulse = sin(time * _EdgePulse + uv.x * 20.0 + uv.y * 20.0) * 0.5 + 0.5;
                edge *= (0.5 + pulse * 0.5);
                
                return edge;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionHCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.uv;
                float time = _TimeParameters.x * _AnimationSpeed;
                
                // UV에 미세한 왜곡 추가 (더 자연스러운 움직임)
                float2 distortion = float2(
                    smoothNoise(uv * 12.0 + time * 0.3),
                    smoothNoise(uv * 12.0 + time * 0.4 + 100.0)
                ) * 0.01;
                
                float2 animatedUV = uv + distortion;
                
                // 기름 자글자글 효과 (향상된 버져나가는 효과 포함)
                float bubbles = bubblePattern(animatedUV + float2(sin(time * 0.3), cos(time * 0.4)) * 0.02, time);
                
                // 가장자리 글로우 효과 (교차점 시뮬레이션)
                float edgeGlow = getEdgeGlow(uv, time * _EdgePulse);
                
                // 온도에 따른 색상 변화
                half3 coldColor = half3(1.0, 0.95, 0.8);
                half3 hotColor = half3(1.0, 0.8, 0.6);
                half3 veryHotColor = half3(1.0, 0.6, 0.3);
                
                half3 oilColor;
                if(_Temperature < 0.5)
                {
                    oilColor = lerp(coldColor, hotColor, _Temperature * 2.0);
                }
                else
                {
                    oilColor = lerp(hotColor, veryHotColor, (_Temperature - 0.5) * 2.0);
                }
                
                // 최종 색상 계산
                half3 finalColor = oilColor * _BaseColor.rgb;
                
                // 발광 효과 (버져나가는 효과도 밝게, 부드럽게 조절)
                half3 emission = finalColor * _EmissionStrength * bubbles * (1.0 + _Temperature * 0.5);
                
                // 온도가 높을 때 추가 스파클 효과 (더 부드럽게)
                if(_Temperature > 0.5)
                {
                    float sparkle = smoothNoise(animatedUV * 100.0 + time * 5.0);
                    sparkle = smoothstep(0.85, 1.0, sparkle) * (_Temperature - 0.5) * 2.0;
                    emission += half3(1, 1, 0.8) * sparkle * 0.3 * _ParticleOpacity;
                }
                
                finalColor += emission;
                
                // 가장자리 하이라이트 추가
                finalColor = lerp(finalColor, _EdgeColor.rgb, edgeGlow * _EdgeIntensity);
                finalColor += _EdgeColor.rgb * edgeGlow * _EdgeIntensity * 0.3;
                
                // 최종 알파 계산 (부드러운 블렌딩)
                half alpha = (bubbles + edgeGlow * 0.7) * _BaseColor.a;
                alpha *= (1.0 + _Temperature * 0.5);
                
                // 부드러운 알파 처리
                alpha = smoothstep(0.05, 0.8, alpha);
                alpha = max(alpha, bubbles * 0.15); // 최소 가시성 보장
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}