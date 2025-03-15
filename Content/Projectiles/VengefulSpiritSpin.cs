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
using MonoMod.Core.Utils;

namespace DimDream.Content.Projectiles
{
    public class VengefulSpiritSpin : ModProjectile
    {
        public int FrameLoop
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public Vector2 OrbitVelocity
        {
            get => new(Projectile.ai[0], Projectile.ai[1]);
            set
            {
                Projectile.ai[0] = value.X;
                Projectile.ai[1] = value.Y;
            }
        }

        public Vector2 OrbitCenter
        {
            get => new(Projectile.localAI[1], Projectile.localAI[2]);
            set
            {
                Projectile.localAI[1] = value.X;
                Projectile.localAI[2] = value.Y;
            }
        }

        public override string Texture => "DimDream/Content/NPCs/OrinEvilSpiritRed";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 200;
            Projectile.tileCollide = false;
        }

        public bool FadedIn
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // When overriding GetAlpha, you usually want to take the projectiles alpha into account. As it is a value between 0 and 255,
            // it's annoying to convert it into a float to multiply. Luckily the Opacity property handles that for us (0f transparent, 1f opaque)
            return Color.White * Projectile.Opacity;
        }

        public override void AI()
        {
            Movement();
            Visuals();
        }

        public void Movement()
        {
            if (OrbitCenter == Vector2.Zero)
                OrbitCenter = Projectile.Center + Projectile.velocity;
            else if (OrbitVelocity != Vector2.Zero)
                OrbitCenter += OrbitVelocity;

            float rotationSpeed = MathHelper.Pi / 20f; // Adjust for slower/faster orbit

            Vector2 offset = Projectile.Center - OrbitCenter;
            Vector2 rotationMovement = offset.RotatedBy(rotationSpeed);

            Projectile.velocity = rotationMovement - offset + OrbitVelocity;
        }

        public void Visuals()
        {
            FadeInAndOut();

            int frameSpeed = 6;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                FrameLoop++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = 0;

                if (FrameLoop % 4 == 0)
                    CreateDust();
            }
        }

        private void CreateDust()
        {
            int dustType = 264;

            Vector2 velocity = Projectile.velocity + new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.2f));
            Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 26, Color.White, Main.rand.NextFloat(1f, 2f));

            dust.noGravity = true;
            dust.fadeIn = Main.rand.NextFloat(0.3f, 0.8f);
        }

        private void FadeInAndOut()
        {
            // Fade in (we have Projectile.alpha = 255 in SetDefaults which means it spawns transparent)
            int fadeSpeed = 30;
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
    }
}
