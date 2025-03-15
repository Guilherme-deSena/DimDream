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
    public class VengefulSpiritHoming : ModProjectile
    {
        public bool FadedIn
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        public override string Texture => "DimDream/Content/NPCs/OrinEvilSpiritBlue";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
        }

        public sealed override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 36;
            Projectile.timeLeft = 400;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 3;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            SearchForTargets(out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter);
            Movement(foundTarget, distanceFromTarget, targetCenter);
            Visuals();
        }

        private void SearchForTargets(out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter)
        {
            distanceFromTarget = 700f;
            targetCenter = Projectile.position;
            foundTarget = false;

            foreach (var npc in Main.ActiveNPCs)
            {
                if (npc.CanBeChasedBy())
                {
                    float between = Vector2.Distance(npc.Center, Projectile.Center);
                    bool closest = Vector2.Distance(Projectile.Center, targetCenter) > between;
                    bool inRange = between < distanceFromTarget;
                    bool lineOfSight = Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height);
                    bool closeThroughWall = between < 100f;

                    if (((closest && inRange) || !foundTarget) && (lineOfSight || closeThroughWall))
                    {
                        distanceFromTarget = between;
                        targetCenter = npc.Center;
                        foundTarget = true;
                    }
                }
            }
        }

        private void Movement(bool foundTarget, float distanceFromTarget, Vector2 targetCenter)
        {
            float speed = 8f;
            float inertia = 20f;

            if (foundTarget && distanceFromTarget > 40f)
            {
                Vector2 direction = targetCenter - Projectile.Center;
                direction.Normalize();
                direction *= speed;

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

        private void CreateDust()
        {
            int dustType = 264;

            Vector2 velocity = Projectile.velocity + new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.2f));
            Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 26, Color.White, Main.rand.NextFloat(1f, 2f));

            dust.noGravity = true;
            dust.fadeIn = Main.rand.NextFloat(0.3f, 0.8f);
        }
    }
}
