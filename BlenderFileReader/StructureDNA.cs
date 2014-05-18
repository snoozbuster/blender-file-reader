using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlenderFileReader
{
    /// <summary>
    /// Represents the file's StructureDNA.
    /// </summary>
    public static class StructureDNA
    {
        private static bool created = false;

        /// <summary>
        /// Initializes the Structure DNA with the data from the DNA1 fileblock.
        /// </summary>
        /// <param name="data"></param>
        public static void Create(byte[] data)
        {
            if(created)
                throw new InvalidOperationException("Can't have more than one SDNA.");
            created = true;

            parseData(data);
        }

        /// <summary>
        /// List of all the names contained in SDNA.
        /// </summary>
        public static List<string> NameList { get; private set; }
        /// <summary>
        /// List of all the types and their sizes contained in SDNA.
        /// </summary>
        public static List<BlenderType> TypeList { get; private set; }
        /// <summary>
        /// List of all the names of the types in SDNA; used primarily for BlenderType and BlenderField's constructors.
        /// </summary>
        public static List<string> TypeNameList { get; private set; }
        /// <summary>
        /// List of all structures defined in SDNA.
        /// </summary>
        public static List<SDNAStructure> StructureList { get; private set; }
        /// <summary>
        /// List of all of the structures' types by index in TypeList/TypeNameList; used primarily for BlenderType and BlenderField's constructors.
        /// </summary>
        public static List<short> StructureTypeIndices { get; private set; }
        
        private static void parseData(byte[] Data)
        {
            int position = 0;
            position += 4; // "read" 'SDNA'
            
            char[] type = { Encoding.ASCII.GetChars(Data, position++, 1)[0], Encoding.ASCII.GetChars(Data, position++, 1)[0],  // read name ID to check
                            Encoding.ASCII.GetChars(Data, position++, 1)[0], Encoding.ASCII.GetChars(Data, position++, 1)[0] };
            if(type[0] != 'N' || type[1] != 'A' || type[2] != 'M' || type[3] != 'E')
                throw new InvalidOperationException("Failed reading SDNA: names could not be read.");

            int numberOfNames = BitConverter.ToInt32(Data, position);
            position += 4; // make sure to increment position
            while(position % 4 != 0) // next field is aligned at four bytes
                position++;

            NameList = new List<string>(numberOfNames);
            List<char> tempCharList = new List<char>();
            for(int i = 0; i < numberOfNames; i++)
            {
                char c;
                do
                {
                    c = Encoding.ASCII.GetChars(Data, position++, 1)[0];
                    tempCharList.Add(c);
                } while(c != '\0');
                tempCharList.RemoveAt(tempCharList.Count - 1); // removes terminating zero
                NameList.Add(new string(tempCharList.ToArray()));
                tempCharList.Clear();
            }
            
            while(position % 4 != 0) // next field is aligned at four bytes
                position++;

            type = new[] { Encoding.ASCII.GetChars(Data, position++, 1)[0], Encoding.ASCII.GetChars(Data, position++, 1)[0],  // read type ID to check
                            Encoding.ASCII.GetChars(Data, position++, 1)[0], Encoding.ASCII.GetChars(Data, position++, 1)[0] };
            if(type[0] != 'T' || type[1] != 'Y' || type[2] != 'P' || type[3] != 'E')
                throw new InvalidOperationException("Failed reading SDNA: types could not be read.");

            int numberOfTypes = BitConverter.ToInt32(Data, position);
            position += 4; // increment position to reflect the read

            TypeNameList = new List<string>(numberOfTypes);
            for(int i = 0; i < numberOfTypes; i++)
            {
                char c;
                do
                {
                    c = Encoding.ASCII.GetChars(Data, position++, 1)[0];
                    tempCharList.Add(c);
                } while(c != '\0');
                tempCharList.RemoveAt(tempCharList.Count - 1); // removes terminating zero
                TypeNameList.Add(new string(tempCharList.ToArray()));
                tempCharList.Clear();
            }

            while(position % 4 != 0) // next field is aligned at four bytes
                position++;

            type = new[] { Encoding.ASCII.GetChars(Data, position++, 1)[0], Encoding.ASCII.GetChars(Data, position++, 1)[0],  // read tlen ID to check
                            Encoding.ASCII.GetChars(Data, position++, 1)[0], Encoding.ASCII.GetChars(Data, position++, 1)[0] };
            if(type[0] != 'T' || type[1] != 'L' || type[2] != 'E' || type[3] != 'N')
                throw new InvalidOperationException("Failed reading SDNA: type lengths could not be read.");

            List<short> typeLengthList = new List<short>(numberOfTypes);
            for(int i = 0; i < numberOfTypes; i++)
            {
                typeLengthList.Add(BitConverter.ToInt16(Data, position));
                position += 2; // add to position to reflect the read
            }

            while(position % 4 != 0) // next field is aligned at four bytes
                position++;

            type = new[] { Encoding.ASCII.GetChars(Data, position++, 1)[0], Encoding.ASCII.GetChars(Data, position++, 1)[0],  // read structure ID to check
                            Encoding.ASCII.GetChars(Data, position++, 1)[0], Encoding.ASCII.GetChars(Data, position++, 1)[0] };
            if(type[0] != 'S' || type[1] != 'T' || type[2] != 'R' || type[3] != 'C')
                throw new InvalidOperationException("Failed reading SDNA: structures could not be read.");

            int numberOfStructures = BitConverter.ToInt32(Data, position);
            position += 4; // you know the drill

            StructureTypeIndices = new List<short>();
            List<Dictionary<short, short>> structureFields = new List<Dictionary<short, short>>();
            for(int i = 0; i < numberOfStructures; i++)
            {
                short structureTypeIndex = BitConverter.ToInt16(Data, position);
                position += 2;
                short numberOfFields = BitConverter.ToInt16(Data, position);
                position += 2;
                Dictionary<short, short> fieldDict = new Dictionary<short, short>();
                for(int j = 0; j < numberOfFields; j++)
                {
                    short typeOfField = BitConverter.ToInt16(Data, position);
                    position += 2;
                    short name = BitConverter.ToInt16(Data, position);
                    position += 2;
                    fieldDict.Add(name, typeOfField);
                }
                StructureTypeIndices.Add(structureTypeIndex);
                structureFields.Add(fieldDict);
            }

            TypeList = new List<BlenderType>();
            for(int i = 0; i < numberOfTypes; i++)
                TypeList.Add(new BlenderType(TypeNameList[i], typeLengthList[i]));

            StructureList = new List<SDNAStructure>(numberOfStructures);
            for(int i = 0; i < numberOfStructures; i++)
            {
                List<BlenderField> fields = new List<BlenderField>();
                for(int j = 0; j < structureFields[i].Count; j++)
                {
                    KeyValuePair<short, short> element = structureFields[i].ElementAt(j);
                    fields.Add(new BlenderField(element.Key, element.Value));
                }
                StructureList.Add(new SDNAStructure(StructureTypeIndices[i], fields));
            }

            foreach(SDNAStructure s in StructureList)
                s.InitializeFields();
        }
    }
}
