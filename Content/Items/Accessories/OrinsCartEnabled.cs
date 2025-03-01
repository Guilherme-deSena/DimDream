using DimDream.Common.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace DimDream.Content.Items.Accessories
{
    public class OrinsCartEnabled : ModItem
    {
        public override string Texture => "DimDream/Content/Items/Accessories/OrinsCart_Enabled";

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.buyPrice(gold: 10);
        }

        public override bool CanRightClick() => true;

        public override void RightClick(Player player)
        {
            Item.SetDefaults(ModContent.ItemType<OrinsCartDisabled>());
            Item.stack++;
        }


        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            string status = "[c/00ff00:Enabled]";
            tooltips.Add(new TooltipLine(Mod, "RevivalStatus", "Status: " + status));
        }
    }

    public class OrinsCartDisabled : OrinsCartEnabled
    {
        public override string Texture => "DimDream/Content/Items/Accessories/OrinsCart_Disabled";

        public override void RightClick(Player player)
        {
            Item.SetDefaults(ModContent.ItemType<OrinsCartEnabled>());
            Item.stack++;
        }

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            string status = "[c/ff0000:Disabled]";
            tooltips.Add(new TooltipLine(Mod, "RevivalStatus", "Status: " + status));
        }
    }
}
