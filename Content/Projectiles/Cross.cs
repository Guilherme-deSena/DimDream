using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DimDream.Content.Projectiles
{
    internal class CrossBoss : ModProjectile
    {
        public const float GROW_SPEED = 10f;
        public const float MAX_LENGTH = 140f;
        public override string Texture => "DimDream/Content/Projectiles/CrossRed";
        public float Distance
        {
            get => Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }
        public bool FadedIn
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }


        public bool ShouldRetract => Projectile.timeLeft + 1 <= MAX_LENGTH / GROW_SPEED;

        public override void SetDefaults()
        {
            Projectile.height = 10;
            Projectile.width = 10;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 200;
            CooldownSlot = ImmunityCooldownID.Bosses;
            Distance = 30f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawCross(Main.spriteBatch, TextureAssets.Projectile[Type].Value, Projectile.position, new Vector2(1, 0), 10, lightColor);
            return false;
        }

        // The core function of drawing a laser
        public void DrawCross(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 unit, float step, Color lightColor, float scale = 1f)
        {
            Color c = lightColor;
            for (int j = 0; j < 4; j++)
            {
                float rotation = (MathHelper.Pi / 2) * j;
                Vector2 direction = new Vector2(0, -1).RotatedBy(rotation);

                float sideDistance = direction.Y == 1 ? Distance : Distance * .60f;
                // Draws the body
                for (float i = 0; i <= sideDistance; i += step)
                {
                    var origin = start + i * direction;
                    spriteBatch.Draw(texture, origin - Main.screenPosition,
                        new Rectangle(0, 0, 48, 10), c, rotation,
                        new Vector2(48 * .5f, 10 * .5f), scale, 0, 0);
                }

                // Draws the border
                spriteBatch.Draw(texture, start + (sideDistance) * direction - Main.screenPosition,
                    new Rectangle(0, 12, 48, 10), c, rotation,
                    new Vector2(48 * .5f, 10 * .5f), scale, 0, 0);
            }

            // Draws the base
            spriteBatch.Draw(texture, start - Main.screenPosition,
                new Rectangle(0, 24, 48, 48), c, 0,
                new Vector2(48 * .5f, 48 * .5f), scale, 0, 0);
        }

        public override void AI()
        {
            RaiseCrossLength();
            CastLights();

            if (ShouldRetract)
            {
                Distance -= GROW_SPEED;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Run an AABB versus Line check to look for collisions, look up AABB collision first to see how it works
            // It will look for collisions on the given line using AABB
            for (int j = 0; j < 4; j++)
            {
                float rotation = (MathHelper.Pi / 2) * j;
                Vector2 direction = new Vector2(0, -1).RotatedBy(rotation);
                float point = 0f;
                float sideDistance = direction.Y == 1 ? Distance : Distance * .60f;

                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.position,
                Projectile.position + direction * sideDistance, 48, ref point))
                    return true;
            }
            return false;
        }


        private void RaiseCrossLength()
        {
            if (Distance <= MAX_LENGTH && !ShouldRetract)
                Distance += GROW_SPEED;
        }

        private void CastLights()
        {
            for (int i = 0; i < 4; i++)
            {
                float rotation = (MathHelper.Pi / 2) * i;
                Vector2 direction = new Vector2(0, -1).RotatedBy(rotation);
                float sideDistance = direction.Y == 1 ? Distance : Distance * .60f;

                // Cast a light along the cross
                DelegateMethods.v3_1 = new Vector3(0.8f, 0.8f, 1f);
                Utils.PlotTileLine(Projectile.Center, Projectile.Center + direction * sideDistance, 26, DelegateMethods.CastLight);

            }
        }
    }

    internal class CrossWeapon : CrossBoss
    {
        public override string Texture => "DimDream/Content/Projectiles/CrossBlue";
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.timeLeft = 60;
            Projectile.height = 10;
            Projectile.width = 10;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Distance = 30f;
        }
    }
}
