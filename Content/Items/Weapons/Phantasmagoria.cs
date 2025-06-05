using DimDream.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace DimDream.Content.Items.Weapons
{
    public class Phantasmagoria : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 20;

            Item.shootSpeed = 6f;
            Item.noMelee = true;
            Item.useTime = 8;
            Item.useAnimation = 8;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Ranged;

            Item.SetWeaponValues(140, 3);
            Item.SetShopValues(ItemRarityColor.Orange3, 120000);

            Item.UseSound = SoundID.Item40;

            Item.consumeAmmoOnLastShotOnly = true;
            Item.shoot = ModContent.ProjectileType<GhostBullet>();
            Item.useAmmo = AmmoID.Bullet;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
                type = Item.shoot;
        }
    }
}
