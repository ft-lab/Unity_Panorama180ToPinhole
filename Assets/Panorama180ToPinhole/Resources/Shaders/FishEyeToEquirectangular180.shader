//----------------------------------------------------------------.
// FishEye to Equirectangular.
//----------------------------------------------------------------.
Shader "Hidden/Panorama180ToPinhole/FishEyeToEquirectangular180"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        ZTest Always
        Cull Off
        ZWrite Off
        Fog { Mode Off }
        Tags { "RenderType"="Opaque" "Queue"="geometry-100" }

        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            #pragma target 3.0

            #define UNITY_PI2 (UNITY_PI * 2.0)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            int _FishEye = 1;			  // Use Fish Eye.
            float4 _BackgroundColor;      // Background color.
            float _TextureAspect = 1.0;   // width / height.
            int _IsSBS = 0;				  // 0 : Two eyes, 1: Single eye.
            float _CameraFOVH = 180.0;    // Camera FOV(H).
            float _CameraFOVV = 180.0;    // Camera FOV(V).

            v2f vert (appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
                return o;
            }

            // Fisheye to Equirectangular.
            float2 convFishEyeToEquirectangular (float2 uv) {
                float2 uv2 = uv;

                // reference : http://paulbourke.net/dome/fish2/
                float theta = UNITY_PI * (uv2.x - 0.5);
                float phi   = UNITY_PI * (uv2.y - 0.5);
                float sinP = sin(phi);
                float cosP = cos(phi);
                float sinT = sin(theta);
                float cosT = cos(theta);
                float3 vDir = float3(cosP * sinT, cosP * cosT, sinP);

                theta = atan2(vDir.z, vDir.x);
                phi   = atan2(sqrt(vDir.x * vDir.x + vDir.z * vDir.z), vDir.y);
                float r = phi / UNITY_PI; 

                uv2.x = 0.5 + r * cos(theta);
                uv2.y = 0.5 + r * sin(theta);

                return uv2;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float4 col = _BackgroundColor;

                if (_IsSBS == 0) uv.x *= 0.5;

                // Fisheye lens correction.
                if (_FishEye == 1) {
                    uv = convFishEyeToEquirectangular(uv);

                    if (abs(_CameraFOVH - 180.0) > 1e-5 || abs(_CameraFOVV - 180.0) > 1e-5) {
                        float centerU = 0.5;
                        float centerV = 0.5;
                        float mx = 180.0 / _CameraFOVH;
                        float my = 180.0 / _CameraFOVV;

                        uv.x = (uv.x - centerU) * mx + centerU;
                        uv.y = (uv.y - centerV) * my + centerV;
                    } else {
                        // For true fisheye.
                        uv.y = (uv.y - 0.5) * _TextureAspect + 0.5;
                    }
                }
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return col;

                col = tex2D(_MainTex, uv);
                return col;
            }
            ENDCG
        }
    }
}
