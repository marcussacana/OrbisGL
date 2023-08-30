#version 100

precision highp float;

varying highp vec2 UV;

uniform lowp vec4 Color;
uniform lowp vec4 BackColor;
uniform sampler2D Texture;
uniform vec4 VisibleRect;

void main(void) {
    vec4 Pixel = texture2D(Texture, UV);
    gl_FragColor = Pixel * Color;

    if (BackColor != vec4(0))
        gl_FragColor = mix(BackColor, gl_FragColor, gl_FragColor.a);

    if (VisibleRect != vec4(0) && (UV.x < VisibleRect.x || UV.y < VisibleRect.y || UV.x > VisibleRect.z + VisibleRect.x || UV.y > VisibleRect.w + VisibleRect.y))
        discard;
}