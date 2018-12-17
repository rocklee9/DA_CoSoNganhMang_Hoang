using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DACoSoNganhMang_Server
{
    delegate void progressBagTruyen();
    public partial class FormServer : Form
    {
        string mergeFolder;
        TcpListener tcpListener = null;
        Socket socketForClient = null;
        NetworkStream nwkStream = null;
        StreamReader sr = null;
        StreamWriter sw = null;
        Stream stmWrite = null;
        Stream stmReader = null;
        Thread t;
        string fileName = null;

        public FormServer()
        {
            InitializeComponent();
            setIP();
        }

        /// <summary>
        /// Phần liên quan đến xử lý cắt file
        /// Author       :   Nguyễn Minh Hoàng - 22/11/2018 - create
        /// </summary>
        #region  Cắt file
        //nơi chứa các sự kiện của phần cắt file
        #region sự kiện
        // dùng để chọn file nguồn để cắt
        private void btnSoureFileCatServer_Click(object sender, EventArgs e)
        {
            try
            {
                FileInfo fi;
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Mở tập tin";

                ofd.Filter = "*.*|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    fi = new FileInfo(ofd.FileName);
                    txtDichCatServer.Text = fi.DirectoryName;
                    txtSoureNguonCatServer.Text = fi.FullName;
                }
            }
            catch (Exception e1)
            {

            }
        }

        // Dùng để chọn nơi lưu các file khi cắt
        private void btnDichCatServer_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtDichCatServer.Text = fbd.SelectedPath;
            }
            catch { }
        }

        // Xử lý khi chọn cắt file
        private void btnStartCatServer_Click(object sender, EventArgs e)
        {
            WorkingStart(txtSoureNguonCatServer.Text, txtDichCatServer.Text);
        }
        private void checkChiaServer_CheckedChanged(object sender, EventArgs e)
        {
            Formload();
        }
        #endregion

        //nơi chứa các hàm xử lý của phần cắt file
        #region Hàm

        public void Formload()
        {
            if (checkChiaServer.Checked)
            {
                checkChiaServer.Text = "Chia theo phấn";
                cmbChiaServer.Visible = false;
                numChiaServer.Maximum = 100;
                numChiaServer.Minimum = 2;
                numChiaServer.Value = 2;
            }
            else
            {
                checkChiaServer.Text = "Chia theo dung lượng";
                cmbChiaServer.Visible = true;
                numChiaServer.Maximum = 1050000;
                numChiaServer.Minimum = 0;
                numChiaServer.Value = 0;
            }
        }

        // Dùng để xử lý khi chọn cắt file
        private void WorkingStart(string pathNguon, string pathDich)
        {
            try
            {

                if (!File.Exists(pathNguon) || !Directory.Exists(pathDich))
                {
                    MessageBox.Show("Vui lòng kiểm tra lại dữ liệu vào!", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                else
                {
                    if (checkChiaServer.Checked)
                    {
                        int soFileChia = Convert.ToInt32(numChiaServer.Value);
                        PbFileCatServer.Maximum = soFileChia;
                        if (numChiaServer.Value < 2)
                        {
                            MessageBox.Show("Vui lòng nhập số file muốn cắt lớn hơn 2 !", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            FileStream fs = new FileStream(pathNguon, FileMode.Open, FileAccess.Read);
                            string strExtexsion = Path.GetExtension(pathNguon).Trim();
                            int SizeofEachFile = (int)Math.Ceiling((double)fs.Length / soFileChia);
                            for (int i = 0; i < soFileChia; i++)
                            {

                                string baseFileName = Path.GetFileNameWithoutExtension(pathNguon);
                                string Extension = Path.GetExtension(pathNguon);
                                FileStream outputFile = new FileStream(pathDich + "/" + baseFileName + "." + i + strExtexsion + ".tmp", FileMode.Create, FileAccess.Write);
                                mergeFolder = Path.GetDirectoryName(pathNguon);
                                int bytesRead = 0;
                                byte[] buffer = new byte[SizeofEachFile];
                                if ((bytesRead = fs.Read(buffer, 0, SizeofEachFile)) > 0)
                                {
                                    outputFile.Write(buffer, 0, bytesRead);
                                }
                                outputFile.Close();
                                PbFileCatServer.PerformStep();

                            }
                            fs.Close();
                        }
                        PbFileCatServer.Value = 0;
                        MessageBox.Show("Cắt file thành công", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        FileStream fs = new FileStream(pathNguon, FileMode.Open, FileAccess.Read);
                        string strExtexsion = Path.GetExtension(pathNguon).Trim();
                        int SizeofEachFile;
                        if (String.Compare(cmbChiaServer.Text, "MB") == 0)
                        {
                            SizeofEachFile = Convert.ToInt32(numChiaServer.Value) * 1024 * 1024;
                        }
                        else
                        {
                            SizeofEachFile = Convert.ToInt32(numChiaServer.Value) * 1024;
                        }
                        long size = fs.Length;
                        if (size < SizeofEachFile)
                        {
                            MessageBox.Show("Kích cỡ file gốc nhỏ hơn kích thước file cắt, vui lòng nhập lại ! ( kích cỡ file gốc là: " + size, "Error !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            int soFileChia = Convert.ToInt32(size / SizeofEachFile);
                            int sizeFileCuoi = (int)size - soFileChia * SizeofEachFile;
                            if (sizeFileCuoi > 0)
                            {
                                PbFileCatServer.Maximum = soFileChia + 1;
                            }
                            else
                            {
                                PbFileCatServer.Maximum = soFileChia;
                            }
                            for (int i = 0; i < soFileChia; i++)
                            {
                                string baseFileName = Path.GetFileNameWithoutExtension(pathNguon);
                                string Extension = Path.GetExtension(pathNguon);
                                FileStream outputFile = new FileStream(pathDich + "/" + baseFileName + "." + i + strExtexsion + ".tmp", FileMode.Create, FileAccess.Write);
                                mergeFolder = Path.GetDirectoryName(pathNguon);
                                int bytesRead = 0;
                                byte[] buffer = new byte[SizeofEachFile];
                                if ((bytesRead = fs.Read(buffer, 0, SizeofEachFile)) > 0)
                                {
                                    outputFile.Write(buffer, 0, bytesRead);
                                }
                                outputFile.Close();
                                PbFileCatServer.PerformStep();
                            }
                            if (sizeFileCuoi > 0)
                            {
                                string baseFileName = Path.GetFileNameWithoutExtension(pathNguon);
                                string Extension = Path.GetExtension(pathNguon);
                                FileStream outputFile = new FileStream(pathDich + "/" + baseFileName + "." + soFileChia + strExtexsion + ".tmp", FileMode.Create, FileAccess.Write);
                                mergeFolder = Path.GetDirectoryName(pathNguon);
                                int bytesRead = 0;
                                byte[] buffer = new byte[SizeofEachFile];
                                if ((bytesRead = fs.Read(buffer, 0, SizeofEachFile)) > 0)
                                {
                                    outputFile.Write(buffer, 0, bytesRead);
                                }
                                outputFile.Close();
                                PbFileCatServer.PerformStep();
                            }
                            PbFileCatServer.Value = 0;
                            MessageBox.Show("Cắt file thành công", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }

                    }

                }
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message, "Error !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                PbFileCatServer.Value = 0;
            }
        }
        #endregion

        #endregion

        /// <summary>
        /// Phần liên quan đến xử lý nối file
        /// Author       :   Nguyễn Minh Hoàng - 24/11/2018 - create
        /// </summary>
        #region Phần nối file
        //Nơi chứa các sự kiện của phần nối file
        #region Sự kiện
        private void btnSoureNoiServer_Click(object sender, EventArgs e)
        {
            try
            {
                FileInfo fi;
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Mở tập tin";

                ofd.Filter = "*.*|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    fi = new FileInfo(ofd.FileName);
                    txtDichNoiServer.Text = fi.DirectoryName;
                    txtSourceNoiServer.Text = fi.FullName;
                }
            }
            catch { }
        }

        private void btnDichNoiServer_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtDichNoiServer.Text = fbd.SelectedPath;
            }
            catch { }
        }

        private void btnNoiServer_Click(object sender, EventArgs e)
        {
            if (!File.Exists(txtSourceNoiServer.Text) || !Directory.Exists(txtDichNoiServer.Text))
            {
                MessageBox.Show("Vui lòng kiểm tra lại dữ liệu vào!", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            else
            {
                WorkingStartNoi(txtSourceNoiServer.Text, txtDichNoiServer.Text);
            }
        }

        #endregion

        //Nơi chứa các hàm xử lý của phần nối file
        #region Hàm
        //Xử lý nối file

        private void WorkingStartNoi(string pathNguon, string pathDich)
        {
            try
            {
                string[] tmpfiles = Directory.GetFiles(Path.GetDirectoryName(pathNguon), "*.tmp");
                PbFileNoiServer.Maximum = tmpfiles.Length;
                FileStream outPutFile = null;
                string PrevFileName = "";
                foreach (string tempFile in tmpfiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(tempFile);
                    string baseFileName = fileName.Substring(0, fileName.IndexOf(Convert.ToChar(".")));
                    string extension = Path.GetExtension(fileName);

                    if (!PrevFileName.Equals(baseFileName))
                    {
                        if (outPutFile != null)
                        {
                            outPutFile.Flush();
                            outPutFile.Close();
                        }
                        outPutFile = new FileStream(pathDich + "/" + baseFileName + extension, FileMode.OpenOrCreate, FileAccess.Write);

                    }

                    int bytesRead = 0;
                    byte[] buffer = new byte[1024];
                    FileStream inputTempFile = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.Read);

                    while ((bytesRead = inputTempFile.Read(buffer, 0, 1024)) > 0)
                        outPutFile.Write(buffer, 0, bytesRead);

                    inputTempFile.Close();
                    File.Delete(tempFile);
                    PrevFileName = baseFileName;
                    PbFileNoiServer.PerformStep();

                }
                outPutFile.Close();
                PbFileNoiServer.Value = 0;
                MessageBox.Show("Nối file thành công", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message, "Error !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                PbFileNoiServer.Value = 0;
            }
        }
        #endregion
        #endregion

        /// <summary>
        /// Phần liên quan đến xử lý truyền file
        /// Author       :   Nguyễn Minh Hoàng - 29/11/2018 - create
        /// </summary>

        #region Phần truyền file

        //Nơi chứa các sự kiện của phần truyền file
        #region Sự kiện

        // chọn file để truyền cho client
        // Author       :   Nguyễn Minh Hoàng - 08/12/2018 - create
        private void btnFileTruyenServer_Click(object sender, EventArgs e)
        {
            try
            {
                FileInfo fi;
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Mở tập tin";

                ofd.Filter = "*.*|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    fi = new FileInfo(ofd.FileName);
                    txtFileTruyenServer.Text = fi.FullName;
                    fileName = fi.Name;
                }
            }
            catch { }
        }

        // dùng để gửi file cho client
        // Author       :   Nguyễn Minh Hoàng - 08/12/2018 - create
        private void btnSendServer_Click(object sender, EventArgs e)
        {
            t = new Thread(new ThreadStart(sendFile));
            t.Start();
        }


        #endregion

        //Nơi chứa các hàm xử lý của phần truyền file
        #region Hàm
        private void sendFile()
        {
            try
            {
                if (pgSendFileServer.InvokeRequired)
                {
                    var d = new progressBagTruyen(sendFile);
                    Invoke(d);
                }
                else
                {
                    socketForClient = tcpListener.AcceptSocket();
                    NetworkStream ns = new NetworkStream(socketForClient);
                    sr = new StreamReader(ns);
                    sw = new StreamWriter(ns);
                    sw.WriteLine("Client gửi file " + fileName + " tới !");
                    sw.WriteLine(fileName);
                    sw.Flush();
                    nwkStream = new NetworkStream(socketForClient);
                    stmReader = File.OpenRead(txtFileTruyenServer.Text);
                    stmWrite = nwkStream;
                    FileInfo flInfo = new FileInfo(txtFileTruyenServer.Text);
                    int max = Convert.ToInt32((flInfo.Length / 1024) / 81920);
                    pgSendFileServer.Maximum = max;
                    pgSendFileServer.Step = 1;
                    int dem = 0;
                    int size = 1024 * 1024 * 2;
                    byte[] buff = new byte[size];
                    int len = 0;

                    while ((len = stmReader.Read(buff, 0, buff.Length)) > 0)
                    {
                        stmWrite.Write(buff, 0, len);
                        stmWrite.Flush();
                        dem++;
                        if (dem > 40)
                        {
                            pgSendFileServer.PerformStep();
                            dem = 0;
                        }
                    }
                    pgSendFileServer.Value = 0;
                    MessageBox.Show("Gửi file thành công ", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
            catch (Exception e1)
            {
                MessageBox.Show("Lỗi ! : " + e1.Message, "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                socketForClient.Close();
            }

        }
        #endregion
        #endregion

        /// <summary>
        /// Phần liên quan đến xử lý nhận file
        /// Author       :   Nguyễn Minh Hoàng - 29/11/2018 - create
        /// </summary>
        /// 

        #region Phần nhận file

        //Nơi chứa các sự kiện liên quan đến phần nhận file
        #region Sự kiện
        private void btnConnectNhan_Click(object sender, EventArgs e)
        {
            try
            {
                IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(txtIPServerServer.Text), Convert.ToInt32(txtPostServer.Text));
                tcpListener = new TcpListener(ipe);
                tcpListener.Start();
                lbServer.Text = "Server đã mở !";


            }
            catch (Exception e1)
            {
                MessageBox.Show("Lỗi ! : " + e1.Message, "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }



        }

        private void btnSaveServer_Click(object sender, EventArgs e)
        {
            try
            {
                socketForClient = tcpListener.AcceptSocket();
                NetworkStream ns = new NetworkStream(socketForClient);
                sr = new StreamReader(ns);
                sw = new StreamWriter(ns);
                txtThongBaoServer.Text = sr.ReadLine();

                if (socketForClient.Connected)
                {

                    t = new Thread(new ThreadStart(SaveFile));
                    t.Start();
                }
            }
            catch (Exception e1)
            {
                MessageBox.Show("Lỗi ! : " + e1.Message, "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }



        private void btnLoadSaveFile_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtSaveFileServer.Text = fbd.SelectedPath;
            }
            catch (Exception e1)
            {
                MessageBox.Show("Lỗi ! : " + e1.Message, "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        //Nơi chứa các hàm xử lý liên quan đến phần nhận file
        #region Hàm
        public void SaveFile()
        {

            try
            {
                if (pgSendFileServer.InvokeRequired)
                {
                    var d = new progressBagTruyen(SaveFile);
                    Invoke(d);
                }
                else
                {

                    nwkStream = new NetworkStream(socketForClient);
                    string duongDan = "D:\\DA_Mang_Server" + @"\" + sr.ReadLine();
                    nwkStream = tcpListener.AcceptTcpClient().GetStream();
                    stmReader = nwkStream;
                    stmWrite = File.OpenWrite(duongDan);
                    int size = 1024 * 1024;
                    byte[] buff = new byte[size];
                    int len;
                    while ((len = stmReader.Read(buff, 0, size)) > 0)
                    {
                        stmWrite.Write(buff, 0, len);
                        stmWrite.Flush();
                    }
                    txtThongBaoServer.Text = "";
                    MessageBox.Show("Nhận file thành công ", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Lỗi ! : " + e.Message, "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                socketForClient.Close();
            }
        }
        #endregion



        #endregion
        #region Common
        // Dùng cho các hàm chung
        // Author       :   Nguyễn Minh Hoàng - 17/12/2018 - create 

        private void setIP()
        {
            try
            {
                IPHostEntry ips = Dns.GetHostByName(Dns.GetHostName());
                txtIPServerServer.Text = ips.AddressList[0].ToString();
            }
            catch (Exception e)
            {
                MessageBox.Show("Lỗi ! : " + e.Message, "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

    }
}
