Shader "Custom/HologramPreview"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0, 1, 0, 0.5)
        _ScanlineColor ("Scanline Color", Color) = (0, 1, 0, 1)
        _ScanSpeed ("Scan Speed", Float) = 5.0
        _ScanDensity ("Scan Density", Float) = 20.0
    }
    SubShader
    {
        // Transparent Render Queue
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            // Standard transparent blending
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            float4 _BaseColor;
            float4 _ScanlineColor;
            float _ScanSpeed;
            float _ScanDensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Passing world position to fragment to calculate scanlines mathematically instead of UVs
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Core effect: A scrolling math-based sine wave over the vertical Y axis
                float scanPattern = sin(i.worldPos.y * _ScanDensity - _Time.y * _ScanSpeed) * 0.5 + 0.5;
                
                // Extremely sharp scanlines (raise to power)
                float sharpScan = pow(scanPattern, 10.0);
                
                // Base color + glowing scan lines
                float4 finalColor = _BaseColor;
                finalColor.rgb += _ScanlineColor.rgb * sharpScan;
                
                // Add a bit of rim opacity so it looks holographic
                finalColor.a = _BaseColor.a + (sharpScan * 0.3);

                return finalColor;
            }
            ENDCG
        }
    }
}
