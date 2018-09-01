using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Client
{
    public partial class ClientForm : Form
    {

        public struct ConnectInfo
        {
            public string IP;
            public Int32 Port;
            public string Cmd;
            public string FullPath;
            public string FileName;
            public long FileLength;  
        };


        public ConnectInfo[] TargetSever = new ConnectInfo[20];
        
        public ConnectInfo MonitorServer = new ConnectInfo { IP = "192.168.3.32", Port = 30000};

        public delegate void setTextValueCallBack(string text);    //定义回调
        public string DownLoadSavePath = "D:\\ClientFile\\";
        public bool IsTransferOk = false;
        public Socket cSocket;
        public int retfilecount = 0;

       
        public ClientForm()
        {
            InitializeComponent();
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            UP_button.Text = "SelectFile";
            SetText("客户端已启动 " + DateTime.Now.ToString());
            SetText("\r\n请手动更新文件列表！");
            if (!Directory.Exists(DownLoadSavePath))
            {
                Directory.CreateDirectory(DownLoadSavePath);
            }

        }


        private void UP_button_Click(object sender, EventArgs e)
        {
            if (UP_button.Text == "SelectFile")
            {
                UP_button.Text = "UpLoad";
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = true;
                fileDialog.Title = "请选择文件";
                fileDialog.Filter = "所有文件(*.*)|*.*";
                fileDialog.ShowDialog();

                string[] filenames = fileDialog.FileNames;
                if (filenames.Length == 0)
                {
                    MessageBox.Show("Please Select File", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    UP_button.Text = "SelectFile";
                }
                else
                {
                    foreach (string file in filenames)
                    {
                        MyFileListUpdate(file);
                    }
                }

                
            }
            else if (UP_button.Text == "UpLoad")
            {

                //询问Monitor目标服务器IP
                //Thread uploadThread = new Thread(UpLoadFiles);
                //uploadThread.Start(MonitorServer);

                MonitorServer.Cmd = "UpLoad";
                Thread AskThread = new Thread(AskMonitor);
                AskThread.Start(MonitorServer);

                //SetText(MonitorServer.Path.Substring(0, MonitorServer.Path.Length - MonitorServer.FileName.Length)+"\\");
                //string[] path = { "F:\\caffe_1.rar", "F:\\caffe_2.rar", "F:\\caffe_3.rar" };
                //SplitFile("G:\\", "D:\\caffe.rar");
                //CombinFile(path, "F:\\caffe.rar");
                UP_button.Text = "SelectFile";

            }
        }


        private void DOWN_button2_Click(object sender, EventArgs e)
        {
            //获取需要下载的文件名，判断是否符合下载条件
            //创建socket询问Monitor，获取目标服务器地址
            //创建socket文件下载线程连接服务器
            //发送文件下载请求
            //得到应答？Y开始接受文件 20000, "sleep.jpg"

            if (this.listView1.SelectedItems.Count != 0)
            {
                string filename = this.listView1.SelectedItems[0].Text;
                SetText("\r\n" + "已选择下载文件：" + filename);
                MonitorServer.FileName = filename;
                MonitorServer.FileLength = GetSelectfilesize(filename);

            //    Thread downloadThread = new Thread(DownLoadFiles);
            //    downloadThread.Start(MonitorServer.FileName);
                StauesModify(MonitorServer.FileName, "DownLoading");
                MonitorServer.Cmd = "DownLoad";
                Thread AskThread = new Thread(AskMonitor);
                AskThread.Start(MonitorServer);
            }
            else
            {
                MessageBox.Show("请选择需要下载的文件！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private void StopLoad_button_Click(object sender, EventArgs e)
        {

            Thread stoploadThread = new Thread(UpdataFiles);
            stoploadThread.Start(MonitorServer);

            stoploadThread.Join();
            SetText("\r\n文件列表更新成功！");

        }

        private void DEL_button_Click(object sender, EventArgs e)
        {
            if (DEL_button.Text == "SelectData")
            {
                DEL_button.Text = "DataProcess";
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = false;
                fileDialog.Title = "请选择文件";
                fileDialog.Filter = "所有文件(*.*)|*.*";
                fileDialog.ShowDialog();

                string dataname = fileDialog.FileName;
                if (dataname.Length == 0)
                {
                    MessageBox.Show("Please Select File", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DEL_button.Text = "SelectData";
                    
                }
                else
                {
                    SetText("\r\n文件选择成功，请开始计算...");
                    MonitorServer.FullPath = dataname;
                }

            }
            else if (DEL_button.Text == "DataProcess")
            {
                DEL_button.Text = "Processing";
                Thread dataprocess = new Thread(DataProcess);
                dataprocess.Start(MonitorServer);

                DEL_button.Enabled = false;

                Thread waitm = new Thread(Socket_Init);
                waitm.Start();
            }

        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            MyFileListUpdate(path);

        }


        public void AskMonitor(object Cnt)
        {
            ConnectInfo connect = (ConnectInfo)Cnt;
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(connect.IP), connect.Port); //指向远程服务端节点   
            Socket AskSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                AskSocket.Connect(ipep);  //连接到发送端  
            }
            catch
            {
                MessageBox.Show("连接服务器出错！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            //获得服务器端节点对象  
            IPEndPoint clientep = (IPEndPoint)AskSocket.RemoteEndPoint;
            //发送[操作命名]到服务器端
            TransferFiles.SendVarData(AskSocket, System.Text.Encoding.Unicode.GetBytes(MonitorServer.Cmd));
            //发送文件信息
            string UpLoadFileInfo = MonitorServer.FileName + "$" + (MonitorServer.FileLength.ToString());
            TransferFiles.SendVarData(AskSocket, System.Text.Encoding.Unicode.GetBytes(UpLoadFileInfo));

            string IPstr = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(AskSocket));
            string[] IPstrArray = IPstr.Split(new string[] { "@", "/" }, StringSplitOptions.RemoveEmptyEntries);

            if (IPstrArray.Length > 2)//文件大于2G开始分块处理
            {
                //接收多组IP信息,IP格式：@192.168.3.32/20000@
                TargetSever[1].IP = IPstrArray[0]; TargetSever[1].Port = Convert.ToInt32(IPstrArray[1]);
                TargetSever[2].IP = IPstrArray[2]; TargetSever[2].Port = Convert.ToInt32(IPstrArray[3]);
                TargetSever[3].IP = IPstrArray[4]; TargetSever[3].Port = Convert.ToInt32(IPstrArray[5]);

                AskSocket.Close();//关闭套接字
                if (0 == string.Compare(MonitorServer.Cmd, "UpLoad"))
                {
                    //文件分快处理
                    SplitFile(MonitorServer.FullPath.Substring(0, MonitorServer.FullPath.Length - MonitorServer.FileName.Length) + "\\", MonitorServer.FullPath);
                    SetText("\r\n" + TargetSever[1].FullPath + "    " + TargetSever[2].FullPath + "    " + TargetSever[3].FullPath);
                    SetText("\r\n" + TargetSever[1].IP + "/" + TargetSever[1].Port);
                    SetText("\r\n" + TargetSever[2].IP + "/" + TargetSever[2].Port);
                    SetText("\r\n" + TargetSever[3].IP + "/" + TargetSever[3].Port);

                    Thread uploadThread1 = new Thread(UpLoadFiles);
                    uploadThread1.Start(TargetSever[1]);

                    Thread uploadThread2 = new Thread(UpLoadFiles);
                    uploadThread2.Start(TargetSever[2]);

                    Thread uploadThread3 = new Thread(UpLoadFiles);
                    uploadThread3.Start(TargetSever[3]);

                    uploadThread1.Join();
                    uploadThread2.Join();
                    uploadThread3.Join();

                    DeleteFile(TargetSever[1].FullPath);
                    DeleteFile(TargetSever[2].FullPath);
                    DeleteFile(TargetSever[3].FullPath);

                    SetText("\r\n文件上传完成!");
                    StauesModify(MonitorServer.FileName, "Ok");

                    //三个文件全部传输完成则删除文件
                }
                else if (0 == string.Compare(MonitorServer.Cmd, "DownLoad"))
                {
                    string[] TempExtra = MonitorServer.FileName.Split('.');
                    TargetSever[1].FileName = TempExtra[0] + "_" + 1.ToString() + "." + TempExtra[1];
                    TargetSever[2].FileName = TempExtra[0] + "_" + 2.ToString() + "." + TempExtra[1];
                    TargetSever[3].FileName = TempExtra[0] + "_" + 3.ToString() + "." + TempExtra[1];
                    SetText("\r\n" + TargetSever[1].FileName + "    " + TargetSever[2].FileName + "    " + TargetSever[3].FileName);

                    Thread downloadThread1 = new Thread(DownLoadFiles);
                    downloadThread1.Start(TargetSever[1]);

                    Thread downloadThread2 = new Thread(DownLoadFiles);
                    downloadThread2.Start(TargetSever[2]);

                    Thread downloadThread3 = new Thread(DownLoadFiles);
                    downloadThread3.Start(TargetSever[3]);

                    downloadThread1.Join();
                    downloadThread2.Join();
                    downloadThread3.Join();

                    //等待文件接收完成则合并文件
                    string[] Path_List = { Path.Combine(DownLoadSavePath, TargetSever[1].FileName), Path.Combine(DownLoadSavePath, TargetSever[2].FileName),
                                        Path.Combine(DownLoadSavePath, TargetSever[3].FileName)};
                    CombinFile(Path_List, Path.Combine(DownLoadSavePath, MonitorServer.FileName));
                    SetText("\r\n文件下载完成!");
                    StauesModify(MonitorServer.FileName, "Ok");
                }
            }
            else
            {
                //只接收一组IP信息 @192.168.3.32/20000
                TargetSever[1].IP = IPstrArray[0]; TargetSever[1].Port = Convert.ToInt32(IPstrArray[1]);

                SetText(IPstrArray[0]); SetText("/" + IPstrArray[1]);

                AskSocket.Close();//关闭套接字

                if (0 == string.Compare(MonitorServer.Cmd, "UpLoad"))
                {
                    TargetSever[1].FullPath = MonitorServer.FullPath;
                    Thread uploadThread = new Thread(UpLoadFiles);
                    uploadThread.Start(TargetSever[1]);
                    
                    uploadThread.Join();
                    SetText("\r\n文件上传完成!");
                    StauesModify(MonitorServer.FileName, "Ok");
                }
                else if (0 == string.Compare(MonitorServer.Cmd, "DownLoad"))
                {
                    TargetSever[1].FileName = MonitorServer.FileName;
                    Thread downloadThread = new Thread(DownLoadFiles);
                    downloadThread.Start(TargetSever[1]);
                    
                    downloadThread.Join();
                    SetText("\r\n文件下载完成!");
                    StauesModify(MonitorServer.FileName, "Ok");
                }

            }

        }

        public void Socket_Init()
        {
            //服务器IP地址  
            IPAddress ip = IPAddress.Parse("192.168.3.32");
            int myProt = Convert.ToInt32(40000);
            cSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            cSocket.Bind(new IPEndPoint(ip, myProt));  //绑定IP地址：端口  
            cSocket.Listen(20);    //设定最多20个排队连接请求  
            Console.WriteLine("等待客户机进行连接......");

            Thread myThread = new Thread(ListenClientConnect);//通过Clientsoket发送数据  
            myThread.Start();
        }


        private  void ListenClientConnect()
        {
            retfilecount = 0;
            while (true)
            {
                if (cSocket != null)
                {
                    try
                    {
                        Socket clientSocket = cSocket.Accept();
                        IPEndPoint clientip = (IPEndPoint)clientSocket.RemoteEndPoint;
                        //Console.WriteLine("Client IP:" + clientip.Address + "  PORT:" + clientip.Port);
                        Thread receiveThread = new Thread(TransferThread);
                        receiveThread.Start(clientSocket);
                        retfilecount++;
                        if (retfilecount == 6)
                        {
                            cSocket.Close();
                            break;
                        }
                    }
                    catch
                    {
                        Console.WriteLine("创建socket出错，程序退出...");
                        break;
                    }
                }
            }
            DEL_button.Enabled = true;
            DEL_button.Text = "SelectData";

        }

        public void TransferThread(object sck)
        {
            Socket client = sck as Socket;
            //获得客户端节点对象  
            IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
            ReceiveFiles(client);
        }


        public void UpLoadFiles(object Cnt)
        {

            ConnectInfo connect = (ConnectInfo)Cnt;
            if (connect.FullPath != null)
            {
                FileInfo EzoneFile = new FileInfo(connect.FullPath); //创建一个文件对象
                FileStream EzoneStream = EzoneFile.OpenRead();   //打开文件流   

                int PacketSize = 1024 * 1024;  //包的大小1M  
                int PacketCount = (int)(EzoneStream.Length / ((long)PacketSize));//包的数量    
                //最后一个包的大小  
                int LastDataPacket = (int)(EzoneStream.Length - ((long)(PacketSize * PacketCount)));

                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(connect.IP), connect.Port); //指向远程服务端节点   
                Socket UploadfileSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    UploadfileSocket.Connect(ipep);  //连接到发送端  
                }
                catch
                {
                    MessageBox.Show("连接服务器出错！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    IsTransferOk = false;
                }
                //获得服务器端节点对象  
                IPEndPoint clientep = (IPEndPoint)UploadfileSocket.RemoteEndPoint;
                //发送[操作命名]到服务器端
                TransferFiles.SendVarData(UploadfileSocket, System.Text.Encoding.Unicode.GetBytes("UpLoad"));
                //发送[文件名]到服务器端  
                TransferFiles.SendVarData(UploadfileSocket, System.Text.Encoding.Unicode.GetBytes(EzoneFile.Name));
                //发送[包的大小]到服务器端  
                TransferFiles.SendVarData(UploadfileSocket, System.Text.Encoding.Unicode.GetBytes(PacketSize.ToString()));
                //发送[包的总数量]到服务器端  
                TransferFiles.SendVarData(UploadfileSocket, System.Text.Encoding.Unicode.GetBytes(PacketCount.ToString()));
                //发送[最后一个包的大小]到服务器端  
                TransferFiles.SendVarData(UploadfileSocket, System.Text.Encoding.Unicode.GetBytes(LastDataPacket.ToString()));

                byte[] data = new byte[PacketSize];  //数据包  

                for (int i = 0; i < PacketCount; i++)   //开始循环发送数据包  
                {
                    //从文件流读取数据并填充数据包  
                    EzoneStream.Read(data, 0, data.Length);
                    //发送数据包  
                    if (TransferFiles.SendVarData(UploadfileSocket, data) == 3)
                    {
                        IsTransferOk = true;
                        MessageBox.Show("文件传输出错！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;  //可有可无?
                    }
                }

                if (LastDataPacket != 0)  //如果还有多余的数据包，则应该发送完毕！  
                {
                    data = new byte[LastDataPacket];
                    EzoneStream.Read(data, 0, data.Length);
                    TransferFiles.SendVarData(UploadfileSocket, data);
                }
                SetText("\r\n"+EzoneFile.Name+"发送完成!");
                UploadfileSocket.Close();//关闭套接字    
                EzoneStream.Close(); //关闭文件流  
                
            }
        }

        public void DataProcess(object Cnt)
        {
            ConnectInfo connect = (ConnectInfo)Cnt;
            if (connect.FullPath != null)
            {
                FileInfo EzoneFile = new FileInfo(connect.FullPath); //创建一个文件对象
                FileStream EzoneStream = EzoneFile.OpenRead();   //打开文件流   

                int PacketSize = 1024 * 1024;  //包的大小1M  
                int PacketCount = (int)(EzoneStream.Length / ((long)PacketSize));//包的数量    
                //最后一个包的大小  
                int LastDataPacket = (int)(EzoneStream.Length - ((long)(PacketSize * PacketCount)));

                IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(connect.IP), connect.Port); //指向远程服务端节点   
                Socket UploadfileSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    UploadfileSocket.Connect(ipep);  //连接到发送端  
                }
                catch
                {
                    MessageBox.Show("连接服务器出错！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    IsTransferOk = false;
                }
                //获得服务器端节点对象  
                IPEndPoint clientep = (IPEndPoint)UploadfileSocket.RemoteEndPoint;
                //发送[操作命名]到服务器端
                TransferFiles.SendVarData(UploadfileSocket, System.Text.Encoding.Unicode.GetBytes("DataProcess"));
                //发送[文件名]到服务器端  
                TransferFiles.SendVarData(UploadfileSocket, System.Text.Encoding.Unicode.GetBytes(EzoneFile.Name));
                //发送[包的大小]到服务器端  
                TransferFiles.SendVarData(UploadfileSocket, System.Text.Encoding.Unicode.GetBytes(PacketSize.ToString()));
                //发送[包的总数量]到服务器端  
                TransferFiles.SendVarData(UploadfileSocket, System.Text.Encoding.Unicode.GetBytes(PacketCount.ToString()));
                //发送[最后一个包的大小]到服务器端  
                TransferFiles.SendVarData(UploadfileSocket, System.Text.Encoding.Unicode.GetBytes(LastDataPacket.ToString()));

                byte[] data = new byte[PacketSize];  //数据包  

                for (int i = 0; i < PacketCount; i++)   //开始循环发送数据包  
                {
                    //从文件流读取数据并填充数据包  
                    EzoneStream.Read(data, 0, data.Length);
                    //发送数据包  
                    if (TransferFiles.SendVarData(UploadfileSocket, data) == 3)
                    {
                        IsTransferOk = true;
                        MessageBox.Show("文件传输出错！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;  //可有可无?
                    }
                }

                if (LastDataPacket != 0)  //如果还有多余的数据包，则应该发送完毕！  
                {
                    data = new byte[LastDataPacket];
                    EzoneStream.Read(data, 0, data.Length);
                    TransferFiles.SendVarData(UploadfileSocket, data);
                }
                SetText("\r\n数据正在处理，请稍等...");
                UploadfileSocket.Close();//关闭套接字    
                EzoneStream.Close(); //关闭文件流  
            }
 
        }

        public void DownLoadFiles(object Cnt)
        {
            ConnectInfo connect = (ConnectInfo)Cnt;
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(connect.IP), connect.Port); //指向远程服务端节点   
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                client.Connect(ipep);  //连接到发送端  
            }
            catch
            {
                MessageBox.Show("连接服务器出错！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //获得服务器端节点对象,clientep用于存储对象节点信息，没有其他作用   
            IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
            //发送操作命令
            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes("DownLoad"));
            //发送需要下载文件名
            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(connect.FileName));
            //接收下载的文件
            ReceiveFiles(client);
        }



        public void ReceiveFiles(Socket client)
        {
            //获得[文件名]     
            string ReceiveFileName = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));
            //获得[包的大小]     
            string bagSize = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));
            //获得[包的总数量]     
            int bagCount = int.Parse(System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client)));
            //获得[最后一个包的大小]     
            string bagLast = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));
            string fullPath = Path.Combine(DownLoadSavePath, ReceiveFileName);
            FileStream MyFileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);

            //已收到包的个数     
            int ReceivedCount = 0;
            while (true)
            {
                byte[] data = TransferFiles.ReceiveVarData(client);
                if (data.Length == 0)
                {
                    break;
                }
                else
                {
                    ReceivedCount++;//将接收到的数据包写入到文件流对象   
                    MyFileStream.Write(data, 0, data.Length);
                }
            }
            //关闭文件流     
            MyFileStream.Close();
            //关闭套接字     
            client.Close();
            SetText("\r\n" + ReceiveFileName + "下载完毕!");
        }

        public void DeleteFiles(Object Cnt)
        {
            ConnectInfo connect = (ConnectInfo)Cnt;
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(connect.IP), connect.Port); //指向远程服务端节点   
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                client.Connect(ipep);  //连接到发送端  
            }
            catch
            {
                MessageBox.Show("连接服务器失败！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            //获得服务器端节点对象,clientep用于存储对象节点信息，没有其他作用   
            IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
            //发送操作命令
            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes("Delete"));
            //发送需要删除的文件名
            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(connect.FileName));

            client.Close();
            SetText("\r\n文件删除完成!");
        }

        public void UpdataFiles(Object Cnt)
        {
            ConnectInfo connect = (ConnectInfo)Cnt;
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(connect.IP), connect.Port); //指向远程服务端节点   
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                client.Connect(ipep);  //连接到发送端  
            }
            catch
            {
                MessageBox.Show("连接服务器失败！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //获得服务器端节点对象,clientep用于存储对象节点信息，没有其他作用   
            IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
            //发送操作命令
            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes("UpdataFile"));
            //等带接收返回数据
            int filecount = int.Parse(System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client)));

            string[] filelist = new string[100]; 
            for (int i = 0; i <= filecount-1; i++)
            {
                filelist[i] = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));
            }
            client.Close();

            listView1.Items.Clear();

            for (int i = 0; i <= filecount-1; i++)
            {
                string[] stArray = filelist[i].Split('$');
                //listView1.Items[i].SubItems[0].Text = stArray[0];
                //listView1.Items[i].SubItems[1].Text = (Convert.ToInt64(stArray[1]) / 1024.0).ToString() + "KB";
                //listView1.Items[i].SubItems[2].Text = ("...");
                //listView1.Items[i].SubItems[3].Text = (DateTime.Now.ToString());
                //listView1.Items[i].SubItems[4].Text = ("Ok");
                ListViewItem fileItem = listView1.Items.Add(stArray[0]);
                fileItem.Name = stArray[0];
                fileItem.SubItems.Add((Convert.ToInt64(stArray[1])).ToString() + "Byte");
                fileItem.SubItems.Add("...");
                fileItem.SubItems.Add(DateTime.Now.ToString());
                fileItem.SubItems.Add("Ok");
            }

        }


        private void MyFileListUpdate(string newPath)
        {
            //if (Directory.Exists(newPath))
            //{
            //    try
            //    {
            //        DirectoryInfo dir = new DirectoryInfo(newPath);
            //        ListViewItem dirItem = listView1.Items.Add(dir.Name, 2);
            //        dirItem.Name = dir.FullName;
            //        dirItem.SubItems.Add("");
            //        dirItem.SubItems.Add("文件夹");
            //        dirItem.SubItems.Add(dir.LastWriteTimeUtc.ToString());
            //        dirItem.SubItems.Add("UpLoading...");
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }
            //}
            if (File.Exists(newPath))
            {
                bool fileisexist = false;
                try
                {
                    TargetSever[1].FullPath = newPath;//上传文件的存储路径
                    FileInfo file = new FileInfo(newPath);

                    for (int i = 0; i < listView1.Items.Count; i++)
                    {
                        if (listView1.Items[i].Text == file.Name)
                        {
                            MessageBox.Show("是否要覆盖已有文件！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                           
                            listView1.Items[i].SubItems[1].Text = ((file.Length).ToString() + "Byte");
                            listView1.Items[i].SubItems[2].Text = (file.Extension);
                            listView1.Items[i].SubItems[3].Text = (file.LastWriteTimeUtc.ToString());
                            listView1.Items[i].SubItems[4].Text = ("UpLoading...");
                            fileisexist = true;
                            break;
                        }
                    }
                    if (!fileisexist)
                    {
                        ListViewItem fileItem = listView1.Items.Add(file.Name);
                        fileItem.Name = file.FullName;
                        fileItem.SubItems.Add((file.Length).ToString() + "Byte");
                        fileItem.SubItems.Add(file.Extension);
                        fileItem.SubItems.Add(file.LastWriteTimeUtc.ToString());
                        fileItem.SubItems.Add("UpLoading...");
                    }

                    MonitorServer.FullPath = newPath;//包含文件名
                    MonitorServer.FileName = file.Name;
                    MonitorServer.FileLength = file.Length;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void SetText(string text)
        {
            if (this.InforichTextBox.InvokeRequired)
            {
                setTextValueCallBack setCallBack = new setTextValueCallBack(SetText);
                this.Invoke(setCallBack, new object[] { text });
            }
            else
            {
                this.InforichTextBox.AppendText(text);
            }
        }

        public void StauesModify(string name, string status)
        {
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                if (listView1.Items[i].Text == name)
                {
                    listView1.Items[i].SubItems[4].Text = status;
                    break;
                }
            }
        }

        public long GetSelectfilesize(string name)
        {
            long fsize = 0;
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                if (listView1.Items[i].Text == name)
                {
                    string s = listView1.Items[i].SubItems[1].Text;
                    s = s.Substring(0, s.Length - 4);
                    //s = s.Replace(".", "");
                    fsize = Convert.ToInt64(s);
                    SetText(listView1.Items[i].SubItems[1].Text);
                    break;
                }
            }
            return fsize;
        }

        public void SplitFile( string strPath, string strFile)
        {
            int iFileSize = 0;
            //int iFileCount = 3;
            byte[] TempBytes;//每次分割读取的最大数据
            string[] sTempFileName = {"","","",""};
            int i = 1;

            FileStream SplitFileStream = new FileStream(strFile, FileMode.Open);
            FileInfo ifile = new FileInfo(strFile);
            //以FileStream文件流来初始化BinaryReader文件阅读器
            BinaryReader SplitFileReader = new BinaryReader(SplitFileStream);
            
            iFileSize = (int)(SplitFileStream.Length - (SplitFileStream.Length % 3)) / 3;
            string[] TempExtra = ifile.Name.Split('.');

            for (i = 1; i <= 3; i++)
            {
                //确定小文件的文件名称
                //string sTempFileName = strPath + @"\" + i.ToString().PadLeft(4, '0') + "." + TempExtra[TempExtra.Length - 1]; //小文件名
                sTempFileName[i] = strPath + TempExtra[0] + "_" + i.ToString() + "." + TempExtra[1]; //小文件名
                //根据文件名称和文件打开模式来初始化FileStream文件流实例
                FileStream TempStream = new FileStream(sTempFileName[i], FileMode.OpenOrCreate);
                //以FileStream实例来创建、初始化BinaryWriter书写器实例
                BinaryWriter TempWriter = new BinaryWriter(TempStream);
                //从大文件中读取指定大小数据
                if (i == 3)
                    TempBytes = SplitFileReader.ReadBytes((int)(iFileSize+(SplitFileStream.Length % 3)));
                else
                    TempBytes = SplitFileReader.ReadBytes(iFileSize);
                //把此数据写入小文件
                TempWriter.Write(TempBytes);
                //关闭书写器，形成小文件
                TempWriter.Close();
                //关闭文件流
                TempStream.Close();
            }
            TargetSever[1].FullPath = sTempFileName[1];
            TargetSever[2].FullPath = sTempFileName[2];
            TargetSever[3].FullPath = sTempFileName[3];
            //关闭大文件阅读器
            SplitFileReader.Close();
            SplitFileStream.Close();

            SetText("\r\n文件分块完成，准备上传......");
        }

        public void CombinFile(string[] strFile, string strPath)
        {
            FileStream AddStream = null;
            //以合并后的文件名称和打开方式来创建、初始化FileStream文件流
            AddStream = new FileStream(strPath, FileMode.Append);
            //以FileStream文件流来初始化BinaryWriter书写器，此用以合并分割的文件
            BinaryWriter AddWriter = new BinaryWriter(AddStream);
            FileStream TempStream = null;
            BinaryReader TempReader = null;
            //循环合并小文件，并生成合并文件
            for (int i = 0; i < strFile.Length; i++)
            {
                //以小文件所对应的文件名称和打开模式来初始化FileStream文件流，起读取分割作用
                TempStream = new FileStream(strFile[i].ToString(), FileMode.Open);
                TempReader = new BinaryReader(TempStream);
                //读取分割文件中的数据，并生成合并后文件
                AddWriter.Write(TempReader.ReadBytes((int)TempStream.Length));
                //关闭BinaryReader文件阅读器
                TempReader.Close();
                //关闭FileStream文件流
                TempStream.Close();
            }
            //关闭BinaryWriter文件书写器
            AddWriter.Close();
            //关闭FileStream文件流
            AddStream.Close();

            foreach (string str_path in strFile)
            {
                DeleteFile(str_path);
            }
            
            SetText("\r\n块文件已合并完成！");
        }

        public void DeleteFile(string path)
        {
            FileAttributes attr = File.GetAttributes(path);
            if (attr == FileAttributes.Directory)
            {
                Directory.Delete(path, true);
            }
            else
            {
                File.Delete(path);
            }
        }

        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Environment.Exit(0);
        }

    }
}