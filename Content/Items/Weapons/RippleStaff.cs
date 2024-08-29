using DimDream.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace DimDream.Content.Items.Weapons
{
    // Staff sprites, by convention, are angled to point up and to the right. "Item.staff[Type] = true;" is essential for correctly drawing staffs.
    // Staffs use mana and shoot a specific projectile instead of using ammo. Item.DefaultToStaff takes care of that.
    public class RippleStaff : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.staff[Type] = true; // This makes the useStyle animate as a staff instead of as a gun.
        }

        public override void SetDefaults()
        {
            // DefaultToStaff handles setting various Item values that magic staff weapons use.
            // Hover over DefaultToStaff in Visual Studio to read the documentation!
            Item.DefaultToStaff(ModContent.ProjectileType<Ripple>(), 16, 6, 3);

            // Item21 is mini-splash sound
            Item.UseSound = SoundID.Item21;

            // Set damage and knockBack
            Item.SetWeaponValues(15, 3);

            // Set rarity and value
            Item.SetShopValues(ItemRarityColor.Green2, 60000);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockBack)
        {
            // Offset amount for the parallel projectiles
            float offset = 8f;  // Adjust this value for more or less distance between projectiles

            // Calculate the perpendicular vector to the velocity
            Vector2 perpendicular = Vector2.Normalize(new Vector2(-velocity.Y, velocity.X)) * offset;

            // First projectile
            Vector2 firstPosition = position + perpendicular;
            Projectile.NewProjectile(source, firstPosition, velocity, type, damage, knockBack, player.whoAmI);

            // Second projectile
            Vector2 secondPosition = position - perpendicular;
            Projectile.NewProjectile(source, secondPosition, velocity, type, damage, knockBack, player.whoAmI);

            return false;  // Return false to prevent vanilla behavior
        }
    }
}