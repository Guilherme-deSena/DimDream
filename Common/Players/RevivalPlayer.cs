using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using DimDream.Content.Items.Accessories;

namespace DimDream.Common.Players
{
    // This player allows teammates to respawn on top of the wearer
    public class RevivalPlayer : ModPlayer
    {
        public bool HasRevivalAccessory
        { 
            get
            {
                foreach (Item item in Player.inventory)
                {
                    if (item.type == ModContent.ItemType<OrinsCartEnabled>())
                    {
                        return true;
                    }
                }
                return false;
            } 
        }

        public int countdown = -1; // Delays the logic to prevent it from being overridden by the vanilla logic

        public override void OnRespawn()
        {
            if (Main.netMode != NetmodeID.SinglePlayer) // This whole system is redundant in singleplayer
                countdown = 3;
        }

        public override void PostUpdate()
        {
            if (countdown < 0)
                return;

            if (countdown == 0)
                foreach (Player otherPlayer in Main.player)
                {
                    if (otherPlayer != null && otherPlayer.active && otherPlayer.whoAmI != Player.whoAmI && Player.team != 0 && otherPlayer.team == Player.team && otherPlayer.GetModPlayer<RevivalPlayer>().HasRevivalAccessory)
                    {
                        // Move the respawning player to the accessory holder's position
                        Player.Teleport(otherPlayer.position, 1);
                        break;
                    }
                }

            countdown--;
        }
    }
}