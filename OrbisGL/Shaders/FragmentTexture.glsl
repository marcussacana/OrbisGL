#version 100

precision highp float;

varying highp vec2 UV;

uniform sampler2D Texture;
uniform vec4 VisibleRect;

void main(void) {
    gl_FragColor = texture2D(Texture, UV);

    if ((UV.x < VisibleRect.x || UV.y < VisibleRect.y || UV.x > VisibleRect.z + VisibleRect.x || UV.y > VisibleRect.w + VisibleRect.y) && VisibleRect != vec4(0))
        discard;
}