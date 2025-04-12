using DimDream.Content.Buffs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using System.Diagnostics.Metrics;

namespace DimDream.Content.Projectiles
{
    public class GhostBullet : ModProjectile
    {
        private const int TOTAL_TIME_LEFT = 500;
        public int FrameToMove
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        public float DynamicRotation
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public bool FadedIn
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        public bool Initialized
        {
            get => Projectile.localAI[1] == 1f;
            set => Projectile.localAI[1] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 7;
        }

        public sealed override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.timeLeft = TOTAL_TIME_LEFT;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            if (!Initialized)
            {
                Initialized = true;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.velocity *= Main.rand.NextFloat(.5f, 2f);
                    FrameToMove = Main.rand.Next(10, 50);
                    Projectile.velocity.RotateRandom(.6f);
                    DynamicRotation = Main.rand.NextFloat(-.4f, .4f);
                }
            }

            Movement();
            Visuals();
        }

        private void Movement()
        {
            int frameToStop = FrameToMove + 60;
            if (Projectile.timeLeft < TOTAL_TIME_LEFT - FrameToMove && Projectile.timeLeft > TOTAL_TIME_LEFT - frameToStop)
            {
                float inertia = 20f;
                Vector2 direction = Projectile.velocity.RotatedBy(DynamicRotation);
                Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction) / inertia;
            }
        }

        private void Visuals()
        {
            FadeInAndOut();

            int frameSpeed = 6;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }

                CreateDust();
            }

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.78f);
        }

        private void FadeInAndOut()
        {
            // Fade in (we have Projectile.alpha = 255 in SetDefaults which means it spawns transparent)
            int fadeSpeed = 4;
            if (!FadedIn && Projectile.alpha > 0)
            {
                Projectile.alpha -= fadeSpeed;
                if (Projectile.alpha < 0)
                {
                    FadedIn = true;
                    Projectile.alpha = 0;
                }
            }
            else if (FadedIn && Projectile.timeLeft < 255f / fadeSpeed)
            {
                // Fade out so it aligns with the projectile despawning
                Projectile.alpha += fadeSpeed;
                if (Projectile.alpha > 255)
                {
                    Projectile.alpha = 255;
                }
            }
        }

        private void CreateDust()
        {
            int dustType = 264;

            Vector2 velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.2f));
            Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 26, Color.White, Main.rand.NextFloat(.5f, 1f));

            dust.noGravity = true;
            dust.fadeIn = Main.rand.NextFloat(0.3f, 0.8f);
        }
    }
}
