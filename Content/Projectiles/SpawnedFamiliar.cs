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
    internal class SpawnedFamiliar : ModProjectile
    {
        //public int ProjectileType { get => (int)Projectile.ai[0]; }
        public NPC ParentNPC { get => Main.npc[(int)Projectile.ai[0]]; }
        public bool FadedIn
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.alpha = 255;
            Projectile.timeLeft = 90;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.aiStyle = -1;
            CooldownSlot = ImmunityCooldownID.Bosses; // use the boss immunity cooldown counter, to prevent ignoring boss attacks by taking damage from other sources

        }

        public override Color? GetAlpha(Color lightColor)
        {
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

        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Projectile.timeLeft <= 1)
                {
                    Vector2 position = Projectile.Center;
                    Vector2 offset = new Vector2(14, 14); // Necessary to align spawning correctly
                    int type = ModContent.ProjectileType<Cross>();
                    var entitySource = Projectile.GetSource_FromAI();

                    Projectile.NewProjectile(entitySource, position + offset, Vector2.Zero, type, Projectile.damage, 0f, Main.myPlayer);
                    Projectile.Kill();
                }
            }

            if (!ParentNPC.active)
                Projectile.Kill();


            Visuals();
        }

        private void Visuals()
        {
            int frameSpeed = 5;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame > 2)
                    Projectile.frame = 0;
            }

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.78f);
            FadeInAndOut();
        }
    }
}
