using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using DimDream.Content.Projectiles;

namespace DimDream.Content.Items.Weapons
{
    internal class RedButton : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToRangedWeapon(ModContent.ProjectileType<MimiChanRanged>(), AmmoID.None, 40, 10, true);
            //Item.useStyle = ItemUseStyleID.HoldUp;
            Item.width = 32;
            Item.height = 32;
            Item.SetWeaponValues(200, 10);
            Item.value = Item.sellPrice(gold: 9);
            Item.rare = ItemRarityID.Cyan;
            Item.UseSound = SoundID.Item92;
        }


        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 target = Main.screenPosition + new Vector2(Main.mouseX, Main.mouseY);
            
            position = player.Center - new Vector2(Main.rand.NextFloat(401f) * player.direction, 600f);
            Vector2 heading = target - position;

            if (heading.Y < 0f)
                heading.Y *= -1f;

            if (heading.Y < 20f)
                heading.Y = 20f;

            heading.Normalize();
            heading *= velocity.Length();
            heading.Y += Main.rand.Next(-40, 41) * 0.02f;
            Projectile.NewProjectile(source, position, heading, type, damage, knockback, player.whoAmI, target.X, target.Y);


            return false;
        }
    }
}
