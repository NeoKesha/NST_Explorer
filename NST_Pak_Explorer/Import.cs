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
    public partial class Import : Form {
        public Import() {
            InitializeComponent();
        }
        IGA iga;
        List<File> files1;
        List<File> files2 = new List<File>();
        private EndiannessAwareBinaryReader.Endianness endianness;
        private void Import_Load(object sender, EventArgs e) {
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
                files1 = iga.file;
                buildTree(treeView1, files1);
                buildTree(treeView2, files2);
            } else {
                this.Close();
            }
        }
        private void buildTree(TreeView tree, List<File> files) {
            tree.BeginUpdate();
            tree.Nodes.Clear();
            tree.Nodes.Add("IGA");
            for (int i = 0; i < files.Count; ++i) {
                TreeNode parent = tree.Nodes[0];
                TreeNode node = new TreeNode(files[i].getFullName().Split('/').Last());
                node.Tag = files[i];
                String[] path = files[i].getFullName().Split('/');
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

            tree.EndUpdate();
        }
        int a = 0;
        private void button1_Click(object sender, EventArgs e) {
            Add(treeView1.SelectedNode, files2, files1);
            buildTree(treeView2, files2);
        }
        private void Add(TreeNode node, List<File> files, List<File> src) {
            if (node.Nodes.Count == 0) {
                files.Add((File)node.Tag);
                src.Remove((File)node.Tag);
                node.Tag = null;
            } else {
                for (int i = 0; i < node.Nodes.Count; ++i) {
                    Add(node.Nodes[i], files,src);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e) {
            Add(treeView2.SelectedNode, files1, files2);
            buildTree(treeView1, files1);
        }
        public Form1 par;
        private void button3_Click(object sender, EventArgs e) {
            for (int i = 0; i < files2.Count; ++i) {
                par.iga.add(files2[i]);
            }
            Close();
        }

        private void button4_Click(object sender, EventArgs e) {
            buildTree(treeView1, files1);
            buildTree(treeView2, files2);
        }
    }
}
