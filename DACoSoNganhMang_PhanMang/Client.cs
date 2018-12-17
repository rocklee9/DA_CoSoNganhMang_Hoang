using DACoSoNganhMang_PhanMang.Properties;
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

namespace DACoSoNganhMang_PhanMang
{
    delegate void progressBagTruyen();
    public partial class FormCatVsTruyenFile : Form
    {
        string mergeFolder;
        private Socket socketForServer = null;
        private TcpClient tcpClient = null;
        private Stream stmReadClient = null;
        private Stream stmWriteClient = null;
        private NetworkStream nwkStreamServer = null;
        private Thread t = null;
        string fileName = null;
        StreamReader sr = null;
        StreamWriter sw = null;


        //public FormCatVsTruyenFile(string path)
        //{
        //    InitializeComponent();
        //    if (path != "")
        //    {
        //        if (File.Exists(path))
        //        {
        //            txtSoureNguonCatClient.Text = Path.GetFullPath(path);
        //            txtDichCatClient.Text = Environment.CurrentDirectory;
        //        }

        //    }
        //}
        public FormCatVsTruyenFile()
        {
            InitializeComponent();
        }
        //[STAThread]
        //static void Main(string[] args)
        //{

        //    Application.EnableVisualStyles();
        //    Application.SetCompatibleTextRenderingDefault(false);
        //    if (args != null && args.Length > 0)
        //    {

        //        Application.Run(new FormCatVsTruyenFile(args[0].ToString()));
        //    }
        //    else
        //        Application.Run(new FormCatVsTruyenFile());

        //}


        /// <summary>
        /// Phần liên quan đến xử lý cắt file
        /// Author       :   Nguyễn Minh Hoàng - 22/11/2018 - create
        /// </summary>
        #region  Cắt file
        //nơi chứa các sự kiện của phần cắt file
        #region sự kiện

        //Chọn tập tin muốn cắt
        private void btnSoureFileCat_Click(object sender, EventArgs e)
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
                    txtDichCatClient.Text = fi.DirectoryName;
                    txtSoureNguonCatClient.Text = fi.FullName;
                }
            }
            catch { }
        }
        //Nơi lưu file sau khi cắt thành công
        private void btnDichCat_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtDichCatClient.Text = fbd.SelectedPath;
            }
            catch { }
        }

        //xử lý khi cắt file
        private void btnStartCat_Click(object sender, EventArgs e)
        {
            WorkingStart(txtSoureNguonCatClient.Text, txtDichCatClient.Text);
        }

        //Hủy quá trình cắt file

        private void btnCancelCat_Click(object sender, EventArgs e)
        {
            WorkingCancel();
        }

        //Chọn cách cắt file
        private void checkChia_CheckedChanged(object sender, EventArgs e)
        {
            Formload();
        }
        #endregion

        //nơi chứa các hàm xử lý của phần cắt file
        #region Hàm
        public void Formload()
        {
            if (checkChiaClient.Checked)
            {
                checkChiaClient.Text = "Chia theo phấn";
                cmbChiaClient.Visible = false;
                numChiaClient.Maximum = 100;
                numChiaClient.Minimum = 2;
                numChiaClient.Value = 2;
            }
            else
            {
                checkChiaClient.Text = "Chia theo dung lượng";
                cmbChiaClient.Visible = true;
                numChiaClient.Maximum = 1050000;
                numChiaClient.Minimum = 0;
                numChiaClient.Value = 0;
            }
        }
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
                    if (checkChiaClient.Checked)
                    {
                        int soFileChia = Convert.ToInt32(numChiaClient.Value);
                        PbFileCatClient.Maximum = soFileChia;
                        if (numChiaClient.Value < 2)
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
                                PbFileCatClient.PerformStep();

                            }
                            fs.Close();
                        }
                        PbFileCatClient.Value = 0;
                        MessageBox.Show("Cắt file thành công", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        FileStream fs = new FileStream(pathNguon, FileMode.Open, FileAccess.Read);
                        string strExtexsion = Path.GetExtension(pathNguon).Trim();
                        int SizeofEachFile;
                        if (String.Compare(cmbChiaClient.Text, "MB") == 0)
                        {
                            SizeofEachFile = Convert.ToInt32(numChiaClient.Value) * 1024 * 1024;
                        }
                        else
                        {
                            SizeofEachFile = Convert.ToInt32(numChiaClient.Value) * 1024;
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
                                PbFileCatClient.Maximum = soFileChia + 1;
                            }
                            else
                            {
                                PbFileCatClient.Maximum = soFileChia;
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
                                PbFileCatClient.PerformStep();
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
                                PbFileCatClient.PerformStep();
                            }
                            PbFileCatClient.Value = 0;
                            MessageBox.Show("Cắt file thành công", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }

                    }

                }
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message, "Error !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                PbFileCatClient.Value = 0;
            }
        }
        private void WorkingCancel()
        {
            try
            {
                DialogResult dia = MessageBox.Show("Ứng dụng đang xử lý, bạn có chắc là muốn dừng lại không ?", "Chương trình cắt và truyền file", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            }
            catch { }
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

        //lấy file muốn nối
        private void btnSoureNoi_Click(object sender, EventArgs e)
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
                    txtDichNoiClient.Text = fi.DirectoryName;
                    txtSourceNoiClient.Text = fi.FullName;
                }
            }
            catch { }
        }
        //Nơi lưu file sau khi lưu
        private void btnDichNoi_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtDichNoiClient.Text = fbd.SelectedPath;
            }
            catch { }
        }

        // dùng để nối file
        private void btnNoi_Click(object sender, EventArgs e)
        {
            if (!File.Exists(txtSourceNoiClient.Text) || !Directory.Exists(txtDichNoiClient.Text))
            {
                MessageBox.Show("Vui lòng kiểm tra lại dữ liệu vào!", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            else
            {
                WorkingStartNoi(txtSourceNoiClient.Text, txtDichNoiClient.Text);
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
                PbFileNoiClient.Maximum = tmpfiles.Length;
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
                    PbFileNoiClient.PerformStep();

                }

                outPutFile.Close();
                PbFileNoiClient.Value = 0;
                MessageBox.Show("Nối file thành công", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message, "Error !!!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                PbFileNoiClient.Value = 0;
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
        //Kết nối đến server
        private void btnConnectClient_Click(object sender, EventArgs e)
        {
            try
            {
                if (String.Compare(txtIpServerClient.Text, "") == 0 && String.Compare(txtPostClient.Text, "") == 0)
                {
                    MessageBox.Show("Vui lòng nhập địa chỉ ip và cổng vào", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    lbKetNoi.Text = "Chưa có kết nối nào !";
                }
                else
                {
                    Connect();
                    
                }

            }
            catch (Exception e1)
            {
                MessageBox.Show("Bạn vui lòng kiểm tra kết nôi . Error : " + e1.Message, "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        // lấy file muốn truyền vào
        private void btnFileTruyenClient_Click(object sender, EventArgs e)
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
                    txtFileTruyenClient.Text = fi.FullName;
                    fileName = fi.Name;
                }
            }
            catch { }
        }
        private void btnSendClient_Click(object sender, EventArgs e)
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
                if (pgTruyenClient.InvokeRequired)
                {
                    var d = new progressBagTruyen(sendFile);
                    Invoke(d);
                }
                else
                {
                    StreamReader sr = new StreamReader(tcpClient.GetStream());
                    StreamWriter sw = new StreamWriter(tcpClient.GetStream());
                    sw.WriteLine("Client gửi file " + fileName + " tới !");
                    sw.WriteLine(fileName);
                    sw.Flush();
                    nwkStreamServer = new NetworkStream(socketForServer);
                    stmReadClient = File.OpenRead(txtFileTruyenClient.Text);
                    stmWriteClient = nwkStreamServer;
                    FileInfo flInfo = new FileInfo(txtFileTruyenClient.Text);

                    int max = Convert.ToInt32((flInfo.Length / 1024) / 81920);
                    pgTruyenClient.Maximum = max;
                    pgTruyenClient.Step = 1;
                    int dem = 0;
                    int size = 1024 * 1024 * 2;
                    byte[] buff = new byte[size];
                    int len = 0;

                    while ((len = stmReadClient.Read(buff, 0, buff.Length)) > 0)
                    {
                        stmWriteClient.Write(buff, 0, len);
                        stmWriteClient.Flush();
                        dem++;
                        if (dem > 40)
                        {
                            pgTruyenClient.PerformStep();
                            dem = 0;
                        }
                    }
                    pgTruyenClient.Value = 0;
                    MessageBox.Show("Gửi file thành công !", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
            catch (Exception e1)
            {
                pgTruyenClient.Value = 0;
                MessageBox.Show("Lỗi : " + e1.Message, "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                //nwkStreamServer.Close();
                //stmReadClient.Close();
                //stmWriteClient.Close();
                tcpClient.Close();
                socketForServer.Close();
                //socketForServer.Close();
                //lbKetNoi.Text = "Đã đóng kết nối !";
                Connect();
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
        // Dùng để chọn thư mục lưu file từ server gửi
        // Author       :   Nguyễn Minh Hoàng - 08/12/2018 - create 
        private void btnLoadSaveFileClient_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtSaveFileClient.Text = fbd.SelectedPath;
            }
            catch { }
        }
        // Dùng để lưu file nhận từ server
        // Author       :   Nguyễn Minh Hoàng - 08/12/2018 - create 
        private void btnSaveClient_Click(object sender, EventArgs e)
        {
            try
            {
                t = new Thread(new ThreadStart(SaveFile));
                t.Start();

            }catch(Exception e1)
            {
                MessageBox.Show("Lỗi ! : " + e1.Message, "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        //Nơi chứa các hàm xử lý liên quan đến phần nhận file
        #region Hàm
        private void SaveFile()
        {
            try
            {
                if (pgTruyenClient.InvokeRequired)
                {
                    var d = new progressBagTruyen(SaveFile);
                    Invoke(d);
                }
                else
                {
                    StreamReader sr = new StreamReader(tcpClient.GetStream());
                    StreamWriter sw = new StreamWriter(tcpClient.GetStream());
                    txtThongBaoClient.Text = sr.ReadLine();
                    string duongDan = txtSaveFileClient.Text + @"\" + sr.ReadLine();
                    nwkStreamServer = tcpClient.GetStream();
                    stmReadClient = nwkStreamServer;
                    stmWriteClient = File.OpenWrite(duongDan);
                    int size = 1024 * 1024;
                    byte[] buff = new byte[size];
                    int len;
                    while ((len = stmReadClient.Read(buff, 0, size)) > 0)
                    {
                        stmWriteClient.Write(buff, 0, len);
                        stmWriteClient.Flush();
                    }
                    MessageBox.Show("Nhận file thành công ", "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtThongBaoClient.Text = "";
                }
            }catch(Exception e1)
            {
                MessageBox.Show("Lỗi ! : " + e1.Message, "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                tcpClient.Close();
                Connect();
            }
            
        }

        #endregion


        #endregion
        #region Common
        // Dùng cho các hàm chung
        // Author       :   Nguyễn Minh Hoàng - 17/12/2018 - create 

        private void Connect()
        {
            try
            {
                IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(txtIpServerClient.Text), int.Parse(txtPostClient.Text));
                tcpClient = new TcpClient();
                tcpClient.Connect(ipe);
                socketForServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socketForServer.Connect(ipe);
                lbKetNoi.Text = "Đã kết nối đến server có địa chỉ Ip : " + txtIpServerClient.Text;
            }catch(Exception e)
            {
                lbKetNoi.Text = "Chưa có kết nối nào !";
                MessageBox.Show("Lỗi ! : " + e.Message, "Chương trình cắt và truyền file", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            
            //sr = new StreamReader(tcpClient.GetStream());
            //sw = new StreamWriter(tcpClient.GetStream());
        }
        #endregion
    }
}
