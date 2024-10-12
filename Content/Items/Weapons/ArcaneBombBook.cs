using DimDream.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace DimDream.Content.Items.Weapons
{
    public class ArcaneBombBook : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToStaff(ModContent.ProjectileType<ArcaneBomb>(), 14, 75, 40);
            Item.SetWeaponValues(80, 2);
            Item.SetShopValues(ItemRarityColor.Green2, 60000);

            Item.UseSound = SoundID.DD2_DarkMageCastHeal;
        }
    }
}
