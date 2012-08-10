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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AO.Core;
#endregion

namespace ZoneEngine
{
    // Addition can be performed on DropHashes as follows:

    // DropHashes:  "NANITM+HTHITM, INSTDC+NANORK, SHOPFD"
    // DropSlots:   "            1,             2,      2"
    // DropRates:   "         5000,          3000,   7000"

    // This mob would drop a maximum of two items with a 50%
    // chance of getting either a nano or health item, a 100%
    // chance of getting shopfood or an instruction disk or 
    // rk nano. The instruction disk and rk nano would share
    // the same loot roll (NOT only slot). Any number of drop
    // hashes can be added in this way to share rolls. Any
    // number of slots can also be added.

    public static class LootHandler
    {
        public class LootItem
        {
            public string Hash;
            public int LowID;
            public int HighID;
            public int MinQL;
            public int MaxQL;
            public bool RangeCheck;

            public LootItem(string hash, string lowid, string highid, string minql, string maxql, string rangecheck)
            {
                Hash = hash;
                LowID = Convert.ToInt32(lowid);
                HighID = Convert.ToInt32(highid);
                MinQL = Convert.ToInt32(minql);
                MaxQL = Convert.ToInt32(maxql);
                RangeCheck = Convert.ToBoolean(Convert.ToByte(rangecheck));
            }
        }

        public class PartialSlot
        {
            public List<string> HashList;
            public int Slot;
            public int Chance;

            public PartialSlot(string hash, string slot, string chance)
            {
                string[] hasharray = hash.Split('+');
                foreach (string h in hasharray)
                {
                    HashList.Add(h.Trim());
                }
                Slot = Convert.ToInt32(slot);
                Chance = Convert.ToInt32(chance);
            }
        }

        public static List<LootItem> FullDropList = new List<LootItem>();

        public static void CacheAllFromDB()
        {
            SqlWrapper wrapper = new SqlWrapper();
            DataTable dt = wrapper.ReadDT("SELECT * FROM mobdroptable");

            DataRowCollection drc = dt.Rows;
            foreach (DataRow row in drc)
            {
                FullDropList.Add(new LootItem(row[0].ToString(), row[1].ToString(), row[2].ToString(), row[3].ToString(),
                                              row[4].ToString(), row[5].ToString()));
            }
        }

        public static List<AOItem> GetLoot(NonPC npc)
        {
            List<AOItem> drops = new List<AOItem>();

            int minql = (int) Math.Ceiling(npc.Stats.Level.Value - 0.2*npc.Stats.Level.Value);
            int maxql = (int) Math.Floor(npc.Stats.Level.Value + 0.2*npc.Stats.Level.Value);

            var lootinfo =
                new SqlWrapper().ReadDT("SELECT drophashes, dropslots, droppercents FROM mobtemplate WHERE hash = " +
                                        npc.Hash + ";").Rows;

            int numberofslots = 0;

            string[] hashes = lootinfo[0][0].ToString().ToLower().Split(',');
            string[] slots = lootinfo[0][1].ToString().ToLower().Split(',');
            string[] percents = lootinfo[0][2].ToString().ToLower().Split(',');

            if (hashes.Count() == 0)
                return null;
            if (hashes[0] == string.Empty)
                return null;

            foreach (string s in slots)
            {
                numberofslots = Math.Max(numberofslots, Convert.ToInt32(s.Trim()));
            }

            List<PartialSlot> list = new List<PartialSlot>();

            for (int i = 0; i < hashes.Count(); ++i)
            {
                list.Add(new PartialSlot(hashes[i].Trim(), slots[i].Trim(), percents[i].Trim()));
            }

            for (int i = 1; i <= numberofslots; ++i)
            {
                var fullSlot = list.Where(match => match.Slot == i).Select(match => match);

                Random rand = new Random();
                double num = rand.NextDouble();

                double chance = 0;
                foreach (PartialSlot slot in fullSlot)
                {
                    chance = chance + (slot.Chance/(double) 10000);
                    if (num <= chance)
                    {
                        List<LootItem> union = new List<LootItem>();

                        foreach (string hash in slot.HashList)
                        {
                            var matches =
                                FullDropList.Where(
                                    match =>
                                    ((match.MinQL <= minql && match.MaxQL >= maxql) ||
                                     (match.MinQL < maxql && match.MaxQL >= maxql) ||
                                     (match.MinQL <= minql && match.MaxQL > minql) ||
                                     (match.MinQL >= minql && match.MaxQL <= maxql) || !match.RangeCheck) &&
                                    match.Hash == hash).Select(match => match);
                            foreach (LootItem li in matches)
                            {
                                if (
                                    !union.Exists(
                                        duplicate => duplicate.HighID == li.HighID && duplicate.MaxQL == li.MaxQL))
                                {
                                    union.Add(li);
                                }
                            }
                        }

                        int ql = rand.Next(minql - 1, maxql + 1);

                        if (union.Count() > 0)
                        {
                            int select = rand.Next(-1, union.Count());

                            AOItem item = ItemHandler.interpolate(union.ElementAt(@select).LowID,
                                                                  union.ElementAt(@select).HighID, ql);

                            if (item.ItemType != 1)
                            {
                                item.multiplecount = Math.Max(1, item.getItemAttribute(212));
                            }
                            else
                            {
                                bool found = false;
                                foreach (AOItemAttribute a in item.Stats)
                                {
                                    if (a.Stat != 212)
                                        continue;
                                    found = true;
                                    a.Value = Math.Max(1, item.getItemAttribute(212));
                                    break;
                                }
                                if (!found)
                                {
                                    AOItemAttribute aoi = new AOItemAttribute();
                                    aoi.Stat = 212;
                                    aoi.Value = Math.Max(1, item.getItemAttribute(212));
                                    item.Stats.Add(aoi);
                                }
                            }
                            drops.Add(item);
                            break;
                        }
                    }
                }
            }
            return drops;
        }
    }
}