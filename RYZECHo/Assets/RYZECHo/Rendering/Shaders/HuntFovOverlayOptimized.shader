Shader "RYZECHo/FovOverlayOptimized"
{
    Properties
    {
        _CenterColor ("Center Color", Color) = (0.58, 0.92, 1.0, 0.15)
        _EdgeColor   ("Edge Color",   Color) = (0.25, 0.85, 1.0, 0.04)
        _DarknessColor ("Darkness Color", Color) = (0.01, 0.02, 0.035, 0.82)
        _InnerRadius ("Inner Radius", Float) = 2.9
        _OuterRadius ("Outer Radius", Float) = 5.0
        _FovAngle    ("FOV Angle",    Float) = 100.0
        _DarknessRadius ("Darkness Radius", Float) = 32.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CenterColor;
                half4 _EdgeColor;
                half4 _DarknessColor;
                float _InnerRadius;
                float _OuterRadius;
                float _FovAngle;
                float _DarknessRadius;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 barycentric : BARYCENTRIC;
                half4 vertexColor : COLOR;
            };

            // Convert local position to polar coordinates
            // Returns (radius, angleInDegrees) where angle is measured from +X axis
            static float2 LocalToPolar(float2 uv)
            {
                float r = length(uv);
                float a = atan2(uv.y, uv.x) * 180.0 / UNITY_PI;
                return float2(r, a);
            }

            // Compute the half-angle of the FOV cone
            static float ComputeHalfAngle()
            {
                return _FovAngle * 0.5;
            }

            // Check if a local position is inside the FOV cone
            // The cone is centered on +X axis, spanning [-halfAngle, +halfAngle]
            static bool IsInsideFovCone(float2 uv, float halfAngle)
            {
                float angle = atan2(uv.y, uv.x) * 180.0 / UNITY_PI;
                // Normalize angle to [-180, 180]
                if (angle > 180.0) angle -= 360.0;
                if (angle < -180.0) angle += 360.0;
                return abs(angle) <= halfAngle;
            }

            // Smooth edge falloff for anti-aliasing
            static float SmoothEdge(float value, float edge, float softness)
            {
                return smoothstep(edge - softness, edge + softness, value);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                // Transform to clip space
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                // Compute barycentric coordinates for GPU-driven rendering support
                output.barycentric = ComputeBarycentric(input.positionOS.xyz, output.positionCS);

                // Pre-compute vertex color in the vertex shader:
                // Compute distance from center in object space
                float dist = length(input.positionOS.xy);

                // Compute angle for FOV check
                float angle = atan2(input.positionOS.y, input.positionOS.x) * 180.0 / UNITY_PI;

                // Determine if this vertex is inside the FOV cone
                float halfAngle = _FovAngle * 0.5;
                bool insideCone = abs(angle) <= halfAngle;

                // Determine if inside the outer radius
                bool insideOuter = dist <= _OuterRadius;

                // Determine if inside the darkness radius
                bool insideDarkness = dist <= _DarknessRadius;

                // Pre-compute color based on position
                // For vertices: use a blended approach
                if (!insideOuter)
                {
                    // Outside outer radius: fully dark
                    output.vertexColor = half4(0.0, 0.0, 0.0, 0.0);
                }
                else if (!insideCone && insideDarkness)
                {
                    // Inside darkness radius but outside FOV cone: darkness color
                    output.vertexColor = half4(_DarknessColor.rgb, _DarknessColor.a);
                }
                else if (insideCone)
                {
                    // Inside FOV cone: interpolate between center and edge colors
                    // Normalized distance for gradient
                    float t = saturate(dist / _OuterRadius);
                    half4 color = lerp(_CenterColor, _EdgeColor, t);
                    output.vertexColor = color;
                }
                else
                {
                    // Default: white
                    output.vertexColor = half4(1.0, 1.0, 1.0, 1.0);
                }

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // For transparency with URP 2D Renderer, we use the pre-computed vertex color
                // and apply fragment-level FOV checks for accuracy

                // Get local position from clip space (approximate)
                // We use barycentric interpolation of vertex positions instead
                float2 localPos = lerp_3d(input.positionCS, input.barycentric);

                // Compute polar coordinates in fragment shader for precise FOV check
                float dist = length(localPos);
                float angle = atan2(localPos.y, localPos.x) * 180.0 / UNITY_PI;

                // Normalize angle to [-180, 180]
                if (angle > 180.0) angle -= 360.0;
                if (angle < -180.0) angle += 360.0;

                float halfAngle = _FovAngle * 0.5;

                // Alpha clipping: skip fully transparent fragments
                // This optimizes by not writing to the framebuffer for transparent areas
                if (input.vertexColor.a < 0.01)
                {
                    discard;
                }

                // Determine region and compute final color
                half4 finalColor;

                if (dist > _OuterRadius)
                {
                    // Outside outer radius: fully transparent (discard handled above)
                    discard;
                }
                else if (dist > _InnerRadius && dist <= _OuterRadius)
                {
                    // Inner ring: darkness gradient from inner to outer edge
                    float t = (dist - _InnerRadius) / (_OuterRadius - _InnerRadius);
                    half4 innerDark = half4(_DarknessColor.rgb, 0.0);
                    half4 outerDark = half4(_DarknessColor.rgb, _DarknessColor.a);
                    finalColor = lerp(innerDark, outerDark, t);
                }
                else if (dist <= _InnerRadius && abs(angle) <= halfAngle)
                {
                    // Inside inner radius and inside FOV cone: center color
                    finalColor = _CenterColor;
                }
                else if (dist <= _InnerRadius && abs(angle) > halfAngle)
                {
                    // Inside inner radius but outside FOV cone: darkness
                    finalColor = half4(_DarknessColor.rgb, _DarknessColor.a);
                }
                else
                {
                    // Inside FOV cone, between inner and outer radius
                    float t = (dist - _InnerRadius) / (_OuterRadius - _InnerRadius);
                    half4 centerGrad = lerp(_CenterColor, _EdgeColor, t);
                    half4 darkGrad = lerp(half4(_DarknessColor.rgb, 0.0), _DarknessColor, t);

                    // Blend based on FOV angle proximity
                    float fovProximity = 1.0 - saturate(abs(angle) / halfAngle);
                    finalColor = lerp(darkGrad, centerGrad, fovProximity);
                }

                return finalColor;
            }
            ENDHLSL
        }
    }
}
