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
	public class BlueLaser : ModProjectile
	{
		// Use a different style for constant so it is very clear in code when a constant is used
		// The distance charge particle from the projectile start
		private const float MOVE_DISTANCE = 0f;
		// How fast the projectile grows and retracts at the beginning and end of its lifespan
		private const float GROW_SPEED = 11f;

		// The actual distance is stored in the ai0 field
		// By making a property to handle this it makes our life easier, and the accessibility more readable
		public float Distance {
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		// Where the laser starts. This changes at the end of the projectile's lifespan to simulate retraction
		public float StartPosition {
			get => Projectile.ai[1];
			set => Projectile.ai[1] = value;
		}

		// How long the laser can be
		public float MaxLength { get; set; } = 2200f;


        public bool ShouldRetract => Projectile.timeLeft + 1 <= MaxLength / GROW_SPEED;
		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 3000;
		}
		public override void SetDefaults() {
			Projectile.height = 10;
			Projectile.width = 10;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 400;
            Projectile.netImportant = true;
            CooldownSlot = ImmunityCooldownID.Bosses; // use the boss immunity cooldown counter, to prevent ignoring boss attacks by taking damage from other sources
        }

		public override bool PreDraw(ref Color lightColor) {
			DrawLaser(Main.spriteBatch, TextureAssets.Projectile[Type].Value, Projectile.position,
					  Projectile.velocity, 10, -1.57f, 1f, 1000f, Color.White, (int)MOVE_DISTANCE);
			return false;
		}

		// The core function of drawing a laser
		public void DrawLaser(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 unit, float step, float rotation = 0f, float scale = 1f, float maxDist = 2000f, Color color = default(Color), int transDist = 50) {
			float r = unit.ToRotation() + rotation;
			// Draws the laser 'body'
			for (float i = transDist; i <= Distance; i += step) {
				Color c = Color.White;
				var origin = start + i * unit;
				spriteBatch.Draw(texture, origin - Main.screenPosition,
					new Rectangle(0, 30, 26, 34), i < transDist ? Color.Transparent : c, r,
					new Vector2(28 * .5f, 36 * .5f), scale, 0, 0);
			}

			// Draws the laser 'tail'
			spriteBatch.Draw(texture, start + unit * (transDist - step) - Main.screenPosition,
				new Rectangle(0, 0, 26, 30), Color.White, r, new Vector2(28 * .5f, 26 * .5f), scale, 0, 0); //28 26

			// Draws the laser 'head'
			spriteBatch.Draw(texture, start + (Distance + step) * unit - Main.screenPosition,
				new Rectangle(0, 60, 26, 32), Color.White, r, new Vector2(28 * .5f, 26 * .5f), scale, 0, 0);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			Vector2 unit = Projectile.velocity;
			float point = 0f;
			// Run an AABB versus Line check to look for collisions, look up AABB collision first to see how it works
			// It will look for collisions on the given line using AABB
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.position,
				Projectile.position + unit * Distance, 22, ref point);
		}

		// The AI of the Projectile
		public override void AI() {
			Player player = Main.player[Projectile.owner];
			//Projectile.position += Projectile.velocity * MOVE_DISTANCE;

			Projectile.netUpdate = true;

			SetLaserPosition();
			SpawnDusts();
			ChargeLaser();
			CastLights();

			if (ShouldRetract) {
				Projectile.position += Projectile.velocity * GROW_SPEED;
				Distance -= GROW_SPEED;
			}
		}

		private void SpawnDusts() {
			Vector2 unit = Projectile.velocity * -1;
			Vector2 dustPos = Projectile.position + Projectile.velocity * (Distance + 25);

			for (int i = 0; i < 2; ++i) {
				float num1 = Projectile.velocity.ToRotation() + (Main.rand.NextBool(2) ? -1.0f : 1.0f) * 1.57f;
				float num2 = (float)(Main.rand.NextDouble() * 0.8f + 1.0f);
				Vector2 dustVel = new Vector2((float)Math.Cos(num1) * num2, (float)Math.Sin(num1) * num2);
				Dust dust = Main.dust[Dust.NewDust(dustPos, 0, 0, DustID.Electric, dustVel.X, dustVel.Y)];
				dust.noGravity = true;
				dust.scale = 1.2f;
				dust = Dust.NewDustDirect(Projectile.position, 0, 0, DustID.Smoke,
					-unit.X * Distance, -unit.Y * Distance);
				dust.fadeIn = 0f;
				dust.noGravity = true;
				dust.scale = 0.88f;
				dust.color = Color.Cyan;
			}

			if (Main.rand.NextBool(5)) {
				Vector2 offset = Projectile.velocity.RotatedBy(1.57f) * ((float)Main.rand.NextDouble() - 0.5f) * Projectile.width;
				Dust dust = Main.dust[Dust.NewDust(dustPos + offset - Vector2.One * 4f, 8, 8, DustID.Smoke, 0.0f, 0.0f, 100, new Color(), 1.5f)];
				dust.velocity *= 0.5f;
				dust.velocity.Y = -Math.Abs(dust.velocity.Y);
				unit = dustPos - Projectile.position;
				unit.Normalize();
				dust = Main.dust[Dust.NewDust(Projectile.position + 55 * unit, 8, 8, DustID.Smoke, 0.0f, 0.0f, 100, new Color(), 1.5f)];
				dust.velocity = dust.velocity * 0.5f;
				dust.velocity.Y = -Math.Abs(dust.velocity.Y);
			}
		}

		/*
		* Sets the end of the laser position based on where it collides with something
		*/
		private void SetLaserPosition() {
			if (Distance <= MaxLength && !ShouldRetract)
				Distance += GROW_SPEED;
		}

		private void ChargeLaser() {
			Vector2 offset = Projectile.velocity;
			offset *= MOVE_DISTANCE - 20;
			Vector2 pos = Projectile.position + offset - new Vector2(10, 10);
			Vector2 dustVelocity = Vector2.UnitX * 18f;
			dustVelocity = dustVelocity.RotatedBy(Projectile.rotation - 1.57f);
			Vector2 spawnPos = Projectile.Center + dustVelocity;
			for (int k = 0; k < 10; k++) {
				Vector2 spawn = spawnPos + ((float)Main.rand.NextDouble() * 6.28f).ToRotationVector2() * (12f - 20f);
				Dust dust = Main.dust[Dust.NewDust(pos, 20, 20, DustID.Electric, Projectile.velocity.X / 2f, Projectile.velocity.Y / 2f)];
				dust.velocity = Vector2.Normalize(spawnPos - spawn) * 1.5f * (10f - 20f) / 10f;
				dust.noGravity = true;
				dust.scale = Main.rand.Next(10, 20) * 0.05f;
			}
		}


		private void CastLights() {
			// Cast a light along the line of the laser
			DelegateMethods.v3_1 = new Vector3(0.8f, 0.8f, 1f);
			Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * (Distance - MOVE_DISTANCE), 26, DelegateMethods.CastLight);
		}

		public override bool ShouldUpdatePosition() => false;
	}
}
