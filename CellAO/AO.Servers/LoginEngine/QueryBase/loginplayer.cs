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
using System.Data;
using System.Text;
using AO.Core;
#endregion

namespace LoginEngine.QueryBase
{
    /// <summary>
    /// 
    /// </summary>
    public class LoginPlayer
    {
        private int cbreedint, cprofint, playfield;
        public byte[] name, breed, prof, zone;
        private readonly SqlWrapper ms = new SqlWrapper();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RecvLogin"></param>
        public void GetCharacterName(string RecvLogin)
        {
            string SqlQuery = "SELECT `Name`, `Breed`, `Profession` FROM `characters` WHERE Username = " + "'" +
                              RecvLogin + "'";
            DataTable dt = ms.ReadDT(SqlQuery);


            foreach (DataRow datarow1 in dt.Rows)
            {
                name = Encoding.ASCII.GetBytes(datarow1["Name"].ToString().PadRight(11, '\u0000'));
                cbreedint = int.Parse(datarow1["Breed"].ToString());
                breed = BitConverter.GetBytes(cbreedint);
                cprofint = int.Parse(datarow1["Profession"].ToString());
                prof = BitConverter.GetBytes(cprofint);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RecvLogin"></param>
        public void GetCharacterZone(string RecvLogin)
        {
            string SqlQuery = "SELECT `playfield` FROM `characters` WHERE Username = " + "'" + RecvLogin + "'";
            DataTable dt = ms.ReadDT(SqlQuery);

            foreach (DataRow datarow2 in dt.Rows)
            {
                playfield = (Int32) datarow2["playfield"];
                zone = BitConverter.GetBytes(playfield);
            }
        }
    }
}