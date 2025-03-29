
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Szeminarium1
{
    internal static class Program
    {
        private static IWindow window;
        private static GL? Gl;
        private static uint shaderProgram;
        private static readonly List<Cube> cubes = new();

        private const string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

        unifrom mat4 model;
        uniform mat4 view;
        uniform mat4 projection;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = projection * view * model * vec4(vPos, 1.0);
        }
        ";


        private const string FragmentShaderSource = @"
        #version 330 core
        in vec4 color;
        out vec4 FragColor;

        void main()
        {
            FragColor = color;
        }
        ";

        static void Main(string[] args)
        {
            var options = WindowOptions.Default;
            options.Title = "2.Labor - Rubik kocka";
            options.Size = new(500, 500);

            window = Window.Create(options);
            window.Load += OnLoad;
            window.Render += OnRender;
            window.Run();
        }

        private static unsafe void OnLoad()
        {
            // egszeri beallitasokat
            //Console.WriteLine("Loaded");

            Gl = window.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.White);
            Gl.Enable(GLEnum.DepthTest);


            //shader compile 
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);


            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            shaderProgram = Gl.CreateProgram();
            Gl.AttachShader(shaderProgram, vshader);
            Gl.AttachShader(shaderProgram, fshader);
            Gl.LinkProgram(shaderProgram);
            Gl.DetachShader(shaderProgram, vshader);
            Gl.DetachShader(shaderProgram, fshader);
            Gl.LinkProgram(shaderProgram);

            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);

            Gl.GetProgram(shaderProgram, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(shaderProgram)}");
            }

            CreateColorfulCubes();

        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // NO GL
            // make it threadsave
            //Console.WriteLine($"Update after {deltaTime} [s]");
        }

        private static unsafe void CreateColorfulCubes()
        {
            Random rand = new Random();
            float cubeSize = 0.25f;
            float spacing = 0.05f;
            float offset = cubeSize + spacing;

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        Vector3 positon = new(
                            (x - 1) * offset,
                            (y - 1) * offset,
                            (z - 1) * offset
                        );

                        Vector4 cubeColor = new Vector4(
                            (float)rand.NextDouble(),
                            (float)rand.NextDouble(),
                            (float)rand.NextDouble(),
                            1.0f
                            );

                        cubes.Add(new Cube(Gl!, positon, cubeSize, cubeColor));
                    }
                }
            }
        }

        private static unsafe void OnRender(double deltaTime)
        {
            Gl!.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.UseProgram(shaderProgram);

            Matrix4x4 view = Matrix4x4.CreateLookAt(
                new Vector3(2.5f, 2.5f, 2.5f),
                Vector3.Zero,
                Vector3.UnitY
                );

            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
                MathF.PI / 3,
                (float)window!.Size.X / window.Size.Y,
                0.1f,
                100.0f
                );

            int viewLoc = Gl.GetUniformLocation(shaderProgram, "view");
            int projLoc = Gl.GetUniformLocation(shaderProgram, "projection");
            Gl.UniformMatrix4(viewLoc, 1, false, (float*)&view);
            Gl.UniformMatrix4(projLoc, 1, false, (float*)&projection);

            foreach (var cube in cubes)
            {
                cube.Render(Gl, shaderProgram);
            }
        }

        public unsafe class Cube
        {
            //Console.WriteLine($"Render after {deltaTime} [s]");
            private readonly uint vao;
            private readonly uint vertexBuffer;
            private readonly uint colorBuffer;
            private readonly uint indexBuffer;
            private readonly Matrix4x4 modelMatrix;
            private readonly Vector4 cubeColor;

            public Cube(GL gl, Vector3 position, float size, Vector4 color)
            {
                modelMatrix = Matrix4x4.CreateScale(size) * Matrix4x4.CreateTranslation(position);
                cubeColor = color;

                float[] vertices = {


                 -0.5f, -0.5f, 0.5f,//0
                 0.5f, -0.5f, 0.5f,//1
                 0.5f, 0.5f, 0.5f,//2
                 -0.5f, 0.5f, 0.5f,//3

                 -0.5f, -0.5f, -0.5f,//4
                 0.5f, -0.5f, -0.5f,//5
                 0.5f, 0.5f, -0.5f,//6
                 -0.5f, 0.5f, -0.5f,//7
                };



                uint[] indices =
                {
                0,1,2,2,3,0,//front
                1,5,6,6,2,1,//right
                5,4,7,7,6,5,//back
                4,0,3,3,7,4,//left
                3,2,6,6,7,3,//top
                0,4,5,5,1,0//bottom
                };



                Vector4[] vertexColors = new Vector4[8];
                for (int i = 0; i < 8; i++)
                {
                    vertexColors[i] = cubeColor;
                }


                vao = gl.GenVertexArray();
                gl.BindVertexArray(vao);

                vertexBuffer = gl.GenBuffer();
                gl.BindBuffer(GLEnum.ArrayBuffer, vertexBuffer);
                fixed (float* ptr = vertices)
                    gl.BufferData(GLEnum.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), ptr, GLEnum.StaticDraw);
                gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
                gl.EnableVertexAttribArray(0);

                colorBuffer = gl.GenBuffer();
                gl.BindBuffer(GLEnum.ArrayBuffer, colorBuffer);
                fixed (Vector4* ptr = vertexColors)
                    gl.BufferData(GLEnum.ArrayBuffer, (nuint)(vertexColors.Length * sizeof(Vector4)), ptr, GLEnum.StaticDraw);
                gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
                gl.EnableVertexAttribArray(1);

                indexBuffer = gl.GenBuffer();
                gl.BindBuffer(GLEnum.ElementArrayBuffer, indexBuffer);
                fixed (uint* ptr = indices)
                    gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), ptr, GLEnum.StaticDraw);

                gl.BindVertexArray(0);
            }

            public unsafe void Render(GL gl, uint shaderProgram)
            {
                gl.BindVertexArray(vao);

                int modelLoc = gl.GetUniformLocation(shaderProgram, "model");
                fixed (float* modelPtr = &modelMatrix.M11)
                {
                    gl.UniformMatrix4(modelLoc, 1, false, modelPtr);
                }

                gl.DrawElements(GLEnum.Triangles, 36, GLEnum.UnsignedInt, null);
                gl.BindVertexArray(0);
            }
        }
    }
}