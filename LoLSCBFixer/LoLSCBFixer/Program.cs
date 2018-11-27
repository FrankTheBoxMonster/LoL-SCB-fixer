using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoLSCBFixer {
    public class Program {
        public static void Main(string[] args) {
            Console.WriteLine("LoL SCB Fixer by FrankTheBoxMonster");

            if(args.Length < 1) {
                Console.WriteLine("Error:  must provide a file (you can drag-and-drop one or more files onto the .exe)");
                Pause();
                System.Environment.Exit(1);
            }

            for(int i = 0; i < args.Length; i++) {
                try {
                    Console.WriteLine("\nFixing file " + (i + 1) + "/" + args.Length + ":  " + args[i].Substring(args[i].LastIndexOf('\\') + 1));
                    TryReadFile(args[i]);
                } catch (System.Exception e) {
                    Console.WriteLine("\n\nError:  " + e.ToString());
                }
            }


            Console.WriteLine("\n\nDone");
            Pause();
        }


        private static void Pause() {
            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey(true);
        }


        private static void TryReadFile(string filePath) {
            FileWrapper inputFile = new FileWrapper(filePath);

            if(filePath.ToLower().EndsWith(".scb") == false) {
                Console.WriteLine("Error:  not an .SCB file");
                return;
            }


            string magic = inputFile.ReadString(8);

            if(magic != "r3d2Mesh") {
                Console.WriteLine("Error:  invalid .SCB file");
                return;
            }


            int versionMajor = inputFile.ReadShort();
            int versionMinor = inputFile.ReadShort();

            if(versionMinor != 2 && versionMajor != 2 && versionMajor != 3) {
                Console.WriteLine("Error:  unrecognized version " + versionMajor + "." + versionMinor);
                return;
            }

            if(versionMajor == 2) {
                Console.WriteLine("Error:  file is already fixed");
                return;
            }


            string objectName = inputFile.ReadString(128);
            int vertexCount = inputFile.ReadInt();
            int triCount = inputFile.ReadInt();
            int unknown = inputFile.ReadInt();

            float[] aabb = new float[6];
            for(int i = 0; i < aabb.Length; i++) {
                aabb[i] = inputFile.ReadFloat();
            }


            int hasFFBlock = inputFile.ReadInt();


            List<float> vertexCoords = new List<float>();
            for(int i = 0; i < vertexCount * 3; i++) {
                float coord = inputFile.ReadFloat();
                vertexCoords.Add(coord);
            }


            // unknown "FF" block, four bytes per vertex,
            // appears to always be either "0xffffffff" or "0xffffff00"
            // significance unknown, but it's not present in past formats

            if(hasFFBlock == 1) {
                for(int i = 0; i < vertexCount; i++) {
                    inputFile.ReadInt();
                }
            } else if(hasFFBlock == 0) {
                // skip
            } else {
                Console.WriteLine("Error:  unknown \"has FF block\" value " + hasFFBlock);
                return;
            }


            // unknown, appear to be always zero, NOT an extra vertex because
            // it always comes after the FF block in files that have an FF block
            int[] unknown2 = new int[3];
            for(int i = 0; i < unknown2.Length; i++) {
                inputFile.ReadInt();
            }


            List<Tri> tris = new List<Tri>();
            for(int i = 0; i < triCount; i++) {
                int[] indices = new int[3];
                for(int j = 0; j < indices.Length; j++) {
                    indices[j] = inputFile.ReadInt();
                }

                string materialName = inputFile.ReadString(64);

                float[] uvs = new float[6];
                for(int j = 0; j < uvs.Length; j++) {
                    uvs[j] = inputFile.ReadFloat();
                }


                Tri tri = new Tri(indices, materialName, uvs);
                tris.Add(tri);
            }


            inputFile.Close();



            string outputFilePath = inputFile.folderPath + inputFile.name + "_fixed" + inputFile.fileExtension;
            FileWrapper outputFile = new FileWrapper(outputFilePath);

            outputFile.Clear();

            outputFile.WriteChars(magic.ToCharArray());
            outputFile.WriteShort(2);
            outputFile.WriteShort(2);

            outputFile.WriteChars(objectName.ToCharArray());

            outputFile.WriteInt(vertexCount);
            outputFile.WriteInt(triCount);
            outputFile.WriteInt(unknown);

            for(int i = 0; i < aabb.Length; i++) {
                outputFile.WriteFloat(aabb[i]);
            }

            for(int i = 0; i < vertexCoords.Count; i++) {
                outputFile.WriteFloat(vertexCoords[i]);
            }

            // FF block is not written out

            for(int i = 0; i < unknown2.Length; i++) {
                outputFile.WriteInt(unknown2[i]);
            }

            for(int i = 0; i < tris.Count; i++) {
                Tri tri = tris[i];

                for(int j = 0; j < tri.indices.Length; j++) {
                    outputFile.WriteInt(tri.indices[j]);
                }

                outputFile.WriteChars(tri.materialName.ToCharArray());

                for(int j = 0; j < tri.uvs.Length; j++) {
                    outputFile.WriteFloat(tri.uvs[j]);
                }
            }


            outputFile.Close();


            Console.WriteLine("Successfully fixed");
        }
    }


    public class Tri {
        public int[] indices;
        public string materialName;
        public float[] uvs;  // order "u1 u2 u3 (1 - v1) (1 - v2) (1 - v3)"


        public Tri(int[] indices, string materialName, float[] uvs) {
            this.indices = indices;
            this.materialName = materialName;
            this.uvs = uvs;
        }
    }
}
