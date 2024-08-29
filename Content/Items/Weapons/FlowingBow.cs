using DimDream.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace DimDream.Content.Items.Weapons
{
    public class FlowingBow : ModItem
    {
        public override void SetStaticDefaults()
        {
            
        }

        public override void SetDefaults()
        {
            Item.DefaultToBow(40, 24, true);

            // Overrides over DefaultToBow()
            Item.width = 34;
            Item.height = 72;
            Item.useTime = Item.useAnimation / 4;

            Item.reuseDelay = 40;
            Item.consumeAmmoOnLastShotOnly = true;

            // Item21 is mini-splash sound
            Item.UseSound = SoundID.Item21;

            // Damage and knockBack
            Item.SetWeaponValues(70, 6);

            // Rarity and value
            Item.SetShopValues(ItemRarityColor.Green2, 60000);

            Item.shoot = ModContent.ProjectileType<Wave>();
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            // If player is using wooden arrows, this weapon should shoot Wave projectiles instead
            if (type == ProjectileID.WoodenArrowFriendly)
            {
                type = ModContent.ProjectileType<Wave>();
            }
        }
    }
}