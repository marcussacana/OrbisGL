#version 100

attribute vec3 Position;
attribute vec2 uv;
attribute vec2 uv00;
attribute vec2 uv01;
attribute vec2 uv10;
attribute vec2 uv11;

uniform vec3 Offset;
 
varying highp vec2 UV;
varying highp vec2 UV00;
varying highp vec2 UV01;
varying highp vec2 UV10;
varying highp vec2 UV11;

void main(void) {
    gl_Position = vec4(Position + Offset, 1.0);
    UV = uv;
    UV00 = uv00;
    UV01 = uv01;
    UV10 = uv10;
    UV11 = uv11;
}