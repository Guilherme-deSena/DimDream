﻿using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DimDream.Common.Systems
{
	// Acts as a container for "downed boss" flags.
	// Set a flag like this in your bosses OnKill hook:
	//    NPC.SetEventFlagCleared(ref DownedBossSystem.downedMinionBoss, -1);

	// Saving and loading these flags requires TagCompounds, a guide exists on the wiki: https://github.com/tModLoader/tModLoader/wiki/Saving-and-loading-using-TagCompound
	public class DownedBossSystem : ModSystem
	{
		public static bool downedChiyuriBoss = false;
        public static bool downedYumemiBoss = false;
        public static bool downedOrinBossCat = false;
        public static bool downedOrinBossHumanoid = false;

        public override void ClearWorld() {
			downedChiyuriBoss = false;
			downedYumemiBoss = false;
            downedOrinBossCat = false;
			downedOrinBossHumanoid = false;
    }

		// We save our data sets using TagCompounds.
		// NOTE: The tag instance provided here is always empty by default.
		public override void SaveWorldData(TagCompound tag) {
			if (downedChiyuriBoss) {
				tag["downedChiyuriBoss"] = true;
            }
            if (downedYumemiBoss)
            {
                tag["downedYumemiBoss"] = true;
            }
            if (downedOrinBossCat)
            {
                tag["downedOrinBossCat"] = true;
            }
            if (downedOrinBossHumanoid)
            {
                tag["downedOrinBossHumanoid"] = true;
            }
        }

		public override void LoadWorldData(TagCompound tag) {
			downedChiyuriBoss = tag.ContainsKey("downedChiyuriBoss");
            downedYumemiBoss = tag.ContainsKey("downedYumemiBoss");
            downedOrinBossCat = tag.ContainsKey("downedOrinBossCat");
            downedOrinBossHumanoid = tag.ContainsKey("downedOrinBossHumanoid");
        }

		public override void NetSend(BinaryWriter writer) {
			// Order of operations is important and has to match that of NetReceive
			var flags = new BitsByte();
			flags[0] = downedChiyuriBoss;
            flags[1] = downedChiyuriBoss;
            flags[2] = downedOrinBossCat;
            flags[3] = downedOrinBossHumanoid;
            writer.Write(flags);

			/*
			Remember that Bytes/BitsByte only have up to 8 entries. If you have more than 8 flags you want to sync, use multiple BitsByte:
				This is wrong:
			flags[8] = downed9thBoss; // an index of 8 is nonsense.
				This is correct:
			flags[7] = downed8thBoss;
			writer.Write(flags);
			BitsByte flags2 = new BitsByte(); // create another BitsByte
			flags2[0] = downed9thBoss; // start again from 0
			// up to 7 more flags here
			writer.Write(flags2); // write this byte
			*/

			// If you prefer, you can use the BitsByte constructor approach as well.
			// BitsByte flags = new BitsByte(downedMinionBoss, downedOtherBoss);
			// writer.Write(flags);

			// This is another way to do the same thing, but with bitmasks and the bitwise OR assignment operator (the |=)
			// Note that 1 and 2 here are bit masks. The next values in the pattern are 4,8,16,32,64,128. If you require more than 8 flags, make another byte.
			// byte flags = 0;
			// if (downedMinionBoss)
			// {
			//	flags |= 1;
			// }
			// if (downedOtherBoss)
			// {
			//	flags |= 2;
			// }
			// writer.Write(flags);

			// If you plan on having more than 8 of these flags and don't want to use multiple BitsByte, an alternative is using a System.Collections.BitArray
			/*
			bool[] flags = new bool[] {
				downedMinionBoss,
				downedOtherBoss,
			};
			BitArray bitArray = new BitArray(flags);
			byte[] bytes = new byte[(bitArray.Length - 1) / 8 + 1]; // Calculation for correct length of the byte array
			bitArray.CopyTo(bytes, 0);

			writer.Write(bytes.Length);
			writer.Write(bytes);
			*/
		}

		public override void NetReceive(BinaryReader reader) {
			// Order of operations is important and has to match that of NetSend
			BitsByte flags = reader.ReadByte();
			downedChiyuriBoss = flags[0];
            downedYumemiBoss = flags[1];
            downedOrinBossCat = flags[2];
			downedOrinBossHumanoid = flags[3];

            // As mentioned in NetSend, BitBytes can contain up to 8 values. If you have more, be sure to read the additional data:
            // BitsByte flags2 = reader.ReadByte();
            // downed9thBoss = flags2[0];

            // System.Collections.BitArray approach:
            /*
			int length = reader.ReadInt32();
			byte[] bytes = reader.ReadBytes(length);

			BitArray bitArray = new BitArray(bytes);
			downedMinionBoss = bitArray[0];
			downedOtherBoss = bitArray[1];
			*/
        }
	}
}
