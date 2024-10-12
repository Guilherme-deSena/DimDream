using System;
using System.Collections.Generic;
using Terraria.ID;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using DimDream.Content.Projectiles;
using DimDream.Common.Systems;
using Microsoft.Xna.Framework;
using DimDream.Content.Buffs;
using Terraria.Audio;

namespace DimDream.Content.Projectiles
{
    public class MimiChanRanged : ModProjectile
    {
        public bool Exploding { get; set; } = false;
        public Vector2 Target { get => new(Projectile.ai[0], Projectile.ai[1]); }
        public override string Texture => "DimDream/Content/Projectiles/MimiChan";
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        public sealed override void SetDefaults()
        {
            Projectile.width = 74;
            Projectile.height = 24;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            HandleExplosion();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            HandleExplosion();
            return false;
        }

        public void HandleExplosion()
        {
            if (!Exploding)
            {
                Exploding = true;
                SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
                Projectile.timeLeft = 30;
                Projectile.Resize(250, 250);
                Projectile.velocity = Vector2.Zero;
                Projectile.alpha = 255;
                Projectile.tileCollide = false;
            }
        }

        public override void AI()
        {
            if (Exploding)
            {
                if (Projectile.timeLeft % 10 == 0)
                    ExplosionDust(Projectile.Center, Projectile.timeLeft / 5);

                return; // Prevent all the regular AI
            }

            if (!AreThereEmptySpaces(Projectile.position, Target) || Target.Y < Projectile.position.Y)
                Projectile.tileCollide = true;

            Visuals();
        }

        public bool AreThereEmptySpaces(Vector2 startPosition, Vector2 endPosition)
        {
            // Convert world positions to tile coordinates
            Vector2 start = startPosition / 16f;
            Vector2 end = endPosition / 16f;

            Vector2 toDestination = end - start;
            float distance = toDestination.Length();
            Vector2 destNormalized = toDestination.SafeNormalize(Vector2.UnitY);

            // Iterate through all tiles in the line between the start and end positions
            for (int i = 0; i < distance; i++)
            {
                Tile tile = Main.tile[(int)(start.X + i * destNormalized.X), (int)(start.Y + i * destNormalized.Y)];
                if (tile == null || !tile.HasTile)
                    return true;
            }

            return false;
        }

        public void ExplosionDust(Vector2 position, int counter)
        {

            // The size and scale of the explosion (can be adjusted)
            int explosionSize = 50;  // Control how many dust particles to spawn
            float dustSpeed = 6f;   // How fast the dust particles should move


            for (int i = 0; i < explosionSize; i++)
            {
                // Determine a random direction for the dust
                Vector2 velocity = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * dustSpeed;

                Color color = new(60 * counter, 40 * counter, 10 * counter, 0);
                Dust dust = Dust.NewDustPerfect(position, DustID.Smoke, velocity * Main.rand.NextFloat(.1f, 1f), 100, color, 1.5f);
                dust.noGravity = false;  // Make sure the dust floats up
                dust.scale = 1.5f + (Main.rand.NextFloat(.5f));  // Randomize size for variation

                // Add more layers of dust based on the position
                if (i < explosionSize / 2)
                {
                    // Fiery glow and orange effect at the center
                    Vector2 smokeVelocity = new Vector2(Main.rand.NextFloat(-.4f, .4f), Main.rand.NextFloat(.5f, .9f)) * dustSpeed;
                    Dust fireDust = Dust.NewDustPerfect(position, DustID.Smoke, smokeVelocity, 150, Color.Orange, 2f);
                    fireDust.noGravity = false;
                    fireDust.fadeIn = 1.5f;
                }
            }

            // Rising smoke plume (grey to black dust)
            for (int j = 0; j < explosionSize / 2; j++)
            {
                Vector2 smokeVelocity = new Vector2(Main.rand.NextFloat(-.7f, .7f), Main.rand.NextFloat(-1.2f, -0.2f)) * dustSpeed;
                Dust smokeDust = Dust.NewDustPerfect(position - new Vector2(0, 30), DustID.Smoke, smokeVelocity, 100, Color.Gray, 2f);
                smokeDust.noGravity = false;  // Smoke should rise but settle down after a while
                smokeDust.scale = 2f + (Main.rand.NextFloat(.8f));
            }
        }

        private void Visuals()
        {
            Projectile.spriteDirection = Projectile.velocity.X >= 0 ? 1 : -1;
            float rotationOffset = Projectile.spriteDirection == 1 ? 0 : MathHelper.Pi;
            Projectile.rotation = Projectile.velocity.ToRotation() + rotationOffset;
            int frameSpeed = 5;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.78f);
        }
    }
}
