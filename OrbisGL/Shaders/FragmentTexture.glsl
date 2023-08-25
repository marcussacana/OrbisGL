#version 100

precision highp float;

varying highp vec2 UV;

uniform sampler2D Texture;
uniform vec4 VisibleRect;
uniform vec4 Color;
uniform bool Mirror;

void main(void) {

    if (Mirror)
        gl_FragColor = texture2D(Texture, vec2(-1, 1) * UV);
    else
        gl_FragColor = texture2D(Texture, UV);

    gl_FragColor.rgb *= Color.rgb;
    gl_FragColor.a *= Color.a;

    if ((UV.x < VisibleRect.x || UV.y < VisibleRect.y || UV.x > VisibleRect.z + VisibleRect.x || UV.y > VisibleRect.w + VisibleRect.y) && VisibleRect != vec4(0))
        discard;
}