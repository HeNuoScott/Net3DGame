using Net.Client;
using Net.Server;
using Net.Share;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Net.Entity
{
    /// <summary>
    /// 网络文件传输实体组建
    /// </summary>
    public class FileTransfer : IDisposable
    {
        NetClientBase client;
        NetServerBase server;
        Dictionary<int, byte[]> datas;
        int fileCount;
        MemoryStream stream = new MemoryStream();
        int index;
        event Action<byte[]> filedata;

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
            Server.NetBehaviour.AddRPCFuns(server, this);
        }

        /// <summary>
        /// 析构
        /// </summary>
        ~FileTransfer()
        {
            if(server!=null)
                Server.NetBehaviour.RemoveRpc(server, this);
            if (client != null)
                Client.NetBehaviour.RemoveRpcDelegate(client, this);
            stream?.Dispose();
        }

        /// <summary>
        /// 写文件-  将文件从本地发送给服务器
        /// </summary>
        /// <param name="file"></param>
        public void WriteFile(string file)
        {
            byte[] filedata = File.ReadAllBytes(file);
            int index = 0;
            int key = 0;
            datas = new Dictionary<int, byte[]>();
            while (index < filedata.Length)
            {
                int count = 5000;
                if (index + count >= filedata.Length)
                    count = filedata.Length - index;
                using (MemoryStream stream = new MemoryStream(filedata, index, count))
                {
                    byte[] buf1 = stream.ToArray();
                    datas.Add(key, buf1);
                    key++;
                }
                index += 5000;
            }
            fileCount = filedata.Length;
            this.index = 0;
            client.Send("ReadFile", this.index, fileCount, datas[this.index]);
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
                int count = 5000;
                if (index + count >= filedata.Length)
                    count = filedata.Length - index;
                using (MemoryStream stream = new MemoryStream(filedata, index, count))
                {
                    byte[] buf1 = stream.ToArray();
                    datas.Add(key, buf1);
                    key++;
                }
                index += 5000;
            }
            fileCount = filedata.Length;
            this.index = 0;
            server.Send(client, "ClientReadFile", this.index, fileCount, datas[this.index]);
        }

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
                    this.filedata?.Invoke(buff);
                }
                else
                {
                    this.index++;
                    server.Send(player, "DownloadIndex", this.index, false);
                }
            }
        }

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
                    this.filedata?.Invoke(buff);
                }
                else
                {
                    this.index++;
                    client.Send("ClientDownloadIndex", this.index, false);
                }
            }
        }

        [Rpc]
        void DownloadIndex(int index, bool done)
        {
            if (done)
                return;
            client.Send("ReadFile", index, fileCount, datas[index]);
        }

        [Rpc(NetCmd.SafeCall)]
        void ClientDownloadIndex(NetPlayer client, int index, bool done)
        {
            if (done)
                return;
            server.Send(client, "ClientReadFile", index, fileCount, datas[index]);
        }

        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="filedata"></param>
        public void ReadFile(Action<byte[]> filedata)
        {
            this.filedata = filedata;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            if (server != null)
                Server.NetBehaviour.RemoveRpc(server, this);
            if (client != null)
                Client.NetBehaviour.RemoveRpcDelegate(client, this);
            stream?.Dispose();
        }
    }
}
