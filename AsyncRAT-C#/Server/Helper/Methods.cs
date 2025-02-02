﻿using Microsoft.VisualBasic;
using Server.Algorithm;
using Server.Handle_Packet;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server.Helper
{
    public static class Methods
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        public static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        public static async Task FadeIn(Form o, int interval = 80)
        {
            while (o.Opacity < 1.0)
            {
                await Task.Delay(interval);
                o.Opacity += 0.05;
            }
        }

        public static Random Random = new Random();
        public static string GetRandomString(int length)
        {
            StringBuilder randomName = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                randomName.Append(Alphabet[Random.Next(Alphabet.Length)]);

            return randomName.ToString();
        }

        public static void SetPlugins()
        {
            try
            {
                foreach (string plugin in Directory.GetFiles("Plugins", "*.dll", SearchOption.TopDirectoryOnly))
                {
                    Settings.Plugins.Add(GetHash.GetChecksum(plugin), Strings.StrReverse(Convert.ToBase64String(Zip.Compress(File.ReadAllBytes(plugin)))));
#if DEBUG
                    byte[] plg = Zip.Compress(File.ReadAllBytes(plugin));
                    Debug.WriteLine($"{plugin} : {BytesToString(plg.Length)}");
#endif
                }
            }
            catch (Exception ex)
            {
                new HandleLogs().Addmsg(ex.Message, Color.Red);
            }
        }
    }
}
