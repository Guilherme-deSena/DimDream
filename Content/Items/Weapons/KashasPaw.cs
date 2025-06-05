using DimDream.Content.Projectiles;
using Microsoft.Xna.Framework;
using System.Net.Mail;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DimDream.Content.Items.Weapons
{
    public class KashasPaw : ModItem
    {
        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 12;
            Item.useTime = 12;
            Item.damage = 120;
            Item.crit = 4;
            Item.knockBack = 10f;
            Item.shootSpeed = 8f;
            Item.width = 34;
            Item.height = 34;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(gold: 7); // Sell price is 5 times less than the buy price.
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = true;
            Item.shootsEveryUse = true;
            Item.shoot = ModContent.ProjectileType<VengefulSpiritHoming>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (Main.rand.Next(100) <= (player.GetCritChance(DamageClass.Melee) + player.GetCritChance(DamageClass.Generic) + Item.crit))
                return true;

            return false;
        }
    }
}
