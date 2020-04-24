using System;
using System.Collections.Generic;
using System.IO;

namespace ObjToCS4247
{
    class Vertex
    {
        float x, y, z;

        public Vertex(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
        {
            return x + " " + y + " " + z;
        }
    }

    class Face
    {
        public int v1, v2, v3, v4;
        public int material;

        public Face(int x, int y, int z, int w, int m)
        {
            v1 = x;
            v2 = y;
            v3 = z;
            v4 = w;
            material = m;
        }

        public string GetVertexString()
        {
            return v1 + " " + v2 + " " + v3 + " " + v4;
        }
    }

    struct Color
    {
        float r, g, b;

        public Color(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }
        public Color (float v)
        {
            this.r = this.g = this.b = v;
        }

        public override string ToString()
        {
            return r + " " + g + " " + b;
        }
    }


    class Material
    {
        public readonly string id;

        // Only support diffuse and emissive term for now
        public Color diffuse;
        public Color emissive;

        public Material(String id)
        {
            this.id = id;
        }
    }


    class Program
    {
        static void Main(string[] args)
        {

            string filename = "";

            if (args.Length == 0)
            {
                Console.WriteLine("Please specify a input file");
                filename = Console.ReadLine();
            }
            else
            {
                filename = args[0];
            }

            new Parser().Run(filename);
        }
    }

    class Parser
    {
        List<Face> faces;
        List<Vertex> vertices;
        List<Material> materials;
        Dictionary<string, int> materialIdTable;

        public Parser()
        {
            faces = new List<Face>();
            vertices = new List<Vertex>();
            materials = new List<Material>();
            materialIdTable = new Dictionary<string, int>();
        }

        public void Run(string fileName)
        {

            Material currentActiveMaterial = null;

            string[] lines = File.ReadAllLines(fileName);

            for (int i = 0; i < lines.Length; ++i)
            {
                string line = lines[i];

                if (line.Equals(""))
                    continue;

                if (line[0] == '#')
                    continue;

                if (line.StartsWith("mtllib"))
                {
                    ParseMaterials(line.Split(' ')[1]);
                    continue;
                }

                if (line.StartsWith("v"))
                {
                    ParseVertex(line.Substring(2));
                    continue;
                }

                if (line.StartsWith("usemtl"))
                {
                    String materialKey = line.Substring(7);
                    int materialID = -1;
                    materialIdTable.TryGetValue(materialKey, out materialID);

                    if (materialID != -1)
                    {
                        currentActiveMaterial = materials[materialID];
                    }
                }

                if (line.StartsWith("f"))
                {
                    ParseFace(line.Substring(2), currentActiveMaterial);
                }
            }

            WriteToFile(fileName + ".parserout");
            Console.WriteLine("Complete!");
        }

        void ParseVertex(string line)
        {
            string[] substring = line.Split(' ');
            vertices.Add(new Vertex(float.Parse(substring[0]), float.Parse(substring[1]), float.Parse(substring[2])));
        }

        void ParseFace(string line, Material currentActiveMaterial)
        {
            string[] substring = line.Split(' ');
            int materialID = -1;
            materialIdTable.TryGetValue(currentActiveMaterial.id, out materialID);

            if (substring.Length == 4)
                faces.Add(new Face(int.Parse(substring[0]) - 1, int.Parse(substring[1]) - 1, int.Parse(substring[2]) - 1, int.Parse(substring[3]) - 1, materialID));
            else
                faces.Add(new Face(int.Parse(substring[0]) - 1, int.Parse(substring[1]) - 1, int.Parse(substring[2]) - 1, int.Parse(substring[2]) - 1, materialID));
        }

        void ParseMaterials(string fileName)
        {
            if (fileName.Equals(""))
            {
                Console.WriteLine("Material File Path is Blank");
                return;
            }

            Material currentMaterial = null;
            string[] lines = File.ReadAllLines(fileName);

            for (int i = 0; i < lines.Length; ++i)
            {
                string line = lines[i];
                if (line.StartsWith("newmtl"))
                {
                    if (currentMaterial != null)
                    {
                        materials.Add(currentMaterial);
                        materialIdTable.Add(currentMaterial.id, materials.Count - 1);
                    }

                    currentMaterial = new Material(line.Split(' ')[1]);

                    // Hack for light, since .mtl files don't support emission coefficients
                    if (currentMaterial.id.StartsWith("Light"))
                    {
                        currentMaterial.emissive = new Color(20.0f, 12.0f, 10.0f);
                    }

                    continue;
                }

                if (line.StartsWith("Kd"))
                {
                    string[] colors = line.Split(' ');
                    currentMaterial.diffuse = new Color(float.Parse(colors[1]), float.Parse(colors[2]), float.Parse(colors[3]));
                    continue;
                }

            }

            // Save old material, if any
            if (currentMaterial != null)
            {
                materials.Add(currentMaterial);
                materialIdTable.Add(currentMaterial.id, materials.Count - 1);
            }
        }

        void WriteToFile(string outputName)
        {
            Console.WriteLine("Writing to file: " + outputName);
            using (StreamWriter writer = new StreamWriter(outputName))
            {
                // First print the number of vertices
                writer.WriteLine("# Number of vertices");
                writer.WriteLine(vertices.Count);
                writer.WriteLine("");
                
                // Output each vertex
                foreach(Vertex v in vertices)
                {
                    writer.WriteLine(v.ToString());
                }
                writer.WriteLine("");


                // Next, print the number of materials
                writer.WriteLine("# Number of material");
                writer.WriteLine(materials.Count);
                writer.WriteLine("");

                // Output each material
                foreach(Material m in materials)
                {
                    writer.WriteLine("# " + m.id);
                    writer.WriteLine(m.diffuse.ToString());
                    writer.WriteLine(m.emissive.ToString());
                    writer.WriteLine("");
                }
                writer.WriteLine("");


                // Finally, print the number of faces (or as they call it, 'surfaces')
                writer.WriteLine("# Number of faces");
                writer.WriteLine(faces.Count);
                writer.WriteLine("");

                // Output each material
                foreach(Face f in faces)
                {
                    writer.WriteLine(f.material);
                    writer.WriteLine("1"); // Since we're 3d modeling this, let each face be one patch
                    writer.WriteLine(f.GetVertexString());
                    writer.WriteLine("");
                }

                writer.WriteLine("");
            }

        }
    }
}
