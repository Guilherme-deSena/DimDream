﻿using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace DimDream.Content.Projectiles
{
    public class BarrowsWheelProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // The following sets are only applicable to yoyo that use aiStyle 99.

            // YoyosLifeTimeMultiplier is how long in seconds the yoyo will stay out before automatically returning to the player. 
            // Vanilla values range from 3f (Wood) to 16f (Chik), and defaults to -1f. Leaving as -1 will make the time infinite.
            ProjectileID.Sets.YoyosLifeTimeMultiplier[Projectile.type] = 6f;

            // YoyosMaximumRange is the maximum distance the yoyo sleep away from the player. 
            // Vanilla values range from 130f (Wood) to 400f (Terrarian), and defaults to 200f.
            ProjectileID.Sets.YoyosMaximumRange[Projectile.type] = 240f;

            // YoyosTopSpeed is top speed of the yoyo Projectile.
            // Vanilla values range from 9f (Wood) to 17.5f (Terrarian), and defaults to 10f.
            ProjectileID.Sets.YoyosTopSpeed[Projectile.type] = 10f;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;

            Projectile.aiStyle = ProjAIStyleID.Yoyo; // Yoyos use aiStyle 99 (ProjAIStyleID.Yoyo). A lot of yoyo code checks for this aiStyle to work properly.

            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = -1; // All vanilla yoyos have infinite penetration. The number of enemies the yoyo can hit before being pulled back in is based on YoyosLifeTimeMultiplier.
        }

        // notes for aiStyle 99: 
        // localAI[0] is used for timing up to YoyosLifeTimeMultiplier
        // localAI[1] can be used freely by specific types
        // ai[0] and ai[1] usually point towards the x and y world coordinate hover point
        // ai[0] is -1f once YoyosLifeTimeMultiplier is reached, when the player is stoned/frozen, when the yoyo is too far away, or the player is no longer clicking the shoot button.
        // ai[0] being negative makes the yoyo move back towards the player
        // Any AI method can be used for dust, spawning projectiles, etc specific to your yoyo.

        public int Counter
        {
            get => (int)Projectile.localAI[2];
            set => Projectile.localAI[2] = value;
        }

        public override void PostAI()
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Counter++;

                float angle = MathHelper.Pi / 90 * Counter;

                if (Counter % 10 == 0)
                ShootSpiralBullet(Projectile.position, angle);
            }

            if (Main.rand.NextBool(5))
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.SilverFlame);
        }

        public void ShootSpiralBullet(Vector2 position, float angle)
        {
            Vector2 velocity = new Vector2(0, -1).RotatedBy(angle);

            float speed = Main.rand.NextFloat(3f, 5f);
            var entitySource = Projectile.GetSource_FromThis();
            int type = ModContent.ProjectileType<BasicSpiralBulletFriendly>();

            Projectile.NewProjectile(entitySource, position, velocity * speed, type, Projectile.damage / 2, Projectile.knockBack / 2, Main.myPlayer);
        }
    }
}
