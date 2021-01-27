using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BDLMerge
{
    public partial class FormMain : Form
    {
        string[] files = new string[0];
        List<DataTable> tables = new List<DataTable>();
        List<DataColumn> primaryKeys = new List<DataColumn>();
        private FormJob formJob;
        private DataTable resultTable;

        public FormMain()
        {
            InitializeComponent();
            dataGridView1.BackgroundColor = Color.White;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {

        }

        private void buttonSelect_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog()
            {
                Filter = "Pliki CSV|*.csv",
                Multiselect = true
            };

            if (fileDialog.ShowDialog().HasFlag(DialogResult.OK))
            {
                if (fileDialog.FileNames.Length >= 2)
                {
                    dataGridView1.DataSource = null;
                    buttonMerge.Enabled = true;
                    buttonExport.Enabled = false;

                    files = fileDialog.FileNames;

                    listViewFiles.Items.Clear();

                    foreach (var file in files)
                    {
                        listViewFiles.Items.Add(Path.GetFileName(file));
                    }

                    listViewFiles.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

                    foreach (var t in tables)
                    {
                        t.Dispose();
                    }

                    tables.Clear();

                    FindPrimaryKey();

                    listViewKeys.Items.Clear();

                    foreach (var key in primaryKeys)
                    {
                        var item = listViewKeys.Items.Add(key.ColumnName);
                        item.Tag = key;
                    }

                    for (int i = 0; i < listViewKeys.Items.Count; i++)
                    {
                        listViewKeys.Items[i].Checked = true;
                    }
                }
                else
                {
                    MessageBox.Show("Należy wybrać przynajmniej 2 pliki.", "Uwaga!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void FindPrimaryKey()
        {
            primaryKeys.Clear();

            List<DataColumn> k = new List<DataColumn>();
            foreach (var file in files)
            {
                var table = CsvParser.Parse(file);
                table.TableName = Path.GetFileName(file);
                tables.Add(table);

                foreach (DataColumn column in table.Columns)
                {
                    k.Add(column);
                }
            }

            for (int i = 0; i < k.Count; i++)
            {
                int count = 0;

                foreach (var key in k)
                {
                    if (key.ColumnName == k[i].ColumnName) count++;
                }

                if (count == files.Length)
                {
                    if (primaryKeys.Where(col => col.ColumnName == k[i].ColumnName).Count() == 0)
                    {
                        primaryKeys.Add(k[i]);
                    }
                }
            }
        }

        private string GetLongColumnName(DataTable table, DataColumn column)
        {
            return $"({table.TableName}) {column.ColumnName}";
        }

        private bool IsPrimaryKey(DataColumn column)
        {
            return primaryKeys.Where(col => col.ColumnName == column.ColumnName).Count() > 0;
        }

        private async void Merge()
        {
            await Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();

                toolStripStatusLabelStatus.Text = $"Scalanie w toku...";

                resultTable = new DataTable();

                // Add primary key columns

                foreach (var column in primaryKeys)
                {
                    resultTable.Columns.Add(column.ColumnName);
                }

                // Merge headers

                foreach (var table in tables)
                {
                    foreach (DataColumn column in table.Columns)
                    {
                        var longColumnName = GetLongColumnName(table, column);

                        if (!resultTable.Columns.Contains(longColumnName) && !IsPrimaryKey(column))
                        {
                            resultTable.Columns.Add(longColumnName);
                        }
                    }
                }

                // Merge data

                int tableCount = tables.Count;
                int finishedTables = 0;

                List<DataColumn> selectedPrimaryKeys = new List<DataColumn>();

                Invoke(new Action(() =>
                {
                    foreach (ListViewItem item in listViewKeys.CheckedItems)
                    {
                        selectedPrimaryKeys.Add((DataColumn)item.Tag);
                        Debug.WriteLine(item.Text);
                    }
                }));

                foreach (var table in tables)
                {
                    foreach (DataRow row in table.Rows)
                    {
                        bool inserted = false;

                        // Find matching primary keys
                        foreach (DataRow targetRow in resultTable.Rows)
                        {
                            bool matches = true;

                            foreach (DataColumn primaryKey in selectedPrimaryKeys)
                            {
                                if (!targetRow[primaryKey.ColumnName].Equals(row[primaryKey.ColumnName]))
                                {
                                    matches = false;
                                    break;
                                }
                            }

                            // Compare primary keys
                            if (matches)
                            {
                                // INSERT
                                foreach (DataColumn column in table.Columns)
                                {
                                    if (!IsPrimaryKey(column))
                                    {
                                        var longColumnName = GetLongColumnName(table, column);
                                        targetRow[longColumnName] = row[column];
                                    }
                                }

                                inserted = true;
                                break;
                            }
                        }

                        // Insert new
                        if (!inserted)
                        {
                            var newRow = resultTable.Rows.Add();

                            foreach (DataColumn column in table.Columns)
                            {
                                if (IsPrimaryKey(column))
                                {
                                    newRow[column.ColumnName] = row[column.ColumnName];
                                }
                                else
                                {
                                    var longColumnName = GetLongColumnName(table, column);
                                    newRow[longColumnName] = row[column];
                                }
                            }
                        }
                    }

                    finishedTables++;

                    Invoke(new Action(() =>
                    {
                        toolStripProgressBar1.Value = (int)(finishedTables / (float)tableCount * 100);
                    }));
                }

                sw.Stop();

                Invoke(new Action(() =>
                {
                    toolStripStatusLabelStatus.Text = $"Scalanie zakończone po {sw.Elapsed.TotalSeconds:0.0} sekundach.";
                    buttonExport.Enabled = true;
                    formJob.Close();
                }));

                int elementCount = resultTable.Rows.Count * resultTable.Columns.Count;


                // Print result
                Invoke(new Action(() =>
                {
                    labelRowCount.Text = $"{resultTable.Rows.Count} rekordów";

                    if (elementCount > 200000)
                    {
                        if (MessageBox.Show($"Liczba elementów do wyświetlenia przekracza 200.000 ({elementCount}). Czy chcesz wyświetlić wynik?", "Uwaga!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                        {
                            return;
                        }
                    }

                    dataGridView1.DataSource = resultTable;
                    dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

                }));
            });
        }

        private void buttonMerge_Click(object sender, EventArgs e)
        {
            formJob = new FormJob();
            Merge();
            formJob.ShowDialog();
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                AddExtension = true,
                Filter = "Pliki CSV|*.csv"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                CsvExport.Export(resultTable, saveFileDialog.FileName);
                toolStripStatusLabelStatus.Text = $"Wyeksportowano jako {saveFileDialog.FileName}";
            }
        }
    }
}
