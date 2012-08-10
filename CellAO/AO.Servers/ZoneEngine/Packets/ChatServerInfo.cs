﻿#region License
/*
Copyright (c) 2005-2012, CellAO Team

All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion

#region Usings...
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AO.Core;
using AO.Core.Config;
#endregion

namespace ZoneEngine.Packets
{
    /// <summary>
    /// Chat server info packet writer
    /// </summary>
    public static class ChatServerInfo
    {
        /// <summary>
        /// Sends chat server info to client
        /// </summary>
        /// <param name="client">Client that gets the info</param>
        public static void Send(Client client)
        {
            /* get chat settings from config */
            string ChatServerIP = string.Empty;
            IPAddress tempIP;
            if (IPAddress.TryParse(ConfigReadWrite.Instance.CurrentConfig.ChatIP, out tempIP))
            {
                ChatServerIP = ConfigReadWrite.Instance.CurrentConfig.ChatIP;
            }
            else
            {
                IPHostEntry chatHost = Dns.GetHostEntry(ConfigReadWrite.Instance.CurrentConfig.ChatIP);
                foreach (IPAddress ip in chatHost.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ChatServerIP = ip.ToString();
                        break;
                    }
                }
            }
            int ChatPort = Convert.ToInt32(ConfigReadWrite.Instance.CurrentConfig.ChatPort);

            PacketWriter writer = new PacketWriter();

            writer.PushBytes(new byte[] {0xDF, 0xDF});
            writer.PushShort(1);
            writer.PushShort(1);
            writer.PushShort(0);
            writer.PushInt(3086);
            writer.PushInt(client.Character.ID);
            writer.PushInt(67);
            writer.PushInt(1);
            writer.PushInt(ChatServerIP.Length);
            writer.PushBytes(Encoding.ASCII.GetBytes(ChatServerIP));
            writer.PushInt(ChatPort);
            writer.PushInt(0);
            byte[] reply = writer.Finish();
            client.SendCompressed(reply);
        }
    }
}