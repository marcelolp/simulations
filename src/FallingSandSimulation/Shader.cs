using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FallingSandSimulation
{
    internal class Shader
    {
        internal readonly int Handle;

        private readonly Dictionary<string, int> _uniformLocation;
        internal Shader(string vertexPath, string fragmentPath)
        {
            string vertexSource = File.ReadAllText(vertexPath);
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);

            GL.ShaderSource(vertexShader, vertexSource);
            CompileShader(vertexShader);

            string fragmentSource = File.ReadAllText(fragmentPath);
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(fragmentShader, fragmentSource);
            CompileShader(fragmentShader);

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            LinkProgram(Handle);

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            _uniformLocation = new();

            for (int i = 0; i < numberOfUniforms; i++)
            {
                string key = GL.GetActiveUniform(Handle, i, out _, out _);
                int location = GL.GetUniformLocation(Handle, key);

                _uniformLocation.Add(key, location);
            }
        }

        private static void CompileShader(int shader)
        {
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Error while compiling {shader}:\n {infoLog}");
            }
        }

        private static void LinkProgram(int program)
        {
            GL.LinkProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if (code != (int)All.True)
            {
                throw new Exception($"Error while compiling {program}");
            }
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public int GetAttributeLocation(string attrName)
        {
            return GL.GetAttribLocation(Handle, attrName);
        }

        public void SetIntUniform(string name, int val)
        {
            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocation[name], val);
        }

        public void SetFloatUniform(string name, float val)
        {
            GL.UseProgram(Handle);
            GL.Uniform1(_uniformLocation[name], val);
        }

        public void SetMatrix4Uniform(string name, Matrix4 val)
        {
            GL.UseProgram(Handle);
            GL.UniformMatrix4(_uniformLocation[name], true, ref val);
        }

        public void SetVec3Uniform(string name, Vector3 val)
        {
            GL.UseProgram(Handle);
            GL.Uniform3(_uniformLocation[name], val);
        }

    }
}
