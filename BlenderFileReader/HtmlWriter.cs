using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderFileReader
{
    /// <summary>
    /// An object that writes a file to a handy HTML file.
    /// </summary>
    class HtmlWriter
    {
        private string path;
        private string friendly;

        private string versionNumber;
        private List<PopulatedStructure> structures;

        private int tabDepth = 0;
        private string tabs { get { string s = ""; for(int i = 0; i < tabDepth; i++) s += "    "; return s; } }

        private Stack<string> breadcrumbs = new Stack<string>();

        /// <summary>
        /// Creates a new HtmlWriter.
        /// </summary>
        /// <param name="path">Output path.</param>
        /// <param name="friendlyFileName">Input file's friendly name ("cow.blend").</param>
        public HtmlWriter(string path, string friendlyFileName)
        {
            this.path = path;
            friendly = friendlyFileName;
        }

        /// <summary>
        /// Writes information to HTML.
        /// </summary>
        /// <param name="structures">List of populated structures.</param>
        public void WriteBlendFileToHtml(List<PopulatedStructure> structures, string versionNumber)
        {
            this.versionNumber = versionNumber;
            this.structures = structures;
            writeCSS();
            using(StreamWriter writer = new StreamWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            {
                writer.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHMTL 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1-transitional.dtd\">");
                writeStartTag(writer, "html", "xmlns=\"http://www.w3.org/1999/xhtml\" lang=\"en\" xml:lang=\"en\"");
                writeHtmlHead(writer); // <head> ... </head>
                writeBody(writer); // <body> ... </body>
                writeEndTag(writer); // </html>
            }
        }

        private void writeBody(StreamWriter writer)
        {
            writeStartTag(writer, "body");
            writeStartTag(writer, "div", "id=\"container\"");
            writeBodyHead(writer);
            writeBodyContent(writer);
            writeEndTag(writer); // </div>
            writeEndTag(writer); // </body>
        }

        private void writeBodyContent(StreamWriter writer)
        {
            foreach(PopulatedStructure s in structures)
            {
                bool odd = true;
                int fieldNumber = 0;
                writeStartTag(writer, "div", "id=\"" + s.Type + "\" class=\"structure\"");
                writeTable(writer, new[] { "structure_head" }, 
                    new[] { "Structure Type:", "Structure Size:", "Number of Fields:", "File Block Address:" }, "0x" + s.ContainingBlock.OldMemoryAddress.ToString("X"),
                    false, new[] { s.Type, s.Size.ToString(), s.FlattenedData.Count.ToString(), "0x" + s.ContainingBlock.OldMemoryAddress.ToString("X") });
                //writer.WriteLine("<table border=\"1\" id=\"0x" + s.ContainingBlock.OldMemoryAddress.ToString("X") + "\" alt=\"0x" + s.ContainingBlock.OldMemoryAddress.ToString("X") + "\">");
                //writer.WriteLine("</table>");

                writeStartTag(writer, "table", "class=\"structure_body\"");
                writeTableHead(writer, new[] { "Field No.", "Identifier", "Parent Object Type", "Field Type", "Field Size", "Field Value" });
                foreach(FieldInfo field in s.FlattenedData)
                {
                    writeField(writer, field, odd, fieldNumber++);
                    odd = !odd;
                }
                writeEndTag(writer); // </table>
                writeEndTag(writer); // </div>
            }
            writeWarnings(writer);
        }

        private void writeWarnings(StreamWriter writer)
        {
            writeStartTag(writer, "div", "id=\"warnings\"");
            string[][] rows = new string[PopulatedStructure.WarningMessages.Count][];
            for(int i = 0; i < PopulatedStructure.WarningMessages.Count; i++)
                rows[i] = PopulatedStructure.WarningMessages[i].Split(' ');
            writeTable(writer, new[] { "Warnings:" });
            writeTable(writer, null, new[] { "Block number:", "Block code:", "Block index:", "Bytes expected:", "Bytes given:" }, "warnings_body", true, rows);
            writeEndTag(writer);
        }

        private void writeField(StreamWriter writer, FieldInfo field, bool odd, int fieldNumber)
        {
            string fieldVal = field.ToString();
            if(field.IsPointer)
            {
                if(field.IsArray)
                {
                    for(int i = 0; i < fieldVal.Length; i++)
                        if(fieldVal[i] == '0') // we can assume this is the start of a pointer, so the next few chars are valid 
                        {
                            int j = 0;
                            do
                            {
                                j++;
                            } while(fieldVal[i + j] != ',' && fieldVal[i + j] != ' ');
                            if(fieldVal.Substring(i, j) != "0x0")
                            {
                                string newString = "<a href=\"#" + fieldVal.Substring(i, j) + "\">" + fieldVal.Substring(i, j) + "</a>";
                                fieldVal = fieldVal.Replace(fieldVal.Substring(i, j), newString);
                                j = newString.Length;
                            }
                            i += j;
                        }
                }
                else
                {
                    if(fieldVal != "0x0")
                        fieldVal = "<a href=\"#" + fieldVal + "\">" + fieldVal + "</a>";
                }
                        
            }

            string typeName = field.TypeName + (field.IsArray ? (field.IsMultidimensional ? "[]" : "") + "[]" : "");
            if(!field.IsArray && field.IsPointer && field.TypeName == "void" && field.GetValueAsPointer() != "0x0")
            {
                FileBlock associatedBlock = FileBlock.GetBlockByAddress(Program.PointerSize == 4 ? field.GetValueAsUInt() : field.GetValueAsULong());
                if(associatedBlock != null)
                    typeName += " (points to " + StructureDNA.StructureList[associatedBlock.SDNAIndex].StructureTypeName + ")";
            }

            writeTableRow(odd ? "odd" : "even", writer, fieldNumber.ToString(), field.Name, field.ParentType, typeName, field.Length > 1 ? field.Size + " * " + field.Length + " (" + (field.Size * field.Length) + ")" : (field.Size * field.Length).ToString(), fieldVal);
        }

        private void writeBodyHead(StreamWriter writer)
        {
            writeTable(writer, null, new[] { "Version Number", "File Blocks", "Structures", "Types", "Names", "<a href=\"#warnings\">Warnings</a>" }, "blender_header", 
                               false, new[] { versionNumber, FileBlock.GetBlockList().Count.ToString(), StructureDNA.StructureList.Count.ToString(), StructureDNA.TypeList.Count.ToString(), StructureDNA.NameList.Count.ToString(), PopulatedStructure.WarningMessages.Count.ToString() });
        }

        private void writeTable(StreamWriter writer, string[] titles, params string[][] rows)
        {
            writeTable(writer, null, titles, null, false, rows);
        }

        private void writeTable(StreamWriter writer, string[] classes, string[] titles, string id, bool useOddEven, params string[][] rows)
        {
            string data = id != null ? "id=\"" + id + "\"" : "";
            if(classes != null)
            {
                data += " class=\"";
                for(int i = 0; i < classes.Length; i++)
                    data += classes[i] + " ";
                data = data.Substring(0, data.Length - 1) + "\"";
            }
            writeStartTag(writer, "table", data);
            writeTableHead(writer, titles);
            writeTableBody(writer, useOddEven, rows);
            writeEndTag(writer); // </table>
        }

        private void writeTableBody(StreamWriter writer, bool useOddEven, params string[][] rows)
        {
            bool odd = true;
            writeStartTag(writer, "tbody");
            for(int i = 0; i < rows.Length; i++)
                if(!useOddEven)
                    writeTableRow(writer, rows[i]);
                else
                {
                    writeTableRow(odd ? "odd" : "even", writer, rows[i]);
                    odd = !odd;
                }
            writeEndTag(writer); // </tbody>
        }

        private void writeTableHead(StreamWriter writer, params string[] titles)
        {
            writeStartTag(writer, "thead");
            writeStartTag(writer, "tr");
            for(int i = 0; i < titles.Length; i++)
                writeLine(writer, "<th>" + titles[i] + "</th>");
            writeEndTag(writer); // </tr>
            writeEndTag(writer); // </thead>
        }

        private void writeTableRow(StreamWriter writer, params string[] row)
        {
            writeTableRow(null, writer, row);
        }

        private void writeTableRow(string id, StreamWriter writer, params string[] row)
        {
            string data = id != null ? "id=\"" + id + "\"" : "";
            writeStartTag(writer, "tr", data);
            for(int i = 0; i < row.Length; i++)
                writeLine(writer, "<td>" + row[i] + "</td>");
            writeEndTag(writer);
        }

        private void writeHtmlHead(StreamWriter writer)
        {
            writeStartTag(writer, "head");
            writeLine(writer, "<title>Parsed data for " + friendly + "</title>");
            writeLine(writer, "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />");
            writeLine(writer, "<link rel=\"stylesheet\" type=\"text/css\" href=\"style.css\">");
            writeEndTag(writer); // </head>
        }

        private void writeStartTag(StreamWriter writer, string tag)
        {
            writeStartTag(writer, tag, null);
        }

        private void writeStartTag(StreamWriter writer, string tag, string attr)
        {
            if(attr == null)
                attr = "";
            else if(attr != "")
                attr = " " + attr;

            breadcrumbs.Push(tag);
            writeLine(writer, "<" + tag + attr + ">");
            tabDepth++;
        }

        private void writeEndTag(StreamWriter writer)
        {
            tabDepth--;
            writeLine(writer, "</" + breadcrumbs.Pop() + ">");
        }

        private void writeLine(StreamWriter writer, string text)
        {
            writer.WriteLine(tabs + text);
        }

        // I dislike this function because I can't make it nice and formatted.
        private void writeCSS()
        {
            if(File.Exists("style.css"))
                return;

            using(StreamWriter s = new StreamWriter(File.Open("style.css", FileMode.CreateNew)))
            {
                s.Write(
@"/*
Feel free to edit this file to preference; as long as it exists the utility will not overwrite it.
Just delete it and run the utility to return it to default settings.
*/

table
{
	width:100%;
	margin-top:2px;
}

thead, tr, td, tbody, table, th
{
	border:1px solid;
}

div
{
	width:100%;
	margin-top:15px;
}

thead
{
	background-color:rgb(230,170,170);
}

tr#even
{
	background-color:rgb(190,190,190);
}

tr#odd
{
	background-color:rgb(170,170,230);
}

div#container
{
	width:960px;
	margin-left:auto;
	margin-right:auto;
}

table.structure_head tbody tr, table#blender_header tbody tr
{
	background-color:rgb(190,190,190);
}

body
{
	background-color:rgb(230,230,230);
}"
);
            }
        }
    }
}
