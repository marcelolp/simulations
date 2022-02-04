#version 460 core

out vec4 output_color;

in vec3 vertex_color;

uniform float time;

void main() 
{
	output_color = vec4(vertex_color, 1.0);
}