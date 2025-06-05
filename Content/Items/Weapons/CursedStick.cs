using DimDream.Content.Projectiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Terraria.Enums;
using DimDream.Content.NPCs;
using Mono.Cecil;
using static System.Net.Mime.MediaTypeNames;

namespace DimDream.Content.Items.Weapons
{
    public class CursedStick : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToStaff(ModContent.ProjectileType<VengefulSpiritSpin>(), 14, 25, 16);
            Item.SetWeaponValues(100, 2);
            Item.SetShopValues(ItemRarityColor.Green2, 70000);
            Item.width = 38;
            Item.height = 34;

            Item.UseSound = SoundID.DD2_JavelinThrowersAttack;
            Item.shootsEveryUse = true; // This makes sure Player.ItemAnimationJustStarted is set when swinging.
            Item.autoReuse = true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 target = Main.screenPosition + new Vector2(Main.mouseX, Main.mouseY);
            Vector2 toTargetNormalized = (target - position).SafeNormalize(Vector2.UnitY);
            float speed = 10f;

            SpawnRotatingSpirits(player.MountedCenter, toTargetNormalized * speed, 70, 5, type, player, source, damage, knockback);
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, player.whoAmI); // Sync the changes in multiplayer.
            return false;
        }

        public void SpawnRotatingSpirits(Vector2 center, Vector2 orbitVelocity, int distance, int count, int type,
                                         Player player, EntitySource_ItemUse_WithAmmo source, int damage, float knockback)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi / count * i;
                Vector2 position = new(center.X + MathF.Sin(angle) * distance, center.Y - MathF.Cos(angle) * distance);
                Vector2 velocity = new(-MathF.Sin(angle), MathF.Cos(angle));

                Projectile.NewProjectile(source, position, velocity * distance, type, damage, knockback, player.whoAmI, orbitVelocity.X, orbitVelocity.Y);
            }
        }
    }
}
