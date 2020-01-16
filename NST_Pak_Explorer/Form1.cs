using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace NST_Pak_Explorer {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }
        public IGA iga;
        private EndiannessAwareBinaryReader.Endianness endianness;

        private void buildTree(bool no_lzma) {
            if (iga == null) return;
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();

            treeView1.Nodes.Add("IGA");
            for (int i = 0; i < iga.file.Count; ++i) {
                File f = iga.file[i];
                TreeNode parent = treeView1.Nodes[0];
                TreeNode node;
                if (iga.version == 10)
                {
                    node = new TreeNode(f.getFullName().Split('\\').Last());
                }
                else
                {
                    node = new TreeNode(f.getFullName().Split('/').Last());
                }

                node.Tag = i;
                String[] path;
                if (iga.version == 10)
                {
                    path = f.getFullName().Split('\\');
                }
                else
                {
                    path = f.getFullName().Split('/');
                }
                for (int j = 0; j < path.Length - 1; ++j) {
                    TreeNode[] res = parent.Nodes.Find(path[j], false);
                    if (res.Length == 0) {
                        parent = parent.Nodes.Add(path[j], path[j]);
                    } else {
                        parent = res[0];
                    }
                }

                parent.Nodes.Add(node);
            }

            treeView1.EndUpdate();
        }
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e) {
            updateInfo();
        }
        private void updateInfo() {
            TreeNode node = treeView1.SelectedNode;
            if (node == null) return;
            textBox1.Clear();
            if (node.Nodes.Count > 0) return;
            File f = iga.file[(int)node.Tag];
            textBox1.AppendText("ID: " + ((UInt32)(f.getID())).ToString() + System.Environment.NewLine);
            textBox1.AppendText("Name: " + f.getRelName().Split('/').Last() + System.Environment.NewLine);
            textBox1.AppendText("Offset: " + f.getOffset().ToString() + System.Environment.NewLine);
            textBox1.AppendText("Size: " + f.getSize().ToString() + System.Environment.NewLine);
            textBox1.AppendText("Ordinal: " + f.getOrdinal().ToString() + System.Environment.NewLine);
            switch (f.getCompression()) {
                case Compression.NONE:
                    textBox1.AppendText("Compression: NONE" + System.Environment.NewLine);
                    break;
                case Compression.LZMA:
                    textBox1.AppendText("Compression: LZMA" + System.Environment.NewLine);
                    break;
                case Compression.DEFLATE:
                    textBox1.AppendText("Compression: DEFLATE" + System.Environment.NewLine);
                    break;
            }
        }
        private void button2_Click(object sender, EventArgs e) {
            if (button2.Text == "No LZMA") {
                button2.Text = "All";
                buildTree(true);
            } else {
                button2.Text = "No LZMA";
                buildTree(false);
            }
        }

        private void button4_Click(object sender, EventArgs e) {
            if (openFileDialog1.ShowDialog() == DialogResult.OK) {
                if (radioButton1.Checked)
                {
                    endianness = EndiannessAwareBinaryReader.Endianness.Little;
                }
                else if (radioButton2.Checked)
                {
                    endianness = EndiannessAwareBinaryReader.Endianness.Big;
                }
                iga = new IGA(openFileDialog1.FileName, endianness);
                buildTree(false);
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            if (openFileDialog2.ShowDialog() == DialogResult.OK) {
                int index = (int)treeView1.SelectedNode.Tag;
                FileInfo info = new FileInfo(openFileDialog2.FileName);
                iga.replace(index, openFileDialog2.FileName, 0, (int)info.Length);
                updateInfo();
            }
        }

        private void button3_Click(object sender, EventArgs e) {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
                iga.repack(saveFileDialog1.FileName, progressBar1, endianness);
            }
        }

        private void button5_Click(object sender, EventArgs e) {
            if (treeView1.SelectedNode == null) {
                return;
            }
            if (treeView1.SelectedNode.Nodes.Count != 0) {
                if (folderBrowserDialog3.ShowDialog() == DialogResult.OK) {
                    String path_base = folderBrowserDialog3.SelectedPath;
                    goDeeper(path_base, treeView1.SelectedNode);
                }
            } else {
                int index = (int)treeView1.SelectedNode.Tag;
                File f = iga.file[index];
                saveFileDialog2.FileName = f.getRelName().Replace('\\', '/').Split('/').Last();
                if (saveFileDialog2.ShowDialog() == DialogResult.OK) {
                    iga.extract(index, saveFileDialog2.FileName, endianness);
                }
            }
        }
        private void goDeeper(String base_path, TreeNode node) {
            if (node.Nodes.Count != 0) {
                System.IO.Directory.CreateDirectory(base_path + "\\" + node.Text);
                base_path += "\\" + node.Text;
                foreach (TreeNode c in node.Nodes) {
                    goDeeper(base_path, c);
                }
            } else {
                int index = (int)node.Tag;
                File f = iga.file[index];
                iga.extract(index, base_path + "\\" + f.getRelName().Replace('\\', '/').Split('/').Last(), endianness);
            }
        }
        private void Form1_Load(object sender, EventArgs e) {
            
        }
        
        private void button6_Click(object sender, EventArgs e) {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK) {
                iga.normalize(folderBrowserDialog1.SelectedPath + '/');
                updateInfo();
            }
        }

        private void button2_Click_1(object sender, EventArgs e) {
            Import import = new Import();
            import.par = this;
            import.ShowDialog();
            buildTree(true);
        }

        private void button7_Click(object sender, EventArgs e) {
            if (openFileDialog3.ShowDialog() == DialogResult.OK) {
                if (folderBrowserDialog2.ShowDialog() == DialogResult.OK) {
                    String NST_Folder = folderBrowserDialog2.SelectedPath.Replace('\\', '/') + '/';
                    String Patch_Folder = openFileDialog3.FileName.Replace('\\', '/');
                    while (Patch_Folder.Last() != '/') Patch_Folder = Patch_Folder.Remove(Patch_Folder.Length - 1);
                    FileStream patch = new FileStream(openFileDialog3.FileName, FileMode.Open, FileAccess.Read);
                    StreamReader reader = new StreamReader(patch);
                    IGA iga = null;
                    IGA import = null;
                    Int32 index = 0; UInt32 ID = 0; FileInfo info = null; IGA tmp_pak = null; File file = null; String open = "";
                    Int32 line_num = 0;
                    while (!reader.EndOfStream) {
                        line_num++;
                        String line = reader.ReadLine();
                        String[] CMD = line.Split(' ');
                        for (int i = 0; i < CMD.Length; ++i) {
                            CMD[i] = CMD[i].Trim();
                        }
                        label1.Text = CMD[0] + "(" + open + ")";
                        switch (CMD[0].ToLower()) { // open <pak>
                            case "open":
                                if (CMD.Length < 2) {
                                    MessageBox.Show("Too few arguments in line "+line_num.ToString()+" : " + line);
                                    reader.Close();
                                    return;
                                }
                                if (!System.IO.File.Exists(NST_Folder + CMD[1])) {
                                    MessageBox.Show("File "+ NST_Folder + CMD[1] + " does not exists in line " + line_num.ToString() + " : " + line);
                                    reader.Close();
                                    return;
                                }
                                open = CMD[1];
                                iga = new IGA(NST_Folder + CMD[1], endianness);
                                break;
                            case "repack": // repack
                                iga.repack(NST_Folder + open + "tmp",progressBar1, endianness);
                                if (!System.IO.File.Exists(NST_Folder + open + ".patch_bak"))
                                    System.IO.File.Move(NST_Folder + open, NST_Folder + open + ".patch_bak");
                                else
                                    System.IO.File.Delete(NST_Folder + open);
                                System.IO.File.Move(NST_Folder + open + "tmp", NST_Folder + open);
                                break;
                            case "replace":
                                switch(CMD[1].ToLower()) {
                                    case "assets": // replace assets <ID> <asset>
                                        if (CMD.Length < 4) {
                                            MessageBox.Show("Too few arguments in line " + line_num.ToString() + " : " + line);
                                            reader.Close();
                                            return;
                                        }
                                        if (!System.IO.File.Exists(Patch_Folder + CMD[3])) {
                                            MessageBox.Show("File "+ Patch_Folder + CMD[3] + " does not exists in line " + line_num.ToString() + " : " + line);
                                            reader.Close();
                                            return;
                                        }
                                        info = new FileInfo(Patch_Folder + CMD[3]);
                                        try {
                                            ID = UInt32.Parse(CMD[2]);
                                        }
                                        catch (Exception ex) {
                                            MessageBox.Show("Bad format of \"" + CMD[2] + "\" is not found in " + line_num.ToString() + " : " + line + "\nCheck for extra spaces between arguments.");
                                            reader.Close();
                                            return;
                                        }
                                        for (int i = 0; i < iga.file.Count; ++i) {
                                            if ((UInt32)iga.file[i].getID() == ID) {
                                                index = i;
                                                break;
                                            }
                                        }
                                        iga.replace(index, Patch_Folder + CMD[3], 0, (int)info.Length);
                                        break;
                                    case "pak": // replace pak <pak> <ID> <ID>
                                        if (CMD.Length < 5) {
                                            MessageBox.Show("Too few arguments in line " + line_num.ToString() + " : " + line);
                                            reader.Close();
                                            return;
                                        }
                                        tmp_pak = new IGA(NST_Folder + CMD[2], endianness);
                                        index = -1;
                                        try {
                                            ID = UInt32.Parse(CMD[3]);
                                        }
                                        catch (Exception ex) {
                                            MessageBox.Show("Bad format of \"" + CMD[3] + "\" is not found in " + line_num.ToString() + " : " + line + "\nCheck for extra spaces between arguments.");
                                            reader.Close();
                                            return;
                                        }
                                        for (int i = 0; i < tmp_pak.file.Count; ++i) {
                                            if ((UInt32)tmp_pak.file[i].getID() == ID) {
                                                index = i;
                                                break;
                                            }
                                        }
                                        if (index == -1) {
                                            MessageBox.Show("ID "+ID.ToString()+" is not found in " + line_num.ToString() + " : " + line);
                                            reader.Close();
                                            return;
                                        }
                                        file = null;
                                        try {
                                            ID = UInt32.Parse(CMD[4]);
                                        } catch (Exception ex) {
                                            MessageBox.Show("Bad format of \"" + CMD[4] + "\" is not found in " + line_num.ToString() + " : " + line + "\nCheck for extra spaces between arguments.");
                                            reader.Close();
                                            return;
                                        }
                                        for (int i = 0; i < tmp_pak.file.Count; ++i) {
                                            if ((UInt32)tmp_pak.file[i].getID() == ID) {
                                                file = tmp_pak.file[i];
                                                break;
                                            }
                                        }
                                        if (file == null) {
                                            MessageBox.Show("ID " + ID.ToString() + " is not found in " + line_num.ToString() + " : " + line);
                                            reader.Close();
                                            return;
                                        }
                                        iga.replace(index, NST_Folder + CMD[2], file.getSourceOffset(), file.getSize());
                                        break;
                                }
                                break;
                            case "open_import":
                                if (CMD.Length < 2) {
                                    MessageBox.Show("Too few arguments in: " + line);
                                    reader.Close();
                                    return;
                                }
                                if (!System.IO.File.Exists(NST_Folder + CMD[1])) {
                                    MessageBox.Show("File " + NST_Folder + CMD[1] + " does not exists in line " + line_num.ToString() + " : " + line);
                                    reader.Close();
                                    return;
                                }
                                import = new IGA(NST_Folder + CMD[1], endianness);
                                break;
                            case "import":
                                if (CMD.Length < 2) {
                                    MessageBox.Show("Too few arguments in: " + line);
                                    reader.Close();
                                    return;
                                }
                                index = -1;
                                try {
                                    ID = UInt32.Parse(CMD[1]);
                                }
                                catch (Exception ex) {
                                    MessageBox.Show("Bad format of \"" + CMD[1] + "\" is not found in " + line_num.ToString() + " : " + line + "\nCheck for extra spaces between arguments.");
                                    reader.Close();
                                    return;
                                }
                                for (int i = 0; i < tmp_pak.file.Count; ++i) {
                                    if ((UInt32)import.file[i].getID() == ID) {
                                        index = i;
                                        break;
                                    }
                                }
                                if (index == -1) {
                                    MessageBox.Show("ID " + ID.ToString() + " is not found in " + line_num.ToString() + " : " + line);
                                    reader.Close();
                                    return;
                                }
                                iga.add(import.file[index]);
                                break;
                        }
                    }
                    reader.Close();
                    MessageBox.Show("Done.");
                }
            }
         }
    }
}
