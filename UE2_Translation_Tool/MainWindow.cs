using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using C5;

namespace UE2_Translation_Tool {
    public partial class MainWindow : Form {

        OpenFileDialog ofd;

        StreamReader orgs, dests;
        StreamWriter finalw;

        HashDictionary<String, String> org, dest, diff;
        LinkedList<String> orgo, desto, diffo;

        public MainWindow() {
            InitializeComponent(); 
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            ofd = new OpenFileDialog();
            ofd.FileName = "original.frt";
            ofd.Title = "Original";
            ofd.FileOk += originalFileOk;
            ofd.ShowDialog();
        }

        private void originalFileOk(object sender, EventArgs e) {
            orgs = new StreamReader(ofd.OpenFile(), Encoding.Default);
            
            ofd.Dispose();

            ofd = new OpenFileDialog();
            ofd.FileName = "cible.frt";
            ofd.Title = "Cible";
            ofd.FileOk += targetFileOk;
            ofd.ShowDialog();
        }

        private void targetFileOk(object sender, EventArgs e) {
            dests = new StreamReader(ofd.OpenFile(), Encoding.Default);
            ofd.Dispose();

            Thread t = new Thread(processFiles);
            t.IsBackground = true;
            t.Name = "File Processing Thread";
            t.Start();
        }

        private void processFiles() {
            String[] buffer = new String[3];
            String w;

            org = new HashDictionary<string, string>();
            orgo = new LinkedList<string>();
            dest = new HashDictionary<string, string>();
            desto = new LinkedList<String>();

            Int32 orgstr = 0, deststr = 0;

            while (!orgs.EndOfStream) {
                w = orgs.ReadLine();
                if (w.StartsWith("[")) {
                    if (buffer[0] != null) {
                        org.Add(buffer[0], buffer[1]);
                        orgo.Add(buffer[0]);
                        buffer = new String[3];
                        orgstr++;
                    }
                    buffer[0] = w;
                }
                if (w.StartsWith("Caption")) { buffer[1] = w; }
            }

            orgs.Close();

            org.Add(buffer[0], buffer[1]);
            orgstr++;

            buffer = new String[3];

            while (!dests.EndOfStream) {
                w = dests.ReadLine();
                if (w.StartsWith("[")) {
                    if (buffer[0] != null) {
                        dest.Add(buffer[0], buffer[1]);
                        desto.Add(buffer[0]);
                        buffer = new String[3];
                        deststr++;
                    }
                    buffer[0] = w;
                }
                if (w.Split('=').Length >= 2) { buffer[1] = w; }
            }

            dests.Close();

            dest.Add(buffer[0], buffer[1]);
            deststr++;

            setText(label1, "Source : " + orgstr + " tokens");
            setText(label2, "Destination : " + deststr + " tokens");

            Int32 overlapstr = 0;
            diff = new HashDictionary<string, string>();
            diffo = new LinkedList<string>();
            foreach (String s in desto) {
                if (org.Keys.Contains(s)) {
                    overlapstr++;
                } else {
                    diff.Add(s, dest[s]);
                    diffo.Add(s);
                }
            }

           setText(label3, "Overlap : " + overlapstr + " tokens");

           enableButtons();
           initVue();

           Application.ExitThread();
        }

        private void enableButtons() {
            if (button1.InvokeRequired) {
                Action a = new Action(enableButtons);
                button1.Invoke(a);
            } else {
                button1.Enabled = button2.Enabled = button3.Enabled = button4.Enabled = button5.Enabled = button6.Enabled = button7.Enabled = button8.Enabled = button9.Enabled = true;
            }
        }

        private void setText(Label l, String s) {
            if (l.InvokeRequired) {
                Action<Label, String> a = new Action<Label, String>(setText);
                l.Invoke(a, new object[] { l, s });
            } else {
                l.Text = s;
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            foreach (String s in orgo) {
                try { dest[s] = org[s]; } catch (Exception) { }
            }
            MessageBox.Show("Copie effectuée", "OK", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private void button2_Click(object sender, EventArgs e) {
            saveFile(diffo, diff, "diff.frt");
        }

        private void saveFile(LinkedList<String> list, HashDictionary<String, String> dic, String file) {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Sauvegarder un fichier";
            sfd.FileName = file;
            sfd.ShowDialog();

            finalw = new StreamWriter(sfd.OpenFile(), Encoding.Default);

            foreach (String s in list) {
                finalw.Write(s + "\r\n");
                finalw.Write(dic[s] + "\r\n");
                finalw.Write("\r\n");
            }
            finalw.Write("\r\n");

            finalw.Close();

            sfd.Dispose();
        }

        private void button4_Click(object sender, EventArgs e) {
            saveFile(desto, dest, "final.frt");
        }

        private System.Collections.Generic.KeyValuePair<String, String>[] getDiffDataSource() {
            Int32 count = diffo.Count;
            System.Collections.Generic.KeyValuePair<String, String>[] w = new System.Collections.Generic.KeyValuePair<string, string>[count];

            for(int i = 0; i < count; i++){
                w[i] = new System.Collections.Generic.KeyValuePair<string, string>(diffo[i], diff[diffo[i]]);
            }

            return w;
        }

        private void button6_Click(object sender, EventArgs e) {
            initVue();
        }

        private void initVue() {
            if (dataGridView1.InvokeRequired) {
                Action a = new Action(initVue);
                dataGridView1.Invoke(a);
            } else {
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Add("Key", "Key");
                dataGridView1.Columns.Add("Value", "Value");
                foreach(System.Collections.Generic.KeyValuePair<String, String> w in getDiffDataSource()){
                    dataGridView1.Rows.Add(new object[] { w.Key, w.Value });
                }
                dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridView1.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
                dataGridView1.ReadOnly = false;
            }
        }

        private void button3_Click(object sender, EventArgs e) {
            ofd = new OpenFileDialog();
            ofd.FileName = "diff.frt";
            ofd.Title = "Charger fichier de différences";
            ofd.FileOk += chargerDiff;
            ofd.ShowDialog();
        }

        private void chargerDiff(object sender, EventArgs e) {
            StreamReader diffr = new StreamReader(ofd.OpenFile(), Encoding.Default);
            ofd.Dispose();

            String w;
            String[] buffer = new String[3];
            while (!diffr.EndOfStream) {
                w = diffr.ReadLine();
                if (w.StartsWith("[")) {
                    if (buffer[0] != null) {
                        try { diff[buffer[0]] = buffer[1]; } catch (Exception) { }
                        buffer = new String[3];
                    }
                    buffer[0] = w;
                }

                if (w.StartsWith("Caption")) {
                    buffer[1] = w;
                }
            }

            diffr.Close(); 
            initVue();

            MessageBox.Show("Fichier de différences chargé", "OK", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private void button9_Click(object sender, EventArgs e) {
            foreach(String s in diffo){
                dest[s] = diff[s];
            }

            MessageBox.Show("Différences appliquées au fichier résultat");
        }

        private void button8_Click(object sender, EventArgs e) {
            foreach (DataGridViewRow w in dataGridView1.Rows) {
                diff[(String)w.Cells[0].Value] = (String) w.Cells[1].Value;
            }
        }
    }
}
