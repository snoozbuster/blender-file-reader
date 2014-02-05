using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderFileReader
{
    class Program
    {
        /// <summary>
        /// Describes the pointer size of the file (either 4 or 8).
        /// </summary>
        public static int PointerSize { get; private set; }

        static void Main(string[] args)
        {
            string path = null;
            bool verbose = false;

            // gets path from args or user, also gets verbosity
            path = getPath(args, ref verbose);

            if(verbose)
                Console.WriteLine("Reading file...");
            else
                Console.WriteLine("Processing...");

            BinaryReader fileReader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read));
            string versionNumber;

            // readHeader returns true if endianness check fails
            if(readHeader(fileReader, out versionNumber))
                return;
                        
            // file header read; at first file block
            readBlocks(verbose, fileReader, versionNumber);

            // create PopulatedStructures
            List<PopulatedStructure> structures = createStructures(verbose);

            // write html file
            writeFile(path, verbose, versionNumber, structures);

            // "press any key to continue"
            Console.ReadLine();
        }

        private static string getPath(string[] args, ref bool verbose)
        {
            string path = null;
            if(args.Length == 0)
            {
                Console.WriteLine("Alternative usage: BlenderFileReader.exe [path] [-v]. -v designates verbose mode.");
                Console.WriteLine("Output will be in the form of [filename].html.");
                Console.Write("Path to file to parse: ");
                path = getPathFromUser();
            }
            else
            {
                if(args[0] != "-v")
                    path = args[0];
                else
                {
                    verbose = true;
                    path = getPathFromUser();
                }
                if(args.Length > 1 && args[1] == "-v")
                    verbose = true;
            }
            return path;
        }

        private static string getPathFromUser()
        {
            Console.Write("Path to file to parse: ");
            string path = Console.ReadLine();
            if(path[0] == '\"')
                path = path.Substring(1);
            if(path[path.Length - 1] == '\"')
                path = path.Substring(0, path.Length - 1);
            return path;
        }

        private static bool readHeader(BinaryReader fileReader, out string versionNumber)
        {
            versionNumber = null;
            fileReader.ReadBytes(7); // read out 'BLENDER'
            PointerSize = Convert.ToChar(fileReader.ReadByte()) == '_' ? 4 : 8; // '_' = 4, '-' = 8
            char endianness = Convert.ToChar(fileReader.ReadByte()); // 'v' = little, 'V' = big

            if((endianness == 'v' && !BitConverter.IsLittleEndian) || (endianness == 'V' && BitConverter.IsLittleEndian)
                || (endianness != 'v' && endianness != 'V'))
            {
                Console.WriteLine("Endianness of computer does not appear to match endianness of file. Open the file in Blender and save it to convert.");
                Console.Read();
                return true;
            }

            versionNumber = new string(new[] { Convert.ToChar(fileReader.ReadByte()), '.', Convert.ToChar(fileReader.ReadByte()),
                Convert.ToChar(fileReader.ReadByte()) });
            return false;
        }

        private static void readBlocks(bool verbose, BinaryReader fileReader, string versionNumber)
        {
            do
            {
                FileBlock.ReadBlock(fileReader);
            } while(fileReader.BaseStream.Position < fileReader.BaseStream.Length);

            if(verbose)
            {
                Console.WriteLine("Read successful. Information:");
                Console.WriteLine("Blender version number: " + versionNumber);
                Console.WriteLine("Number of file blocks (including SDNA and ENDB): " + FileBlock.GetBlockList().Count);
                Console.WriteLine("Number of structures in SDNA: " + StructureDNA.StructureList.Count);
                Console.WriteLine("Number of types defined in SDNA: " + StructureDNA.TypeList.Count);
                Console.WriteLine("Number of names defined in SDNA: " + StructureDNA.NameList.Count);
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
        }

        private static List<PopulatedStructure> createStructures(bool verbose)
        {
            if(verbose)
                Console.WriteLine("Attaching data in file blocks to structure templates...");
            List<FileBlock> blocks = FileBlock.GetBlockList();
            List<PopulatedStructure> structures = new List<PopulatedStructure>();
            foreach(FileBlock b in blocks)
            {
                PopulatedStructure[] temp = PopulatedStructure.ParseFileBlock(b);
                if(temp[0] != null)
                    structures.AddRange(temp);
            }

            if(PopulatedStructure.WarningMessages.Count > 0)
                Console.WriteLine(PopulatedStructure.WarningMessages.Count + " warnings encountered, more information will be written to file.");
            if(verbose)
            {
                Console.WriteLine("Structure construction complete. Press Enter to continue...");
                Console.ReadLine();
            }
            return structures;
        }

        private static void writeFile(string path, bool verbose, string versionNumber, List<PopulatedStructure> structures)
        {
            if(verbose)
                Console.WriteLine("Writing to file " + path.Substring(path.LastIndexOf('\\') + 1, path.LastIndexOf('.') - path.LastIndexOf('\\') - 1) + ".html" + "...");

            new HtmlWriter(path.Substring(path.LastIndexOf('\\') + 1, path.LastIndexOf('.') - path.LastIndexOf('\\') - 1) + ".html", path.Substring(path.LastIndexOf('\\') + 1, path.LastIndexOf('.') - path.LastIndexOf('\\') - 1) + ".html").WriteBlendFileToHtml(structures, versionNumber);

            if(verbose)
                Console.WriteLine("File output complete. Press Enter to exit.");
            else
                Console.WriteLine("Processing complete. Output in " + path.Substring(path.LastIndexOf('\\') + 1, path.LastIndexOf('.') - path.LastIndexOf('\\') - 1) + ".html.");
        }
    }
}
