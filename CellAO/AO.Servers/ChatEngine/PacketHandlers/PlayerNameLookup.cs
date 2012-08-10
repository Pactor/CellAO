﻿#region License
// Copyright (c) 2005-2012, CellAO Team
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
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
    using System.Data;

    using AO.Core;

    using ChatEngine.Packets;

    using PacketReader = ChatEngine.PacketReader;

    /// <summary>
    /// The player name lookup.
    /// </summary>
    public class PlayerNameLookup
    {
        /// <summary>
        /// The player name.
        /// </summary>
        private string playerName = string.Empty;

        /// <summary>
        /// The player ID.
        /// </summary>
        private uint playerId = uint.MaxValue;

        /// <summary>
        /// Read and send back Player name lookup packet
        /// </summary>
        /// <param name="client">
        /// Client sending
        /// </param>
        /// <param name="packet">
        /// Packet data
        /// </param>
        public void Read(Client client, byte[] packet)
        {
            PacketReader reader = new PacketReader(ref packet);

            reader.ReadUInt16(); // packet ID
            reader.ReadUInt16(); // data length
            this.playerName = reader.ReadString();
            client.Server.Debug(
                client, "{0} >> PlayerNameLookup: PlayerName: {1}", client.Character.characterName, this.playerName);
            reader.Finish();

            SqlWrapper ms = new SqlWrapper();
            string sqlQuery = "SELECT `ID` FROM `characters` WHERE `Name` = " + "'" + this.playerName + "'";
            DataTable dt = ms.ReadDT(sqlQuery);
            if (dt.Rows.Count > 0)
            {
                // Yes, this double cast is correct
                this.playerId = (uint)(int)dt.Rows[0][0];
            }

            byte[] namelookup = new NameLookupResult().Create(this.playerId, this.playerName);
            client.Send(ref namelookup);
            client.KnownClients.Add(this.playerId);
        }
    }
}