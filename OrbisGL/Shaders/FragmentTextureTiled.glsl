#version 100

precision highp float;

varying highp vec2 UV;
varying highp vec2 UV00;
varying highp vec2 UV01;
varying highp vec2 UV10;
varying highp vec2 UV11;

//                TextureXY
uniform sampler2D Texture00;
uniform sampler2D Texture01;
uniform sampler2D Texture10;
uniform sampler2D Texture11;

uniform vec4 VisibleRect;
uniform vec4 Color;
uniform bool Mirror;

void main(void) {    
    vec2 UVMod = Mirror ? vec2(-1, 1) * UV : UV;
    
    if (UVMod.x > 0.5 && UVMod.y > 0.5)
        gl_FragColor = texture2D(Texture11, Mirror ? vec2(-1, 1) * UV11 : UV11);    
    else if (UVMod.x > 0.5)
        gl_FragColor = texture2D(Texture10, Mirror ? vec2(-1, 1) * UV10 : UV10);
    else if (UVMod.y > 0.5)
        gl_FragColor = texture2D(Texture01, Mirror ? vec2(-1, 1) * UV01 : UV01);
    else
        gl_FragColor = texture2D(Texture00, Mirror ? vec2(-1, 1) * UV00 : UV00);
    
    /*
    //Debug Global UV
    if (UVMod.x > 0.5 && UVMod.y > 0.5)
        gl_FragColor = vec4(vec3(UV, 1.), 1.);    
    else if (UVMod.x > 0.5)
        gl_FragColor = vec4(vec3(UV, 1.), 1.);
    else if (UVMod.y > 0.5)
        gl_FragColor = vec4(vec3(UV, 1.), 1.);
    else
        gl_FragColor = vec4(vec3(UV, 1.), 1.);
   */

    /*
    //Debug Local UV
    if (UVMod.x > 0.5 && UVMod.y > 0.5)
        gl_FragColor = vec4(vec3(UV11, 1.), 1.);    
    else if (UVMod.x > 0.5)
        gl_FragColor = vec4(vec3(UV10, 1.), 1.);
    else if (UVMod.y > 0.5)
        gl_FragColor = vec4(vec3(UV01, 1.), 1.);
    else
        gl_FragColor = vec4(vec3(UV00, 1.), 1.);
   */

    gl_FragColor.rgb *= Color.rgb;
    gl_FragColor.a *= Color.a;

    if ((UV.x < VisibleRect.x || UV.y < VisibleRect.y || UV.x > VisibleRect.z + VisibleRect.x || UV.y > VisibleRect.w + VisibleRect.y) && VisibleRect != vec4(0))
        discard;
}