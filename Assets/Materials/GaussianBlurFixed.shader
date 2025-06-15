Shader "Custom/GaussianBlurFixed"
{
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurDir ("Blur Direction", Vector) = (1,0,0,0)
        _TexelSize ("Texel Size", Vector) = (1,1,0,0)
    }

    SubShader {
        Pass {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _BlurDir;
            float4 _TexelSize;

            // max kernel size 15 (i.e., 15x15)
            float _Weights[15];
            int _KernelSize;

            fixed4 frag(v2f_img i) : SV_Target {
                int halfSize = _KernelSize / 2;
                float2 uv = i.uv;
                float2 offset = _BlurDir.xy * _TexelSize.xy;

                fixed4 sum = tex2D(_MainTex, uv) * _Weights[0];

                for (int i = 1; i <= halfSize; ++i) {
                    float2 ofs = offset * i;
                    float w = _Weights[i];
                    sum += tex2D(_MainTex, uv + ofs) * w;
                    sum += tex2D(_MainTex, uv - ofs) * w;
                }
                return sum;
            }
            ENDCG
        }
    }
}
