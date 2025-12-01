// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "GAPH Custom Shader/Integrated Particle/Integrated Particle (ColorControl)"{
	Properties{
		[HDR]_TintColor("For Red Color",Color) = (1,1,1,1)
		_TintColorFactor("Tint Color Factor",float) = 1.0
		[HDR]_TintColor2("For Green Color",Color) = (1,1,1,1)
		_TintColorFactor2("Tint Color2 Factor",float) = 1.0
		[HDR]_TintColor3("For Blue Color",Color) = (1,1,1,1)
		_TintColorFactor3("Tint Color3 Factor",float) = 1.0
		_MainTex("Particle Texture", 2D) = "white" {}
      
        [Enum(UnityEngine.Rendering.BlendMode)]_BlendSrc ("BlendSrc", float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_BlendDst ("BlendDst", float) = 1

        _InvFade("Soft Particle Factor", Range(0.01,3.0)) = 1.0
	}
		Category{
				Tags{"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
				Blend [_BlendSrc] [_BlendDst]
				Cull Off
				Lighting Off
				ZWrite Off

				SubShader {
					Pass{
						CGPROGRAM
						#pragma vertex vert
						#pragma fragment frag
						#pragma target 3.0
						#pragma multi_compile_particles
						#pragma multi_compile_fog

						#include "UnityCG.cginc"

						sampler2D _MainTex;

						fixed4 _TintColor;
						fixed4 _TintColor2;
						fixed4 _TintColor3;

						float _TintColorFactor;
						float _TintColorFactor2;
						float _TintColorFactor3;

						struct appdata_t {
							half4 vertex : POSITION;
							half4 color : COLOR;
							half2 texcoord : TEXCOORD0;
						};

						struct v2f {
							half4 vertex : SV_POSITION;
							half4 color : COLOR;
							half2 texcoord : TEXCOORD0;
							UNITY_FOG_COORDS(1)
							#ifdef SOFTPARTICLES_ON
								half4 projPos : TEXCOORD2;
							#endif
						};

						float4 _MainTex_ST;

						v2f vert(appdata_t v)
						{
							v2f o;
							o.vertex = UnityObjectToClipPos(v.vertex);
							
							#ifdef SOFTPARTICLES_ON
								o.projPos = ComputeScreenPos(o.vertex);
								COMPUTE_EYEDEPTH(o.projPos.z);
							#endif
							o.color = v.color;
							o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
							UNITY_TRANSFER_FOG(o, o.vertex);
							return o;
						}
						
						sampler2D _CameraDepthTexture;
						half _InvFade;

						half4 frag(v2f i ) : SV_Target
						{
                           #ifdef SOFTPARTICLES_ON
								half sceneZ = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture,UNITY_PROJ_COORD(i.projPos))));
								half partZ = i.projPos.z;
								half fade = saturate(_InvFade * (sceneZ - partZ));
                                i.color.a *= fade;
                            #endif

							half4 tex = tex2D(_MainTex,i.texcoord);
							half4 color = half4(0, 0, 0, 0);
							
							//Compose each color data using color info and color factor
							color += (tex.r) *_TintColor *_TintColorFactor *0.75f;
							color += (tex.g) *_TintColor2 * _TintColorFactor2 *0.75f;
							color += (tex.b) *_TintColor3 * _TintColorFactor3 *0.75f;
							
							//if(tex.a == 0)
								color.a = tex.a;

							half4 res = i.color * clamp(color,0,255);
							UNITY_APPLY_FOG_COLOR(i.fogCoord, res, fixed4(0, 0, 0, 0));
							return res;
                            
						}
						ENDCG
				}
			}
	}
}