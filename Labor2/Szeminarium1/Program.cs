
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Szeminarium1
{
    internal static class Program
    {
        private static IWindow window;
        private static GL Gl;
        private static uint shaderProgram;
        private static readonly List<Cube> cubes = new();
        private static IInputContext input = null!;

        //camera cucok
        private static Vector3 cameraPosition = new(2.5f, 2.5f, 2.5f);
        private static Vector3 cameraFront = -Vector3.Normalize(cameraPosition);
        private static readonly Vector3 cameraUp = Vector3.UnitY;
        private static float cameraSpeed = 0.05f;
        private static float yaw = -135f;
        private static float pitch = -30f;
        private static float lastX = 400f;
        private static float lastY = 400f;
        private static bool firstMouse = true;

        private const string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

        uniform mat4 model;
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
        in vec4 outCol;
        out vec4 FragColor;

        void main()
        {
            FragColor = outCol;
        }
        ";

        static void Main(string[] args)
        {
            var options = WindowOptions.Default with
            {
                Title = "2.Labor - Rubik kocka",
                Size = new(800, 800),
                VSync = true
            };
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
            input = window.CreateInput();
            Gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
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

            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to complie" + Gl.GetShaderInfoLog(vshader));

            Gl.GetProgram(shaderProgram,GLEnum.LinkStatus,out var status);
            if(status == 0)
            {
                throw new Exception($"error linking shader: {{Gl.GetProgramInfoLog(shaderProgram)}}");
            }

            //clean 
            Gl.DetachShader(shaderProgram, vshader);
            Gl.DetachShader (fshader, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader (fshader);

            foreach(var keybourd in input.Keyboards)
            {
                keybourd.KeyDown += OnKeyDown;
            }

            foreach (var mouse in input.Mice)
            {
                mouse.MouseMove += OnMouseMove;
                mouse.Scroll += OnMouseScroll;
            }

            CreateColorfulCubes();
        }

        private static void OnMouseMove(IMouse mouse, Vector2 position)
        {
            if(firstMouse)
            {
                lastX = position.X;
                lastY = position.Y;
                firstMouse = false;
            }

            float xOffset = position.X - lastX;
            float yOffset = lastY - position.Y;
            lastX = position.X;
            lastY = position.Y;

            float sensitivity = 0.1f;
            xOffset *= sensitivity;
            yOffset *= sensitivity;

            yaw += xOffset;
            pitch += yOffset;

            if(pitch > 89.0f)
            {
                pitch = 89.0f;
            }

            if (pitch < -89.0f)
            {
                pitch = -89.0f;
            }

            Vector3 front;
            front.X = MathF.Cos(DegreesToRadians(yaw)) * MathF.Cos(DegreesToRadians(pitch));
            front.Y = MathF.Sin(DegreesToRadians(pitch));
            front.Z = MathF.Sin(DegreesToRadians(yaw)) * MathF.Cos(DegreesToRadians(pitch));
            cameraFront = Vector3.Normalize(front);
        }

        private static void OnMouseScroll(IMouse mouse, ScrollWheel scroll)
        {
            cameraSpeed = Math.Clamp(cameraSpeed + scroll.Y * 0.01f, 0.01f, 0.2f);
        }

        private static void OnKeyDown(IKeyboard keyboard, Key key, int _)
        {
            if(key == Key.Escape)
                window.Close();

            if (key == Key.W)
                cameraPosition += cameraFront * cameraSpeed;
            else if (key == Key.S)
                cameraPosition -= cameraFront * cameraSpeed;
            else if (key == Key.A)
                cameraPosition -= Vector3.Normalize(Vector3.Cross(cameraFront, cameraUp)) * cameraSpeed;
            else if (key == Key.D)
                cameraPosition += Vector3.Normalize(Vector3.Cross(cameraFront, cameraUp)) * cameraSpeed;
            if (key == Key.Space)
                cameraPosition += cameraUp * cameraSpeed;
            if (key == Key.X)
                cameraPosition -= cameraUp * cameraSpeed;
        }

        private static float DegreesToRadians(float degrees)
        {
            return degrees * MathF.PI / 180f;
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

            Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPosition, cameraPosition + cameraFront, cameraUp);
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
                DegreesToRadians(45f),
                (float)window.Size.X / window.Size.Y,
                0.1f,
                100.0f
            );

            int viewLoc = Gl.GetUniformLocation(shaderProgram, "view");
            int projLoc = Gl.GetUniformLocation(shaderProgram, "projection");
            Gl.UniformMatrix4(viewLoc, 1, false, (float*)&view);
            Gl.UniformMatrix4(projLoc, 1, false, (float*)&projection);


            //render cubes
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


                //vertex buffer
                vertexBuffer = gl.GenBuffer();
                gl.BindBuffer(GLEnum.ArrayBuffer, vertexBuffer);
                fixed (float* ptr = vertices)
                    gl.BufferData(GLEnum.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), ptr, GLEnum.StaticDraw);
                gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
                gl.EnableVertexAttribArray(0);

                //collor buffer
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