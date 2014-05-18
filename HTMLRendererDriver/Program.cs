using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlenderFileReader;

namespace HTMLRendererDriver
{
    class Program
    {
        static void Main(string[] args)
        {
            // gets path from args or user
            string path = getPath(args);

            Console.WriteLine("Processing...");

            BlenderFile reader = new BlenderFile(path);

            // write html file
            writeFile(path, reader);

            // "press any key to continue"
            Console.ReadLine();
        }

        private static string getPath(string[] args)
        {
            string path = null;
            if(args.Length == 0)
            {
                Console.WriteLine("Alternative usage: BlenderFileReader.exe [path]");
                Console.WriteLine("Output will be in the form of [filename].html.");
                path = getPathFromUser();
            }
            else
                path = args[0];

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

        private static void writeFile(string path, BlenderFile reader)
        {
            new HtmlWriter(reader, path.Substring(path.LastIndexOf('\\') + 1, path.LastIndexOf('.') - path.LastIndexOf('\\') - 1) + ".html", path.Substring(path.LastIndexOf('\\') + 1, path.LastIndexOf('.') - path.LastIndexOf('\\') - 1) + ".html").WriteBlendFileToHtml();

            Console.WriteLine("Processing complete. Output in " + path.Substring(path.LastIndexOf('\\') + 1, path.LastIndexOf('.') - path.LastIndexOf('\\') - 1) + ".html.");
        }
    }
}
