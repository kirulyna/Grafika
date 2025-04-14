using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static IInputContext inputContext;

        private static GL Gl;

        private static ImGuiController controller;

        private static uint program;

        private static GlObject teapot;

        private static GlObject table;

        private static GlCube glCubeRotating;

        private static float Shininess = 50;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        //3.2 hf
        private static Vector3D<float> AmbientStrength = new Vector3D<float>(0.2f);
        private static Vector3D<float> DiffuseStrength = new Vector3D<float>(0.3f);
        private static Vector3D<float> SpecularStrength = new Vector3D<float>(0.5f);
        private static Vector3D<float> LightColor = new Vector3D<float>(1f);
        private static int SelectedFaceColor = 0;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNorm;

        uniform mat4 uModel;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
            outNormal = uNormal*vNorm;
            outWorldPosition = vec3(uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0));
        }
        ";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        
        uniform vec3 lightColor;
        uniform vec3 lightPos;
        uniform vec3 viewPos;
        uniform float shininess;
        uniform vec3 ambientStrength;
        uniform vec3 diffuseStrength;
        uniform vec3 specularStrength;

        out vec4 FragColor;

        in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        void main()
        {
            vec3 ambient = ambientStrength * lightColor;

            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor * diffuseStrength;

            vec3 viewDir = normalize(viewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess) / max(dot(norm,viewDir), -dot(norm,lightDir));
            vec3 specular = specularStrength * spec * lightColor;  

            vec3 result = (ambient + diffuse + specular) * outCol.xyz;
            FragColor = vec4(result, outCol.w);
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(500, 500);

            // on some systems there is no depth buffer by default, so we need to make sure one is created
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
            //Console.WriteLine("Load");

            // set up input handling
            inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();

            controller = new ImGuiController(Gl, window, inputContext);

            // Handle resizes
            window.FramebufferResize += s =>
            {
                // Adjust the viewport to the new window size
                Gl.Viewport(s);
            };


            Gl.ClearColor(System.Drawing.Color.Black);

            SetUpObjects();

            LinkProgram();

            //Gl.Enable(EnableCap.CullFace);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    cameraDescriptor.DecreaseZYAngle();
                    break;
                    ;
                case Key.Right:
                    cameraDescriptor.IncreaseZYAngle();
                    break;
                case Key.Down:
                    cameraDescriptor.IncreaseDistance();
                    break;
                case Key.Up:
                    cameraDescriptor.DecreaseDistance();
                    break;
                case Key.U:
                    cameraDescriptor.IncreaseZXAngle();
                    break;
                case Key.D:
                    cameraDescriptor.DecreaseZXAngle();
                    break;
                case Key.Space:
                    cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld;
                    break;
            }
        }

        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // multithreaded
            // make sure it is threadsafe
            // NO GL calls
            cubeArrangementModel.AdvanceTime(deltaTime);

            controller.Update((float)deltaTime);
        }

        private static void RenderUI()
        {
            ImGui.Begin("Lighting Properties",
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);

            ImGui.SliderFloat("Shininess", ref Shininess, 1, 200);

            // Ambient strength sliders
            float ambientX = AmbientStrength.X;
            float ambientY = AmbientStrength.Y;
            float ambientZ = AmbientStrength.Z;
            ImGui.Text("Ambient Strength");
            ImGui.SliderFloat("Ambient X", ref ambientX, 0, 1);
            ImGui.SliderFloat("Ambient Y", ref ambientY, 0, 1);
            ImGui.SliderFloat("Ambient Z", ref ambientZ, 0, 1);
            AmbientStrength = new Vector3D<float>(ambientX, ambientY, ambientZ);

            // Diffuse strength sliders
            float diffuseX = DiffuseStrength.X;
            float diffuseY = DiffuseStrength.Y;
            float diffuseZ = DiffuseStrength.Z;
            ImGui.Text("Diffuse Strength");
            ImGui.SliderFloat("Diffuse X", ref diffuseX, 0, 1);
            ImGui.SliderFloat("Diffuse Y", ref diffuseY, 0, 1);
            ImGui.SliderFloat("Diffuse Z", ref diffuseZ, 0, 1);
            DiffuseStrength = new Vector3D<float>(diffuseX, diffuseY, diffuseZ);

            // Specular strength sliders
            float specularX = SpecularStrength.X;
            float specularY = SpecularStrength.Y;
            float specularZ = SpecularStrength.Z;
            ImGui.Text("Specular Strength");
            ImGui.SliderFloat("Specular X", ref specularX, 0, 1);
            ImGui.SliderFloat("Specular Y", ref specularY, 0, 1);
            ImGui.SliderFloat("Specular Z", ref specularZ, 0, 1);
            SpecularStrength = new Vector3D<float>(specularX, specularY, specularZ);

            // Light color sliders
            float lightR = LightColor.X;
            float lightG = LightColor.Y;
            float lightB = LightColor.Z;
            ImGui.Text("Light Color");
            ImGui.SliderFloat("Light R", ref lightR, 0, 1);
            ImGui.SliderFloat("Light G", ref lightG, 0, 1);
            ImGui.SliderFloat("Light B", ref lightB, 0, 1);
            LightColor = new Vector3D<float>(lightR, lightG, lightB);

            // Face color selection
            string[] faceColors = { "Red", "Green", "Blue", "Magenta", "Cyan", "Yellow" };
            ImGui.Combo("Teapot Face Color", ref SelectedFaceColor, faceColors, faceColors.Length);

            ImGui.End();
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();
            SetLightingProperties();

            DrawPulsingTeapot();
            DrawRevolvingCube();

            RenderUI();

            controller.Render();
        }

        private static unsafe void SetLightingProperties()
        {
            int location = Gl.GetUniformLocation(program, "ambientStrength");
            Gl.Uniform3(location, AmbientStrength.X, AmbientStrength.Y, AmbientStrength.Z);

            location = Gl.GetUniformLocation(program, "diffuseStrength");
            Gl.Uniform3(location, DiffuseStrength.X, DiffuseStrength.Y, DiffuseStrength.Z);

            location = Gl.GetUniformLocation(program, "specularStrength");
            Gl.Uniform3(location, SpecularStrength.X, SpecularStrength.Y, SpecularStrength.Z);
            CheckError();
        }

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);
            if (location == -1)
            {
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");
            }
            Gl.Uniform3(location, LightColor.X, LightColor.Y, LightColor.Z);
            CheckError();
        }
        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightPositionVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 0f, 10f, 0f);
            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
            CheckError();
        }

        private static unsafe void SetShininess()
        {
            int location = Gl.GetUniformLocation(program, ShininessVariableName);

            if (location == -1)
            {
                throw new Exception($"{ShininessVariableName} uniform not found on shader.");
            }

            Gl.Uniform1(location, Shininess);
            CheckError();
        }

        private static unsafe void DrawRevolvingCube()
        {
            // set material uniform to metal

            Matrix4X4<float> diamondScale = Matrix4X4.CreateScale(1f);
            Matrix4X4<float> rotx = Matrix4X4.CreateRotationX((float)Math.PI / 4f);
            Matrix4X4<float> rotz = Matrix4X4.CreateRotationZ((float)Math.PI / 4f);
            Matrix4X4<float> rotLocY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleOwnRevolution);
            Matrix4X4<float> trans = Matrix4X4.CreateTranslation(4f, 4f, 0f);
            Matrix4X4<float> rotGlobY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleRevolutionOnGlobalY);
            Matrix4X4<float> modelMatrix = diamondScale * rotx * rotz * rotLocY * trans * rotGlobY;

            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(glCubeRotating.Vao);
            Gl.DrawElements(GLEnum.Triangles, glCubeRotating.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void DrawPulsingTeapot()
        {
            // Update teapot color based on selection
            float[] faceColor;
            switch (SelectedFaceColor)
            {
                case 0: faceColor = new float[] { 1f, 0f, 0f, 1.0f }; break; // Red
                case 1: faceColor = new float[] { 0f, 1f, 0f, 1.0f }; break; // Green
                case 2: faceColor = new float[] { 0f, 0f, 1f, 1.0f }; break; // Blue
                case 3: faceColor = new float[] { 1f, 0f, 1f, 1.0f }; break; // Magenta
                case 4: faceColor = new float[] { 0f, 1f, 1f, 1.0f }; break; // Cyan
                case 5: faceColor = new float[] { 1f, 1f, 0f, 1.0f }; break; // Yellow
                default: faceColor = new float[] { 1f, 0f, 0f, 1.0f }; break; // Default to Red
            }

            // Update teapot colors if selection changed
            if (teapot != null)
            {
                teapot.ReleaseGlObject();
                teapot = ObjResourceReader.CreateTeapotWithColor(Gl, faceColor);
            }

            var modelMatrixForCenterCube = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            SetModelMatrix(modelMatrixForCenterCube);
            Gl.BindVertexArray(teapot.Vao);
            Gl.DrawElements(GLEnum.Triangles, teapot.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            var modelMatrixForTable = Matrix4X4.CreateScale(1f, 1f, 1f);
            SetModelMatrix(modelMatrixForTable);
            Gl.BindVertexArray(table.Vao);
            Gl.DrawElements(GLEnum.Triangles, table.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUpObjects()
        {

            float[] face1Color = [1f, 0f, 0f, 1.0f];
            float[] face2Color = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] face3Color = [0.0f, 0.0f, 1.0f, 1.0f];
            float[] face4Color = [1.0f, 0.0f, 1.0f, 1.0f];
            float[] face5Color = [0.0f, 1.0f, 1.0f, 1.0f];
            float[] face6Color = [1.0f, 1.0f, 0.0f, 1.0f];

            teapot = ObjResourceReader.CreateTeapotWithColor(Gl, face1Color);

            float[] tableColor = [System.Drawing.Color.Azure.R/256f,
                                  System.Drawing.Color.Azure.G/256f,
                                  System.Drawing.Color.Azure.B/256f,
                                  1f];
            table = GlCube.CreateSquare(Gl, tableColor);

            glCubeRotating = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color, face4Color, face5Color, face6Color);
        }

        

        private static void Window_Closing()
        {
            teapot.ReleaseGlObject();
            glCubeRotating.ReleaseGlObject();
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 100);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}