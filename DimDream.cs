using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using DimDream.Common.Sky;

namespace DimDream
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class DimDream : Mod
	{
        public override void Load()
        {
            if (!Main.dedServ)
            {
                SkyManager.Instance["DimDream:BossSky"] = new BossSky();
            }
        }
    }
}
