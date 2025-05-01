using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;

namespace Szeminarium1_24_02_17_2
{
    internal class ObjResourceReader
    {
        public static unsafe GlObject CreateTeapotWithColor(GL Gl, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFaces;

            ReadObjDataForTeapot(out objVertices, out var objNormals, out objFaces);

            var glVertices = new List<float>();
            var glColors = new List<float>();
            var glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objNormals, objFaces, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            const uint offsetPos = 0;
            const uint offsetNormal = offsetPos + (3 * sizeof(float));
            const uint vertexSize = offsetNormal + (3 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(), GLEnum.StaticDraw);

            // release array buffer
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)glIndices.Count;

            return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
        }

        private static unsafe void CreateGlArraysFromObjArrays(float[] faceColor, List<float[]> objVertices, List<float[]> objNormals, List<int[]> objFaces, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            var glVertexIndices = new Dictionary<string, int>();

            foreach (var objFace in objFaces)
            {
                bool hasNormals = objFace.Length == 9;
                Vector3D<float> normal = default;

                if (!hasNormals)
                {
                    var aObjVertex = objVertices[objFace[0] - 1];
                    var a = new Vector3D<float>(aObjVertex[0], aObjVertex[1], aObjVertex[2]);
                    var bObjVertex = objVertices[objFace[1] - 1];
                    var b = new Vector3D<float>(bObjVertex[0], bObjVertex[1], bObjVertex[2]);
                    var cObjVertex = objVertices[objFace[2] - 1];
                    var c = new Vector3D<float>(cObjVertex[0], cObjVertex[1], cObjVertex[2]);

                    normal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));
                }

                for (int i = 0; i < 3; ++i)
                {
                    int vertexIndex = objFace[i * (hasNormals ? 3 : 1)] - 1;
                    var objVertex = objVertices[vertexIndex];

                    Vector3D<float> vertexNormal;
                    if (hasNormals)
                    {
                        int normalIndex = objFace[i * 3 + 2] - 1;
                        var objNormal = objNormals[normalIndex];
                        vertexNormal = new Vector3D<float>(objNormal[0], objNormal[1], objNormal[2]);
                    }
                    else
                    {
                        vertexNormal = normal;
                    }

                    var glVertex = new List<float> {
                        objVertex[0], objVertex[1], objVertex[2],
                        vertexNormal.X, vertexNormal.Y, vertexNormal.Z
                    };

                    var glVertexStringKey = string.Join(" ", glVertex);
                    if (!glVertexIndices.TryGetValue(glVertexStringKey, out int index))
                    {
                        index = glVertexIndices.Count;
                        glVertices.AddRange(glVertex);
                        glColors.AddRange(faceColor);
                        glVertexIndices.Add(glVertexStringKey, index);
                    }

                    glIndices.Add((uint)index);
                }
            }
        }

        private static unsafe void ReadObjDataForTeapot(out List<float[]> objVertices, out List<float[]> objNormals, out List<int[]> objFaces)
        {
            objVertices = new List<float[]>();
            objNormals = new List<float[]>();
            objFaces = new List<int[]>();

            using var objStream = typeof(ObjResourceReader).Assembly.GetManifestResourceStream("Szeminarium1_24_02_17_2.Resources.teapot.obj");
            if (objStream == null)
            {
                throw new FileNotFoundException("Could not find embedded resource teapot.obj");
            }

            using var objReader = new StreamReader(objStream);
            {
                while (!objReader.EndOfStream)
                {
                    var line = objReader.ReadLine();

                    if (String.IsNullOrEmpty(line) || line.Trim().StartsWith("#"))
                        continue;

                    int spaceIndex = line.IndexOf(' ');
                    if (spaceIndex < 0) continue;

                    var lineClassifier = line[..spaceIndex];
                    var lineData = line[(spaceIndex + 1)..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    switch (lineClassifier)
                    {
                        case "v":
                            var vertex = new float[3];
                            for (int i = 0; i < vertex.Length; ++i)
                                vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objVertices.Add(vertex);
                            break;
                        case "vn":
                            var normal = new float[3];
                            for (int i = 0; i < normal.Length; ++i)
                                normal[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objNormals.Add(normal);
                            break;
                        case "f":
                            var face = new int[9];
                            bool hasNormals = false;

                            for (int i = 0; i < 3; ++i)
                            {
                                var vertexData = lineData[i].Split('/');
                                face[i * 3] = int.Parse(vertexData[0]);

                                if (vertexData.Length > 2 && !string.IsNullOrEmpty(vertexData[2]))
                                {
                                    face[i * 3 + 2] = int.Parse(vertexData[2]);
                                    hasNormals = true;
                                }
                            }

                            if (!hasNormals)
                            {
                                var simpleFace = new int[3];
                                for (int i = 0; i < 3; ++i)
                                {
                                    simpleFace[i] = int.Parse(lineData[i].Split('/')[0]);
                                }
                                objFaces.Add(simpleFace);
                            }
                            else
                            {
                                objFaces.Add(face);
                            }
                            break;
                    }
                }
            }
        }
    }
}
