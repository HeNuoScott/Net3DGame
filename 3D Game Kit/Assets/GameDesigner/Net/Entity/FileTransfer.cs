namespace Net.Entity
{
    using Net.Client;
    using Net.Server;
    using Net.Share;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    //文件传输组件使用方法: 可直接拷贝到控制台项目进行测试, 注意: 你的C盘必须要有这个文件 @"C:/test.mp4"
    /*
        Net.Server.UdpServer server = new Net.Server.UdpServer();//创建服务器对象
        server.Log += (log) => { Console.WriteLine(log); };
        server.Start();//启动服务器
        server.OnRevdBufferHandle += (a, b, c, d, e) => {//监听客户端发送的数据请求
            var func = Net.Share.NetConvert.Deserialize(c, d, e);//解析客户端数据
            if (func.func == "Download")//如果是下载rpc函数
            {
                server.Send(a, "Start");//告诉客户端可以上传文件流了
                FileTransfer file = new FileTransfer(server);//创建下载文件实例
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                file.ReadFile(buffer => {//监听下载完成后的所有字节
                    File.WriteAllBytes(@"C:/demo.mp4", buffer);//保存文件到磁盘
                    stopwatch.Stop();
                    Console.WriteLine("上传完成,用时:" + stopwatch.Elapsed.ToString() + " 文件大小:" + buffer.Length + "/byte");
                });
            }
        };

        Thread.Sleep(1000);

        Net.Client.UdpClient client = new Net.Client.UdpClient();//创建客户端对象
        client.Log += (log) => { Console.WriteLine(log); };
        client.Connect(result=> {//连接服务器
            if (result) {//当连接成功
                client.Send("进站");//发送一次请求进入服务器
                Thread.Sleep(50);
                client.Send("Download");//发送下载请求,让服务器创建下载组件
                client.OnRevdBufferHandle += (a, b, c, d) => {//监听服务器发送的数据请求
                    var func = Net.Share.NetConvert.Deserialize(b, c, d);//解析服务器数据
                    if (func.func == "Start")//如果是开始上传文件rpc函数
                    {
                        FileTransfer file = new FileTransfer(client);//创建文件上传组件
                        file.WriteFile(@"C:/test.mp4");//发送文件到服务器
                        file.progress += (value) => {
                            Console.WriteLine("上传进度:" + value);
                        };
                    }
                };
            }
        });
    */

    /// <summary>
    /// 网络文件传输实体组建
    /// </summary>
    public sealed class FileTransfer : IDisposable
    {
        NetClientBase client;
        NetServerBase server;
        Dictionary<int, byte[]> datas;
        int fileCount;
        MemoryStream stream = new MemoryStream();
        int index, abnormal;
        event Action<byte[]> Filedata;
        /// <summary>
        /// 上传或下载状态, 结果为真时下载成功，结果为假时下载失败
        /// </summary>
        public Action<bool> state;
        /// <summary>
        /// 进度
        /// </summary>
        public Action<float> progress;
        bool complete;

        /// <summary>
        /// 创建客户端发送文件
        /// </summary>
        /// <param name="client"></param>
        public FileTransfer(NetClientBase client)
        {
            this.client = client;
            client.AddRpcHandle(this);
        }
        
        /// <summary>
        /// 创建服务器文件接收
        /// </summary>
        /// <param name="server"></param>
        public FileTransfer(NetServerBase server)
        {
            this.server = server;
            Server.NetBehaviour.AddRpcs(server, this);
        }

        /// <summary>
        /// 析构
        /// </summary>
        ~FileTransfer()
        {
            if (server != null)
                Server.NetBehaviour.RemoveRpc(server, this);
            if (client != null)
                Client.NetBehaviour.RemoveRpc(client, this);
            stream?.Dispose();
        }

        /// <summary>
        /// 写文件-  将文件从本地发送给服务器
        /// </summary>
        public void WriteFile(string file)
        {
            byte[] filedata = File.ReadAllBytes(file);
            int index = 0;
            int key = 0;
            datas = new Dictionary<int, byte[]>();
            while (index < filedata.Length)
            {
                int count = 15000;
                if (index + count >= filedata.Length)
                    count = filedata.Length - index;
                using (MemoryStream stream = new MemoryStream(filedata, index, count))
                {
                    byte[] buf1 = stream.ToArray();
                    datas.Add(key, buf1);
                    key++;
                }
                index += 15000;
            }
            fileCount = filedata.Length;
            this.index = 0;
            abnormal = 0;
            index = 0;
            Task.Run(()=> 
            {
                while (this!=null & !complete)
                {
                    if (this.index == index)
                    {
                        client.Send("ReadFile", this.index, fileCount, datas[this.index]);
                        abnormal++;
                    }
                    if (abnormal > 5)
                    {
                        state?.Invoke(false);
                        break;
                    }
                    index = this.index;
                    Thread.Sleep(1000);
                }
            });
        }

        /// <summary>
        /// 写文件-  将文件从服务器发送给客户端
        /// </summary>
        /// <param name="client"></param>
        /// <param name="file"></param>
        public void WriteFile(NetPlayer client, string file)
        {
            byte[] filedata = File.ReadAllBytes(file);
            int index = 0;
            int key = 0;
            datas = new Dictionary<int, byte[]>();
            while (index < filedata.Length)
            {
                int count = 15000;
                if (index + count >= filedata.Length)
                    count = filedata.Length - index;
                using (MemoryStream stream = new MemoryStream(filedata, index, count))
                {
                    byte[] buf1 = stream.ToArray();
                    datas.Add(key, buf1);
                    key++;
                }
                index += 15000;
            }
            fileCount = filedata.Length;
            this.index = 0;
            abnormal = 0;
            index = 0;
            Task.Run(() =>
            {
                while (this != null & !complete)
                {
                    if (this.index == index)
                    {
                        server.Send(client, "ClientReadFile", this.index, fileCount, datas[this.index]);
                        abnormal++;
                    }
                    if (abnormal > 5)
                    {
                        state?.Invoke(false);
                        break;
                    }
                    index = this.index;
                    Thread.Sleep(1000);
                }
            });
        }

        //服务器读文件Rpc
        [Rpc(NetCmd.SafeCall)]
        void ReadFile(NetPlayer player, int index, int fileCount, byte[] filedata)
        {
            if (this.index == index)
            {
                stream.Write(filedata, 0, filedata.Length);
                if (stream.Length >= fileCount)
                {
                    server.Send(player, "DownloadIndex", this.index, true);
                    byte[] buff = stream.ToArray();
                    this.Filedata?.Invoke(buff);
                    Dispose();
                }
                else
                {
                    this.index++;
                    server.Send(player, "DownloadIndex", this.index, false);
                }
            }
        }

        //客户端读文件Rpc
        [Rpc]
        void ClientReadFile(int index, int fileCount, byte[] filedata)
        {
            if (this.index == index)
            {
                stream.Write(filedata, 0, filedata.Length);
                if (stream.Length >= fileCount)
                {
                    client.Send("ClientDownloadIndex", this.index, true);
                    byte[] buff = stream.ToArray();
                    Filedata?.Invoke(buff);
                    Dispose();
                }
                else
                {
                    this.index++;
                    client.Send("ClientDownloadIndex", this.index, false);
                }
            }
        }

        //客户端上传文件进度
        [Rpc]
        void DownloadIndex(int index, bool done)
        {
            if (done)
            {
                complete = true;
                state?.Invoke(true);
                Dispose();
                return;
            }
            this.index = index;
            abnormal = 0;
            var value = (float)index / datas.Count * 100;
            progress?.Invoke(value);
            client.Send("ReadFile", index, fileCount, datas[index]);
        }

        //服务器传文件给客户端的进度
        [Rpc(NetCmd.SafeCall)]
        void ClientDownloadIndex(NetPlayer client, int index, bool done)
        {
            if (done)
            {
                complete = true;
                state?.Invoke(true);
                Dispose();
                return;
            }
            this.index = index;
            abnormal = 0;
            var value = (float)index / datas.Count * 100;
            progress?.Invoke(value);
            server.Send(client, "ClientReadFile", index, fileCount, datas[index]);
        }

        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="filedata"></param>
        public void ReadFile(Action<byte[]> filedata)
        {
            Filedata = filedata;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            if (server != null)
                Server.NetBehaviour.RemoveRpc(server, this);
            if (client != null)
                Client.NetBehaviour.RemoveRpc(client, this);
            stream?.Dispose();
        }
    }
}
