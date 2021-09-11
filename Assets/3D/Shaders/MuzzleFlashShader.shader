Shader "Unlit/MuzzleFlashShader"
{
    Properties
    {
        _InnerColor("Inner colour of the flash", Color) = (1.0, 1.0, 1.0, 1.0)
        _OuterColor("Outer colour of the flash", Color) = (0.1, 0.1, 0.1, 1.0)
        _MuzzleFlashDirection("Direction of the muzzle flash", Vector) = (0, 0, 1)
    }
        SubShader
        {
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline"}
            LOD 100

            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                // make fog work
                #pragma multi_compile_fog

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                float4 _InnerColor, _OuterColor;
                float3 _MuzzleFlashDirection;

                // InvLerp. Thanks Freya Holmer, you're a lifesaver :P
                float inverseLerp(float val, float min, float max)
                {
                    return (val - min) / (max - min);
                }
                
                // The structure definition defines which variables it contains.
                // This example uses the Attributes structure as an input structure in
                // the vertex shader.
                struct Attributes
                {
                    // The positionOS variable contains the vertex positions in object
                    // space.
                    float4 positionOS   : POSITION;
                    float2 uv           : TEXCOORD0;
                };

                struct Varyings
                {
                    // The positions in this struct must have the SV_POSITION semantic.
                    float4 positionHCS  : SV_POSITION;
                    float2 uv : TEXCOORD0;
                };

                // The vertex shader definition with properties defined in the Varyings
                // structure. The type of the vert function must match the type (struct)
                // that it returns.
                Varyings vert(Attributes IN)
                {
                    // Declaring the output object (OUT) with the Varyings struct.
                    Varyings OUT;
                    // The TransformObjectToHClip function transforms vertex positions
                    // from object space to homogenous clip space.
                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    // Provide UV coords to the fragment shader.
                    OUT.uv = IN.uv;
                    // Returning the output.
                    return OUT;
                }

                // Muzzle flash flame colour based on distance from centre point.
                half4 flashColour(float distance, float outlineDistance, float maxDistance)
                {
                    float innerFactor = inverseLerp(maxDistance - distance, 0.0, outlineDistance);
                    float outerFactor = inverseLerp(maxDistance - distance, 0.0, maxDistance);

                    half4 innerCol = _InnerColor * half4(1, 1, 1, innerFactor);
                    half4 outerCol = _OuterColor * half4(1, 1, 1, outerFactor);

                    // Defining the color variable and returning it.
                    return lerp(outerCol, innerCol, distance > maxDistance ? 0.0 : max(innerCol.a, outerCol.a));
                }

                // Colour when muzzle flash is aligned on the Z-axis
                half4 facingFront(float2 uv)
                {
                    float dist = distance(uv, float2(0.5, 0.5));

                    return flashColour(dist, 0.1, 0.5);
                }

                // Colour when muzzle flash direction is perpendicular to camera on Z-axis (aligned on X-axis).
                half4 facingSide(float2 uv, float xOrientation)
                {
                    float sideFactor = inverseLerp(xOrientation, -1, 1);
                    sideFactor = xOrientation;

                    float dist[3] = {
                        distance(uv, float2(0.5 + 0.15 * sideFactor, 0.38)),
                        distance(uv, float2(0.5 + 0.15 * sideFactor, 0.62)),
                        distance(uv, float2(0.5 - 0.2 * sideFactor, 0.5))
                    };
                
                    // Modulate size over time.
                    float wave = lerp(0.97, 1, abs(sin(_Time * 400)));
                    float waveSmall = lerp(0.99, 1, abs(sin(_Time * 400)));

                    return 
                        flashColour(dist[0], 0.04, 0.2 * waveSmall) +
                        flashColour(dist[1], 0.04, 0.2 * waveSmall) +
                        flashColour(dist[2], 0.1, 0.5 * wave);
                }

                // The fragment shader definition.
                half4 frag(Varyings IN) : SV_Target
                {
                    // Ignore Y-axis as we want facingSide to also apply when viewed from above.
                    float3 camDir = float3(UNITY_MATRIX_V[0][0], 0, UNITY_MATRIX_V[2][0]);

                    // Get XZ-plane angle.
                    float orientation = dot(camDir, _MuzzleFlashDirection);

                    // Determine final colour.
                    half4 muzzleFlash = lerp(facingSide(IN.uv, -orientation), facingFront(IN.uv), 1 - abs(orientation));

                    // Modulate alpha over time to allow slight flicker in full-auto/burst.
                    muzzleFlash.a *= lerp(0.1, 0.3, max(0, sin(_Time * 800)));

                    return muzzleFlash;
                }
            ENDHLSL
        }
    }
}
