using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using System.Diagnostics.Metrics;

namespace DimDream.Content.Projectiles
{
    internal class ThrownFamiliar : ModProjectile
    {
        public bool SecondPhase { get => Projectile.timeLeft < 60; }
        public NPC ParentNPC { get => Main.npc[(int)Projectile.ai[0]]; }
        public bool FadedIn
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            DrawOffsetX = -16;
            DrawOriginOffsetY = -16;
            Projectile.alpha = 255;
            Projectile.timeLeft = 120;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
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
            int fadeSpeed = 15;
            if (!FadedIn && Projectile.alpha > 0)
            {
                Projectile.alpha -= fadeSpeed;
                if (Projectile.alpha <= 0)
                {
                    FadedIn = true;
                    Projectile.alpha = 0;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone / 10);
            SpawnCross();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnCross();
            return base.OnTileCollide(oldVelocity);
        }

        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Projectile.timeLeft <= 1)
                {
                    SpawnCross();
                    Projectile.Kill();
                }
            } else if (Projectile.timeLeft <= 1) // If spawned by weapon and is multiplayer client, spawn cross anyway
            {
                SpawnCross();
                Projectile.Kill();
            }

            Visuals();
        }

        public void SpawnCross()
        {
            Vector2 position = Projectile.Center;
            Vector2 offset = new Vector2(14, 14); // Necessary to align spawning correctly
            int type = ModContent.ProjectileType<CrossWeapon>();
            var entitySource = Projectile.GetSource_FromAI();

            Projectile.NewProjectile(entitySource, position + offset, Vector2.Zero, type, Projectile.damage, Projectile.knockBack, Main.myPlayer);
        }

        private void Visuals()
        {
            int frameSpeed = SecondPhase ? 5 : 20;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (!SecondPhase && Projectile.frame >= 2)
                    Projectile.frame = 0;
                else if (SecondPhase && Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = 2;
            }

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.78f);
            FadeInAndOut();
        }
    }
}
