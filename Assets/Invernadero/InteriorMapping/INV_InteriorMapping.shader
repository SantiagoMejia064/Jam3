Shader "Jam3/Invernadero/Interior Mapping URP"
{
    Properties
    {
        _InteriorCube("Interior HDR (Cubemap nativo)", Cube) = "black" {}
        _RoomSize("Habitacion: ancho, alto, profundidad", Vector) = (30,12,20,0)
        _Exposure("Exposicion EV", Range(-5,5)) = 0
        _Yaw("Giro cubemap (grados)", Range(-180,180)) = 0
        _DepthScale("Escala del rayo en profundidad", Float) = 1
        [Toggle] _OrthoParallax("Parallax artistico con camara ortografica", Float) = 1
        _OrthoEyeDistance("Distancia del ojo virtual ortografico", Float) = 24
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Interior"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURECUBE(_InteriorCube); SAMPLER(sampler_InteriorCube);
            CBUFFER_START(UnityPerMaterial)
                float4 _RoomSize;
                float _Exposure;
                float _Yaw;
                float _DepthScale;
                float _OrthoParallax;
                float _OrthoEyeDistance;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float2 uv:TEXCOORD1; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS); o.uv=v.uv; return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                // Perspective: camera -> surface. Orthographic: common parallel camera ray.
                // Lateral translation alone cannot create physical parallax with parallel rays.
                float3 rayWS = unity_OrthoParams.w > .5 ? -UNITY_MATRIX_V[2].xyz : i.positionWS-_WorldSpaceCameraPos;
                float3 rayOS = mul((float3x3)GetWorldToObjectMatrix(),rayWS);
                if (unity_OrthoParams.w > .5 && _OrthoParallax > .5)
                {
                    float3 eyeOS=TransformWorldToObject(_WorldSpaceCameraPos);
                    rayOS=float3(i.uv-.5-eyeOS.xy,max(_OrthoEyeDistance,.1));
                }
                clip(rayOS.z-1e-6); // the virtual room is behind the local +Z side of the quad
                float3 size=max(_RoomSize.xyz,float3(.01,.01,.01));
                float3 origin=float3((i.uv-.5)*size.xy,1e-4);
                float3 dir=normalize(rayOS*float3(size.xy,max(_DepthScale,.001)));
                dir=(step(0,dir)*2-1)*max(abs(dir),1e-6);
                float3 lo=float3(-size.xy*.5,0), hi=float3(size.xy*.5,size.z);
                float3 exitTimes=max((lo-origin)/dir,(hi-origin)/dir);
                float t=min(exitTimes.x,min(exitTimes.y,exitTimes.z));
                float3 hit=origin+dir*max(t,0);
                float3 sampleDir=hit-float3(0,0,size.z*.5);
                float a=radians(_Yaw),s=sin(a),c=cos(a);
                sampleDir.xz=float2(c*sampleDir.x+s*sampleDir.z,-s*sampleDir.x+c*sampleDir.z);
                return half4(SAMPLE_TEXTURECUBE(_InteriorCube,sampler_InteriorCube,sampleDir).rgb*exp2(_Exposure),1);
            }
            ENDHLSL
        }
    }
}
