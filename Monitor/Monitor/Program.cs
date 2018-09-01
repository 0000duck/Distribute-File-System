using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Deployment;
using System.IO;
using System.Timers;
using System.Diagnostics;

namespace Monitor
{
    class Program
    {

        public struct ConnectInfo
        {
            public string IP;
            public Int32 Port;
            public int IsLive;
            public string Filename;
        };

        public struct FileSaveTable
        {
            public string Filename;
            public long FileLength;
            public ConnectInfo SaveIP;
            public ConnectInfo BakIP;
            public bool IsSaved;
            public bool IsCut;
            public bool IsUpdata;
        };


        public static ConnectInfo[] TargetServer = new ConnectInfo[10];
        public static FileSaveTable[] Savedfile = new FileSaveTable[50];
        static int ServerCount = 0;
        static int SavedfileCount = 0;
        private static Socket MonitorSocket;
        public static string FileSavePath = "D:\\MonitorFile\\";
        public static string DataFilename = "";
        public static int DataSplitCount = 4;
        public static int RetdataCount = 0;


        static void Main(string[] args)
        {
            if (!Directory.Exists(FileSavePath))
            {
                Directory.CreateDirectory(FileSavePath);
            }
            Socket_Init();
            Timer_Init();
            while (true) ;
        }

        public static void Socket_Init()
        {
            //服务器IP地址  
            IPAddress ip = IPAddress.Parse("192.168.3.32");
            int myProt = Convert.ToInt32(30000);
            MonitorSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            MonitorSocket.Bind(new IPEndPoint(ip, myProt));  //绑定IP地址：端口  
            MonitorSocket.Listen(20);    //设定最多20个排队连接请求  
            Console.WriteLine("等待客户机进行连接......");

            Thread myThread = new Thread(ListenClientConnect);//通过Clientsoket发送数据  
            myThread.Start();
        }

        public static void Socket_Exit()
        {
            MonitorSocket.Close();
            MonitorSocket = null;
        }

        private static void ListenClientConnect()
        {
            while (true)
            {
                if (MonitorSocket != null)
                {
                    try
                    {
                        Socket clientSocket = MonitorSocket.Accept();

                        IPEndPoint clientip = (IPEndPoint)clientSocket.RemoteEndPoint;
                        //Console.WriteLine("Client IP:" + clientip.Address + "  PORT:" + clientip.Port);
                        Thread receiveThread = new Thread(TransferTread);
                        receiveThread.Start(clientSocket);
                    }
                    catch
                    {
                        Console.WriteLine("创建socket出错，程序退出...");
                        break;
                    }
                }

            }
        }

        public static void TransferTread(object clientSocket)
        {
            Socket client = clientSocket as Socket;
            //获得客户端节点对象  
            IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
            //获取[操作命令]
            string operatcmd = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));

            if (0 == string.Compare(operatcmd, "UpLoad"))
            {
                //消息：文件名$文件大小
                //处理消息根据文件大小选择是否分割文件
                string st = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));
                Console.WriteLine(st);
                string[] stArray = st.Split('$');
                string filename = stArray[0];
                Int64 filelen = Convert.ToInt64(stArray[1]);

                bool fileupdateflag = false;

                //文件已经存在，直接覆盖之前文件，不再重新分配IP
                for (int i = 1; i <= SavedfileCount; i++)
                {
                    if (Savedfile[i].Filename == filename)
                    {

                        Savedfile[i].FileLength = filelen;
                        if (Savedfile[i].IsCut)//文件分割标志true
                        {
                            string[] TempExtra = filename.Split('.');
                            string ipstr = "";
                            int fileorder = 0;
                            for (int j = 1; j <= 3; j++)
                            {
                                for (int k = 1; k <= SavedfileCount; k++)
                                {
                                    if (Savedfile[k].Filename == (TempExtra[0] + "_" + j.ToString() + "." + TempExtra[1]))
                                    {
                                        fileorder = k;
                                        break;
                                    }

                                }
                                ipstr += "@" + Savedfile[fileorder].SaveIP.IP + "/" + Savedfile[fileorder].SaveIP.Port.ToString();
                                Console.WriteLine("{0}", Savedfile[fileorder].SaveIP.IP);
                            }

                            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(ipstr));
                            client.Close();
                            Console.WriteLine("分配完成");
                        }
                        else
                        {
                            string ipstr = "";

                            ipstr = "@" + Savedfile[i].SaveIP.IP + "/" + Savedfile[i].SaveIP.Port.ToString();
                            Console.WriteLine("{0}", Savedfile[i].SaveIP.IP);

                            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(ipstr));
                            client.Close();
                            Console.WriteLine("分配完成");

                        }
                        fileupdateflag = true;
                        break;
                    }
                }
                //上传文件不存在文件表，则在文件表中注册文件，根据文件大小分割文件
                if (!fileupdateflag)
                {
                    SavedfileCount++;
                    Savedfile[SavedfileCount].Filename = filename;
                    Savedfile[SavedfileCount].FileLength = filelen;
                    Savedfile[SavedfileCount].IsCut = true;
                    Savedfile[SavedfileCount].IsSaved = false;
                    Savedfile[SavedfileCount].IsUpdata = true;

                    if ((filelen / (1024.0 * 1024.0)) > 700)//文件大于2G开始分块处理
                    {
                        //文件分块，直接发送3个IP地址到client端，分块过程由client直接操作
                        //分块文件命名规则统一按照 "文件名+_number+文件后缀"，各块文件按照IP发送顺序存储
                        //数据表记录文件存储名称和存储Server IP
                        int rass = ServerCount;
                        if (rass >= 3)//服务器数量足够
                        {
                            int[] randomget;
                            randomget = GetRandomArray(3, 1, rass);
                            Console.WriteLine("{0},{1},{2}", randomget[0], randomget[1], randomget[2]);

                            string[] TempExtra = filename.Split('.');
                            string ipstr = "";
                            SavedfileCount++;

                            for (int i = SavedfileCount; i <= SavedfileCount + 2; i++)
                            {
                                Savedfile[i].Filename = TempExtra[0] + "_" + (i - SavedfileCount + 1).ToString() + "." + TempExtra[1];
                                Savedfile[i].FileLength = filelen / 3;
                                Savedfile[i].IsCut = false;
                                Savedfile[i].IsSaved = false;
                                Savedfile[i].SaveIP.IP = TargetServer[randomget[i - SavedfileCount]].IP;
                                Savedfile[i].SaveIP.Port = 20000;
                                Savedfile[i].IsUpdata = false;

                                if (randomget[i - SavedfileCount] >= rass)
                                {
                                    Savedfile[i].BakIP.IP = TargetServer[1].IP;
                                    Savedfile[i].BakIP.Port = 20000;
                                }
                                else
                                {
                                    Savedfile[i].BakIP.IP = TargetServer[randomget[i - SavedfileCount] + 1].IP;
                                    Savedfile[i].BakIP.Port = 20000;
                                }
                                ipstr += "@" + Savedfile[i].SaveIP.IP + "/" + Savedfile[i].SaveIP.Port.ToString();
                            }
                            SavedfileCount += 2;

                            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(ipstr));
                            client.Close();
                            Console.WriteLine("分配完成");
                        }
                        else
                        {
                            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes("ServerError"));
                            client.Close();
                            Console.WriteLine("服务器数量太少，不能完成分配！");
                        }
                    }
                    else
                    {
                        //文件不分块，直接发送一个IP到client端
                        //数据表记录文件名称和文件存储Server IP
                        Savedfile[SavedfileCount].IsCut = false;
                        int rass = ServerCount;

                        if (rass >= 1)
                        {
                            int[] randomget;
                            randomget = GetRandomArray(1, 1, rass);
                            Savedfile[SavedfileCount].SaveIP.IP = TargetServer[randomget[0]].IP;
                            Savedfile[SavedfileCount].SaveIP.Port = 20000;

                            if (randomget[0] >= rass)
                            {
                                Savedfile[SavedfileCount].BakIP.IP = TargetServer[1].IP;
                                Savedfile[SavedfileCount].BakIP.Port = 20000;
                            }
                            else
                            {
                                Savedfile[SavedfileCount].BakIP.IP = TargetServer[randomget[0] + 1].IP;
                                Savedfile[SavedfileCount].BakIP.Port = 20000;
                            }
                            string ipstr = "@" + Savedfile[SavedfileCount].SaveIP.IP + "/" + Savedfile[SavedfileCount].SaveIP.Port.ToString();
                            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(ipstr));
                            client.Close();
                            Console.WriteLine("分配完成");
                        }
                        else
                        {
                            TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes("ServerError"));
                            client.Close();
                            Console.WriteLine("没有服务器连接!");
                        }

                    }

                }
            }
            else if (0 == string.Compare(operatcmd, "DownLoad"))
            {
                string st = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));
                Console.WriteLine(st);
                string[] stArray = st.Split('$');
                string filename = stArray[0];
                Int64 filelen = Convert.ToInt64(stArray[1]);
                Console.WriteLine("文件名称:{0}", filename);
                Console.WriteLine("文件大小:{0}", filelen);

                if ((filelen / (1024.0 * 1024.0)) > 700)//文件大于2G开始分块处理
                {
                    //文件分块，直接发送四个IP地址到client端，分块过程由client直接操作
                    //分块文件命名规则统一按照文件名+"_number"，各块文件按照IP发送顺序存储
                    //数据表记录文件存储名称和存储Server IP
                    string[] TempExtra = filename.Split('.');
                    string ipstr = "";
                    int fileorder = 0;
                    for (int j = 1; j <= 3; j++)
                    {
                        for (int i = 1; i <= SavedfileCount; i++)
                        {
                            if (Savedfile[i].Filename == (TempExtra[0] + "_" + j.ToString() + "." + TempExtra[1]))
                            {
                                fileorder = i;
                                break;
                            }

                        }
                        ipstr += "@" + Savedfile[fileorder].SaveIP.IP + "/" + Savedfile[fileorder].SaveIP.Port.ToString();
                        Console.WriteLine("{0}", Savedfile[fileorder].SaveIP.IP);
                    }

                    TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(ipstr));
                    client.Close();
                    Console.WriteLine("分配完成");
                }
                else
                {
                    //文件不分块，直接发送一个IP到client端
                    //数据表记录文件名称和文件存储Server IP
                    string ipstr = "";
                    for (int i = 1; i <= SavedfileCount; i++)
                    {
                        if (Savedfile[i].Filename == filename)
                        {
                            ipstr = "@" + Savedfile[i].SaveIP.IP + "/" + Savedfile[i].SaveIP.Port.ToString();
                            Console.WriteLine("{0}", Savedfile[i].SaveIP.IP);
                            break;
                        }
                    }
                    TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(ipstr));
                    client.Close();
                    Console.WriteLine("分配完成");
                }

            }
            else if (0 == string.Compare(operatcmd, "DataProcess"))
            {
                Console.WriteLine("正在接收数据文件...");
                Thread dp = new Thread(ReceiveFiles);//接收数据文件的同时把去除后缀名的文件名称记录在全局变量中
                dp.Start(client);

                dp.Join();//等待接收数据完成
                string dfn = DataFilename;
                Process CmdProcess = new Process();
                CmdProcess.StartInfo.FileName = "cmd.exe";
                //命令码格式："python D:\MonitorFile\split.py D:\MonitorFile\ dfn 指令码"
                CmdProcess.StartInfo.Arguments = "/c python " + FileSavePath + "mapreduce.py"
                                                + " " + FileSavePath
                                                + " " + dfn
                                                + " " + DataSplitCount.ToString()
                                                + " " + "cut";//“/C”表示执行完命令后马上退出  
                CmdProcess.Start();//执行  
                //CmdProcess.StandardOutput.ReadToEnd();//输出  
                CmdProcess.WaitForExit();//等待程序执行完退出进程  
                CmdProcess.Close();//结束 
                Console.WriteLine("数据文件分割完成，等待分发服务器...");

                //把文件数据分发服务器
                int rass = ServerCount;
                if (rass >= 4)//服务器数量足够
                {
                    int[] randomget;
                    randomget = GetRandomArray(4, 1, rass);
                    //Console.WriteLine("{0},{1},{2},{3}", randomget[0], randomget[1], randomget[2], randomget[3]);

                    TargetServer[randomget[0]].Filename = dfn.Substring(0, dfn.Length - 4) + "_part0.txt";
                    TargetServer[randomget[1]].Filename = dfn.Substring(0, dfn.Length - 4) + "_part1.txt";
                    TargetServer[randomget[2]].Filename = dfn.Substring(0, dfn.Length - 4) + "_part2.txt";
                    TargetServer[randomget[3]].Filename = dfn.Substring(0, dfn.Length - 4) + "_part3.txt";

                    Thread dps1 = new Thread(DataDeliver);
                    dps1.Start(TargetServer[randomget[0]]);

                    Thread dps2 = new Thread(DataDeliver);
                    dps2.Start(TargetServer[randomget[1]]);

                    Thread dps3 = new Thread(DataDeliver);
                    dps3.Start(TargetServer[randomget[2]]);

                    Thread dps4 = new Thread(DataDeliver);
                    dps4.Start(TargetServer[randomget[3]]);

                    dps1.Join();
                    dps2.Join();
                    dps3.Join();
                    dps4.Join();

                    File.Delete(FileSavePath + dfn.Substring(0, dfn.Length - 4) + "_part0.txt");
                    File.Delete(FileSavePath + dfn.Substring(0, dfn.Length - 4) + "_part1.txt");
                    File.Delete(FileSavePath + dfn.Substring(0, dfn.Length - 4) + "_part2.txt");
                    File.Delete(FileSavePath + dfn.Substring(0, dfn.Length - 4) + "_part3.txt");

                    Console.WriteLine("数据文件分发完成！Server正在计算...");
                }
                else
                {
                    Console.WriteLine("服务器数量不够不能完成计算！");
                }
            }
            else if (0 == string.Compare(operatcmd, "ProcessReturn"))
            {
                //接收Server返回的计算结果
                //做数据合并操作
                //数据合并完成后吧数据返回到Client上
                //需要先记录发送运算请求的Client IP和端口；
                //或者做定时等待client自己索取计算结果操作
                Console.WriteLine("正在回收计算结果...");
                Thread pr = new Thread(ReceiveFiles);
                pr.Start(client);
                pr.Join();
                RetdataCount++;
                if (RetdataCount == 8)
                {
                    Console.WriteLine("计算结果回收完成，正在整合...");
                    string dfn = DataFilename;
                    dfn = dfn.Substring(0, (dfn.Length - 2));
                    Process CmdProcess = new Process();
                    CmdProcess.StartInfo.FileName = "cmd.exe";
                    //命令码格式："python D:\MonitorFile\split.py D:\MonitorFile\ 文件名 分割数 指令码"
                    CmdProcess.StartInfo.Arguments = "/c python " + FileSavePath + "mapreduce.py"
                                                    + " " + FileSavePath
                                                    + " " + dfn
                                                    + " " + "1"
                                                    + " " + "reduce";//“/C”表示执行完命令后马上退出  
                    CmdProcess.Start();//执行  
                    //CmdProcess.StandardOutput.ReadToEnd();//输出  
                    CmdProcess.WaitForExit();//等待程序执行完退出进程  
                    CmdProcess.Close();//结束 
                    RetdataCount = 0;//清空标志位
                    for (int i = 1; i <= 2; i++)
                    {
                        for (int j = 0; j <= 3; j++)
                        {
                            File.Delete(FileSavePath + "result" + i.ToString() + "_" + j.ToString());
                        }
                    }
                    //结果回传 result1.txt,result2.txt LocalCall.png RoamingCall.png TollCall.png

                    DataReturn("result1.txt");
                    DataReturn("result2.txt");
                    DataReturn("LocalCall.png");
                    DataReturn("RoamingCall.png");
                    DataReturn("TollCall.png");
                    DataReturn("result3.txt");
                }
            }
            else if (0 == string.Compare(operatcmd, "ServerRegister"))
            {
                //获得客户端节点对象信息  
                ServerCount++;
                TargetServer[ServerCount].IP = clientep.Address.ToString();
                TargetServer[ServerCount].Port = 20000;
                TargetServer[ServerCount].IsLive = 0;
                client.Close();
                Console.WriteLine("Server {0}/{1} 注册完成!", TargetServer[ServerCount].IP, TargetServer[ServerCount].Port);
            }
            else if (0 == string.Compare(operatcmd, "Pulse"))
            {
                string serverip = clientep.Address.ToString();
                for (int i = 1; i <= ServerCount; i++)
                {
                    if (0 == string.Compare(TargetServer[i].IP, serverip))
                    {
                        TargetServer[i].IsLive++;
                    }
                }
                client.Close();//一定要记得关socket服务！！！
            }
            else if (0 == string.Compare(operatcmd, "Backup"))
            {
                string bakfilename = System.Text.Encoding.Unicode.GetString(TransferFiles.ReceiveVarData(client));
                //查找文件表，把对应文件备份IP传到Server
                string ipstr = "";
                for (int i = 1; i <= SavedfileCount; i++)
                {
                    if (Savedfile[i].Filename == bakfilename)
                    {
                        ipstr = "@" + Savedfile[i].BakIP.IP + "/" + (20000).ToString();
                        break;
                    }

                }
                TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(ipstr));
                client.Close();
                Console.WriteLine("备份分配完成！");
            }
            else if (0 == string.Compare(operatcmd, "UpdataFile"))
            {
                //发送文件数目
                int count = 0;

                for (int i = 1; i <= SavedfileCount; i++)
                {
                    if (Savedfile[i].IsUpdata)
                    {
                        count++;
                    }
                }
                TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(count.ToString()));

                for (int i = 1; i <= SavedfileCount; i++)
                {
                    if (Savedfile[i].IsUpdata)
                    {
                        //发送每个文件文件名和文件长度
                        TransferFiles.SendVarData(client, System.Text.Encoding.Unicode.GetBytes(Savedfile[i].Filename +
                            "$" + Savedfile[i].FileLength.ToString()));
                        count++;
                    }
                }

                client.Close();
                Console.WriteLine("文件表更新完成！");
            }
            else
            {
                client.Close();
                Console.WriteLine("没有匹配的操作命令！");
            }
        }

        public static void DataReturn(string fn)
        {
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("192.168.3.32"), 40000); //指向远程服务端节点   
            Socket cSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                cSocket.Connect(ipep);  //连接到Monitor
            }
            catch
            {
                Console.WriteLine("连接服务器出错！");
                cSocket.Close();
                return;
            }
            string fullPath = FileSavePath + fn;

            FileInfo EzoneFile = new FileInfo(fullPath);  //创建一个文件对象
            FileStream EzoneStream = EzoneFile.OpenRead();  //打开文件流   

            int PacketSize = 1024 * 1024;  //包的大小1M  
            int PacketCount = (int)(EzoneStream.Length / ((long)PacketSize));//包的数量    
            //最后一个包的大小  
            int LastDataPacket = (int)(EzoneStream.Length - ((long)(PacketSize * PacketCount)));
            //发送[文件名]到服务器端  
            TransferFiles.SendVarData(cSocket, System.Text.Encoding.Unicode.GetBytes(EzoneFile.Name));
            //发送[包的大小]到服务器端  
            TransferFiles.SendVarData(cSocket, System.Text.Encoding.Unicode.GetBytes(PacketSize.ToString()));
            //发送[包的总数量]到服务器端  
            TransferFiles.SendVarData(cSocket, System.Text.Encoding.Unicode.GetBytes(PacketCount.ToString()));
            //发送[最后一个包的大小]到服务器端  
            TransferFiles.SendVarData(cSocket, System.Text.Encoding.Unicode.GetBytes(LastDataPacket.ToString()));

            byte[] data = new byte[PacketSize];  //数据包  

            for (int i = 0; i < PacketCount; i++)   //开始循环发送数据包  
            {
                //从文件流读取数据并填充数据包  
                EzoneStream.Read(data, 0, data.Length);
                //发送数据包  
                if (TransferFiles.SendVarData(cSocket, data) == 3)
                {
                    Console.WriteLine("文件传输出错！");
                }
            }

            if (LastDataPacket != 0)  //如果还有多余的数据包，则应该发送完毕！  
            {
                data = new byte[LastDataPacket];
                EzoneStream.Read(data, 0, data.Length);
                TransferFiles.SendVarData(cSocket, data);
            }

            cSocket.Close();//关闭套接字    
            EzoneStream.Close(); //关闭文件流  
            Console.WriteLine("{0}发送成功！", fn);

        }

        public static void ReceiveFiles(object Cnt)
        {
            Socket client = Cnt as Socket;
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
            Console.WriteLine("数据接收完成!");
            DataFilename = ReceiveFileName;
        }

        public static void DataDeliver(object Cnt)
        {
            ConnectInfo ffname = (ConnectInfo)Cnt;

            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(ffname.IP), ffname.Port); //指向远程服务端节点   
            Socket dpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                dpSocket.Connect(ipep);  //连接到Monitor
            }
            catch
            {
                Console.WriteLine("连接服务器出错！");
                dpSocket.Close();
                return;
            }
            string fullPath = FileSavePath + ffname.Filename;

            FileInfo EzoneFile = new FileInfo(fullPath);  //创建一个文件对象
            FileStream EzoneStream = EzoneFile.OpenRead();  //打开文件流   

            int PacketSize = 1024 * 1024;  //包的大小1M  
            int PacketCount = (int)(EzoneStream.Length / ((long)PacketSize));//包的数量    
            //最后一个包的大小  
            int LastDataPacket = (int)(EzoneStream.Length - ((long)(PacketSize * PacketCount)));
            //先发送操作命令
            TransferFiles.SendVarData(dpSocket, System.Text.Encoding.Unicode.GetBytes("DataProcess"));
            //发送[文件名]到服务器端  
            TransferFiles.SendVarData(dpSocket, System.Text.Encoding.Unicode.GetBytes(EzoneFile.Name));
            //发送[包的大小]到服务器端  
            TransferFiles.SendVarData(dpSocket, System.Text.Encoding.Unicode.GetBytes(PacketSize.ToString()));
            //发送[包的总数量]到服务器端  
            TransferFiles.SendVarData(dpSocket, System.Text.Encoding.Unicode.GetBytes(PacketCount.ToString()));
            //发送[最后一个包的大小]到服务器端  
            TransferFiles.SendVarData(dpSocket, System.Text.Encoding.Unicode.GetBytes(LastDataPacket.ToString()));

            //bool IsTransferOk = false;
            byte[] data = new byte[PacketSize];  //数据包  

            for (int i = 0; i < PacketCount; i++)   //开始循环发送数据包  
            {
                //从文件流读取数据并填充数据包  
                EzoneStream.Read(data, 0, data.Length);
                //发送数据包  
                if (TransferFiles.SendVarData(dpSocket, data) == 3)
                {
                    Console.WriteLine("文件传输出错！");
                }
            }

            if (LastDataPacket != 0)  //如果还有多余的数据包，则应该发送完毕！  
            {
                data = new byte[LastDataPacket];
                EzoneStream.Read(data, 0, data.Length);
                TransferFiles.SendVarData(dpSocket, data);
            }

            dpSocket.Close();//关闭套接字    
            EzoneStream.Close(); //关闭文件流  
        }

        public static void Timer_Init()
        {
            System.Timers.Timer heartbeat = new System.Timers.Timer(5 * 1000);//实例化Timer类，设置间隔时间为 5s；
            heartbeat.Elapsed += new System.Timers.ElapsedEventHandler(timeout); //到达时间的时候执行事件；
            heartbeat.AutoReset = true;//设置是执行一次（false）还是一直执行(true)；
            heartbeat.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；
        }

        public static void timeout(object source, System.Timers.ElapsedEventArgs e)
        {
            //将节点活跃度--
            //接收到心跳信息对节点活跃度++
            for (int j = 1; j <= ServerCount; j++)
            {
                if (TargetServer[j].IsLive < (-1))
                {
                    Console.WriteLine("Server{0} is down!", TargetServer[j].IP);
                    ConnectInfo sdpip;

                    sdpip.IP = TargetServer[j].IP;
                    sdpip.Port = TargetServer[j].Port;
                    sdpip.IsLive = j;
                    sdpip.Filename = "";

                    TargetServer[j].IP = TargetServer[ServerCount].IP;
                    TargetServer[j].Port = TargetServer[ServerCount].Port;
                    TargetServer[j].IsLive = TargetServer[ServerCount].IsLive;
                    ServerCount--;
                    j--;

                    Thread SdProcess = new Thread(ServerdownProcess);
                    SdProcess.Start(sdpip);
                }
                else
                {
                    TargetServer[j].IsLive--;
                    //Console.WriteLine("Server {0}/{1} is alive!", TargetServer[j].IP, 20000);
                }
            }
        }

        public static void ServerdownProcess(object Cnt)
        {
            ConnectInfo downIP = (ConnectInfo)Cnt;
            for (int i = 1; i <= SavedfileCount; i++)
            {
                if (Savedfile[i].SaveIP.IP == downIP.IP)
                {
                    Savedfile[i].SaveIP.IP = Savedfile[i].BakIP.IP;
                    //Savedfile[i].BakIP.IP = TargetServer[downIP.IsLive].IP;
                }
            }
        }

        //获取随机数返回一个数组 
        public static int[] GetRandomArray(int Number, int minNum, int maxNum)
        {
            int j;
            int[] b = new int[Number];
            Random r = new Random();
            for (j = 0; j < Number; j++)
            {
                int i = r.Next(minNum, maxNum + 1);
                int num = 0;
                for (int k = 0; k < j; k++)
                {
                    if (b[k] == i)
                    {
                        num = num + 1;
                    }
                }
                if (num == 0)
                {
                    b[j] = i;
                }
                else
                {
                    j = j - 1;
                }
            }
            return b;
        }


    }
}
