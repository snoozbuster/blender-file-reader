using BlenderFileReader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlenderFileBrowser
{
    public partial class MainForm : Form
    {
        private BlenderFile loadedFile;

        private Dictionary<string, Dictionary<string, string>> commentsIndex = new Dictionary<string, Dictionary<string, string>>();

        public MainForm()
        {
            InitializeComponent();

            pointedToValueTreeView.NodeMouseDoubleClick += pointedToValueTreeView_NodeMouseDoubleClick;
            // I bound these to mouse click instead of node select because that way the binding source updates
            // correctly when clicking between the two trees.
            pointedToValueTreeView.NodeMouseClick += fileTree_AfterSelect;
            fileTree.NodeMouseClick += fileTree_AfterSelect;

            readComments();
        }

        private void readComments()
        {
            try
            {
                TextReader reader = new StreamReader(File.Open("comments.txt", FileMode.Open, FileAccess.Read));
                while(true) // counting on an exception to toss us out
                {
                    string line = reader.ReadLine();
                    while(line[0] != '[')
                        line = reader.ReadLine();
                    while(line[0] == '[')
                    {
                        string typeName = line.Substring(1, line.Length - 2);
                        if(!commentsIndex.ContainsKey(typeName))
                            commentsIndex.Add(typeName, new Dictionary<string, string>());
                        Dictionary<string, string> typeDict = commentsIndex[typeName];
                        string comment = reader.ReadLine();
                        while(comment != "\n" && comment != "" && comment != null)
                        {
                            string[] parts = comment.Split('=');
                            if(typeDict.ContainsKey(parts[0]))
                                typeDict[parts[0]] = parts[1];
                            else
                                typeDict.Add(parts[0], parts[1]);
                            comment = reader.ReadLine();
                        }
                        line = reader.ReadLine();
                    }
                }
            }
            catch { }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            blendFileOpenDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if(blendFileOpenDialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                loadFile(blendFileOpenDialog.FileName);
        }

        private void loadFile(string filename)
        {
            loadedFile = new BlenderFile(filename);
            fileTree.Nodes.Clear();
            fileTree.ShowRootLines = true;
            fileTree.BeginUpdate();
            foreach(Structure[] structure in loadedFile.Structures)
                fileTree.Nodes.Add(loadNode(structure));
            TreeNode rawBlocks = new TreeNode("[Raw data blocks]");
            foreach(string s in loadedFile.RawBlockMessages)
                rawBlocks.Nodes.Add(new TreeNode("[" + s.Split(' ')[0] + "]: " + s.Split(' ')[1]) { Tag = s });
            fileTree.Nodes.Add(rawBlocks);
            fileTree.EndUpdate();
        }

        private TreeNode loadNode(Structure[] structure)
        {
            string pointerData = " (at 0x" + structure[0].ContainingBlock.OldMemoryAddress.ToString("X" + loadedFile.PointerSize * 2) + ")";
            string typeName = structure[0].TypeName;
            if(structure.Length > 1)
                typeName = "[List of " + typeName + "s]" + pointerData;
            else
                typeName += pointerData;

            // set up the node
            TreeNode root = new TreeNode(typeName);
            if(structure.Length > 1)
            {
                root.Tag = structure;
                int i = 0;
                foreach(Structure s in structure)
                {
                    TreeNode output = new TreeNode("[" + i++ + "]: " + s.Name + " (type: " + s.TypeName + ")");
                    output.Tag = structure;
                    parseStructure(ref output, s);
                    root.Nodes.Add(output);
                }
            }
            else
            {
                root.Tag = structure[0];
                parseStructure(ref root, structure[0]);
            }

            return root;
        }

        private TreeNode parseStructure(Structure structure)
        {
            TreeNode output = new TreeNode(structure.Name + " (type: " + structure.TypeName + ")");
            output.Tag = structure;
            parseStructure(ref output, structure);
            return output;
        }

        // instead of returning a tree node, adds the stuff to the supplied node
        // turns out this version is more useful
        private void parseStructure(ref TreeNode node, Structure structure)
        {
            // since I don't want to flatten the structure during iteration, iterate over structure.Fields
            Structure s;
            foreach(IField field in structure.Fields)
                if((s = field as Structure) != null)
                    node.Nodes.Add(parseStructure(s));
                else
                    node.Nodes.Add(new TreeNode(field.Name + " (type: " + field.TypeName + ")") { Tag = field });
        }

        private void fileTree_AfterSelect(object sender, TreeNodeMouseClickEventArgs e)
        {
            valueLinkLabel.Links[0].Enabled = false;
            iFieldBindingSource.Clear();
            if(sender == fileTree)
            {
                pointedToValueTreeView.Enabled = false;
                pointedToValueTreeView.Nodes.Clear();
            }
            if(e.Node.Tag != null)
            {
                IField field = e.Node.Tag as IField;
                if(field != null)
                {
                    iFieldBindingSource.Add(field);

                    totalTextBox.Text = (field.Size * field.Length).ToString();
                    valueTextBox.Text = field.ToString();
                    commentsBox.Text = getCommentsText(field);

                    if(field.IsPointer && field.ToString() != "0x0")
                    {
                        valueLinkLabel.Links[0].Enabled = true;
                        if(sender == fileTree)
                            populatedPointedTo(field);
                    }
                }
                else
                {
                    Structure[] structure = e.Node.Tag as Structure[];
                    if(structure != null)
                    {
                        valueTextBox.Text = "[List]";
                        totalTextBox.Text = "";
                    }
                    else
                    {
                        // this is getting silly
                        string s = e.Node.Tag as string;
                        if(s != null)
                        {
                            string[] split = s.Split(' ');
                            valueTextBox.Text = string.Format("Block number: {0} Block address: 0x{1} Block code: {2} Size: {3}",
                                split[0], split[1], split[2], split[5]);
                            totalTextBox.Text = "";
                        }
                    }
                }
            }
        }

        private string getCommentsText(IField field)
        {
            // we'll assume that all dictionary lookups work, and if they don't return empty.
            try
            {
                IStructField structure = field as IStructField;
                if(structure != null)
                    return commentsIndex[structure.TypeName]["(this)"]; // keyword for info about a struct itself
                return commentsIndex[field.Parent.TypeName][field.Name];
            }
            catch
            {
                return "";
            }
        }

        private void populatedPointedTo(IField field)
        {
            try
            {
                Structure[] structures = field.Dereference();
                pointedToValueTreeView.Enabled = true;
                pointedToValueTreeView.Nodes.Clear();
                pointedToValueTreeView.BeginUpdate();
                foreach(TreeNode node in loadNode(structures).Nodes)
                    pointedToValueTreeView.Nodes.Add(node);
                pointedToValueTreeView.EndUpdate();
            }
            catch
            {
                // still dunno what to do
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void pointedToValueTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            IField field = e.Node.Tag as IField;
            if(field != null && field.IsPointer && field.ToString() != "0x0")
                populatedPointedTo(field);
        }

        private void valueLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            IField field = fileTree.SelectedNode.Tag as IField;
            if(field != null && field.IsPointer)
            {
                try
                {
                    Structure structure = field.Dereference()[0];
                    foreach(TreeNode node in fileTree.Nodes)
                        if(node.Text.Contains(structure.ContainingBlock.OldMemoryAddress.ToString("X" + loadedFile.PointerSize * 2)))
                        {
                            node.Expand();
                            fileTree.SelectedNode = node;
                            break;
                        }
                }
                catch
                {
                    // field is an array of pointers, not sure what to do with that
                }
            }
        }
    }
}
