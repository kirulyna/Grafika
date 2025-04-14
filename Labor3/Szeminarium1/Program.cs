using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

class Program
{
    static void Main()
    {
        var nativeSettings = new NativeWindowSettings()
        {
            Size = new Vector2i(1200, 800),
            Title = "Két dézsa egymás fölött - Phong fényeléssel"
        };

        using var window = new DezsaWindow(GameWindowSettings.Default, nativeSettings);
        window.Run();
    }
}

class DezsaWindow : GameWindow
{
    private int _vao, _vbo, _ebo, _shader;
    private Vector3 _cameraPos = new Vector3(0, 0, 20);
    private float _yaw = -90f, _pitch = 0f, _zoom = 45f;
    private Vector2 _lastMousePos;
    private bool _firstMove = true;

    private Vector3 _cameraFront = -Vector3.UnitZ;
    private Vector3 _cameraUp = Vector3.UnitY;
    private Vector3 _lightPos = new Vector3(0, 10, 10);

    float[] vertices = {
        // positions        // normals
        -1f, -2f, 0f,     0f, 0f, 1f,
         1f, -2f, 0f,     0f, 0f, 1f,
         1f,  2f, 0f,     0f, 0f, 1f,
        -1f,  2f, 0f,     0f, 0f, 1f,
    };

    uint[] indices = {
        0, 1, 2,
        2, 3, 0
    };

    public DezsaWindow(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings)
        : base(gameSettings, nativeSettings) { }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        CursorState = CursorState.Grabbed;

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        int stride = 6 * sizeof(float);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        _shader = CreateShader();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        const float cameraSpeed = 10f;
        var input = KeyboardState;

        if (input.IsKeyDown(Keys.W))
            _cameraPos += _cameraFront * cameraSpeed * (float)e.Time;
        if (input.IsKeyDown(Keys.S))
            _cameraPos -= _cameraFront * cameraSpeed * (float)e.Time;
        if (input.IsKeyDown(Keys.A))
            _cameraPos -= Vector3.Normalize(Vector3.Cross(_cameraFront, _cameraUp)) * cameraSpeed * (float)e.Time;
        if (input.IsKeyDown(Keys.D))
            _cameraPos += Vector3.Normalize(Vector3.Cross(_cameraFront, _cameraUp)) * cameraSpeed * (float)e.Time;
        if (input.IsKeyDown(Keys.Up))
            _zoom -= 50f * (float)e.Time;
        if (input.IsKeyDown(Keys.Down))
            _zoom += 50f * (float)e.Time;

        _zoom = Math.Clamp(_zoom, 1f, 90f);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        if (_firstMove)
        {
            _lastMousePos = e.Position;
            _firstMove = false;
        }

        float xoffset = e.Position.X - _lastMousePos.X;
        float yoffset = _lastMousePos.Y - e.Position.Y;
        _lastMousePos = e.Position;

        const float sensitivity = 0.1f;
        xoffset *= sensitivity;
        yoffset *= sensitivity;

        _yaw += xoffset;
        _pitch += yoffset;
        _pitch = Math.Clamp(_pitch, -89f, 89f);

        Vector3 front;
        front.X = MathF.Cos(MathHelper.DegreesToRadians(_yaw)) * MathF.Cos(MathHelper.DegreesToRadians(_pitch));
        front.Y = MathF.Sin(MathHelper.DegreesToRadians(_pitch));
        front.Z = MathF.Sin(MathHelper.DegreesToRadians(_yaw)) * MathF.Cos(MathHelper.DegreesToRadians(_pitch));
        _cameraFront = Vector3.Normalize(front);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.UseProgram(_shader);

        var view = Matrix4.LookAt(_cameraPos, _cameraPos + _cameraFront, _cameraUp);
        var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(_zoom), (float)Size.X / Size.Y, 0.1f, 100f);

        GL.UniformMatrix4(GL.GetUniformLocation(_shader, "view"), false, ref view);
        GL.UniformMatrix4(GL.GetUniformLocation(_shader, "projection"), false, ref projection);
        GL.Uniform3(GL.GetUniformLocation(_shader, "lightPos"), _lightPos);
        GL.Uniform3(GL.GetUniformLocation(_shader, "lightColor"), Vector3.One);
        GL.Uniform3(GL.GetUniformLocation(_shader, "objectColor"), new Vector3(0.8f, 0.3f, 0.2f));

        float angleOffset = MathHelper.DegreesToRadians(10f);
        float radius = 1f / (float)Math.Tan(angleOffset);

        for (int version = 0; version < 2; version++)
        {
            float yShift = version == 0 ? -6f : 6f; // egymás fölött

            for (int i = 0; i < 18; i++)
            {
                float angle = MathHelper.DegreesToRadians(i * 20);
                float x = radius * (float)Math.Sin(angle);
                float z = radius * (float)Math.Cos(angle);

                Matrix4 model = Matrix4.CreateScale(1.5f, 1.5f, 1.5f)
                    * Matrix4.CreateRotationY(angle)
                    * Matrix4.CreateTranslation(x, yShift, z);

                GL.UniformMatrix4(GL.GetUniformLocation(_shader, "model"), false, ref model);

                Vector3 normal = version == 0
                    ? new Vector3(0, 0, 1)
                    : new Vector3((float)Math.Sin(angleOffset), 0, (float)Math.Cos(angleOffset));

                GL.Uniform3(GL.GetUniformLocation(_shader, "overrideNormal"), normal);

                GL.BindVertexArray(_vao);
                GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
            }
        }

        SwapBuffers();
    }

    int CreateShader()
    {
        string vertexSource = @"#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 overrideNormal;

out vec3 FragPos;
out vec3 Normal;

void main()
{
    FragPos = vec3(model * vec4(aPos, 1.0));
    Normal = mat3(transpose(inverse(model))) * overrideNormal;
    gl_Position = projection * view * vec4(FragPos, 1.0);
}";

        string fragmentSource = @"#version 330 core
out vec4 FragColor;

in vec3 FragPos;
in vec3 Normal;

uniform vec3 lightPos;
uniform vec3 lightColor;
uniform vec3 objectColor;

void main()
{
    float ambientStrength = 0.2;
    vec3 ambient = ambientStrength * lightColor;

    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(lightPos - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;

    vec3 result = (ambient + diffuse) * objectColor;
    FragColor = vec4(result, 1.0);
}";

        int vertex = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertex, vertexSource);
        GL.CompileShader(vertex);

        int fragment = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragment, fragmentSource);
        GL.CompileShader(fragment);

        int program = GL.CreateProgram();
        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);

        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        return program;
    }
}
