using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlenderFileReader;

namespace FileReaderDriver
{
    class Program
    {
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

            BlenderFile reader = new BlenderFile(path, verbose);

            // write html file
            writeFile(path, verbose, reader);

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

        private static void writeFile(string path, bool verbose, BlenderFile reader)
        {
            if(verbose)
                Console.WriteLine("Writing to file " + path.Substring(path.LastIndexOf('\\') + 1, path.LastIndexOf('.') - path.LastIndexOf('\\') - 1) + ".html" + "...");

            new HtmlWriter(reader, path.Substring(path.LastIndexOf('\\') + 1, path.LastIndexOf('.') - path.LastIndexOf('\\') - 1) + ".html", path.Substring(path.LastIndexOf('\\') + 1, path.LastIndexOf('.') - path.LastIndexOf('\\') - 1) + ".html").WriteBlendFileToHtml();

            if(verbose)
                Console.WriteLine("File output complete. Press Enter to exit.");
            else
                Console.WriteLine("Processing complete. Output in " + path.Substring(path.LastIndexOf('\\') + 1, path.LastIndexOf('.') - path.LastIndexOf('\\') - 1) + ".html.");
        }
    }
}
