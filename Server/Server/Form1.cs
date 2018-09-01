using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Deployment;
using System.IO;
using System.Diagnostics;



namespace Server
{
    public partial class Form1 : Form
    {

        public struct ConnectInfo
        {
            public string IP;
            public Int32 Port;
            public int IsLive;
            public string FileName;
        };

        public static ConnectInfo MonitorAddress = new ConnectInfo { IP = "192.168.3.32", Port = 30000 };

        public delegate void setTextValueCallBack(string text);    //定义回调
        public static int connections = 0;
        private static Socket serverSocket;
        public string FileSavePath = "D:\\ServerFile\\";



        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Thread Cnt = new Thread(ConnetMonitor);
            Cnt.Start();
            //Cnt.Join();
            if (!Directory.Exists(FileSavePath))
            {
                Directory.CreateDirectory(FileSavePath);
            }
            this.button2.Enabled = false;
            Socket_Init();
            Timer_Init();

        }

        public void SetText(string text)
        {
            if (this.RStextBox.InvokeRequired)
            {
                setTextValueCallBack setCallBack = new setTextValueCallBack(SetText);
                this.Invoke(setCallBack, new object[] { text });
            }
            else
            {
                this.RStextBox.AppendText(text);
            }
        }


        public void Socket_Init()
        {
            //服务器IP地址  
            //IPAddress ip = IPAddress.Parse("192.168.3.76");
            int myProt = Convert.ToInt32(20000);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, myProt));  //绑定IP地址：端口  
            serverSocket.Listen(20);    //设定最多20个排队连接请求  
            this.SetText("等待客户机进行连接......\n");

            Thread myThread = new Thread(ListenClientConnect);//通过Clientsoket发送数据  
            myThread.Start();

        }

        public void ConnetMonitor()
        {
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("192.168.3.32"), 30000); //指向远程服务端节点   
            Socket AskSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                AskSocket.Connect(ipep);  //连接到Monitor
            }
            catch
            {
                SetText("\r\n连接服务器出错,请重启程序...");
                AskSocket.Close();
                return;
            }
            //获得客户端节点对象  
            IPEndPoint clientep = (IPEndPoint)AskSocket.RemoteEndPoint;
            //发送操作命令
            TransferFiles.SendVarData(AskSocket, System.Text.Encoding.Unicode.GetBytes("ServerRegister"));

            AskSocket.Close();
        }

        public static void Socket_Exit()
        {
            serverSocket.Close();
            serverSocket = null;
        }

        private void ListenClientConnect()
        {
            while (true)
            {
                if (serverSocket != null)
                {
                    try
                    {
                        Socket clientSocket = serverSocket.Accept();

                        connections++;//如果Socket不是空，则连接数加1  
                        this.SetText("新客户连接建立：" + connections + "个连接数\n");

                        IPEndPoint clientip = (IPEndPoint)clientSocket.RemoteEndPoint;
                        this.SetText("\n" + "Client IP:" + clientip.Address + "  PORT:" + clientip.Port + "\n");
                        Thread receiveThread = new Thread(TransferThread);
                        receiveThread.Start(clientSocket);
                    }
                    catch
                    {
                        break;
                    }
                }

            }
        }

        public void TransferThread(object clientSocket)
        {
            Socket client = clientSocket as Socket;
            //获得客户端节点对象  
            IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
            //获取[操作命令]
            string operatcmd = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));

            if (0 == string.Compare(operatcmd, "UpLoad"))
            {
                string revfilename = "";
                revfilename = ReceiveFiles(client);//接收文件完成

                Thread bakthread = new Thread(BackupFile);
                bakthread.Start(revfilename);
            }
            else if (0 == string.Compare(operatcmd, "DownLoad"))
            {
                try
                {
                    UpLoadFile(client);
                }
                catch
                {
                    this.SetText("文件传输出现错误！\n");

                }
            }
            else if (0 == string.Compare(operatcmd, "Backup"))
            {
                //接收来自于同级Server的备份请求
                //把传送过来的接收文件直接存储起来
                string revfilename = "";
                revfilename = ReceiveFiles(client);//接收文件完成
            }
            else if (0 == string.Compare(operatcmd, "DataProcess"))
            {
                //接收Monitor传过来的文件
                //调用python程序计算数据
                //计算结束把结果返回给Monitor
                string rn = "";
                rn = ReceiveFiles(client);//接收文件完成

                string i_str = rn.Substring(rn.Length - 5, 1);

                Process CmdProcess = new Process();
                CmdProcess.StartInfo.FileName = "cmd.exe";
                //命令格式："python D:\ServerFile\dataprocess.py D:\ServerFile\ fileneme 文件序号"
                //“/C”表示执行完命令后马上退出  
                CmdProcess.StartInfo.Arguments = "/c python " + FileSavePath + "dataprocess.py" + " "
                                                   + FileSavePath + " " + rn
                                                   + " " + i_str;
                CmdProcess.Start();//执行  
                //CmdProcess.StandardOutput.ReadToEnd();//输出  
                CmdProcess.WaitForExit();//等待程序执行完退出进程  
                CmdProcess.Close();//结束 
                Console.WriteLine("计算完成，正在准备回传数据...");

                string ffname = "result1_" + i_str + ".txt";
                DataReturn(ffname);
                ffname = "result2_" + i_str + ".txt";
                DataReturn(ffname);

            }
            else
            {
                SetText("消息错误，Socket连接已退出！");
                client.Close();
            }
        }

        public void DataReturn(string fname)
        {
            string fn = fname;
            //获取文件名 不包含存储路径

            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(MonitorAddress.IP), MonitorAddress.Port); //指向远程服务端节点   
            Socket ssSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                ssSocket.Connect(ipep);  //连接到Monitor
            }
            catch
            {
                SetText("连接服务器出错！");
                ssSocket.Close();
                return;
            }
            string fullPath = FileSavePath + fname;

            FileInfo EzoneFile = new FileInfo(fullPath);  //创建一个文件对象
            FileStream EzoneStream = EzoneFile.OpenRead();  //打开文件流   

            int PacketSize = 1024 * 1024;  //包的大小1M  
            int PacketCount = (int)(EzoneStream.Length / ((long)PacketSize));//包的数量  
            //最后一个包的大小  
            int LastDataPacket = (int)(EzoneStream.Length - ((long)(PacketSize * PacketCount)));

            //发送命令码
            TransferFiles.SendVarData(ssSocket, System.Text.Encoding.Unicode.GetBytes("ProcessReturn"));
            //发送[文件名]到服务器端  
            TransferFiles.SendVarData(ssSocket, System.Text.Encoding.Unicode.GetBytes(EzoneFile.Name));
            //发送[包的大小]到服务器端  
            TransferFiles.SendVarData(ssSocket, System.Text.Encoding.Unicode.GetBytes(PacketSize.ToString()));
            //发送[包的总数量]到服务器端  
            TransferFiles.SendVarData(ssSocket, System.Text.Encoding.Unicode.GetBytes(PacketCount.ToString()));
            //发送[最后一个包的大小]到服务器端  
            TransferFiles.SendVarData(ssSocket, System.Text.Encoding.Unicode.GetBytes(LastDataPacket.ToString()));

            byte[] data = new byte[PacketSize];  //数据包  

            for (int i = 0; i < PacketCount; i++)   //开始循环发送数据包  
            {
                //从文件流读取数据并填充数据包  
                EzoneStream.Read(data, 0, data.Length);
                //发送数据包  
                if (TransferFiles.SendVarData(ssSocket, data) == 3)
                {
                    MessageBox.Show("文件传输出错！");
                }
            }

            if (LastDataPacket != 0)  //如果还有多余的数据包，则应该发送完毕！  
            {
                data = new byte[LastDataPacket];
                EzoneStream.Read(data, 0, data.Length);
                TransferFiles.SendVarData(ssSocket, data);
            }

            EzoneStream.Close(); //关闭文件流  
            ssSocket.Close();//关闭套接字  
            this.SetText(fname + "回传成功！" + "\n");
        }

        public void BackupFile(object Cnt)
        {
            string ffname = Cnt as string;

            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(MonitorAddress.IP), MonitorAddress.Port); //指向远程服务端节点   
            Socket askSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                askSocket.Connect(ipep);  //连接到Monitor
            }
            catch
            {
                SetText("连接服务器出错！");
                askSocket.Close();
                return;
            }

            //获得客户端节点对象  
            IPEndPoint bakipep = (IPEndPoint)askSocket.RemoteEndPoint;
            //发送操作命令
            TransferFiles.SendVarData(askSocket, System.Text.Encoding.Unicode.GetBytes("Backup"));
            //发送文件名
            TransferFiles.SendVarData(askSocket, System.Text.Encoding.Unicode.GetBytes(ffname));
            //备份IP地址   
            string bakaddress = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(askSocket));
            string[] IPstrArray = bakaddress.Split(new string[] { "@", "/" }, StringSplitOptions.RemoveEmptyEntries);
            askSocket.Close();


            IPEndPoint iipep = new IPEndPoint(IPAddress.Parse(IPstrArray[0]), Convert.ToInt32(IPstrArray[1])); //指向远程服务端节点   
            Socket ssSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                ssSocket.Connect(iipep);  //连接到Monitor
            }
            catch
            {
                SetText("连接服务器出错！");
                ssSocket.Close();
                return;
            }

            //获取文件名 不包含存储路径
            string fullPath = FileSavePath + ffname;

            FileInfo EzoneFile = new FileInfo(fullPath);  //创建一个文件对象
            FileStream EzoneStream = EzoneFile.OpenRead();  //打开文件流   

            int PacketSize = 1024 * 1024;  //包的大小1M  
            int PacketCount = (int)(EzoneStream.Length / ((long)PacketSize));//包的数量  
            //最后一个包的大小  
            int LastDataPacket = (int)(EzoneStream.Length - ((long)(PacketSize * PacketCount)));

            //发送命令码
            TransferFiles.SendVarData(ssSocket, System.Text.Encoding.Unicode.GetBytes("Backup"));
            //发送[文件名]到服务器端  
            TransferFiles.SendVarData(ssSocket, System.Text.Encoding.Unicode.GetBytes(EzoneFile.Name));
            //发送[包的大小]到服务器端  
            TransferFiles.SendVarData(ssSocket, System.Text.Encoding.Unicode.GetBytes(PacketSize.ToString()));
            //发送[包的总数量]到服务器端  
            TransferFiles.SendVarData(ssSocket, System.Text.Encoding.Unicode.GetBytes(PacketCount.ToString()));
            //发送[最后一个包的大小]到服务器端  
            TransferFiles.SendVarData(ssSocket, System.Text.Encoding.Unicode.GetBytes(LastDataPacket.ToString()));

            byte[] data = new byte[PacketSize];  //数据包  

            for (int i = 0; i < PacketCount; i++)   //开始循环发送数据包  
            {
                //从文件流读取数据并填充数据包  
                EzoneStream.Read(data, 0, data.Length);
                //发送数据包  
                if (TransferFiles.SendVarData(ssSocket, data) == 3)
                {
                    MessageBox.Show("文件传输出错！");
                }
            }

            if (LastDataPacket != 0)  //如果还有多余的数据包，则应该发送完毕！  
            {
                data = new byte[LastDataPacket];
                EzoneStream.Read(data, 0, data.Length);
                TransferFiles.SendVarData(ssSocket, data);
            }

            EzoneStream.Close(); //关闭文件流  
            ssSocket.Close();//关闭套接字    
            this.SetText(ffname + "文件已备份到Server:" + IPstrArray[0] + "\n");
        }

        public string ReceiveFiles(Socket client)
        {
            //获得[文件名]     
            string ReceiveFileName = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));
            //获得[包的大小]     
            string bagSize = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));
            //获得[包的总数量]     
            int bagCount = int.Parse(System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client)));
            //获得[最后一个包的大小]     
            string bagLast = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));
            string fullPath = Path.Combine(FileSavePath, ReceiveFileName);
            FileStream MyFileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);

            //已接收包的个数     
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
                    ReceivedCount++;
                    //将接收到的数据包写入到文件流对象     
                    MyFileStream.Write(data, 0, data.Length);
                }
            }

            string sstr = ReceiveFileName;
            //关闭文件流     
            MyFileStream.Close();
            //关闭套接字     
            client.Close();
            connections--;
            this.SetText("\n" + "客户关闭连接：" + connections + "个连接数\n");
            this.SetText("接收完成！\n");
            return sstr;
        }

        public void UpLoadFile(object Cnt)
        {
            Socket client = Cnt as Socket;

            //获取文件名 不包含存储路径
            string fullPath = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));
            fullPath = FileSavePath + fullPath;

            FileInfo EzoneFile = new FileInfo(fullPath);  //创建一个文件对象
            FileStream EzoneStream = EzoneFile.OpenRead();  //打开文件流   

            int PacketSize = 1024 * 1024;  //包的大小1M  
            int PacketCount = (int)(EzoneStream.Length / ((long)PacketSize));//包的数量    
            //最后一个包的大小  
            int LastDataPacket = (int)(EzoneStream.Length - ((long)(PacketSize * PacketCount)));
            //发送[文件名]到服务器端  
            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(EzoneFile.Name));
            //发送[包的大小]到服务器端  
            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(PacketSize.ToString()));
            //发送[包的总数量]到服务器端  
            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(PacketCount.ToString()));
            //发送[最后一个包的大小]到服务器端  
            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(LastDataPacket.ToString()));

            //bool IsTransferOk = false;
            byte[] data = new byte[PacketSize];  //数据包  

            for (int i = 0; i < PacketCount; i++)   //开始循环发送数据包  
            {
                //从文件流读取数据并填充数据包  
                EzoneStream.Read(data, 0, data.Length);
                //发送数据包  
                if (TransferFiles.SendVarData(client, data) == 3)
                {
                    //IsTransferOk = true;
                    MessageBox.Show("文件传输出错！");
                }
            }

            if (LastDataPacket != 0)  //如果还有多余的数据包，则应该发送完毕！  
            {
                data = new byte[LastDataPacket];
                EzoneStream.Read(data, 0, data.Length);
                TransferFiles.SendVarData(client, data);
            }

            client.Close();//关闭套接字    
            EzoneStream.Close(); //关闭文件流  
            connections--;
            this.SetText("\n" + "客户关闭连接：" + connections + "个连接数\n");
            this.SetText("传输完成！\n");
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("确定要退出程序?", "安全提示",
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Warning)
                == System.Windows.Forms.DialogResult.Yes)
            {

                System.Environment.Exit(0);

            }
            else
            {
                e.Cancel = true;
            }

        }

        public static void Timer_Init()
        {
            System.Timers.Timer heartbeat = new System.Timers.Timer(5 * 1000);//实例化Timer类，设置间隔时间为 30S；
            heartbeat.Elapsed += new System.Timers.ElapsedEventHandler(timeout); //到达时间的时候执行事件；
            heartbeat.AutoReset = true;//设置是执行一次（false）还是一直执行(true)；
            heartbeat.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；
        }

        public static void timeout(object source, System.Timers.ElapsedEventArgs e)
        {
            Thread cntthread = new Thread(ServerActiveAsk);
            cntthread.Start();
        }

        public static void ServerActiveAsk()
        {
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(MonitorAddress.IP), MonitorAddress.Port); //指向远程服务端节点   
            Socket AskSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                AskSocket.Connect(ipep);  //连接到Monitor
            }
            catch
            {
                Console.WriteLine("连接服务器出错！");
                return;//能否直接跳出？
            }
            //获得客户端节点对象  
            IPEndPoint clientep = (IPEndPoint)AskSocket.RemoteEndPoint;
            //发送操作命令
            TransferFiles.SendVarData(AskSocket, System.Text.Encoding.Unicode.GetBytes("Pulse"));

            AskSocket.Close();
        }

    }


}
