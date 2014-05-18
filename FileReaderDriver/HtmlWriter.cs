using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlenderFileReader;

namespace FileReaderDriver
{
    /// <summary>
    /// An object that writes a file to a handy HTML file.
    /// </summary>
    class HtmlWriter
    {
        private string path;
        private string friendly;

        private int tabDepth = 0;
        private BlenderFile parsedFile;
        private string tabs { get { string s = ""; for(int i = 0; i < tabDepth; i++) s += "    "; return s; } }

        private Stack<string> breadcrumbs = new Stack<string>();

        /// <summary>
        /// Creates a new HtmlWriter.
        /// </summary>
        /// <param name="path">Output path.</param>
        /// <param name="friendlyFileName">Input file's friendly name ("cow.blend").</param>
        public HtmlWriter(BlenderFile parsedFile, string path, string friendlyFileName)
        {
            this.path = path;
            friendly = friendlyFileName;
            this.parsedFile = parsedFile;
        }

        /// <summary>
        /// Writes information to HTML.
        /// </summary>
        /// <param name="structures">List of populated structures.</param>
        public void WriteBlendFileToHtml()
        {
            writeCSS();
            using(StreamWriter writer = new StreamWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
            {
                writer.WriteLine("<!DOCTYPE html>");
                writeStartTag(writer, "html", "lang=\"en\"");
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
            foreach(PopulatedStructure[] block in parsedFile.Structures)
            {
                int index = 0;
                bool outer_odd = true;
                writeStartTag(writer, "div", "class=\"structure " + block[0].Type + (block.Length > 1 ? " list" : "") + "\"");
                writeTable(writer, new[] { "structure_head" },
                    new[] { "Structure Type:", "Structure Size:", "Number of Fields:", "File Block Address:" }, "0x" + block[0].ContainingBlock.OldMemoryAddress.ToString("X" + (parsedFile.PointerSize * 2)),
                    false, new[] { block[0].Type + (block.Length > 1 ? "[" + block.Length + "]" : ""), block[0].Size.ToString(), block[0].FlattenedData.Count.ToString(), "0x" + block[0].ContainingBlock.OldMemoryAddress.ToString("X") });
                if(block.Length > 1)
                {
                    writeStartTag(writer, "table", "class=\"structure_body\"");
                    writeTableHead(writer, new[] { "Index", "Structure" });
                }
                foreach(PopulatedStructure s in block)
                {
                    if(block.Length > 1)
                    {
                        writeStartTag(writer, "tr", "class=\"index " + (outer_odd ? "odd" : "even") + "\"");
                        outer_odd = !outer_odd;
                        writeStartTag(writer, "td", "class=\"first\"");
                        writer.Write(index++);
                        writeEndTag(writer); // </td>
                        writeStartTag(writer, "td");
                    }
                    bool odd = true;
                    int fieldNumber = 0;

                    writeStartTag(writer, "table", "class=\"structure_body\"");
                    writeTableHead(writer, new[] { "Field No.", "Identifier", "Parent Object Type", "Field Type", "Field Size", "Field Value" });
                    foreach(FieldInfo field in s.FlattenedData)
                    {
                        writeField(writer, field, odd, fieldNumber++);
                        odd = !odd;
                    }
                    writeEndTag(writer); // </table>
                    if(block.Length > 1)
                    {
                        writeEndTag(writer); // </td>
                        writeEndTag(writer); // </tr>
                    }
                }
                if(block.Length > 1)
                    writeEndTag(writer); // </table>
                writeEndTag(writer); // </div>
            }
            writeRawDataInfo(writer);
        }

        private void writeRawDataInfo(StreamWriter writer)
        {
            writeStartTag(writer, "div", "id=\"raw_blocks\"");
            string[][] rows = new string[PopulatedStructure.RawBlockMessages.Count][];
            for(int i = 0; i < PopulatedStructure.RawBlockMessages.Count; i++)
                rows[i] = PopulatedStructure.RawBlockMessages[i].Split(' ');
            for(int i = 0; i < rows.Length; i++)
                rows[i][1] = "<a id=\"0x" + rows[i][1] + "\">0x" + rows[i][1] + "</a>";
            writeTable(writer, new[] { "Raw Data Blocks:" });
            writeTable(writer, null, new[] { "Block number:", "Block Address:", "Block code:", "Block index:", "Bytes expected:", "Bytes given:" }, "raw_blocks_body", true, rows);
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
            if(!field.IsArray && field.IsPointer && field.Type != typeof(FieldInfo) && field.GetValueAsPointer() != "0x0")
            {
                FileBlock associatedBlock = parsedFile.GetBlockByAddress(parsedFile.PointerSize == 4 ? field.GetValueAsUInt() : field.GetValueAsULong());
                if(associatedBlock != null)
                    typeName += " (points to " + (associatedBlock.Size == associatedBlock.Count * StructureDNA.StructureList[associatedBlock.SDNAIndex].StructureTypeSize ? 
                        StructureDNA.StructureList[associatedBlock.SDNAIndex].StructureTypeName : "raw data") + ")";
            }

            writeTableRow(odd ? "odd" : "even", writer, fieldNumber.ToString(), field.Name, field.ParentType, typeName, field.Length > 1 ? field.Size + " * " + field.Length + " (" + (field.Size * field.Length) + ")" : (field.Size * field.Length).ToString(), fieldVal);
        }

        private void writeBodyHead(StreamWriter writer)
        {
            writeTable(writer, null, new[] { "Version Number", "File Blocks", "Structures", "Types", "Names", "<a href=\"#raw_blocks\">Raw Data Blocks</a>" }, "blender_header",
                               false, new[] { parsedFile.VersionNumber, parsedFile.GetBlockList().Count.ToString(), StructureDNA.StructureList.Count.ToString(), StructureDNA.TypeList.Count.ToString(), StructureDNA.NameList.Count.ToString(), PopulatedStructure.RawBlockMessages.Count.ToString() });
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

        private void writeTableRow(string htmlClass, StreamWriter writer, params string[] row)
        {
            string data = htmlClass != null ? "class=\"" + htmlClass + "\"" : "";
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

tr.even
{
	background-color:rgb(190,190,190);
}

tr.odd
{
	background-color:rgb(170,170,230);
}

tr.index .first
{
    text-align: center;
}

tr.index td table
{
    margin: 0;
    border: none;
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
