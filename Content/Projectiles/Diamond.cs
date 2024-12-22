using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace DimDream.Content.Projectiles
{
    public class Diamond : ModProjectile
    {
        private float Behavior // Set to 1 when creating the proj for speed up behavior
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private float SpriteBehavior
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;

        }

        private bool FadedIn
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        private bool PlayedSpawnSound
        {
            get => Projectile.localAI[1] == 1f;
            set => Projectile.localAI[1] = value ? 1f : 0f;
        }

        private int Counter
        {
            get => (int)Projectile.localAI[2];
            set => Projectile.localAI[2] = value;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            DrawOffsetX = -2;
            DrawOriginOffsetY = -7;
            Projectile.alpha = 255;
            Projectile.timeLeft = 250;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.aiStyle = -1;
            CooldownSlot = ImmunityCooldownID.Bosses; // use the boss immunity cooldown counter, to prevent ignoring boss attacks by taking damage from other sources
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // When overriding GetAlpha, you usually want to take the projectiles alpha into account. As it is a value between 0 and 255,
            // it's annoying to convert it into a float to multiply. Luckily the Opacity property handles that for us (0f transparent, 1f opaque)
            return Color.White * Projectile.Opacity;
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

        public override void AI()
        {
            Counter++;

            if (!PlayedSpawnSound)
            {
                PlayedSpawnSound = true;
                SoundEngine.PlaySound(SoundID.Item8, Projectile.position);
            }

            float maxSpeed = 12f;
            if (Behavior == 1f && Projectile.velocity.Length() < maxSpeed && Projectile.timeLeft <= 150)
            {
                float acceleration = .1f;
                Projectile.velocity *= 1f + acceleration / Projectile.velocity.Length();
            }

            Visuals();
        }

        private void Visuals()
        {
            if (SpriteBehavior == 1f || (SpriteBehavior == 2f && Projectile.velocity.Length() <= .1f))
                Projectile.frame = 1;
            else
                Projectile.frame = 0;

            FadeInAndOut();

            // If the sprite points upwards, this will make it point towards the move direction (for other sprite orientations, change MathHelper.PiOver2)
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.spriteDirection = Projectile.direction;
        }
    }
}
