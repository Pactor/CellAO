﻿#region License
// Copyright (c) 2005-2012, CellAO Team
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

namespace ChatEngine.PacketHandlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;

    using AO.Core;
    using AO.Core.Config;

    using ChatEngine.Lists;
    using ChatEngine.Packets;

    /// <summary>
    /// The authenticate.
    /// </summary>
    internal static class Authenticate
    {
        /// <summary>
        /// The read.
        /// </summary>
        /// <param name="client">
        /// </param>
        /// <param name="packet">
        /// </param>
        public static void Read(Client client, ref byte[] packet)
        {
            MemoryStream m_stream = new MemoryStream(packet);
            BinaryReader m_reader = new BinaryReader(m_stream);

            // now we should do password check and then send OK or Error
            // sending OK now
            m_stream.Position = 12;

            short userNameLength = IPAddress.NetworkToHostOrder(m_reader.ReadInt16());
            string userName = Encoding.ASCII.GetString(m_reader.ReadBytes(userNameLength));
            short loginKeyLength = IPAddress.NetworkToHostOrder(m_reader.ReadInt16());
            string loginKey = Encoding.ASCII.GetString(m_reader.ReadBytes(loginKeyLength));

            uint characterId = BitConverter.ToUInt32(new[] { packet[11], packet[10], packet[9], packet[8] }, 0);

            LoginEncryption loginEncryption = new LoginEncryption();

            if (loginEncryption.IsValidLogin(loginKey, client.ServerSalt, userName)
                && loginEncryption.IsCharacterOnAccount(userName, characterId))
            {
                byte[] loginok = LoginOk.Create();
                client.Send(loginok);
            }
            else
            {
                byte[] loginerr = LoginError.Create();
                client.Send(loginerr);
                client.Server.DisconnectClient(client);
                byte[] invalid = BitConverter.GetBytes(characterId);

                ZoneCom.Client.SendMessage(99, invalid);
                return;
            }

            // server welcome message
            string motd = ConfigReadWrite.Instance.CurrentConfig.Motd;

            // save characters ID in client - note, this is usually 0 if it is a chat client connecting
            client.Character = new Character(characterId, client);

            // add client to connected clients list
            if (!client.Server.ConnectedClients.ContainsKey(client.Character.characterId))
            {
                client.Server.ConnectedClients.Add(client.Character.characterId, client);
            }

            // add yourself to that list
            client.KnownClients.Add(client.Character.characterId);

            // and give client its own name lookup
            byte[] pname = PlayerName.New(client, client.Character.characterId);
            client.Send(pname);

            // send server welcome message to client
            byte[] anonv = MsgAnonymousVicinity.Create(
                string.Empty,
                string.Format(motd, AssemblyInfoclass.Description + " " + AssemblyInfoclass.AssemblyVersion),
                string.Empty);
            client.Send(anonv);

            // tell client to join channel "Global"
            // hardcoded right now
            foreach (ChannelsEntry channel in ChatChannels.ChannelNames)
            {
                byte[] chanGlobal = ChannelJoin.Create(
                    channel.Id, channel.Name, channel.ChannelMode, new byte[] { 0x00, 0x00 });
                client.Send(chanGlobal);
            }

            // First Attempt at Guild Channel....
            // This code is completly untested however if it works
            // we will have to add some what for you to join GuildChat on creation of guild
            // and when you join a guild...  this just connects you to it if you already exist in a guild
            // at character login.. enjoy hope it works.. I cant seem to test it my computer wont let me install the sql tables atm..

            if (client.Character.orgId == 0)
            {
            }
            else
            {
                ulong channelBuffer = (ulong)ChannelType.Organization << 32;
                channelBuffer |= (uint)client.Character.orgId;

                byte[] guildChannel = ChannelJoin.Create(channelBuffer, client.Character.orgName, 0x8044, new byte[] { 0x00, 0x00 });
                client.Send(guildChannel);
            }


            // Do Not Delete this just yet!
            // byte[] chn_global = new Packets.ChannelJoin().Create
            // (
            // new byte[] { 0x04, 0x00, 0x00, 0x23, 0x28 },
            // "Global",
            // 0x8044,
            // new byte[] { 0x00, 0x00 }
            // );
            // client.Send(chn_global);
        }
    }
}