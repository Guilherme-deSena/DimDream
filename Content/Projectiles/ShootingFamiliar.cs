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
	// This is a boss familiar, a.k.a. boss slave: a projectile summoned by a boss that does not deal damage by itself
	// but can spawn additional bullets.
	internal class ShootingFamiliar : ModProjectile
	{
		public int BulletCount { get => Main.expertMode ? 10 : 8; }
		public static float Pi { get => MathHelper.Pi; }
		public int BulletType { get => (int)Projectile.ai[0]; }
		public NPC ParentNPC { get => Main.npc[(int)Projectile.ai[1]]; }
		public int Pattern { get => (int)Projectile.ai[2]; }
		public Player Target { get => Main.player[ParentNPC.target]; }
		public float Counter {
			get => Projectile.localAI[2];
			set => Projectile.localAI[2] = value;
		}
		public bool FadedIn {
			get => Projectile.localAI[0] == 1f;
			set => Projectile.localAI[0] = value ? 1f : 0f;
		}

		public void AimedSpore() {
			Vector2 position = Projectile.Center;
			Vector2 direction = (Target.Center - position).SafeNormalize(Vector2.UnitY).RotatedBy(Main.rand.NextFloat(-Pi / 100, Pi / 100));

			float speed = .7f;
			int type = ModContent.ProjectileType<WhiteSpore>();
			var entitySource = Projectile.GetSource_FromAI();

			Projectile.NewProjectile(entitySource, position, direction* speed, type, Projectile.damage, 0f, Main.myPlayer, 1f);
		}

		public void AimedRingLine() {
			for (int i = 0; i < BulletCount; i++) {
				Vector2 position = Projectile.Center;
				Vector2 direction = (Target.Center - position).SafeNormalize(Vector2.UnitY).RotatedBy(Main.rand.NextFloat(-Pi / 100, Pi / 100));

				float speed = 4f + (float)i / (BulletCount / 2);
				int type = ModContent.ProjectileType<BlueRing>();
				var entitySource = Projectile.GetSource_FromAI();

				Projectile.NewProjectile(entitySource, position, direction * speed, type, Projectile.damage, 0f, Main.myPlayer, 1f);
			}
		}

		public void RandomBullet(int bulletType)
		{
            Vector2 position = Projectile.Center;
            Vector2 direction = new Vector2(0, -1).RotatedBy(Main.rand.NextFloat(0, Pi*2));

			float speed = Main.rand.NextFloat(2f, 3f);
            var entitySource = Projectile.GetSource_FromAI();

            Projectile.NewProjectile(entitySource, position, direction * speed, bulletType, Projectile.damage, 0f, Main.myPlayer);
        }

		public override void SetDefaults() {
			Projectile.width = 50;
			Projectile.height = 50;
			Projectile.alpha = 255;
			Projectile.timeLeft = 250;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
			Projectile.aiStyle = -1;
			CooldownSlot = ImmunityCooldownID.Bosses; // use the boss immunity cooldown counter, to prevent ignoring boss attacks by taking damage from other sources
		}

		public override Color? GetAlpha(Color lightColor) {
			// When overriding GetAlpha, you usually want to take the projectiles alpha into account. As it is a value between 0 and 255,
			// it's annoying to convert it into a float to multiply. Luckily the Opacity property handles that for us (0f transparent, 1f opaque)
			return Color.White * Projectile.Opacity;
		}

		private void FadeInAndOut() {
			// Fade in (we have Projectile.alpha = 255 in SetDefaults which means it spawns transparent)
			int fadeSpeed = 15;
			if (!FadedIn && Projectile.alpha > 0) {
				Projectile.alpha -= fadeSpeed;
				if (Projectile.alpha <= 0) {
					FadedIn = true;
					Projectile.alpha = 0;
				}
			}
			else if (Projectile.timeLeft < 255f / fadeSpeed) {
				// Fade out so it aligns with the projectile despawning
				Projectile.alpha += fadeSpeed;
				if (Projectile.alpha > 255) {
					Projectile.alpha = 255;
				}
			}
		}

		public override void AI() {
			if (!ParentNPC.active)
				Projectile.Kill();

			FadeInAndOut();

			if (Pattern == 0)
				Pattern0();
			else
				Pattern1();

			Projectile.rotation += .01f;
			Counter++;
		}

		public void Pattern0()
		{
            if (Main.netMode != NetmodeID.MultiplayerClient && Target.active && !Target.dead && Counter % 5 == 0 && Projectile.velocity == Vector2.Zero)
            {
                if (BulletType == 0)
                {
                    AimedSpore();
                }
                else if (Counter % 30 == 0)
                {
                    AimedRingLine();
                }
            }

            Projectile.velocity *= .97f;
            if (Math.Abs(Projectile.velocity.X) < .2f && Math.Abs(Projectile.velocity.Y) < .2f)
                Projectile.velocity = Vector2.Zero;
        }

        public void Pattern1()
        {
            if (Projectile.timeLeft < 90)
            {
				Projectile.velocity.Y += 1f;
				Projectile.velocity.Y = Math.Min(Projectile.velocity.Y, 20f);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
					int bulletCount = 2;
					for (int i = 0; i < bulletCount; i++)
                    {
                        int bulletType = Projectile.timeLeft % 2 == 0 ? ModContent.ProjectileType<ReceptacleBullet>() : ModContent.ProjectileType<WhiteSpore>();
                        RandomBullet(bulletType);
                    }
                }
            }

			if (Projectile.timeLeft >= 90)
				Projectile.velocity *= .97f;

            if (Math.Abs(Projectile.velocity.X) < .2f && Math.Abs(Projectile.velocity.Y) < .2f)
                Projectile.velocity = Vector2.Zero;
        }
    }
}
