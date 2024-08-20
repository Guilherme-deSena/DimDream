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
	internal class WhiteSpore : ModProjectile
	{
		public bool initialized = false;
		public Vector2 initialVelocity;

		public bool FadedIn {
			get => Projectile.localAI[0] == 1f;
			set => Projectile.localAI[0] = value ? 1f : 0f;
		}

		public bool PlayedSpawnSound {
			get => Projectile.localAI[1] == 1f;
			set => Projectile.localAI[1] = value ? 1f : 0f;
		}

		public override void SetDefaults() {
			Projectile.width = 4;
			Projectile.height = 4;
            DrawOffsetX = -4;
            DrawOriginOffsetY = -4;
            Projectile.alpha = 255;
			Projectile.timeLeft = 1000;
			Projectile.penetrate = -1;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
			Projectile.aiStyle = -1;
			CooldownSlot = ImmunityCooldownID.Bosses; // use the boss immunity cooldown counter, to prevent ignoring boss attacks by taking damage from other sources
		}

		public override Color? GetAlpha(Color lightColor) {
			// When overriding GetAlpha, you usually want to take the projectiles alpha into account. As it is a value between 0 and 255,
			// it's annoying to convert it into a float to multiply. Luckily the Opacity property handles that for us (0f transparent, 1f opaque)
			Color color = Projectile.ai[0] > 0 ? Color.LightYellow : Color.White;
			return color * Projectile.Opacity;
		}

		private void FadeInAndOut() {
			// Fade in (we have Projectile.alpha = 255 in SetDefaults which means it spawns transparent)
			int fadeSpeed = 30;
			if (!FadedIn && Projectile.alpha > 0) {
				Projectile.alpha -= fadeSpeed;
				if (Projectile.alpha < 0) {
					FadedIn = true;
					Projectile.alpha = 0;
				}
			}
			else if (FadedIn && Projectile.timeLeft < 255f / fadeSpeed) {
				// Fade out so it aligns with the projectile despawning
				Projectile.alpha += fadeSpeed;
				if (Projectile.alpha > 255) {
					Projectile.alpha = 255;
				}
			}
		}

		public override void AI() {
			if (!initialized) {
				initialVelocity = Projectile.velocity;
				initialized = true;
			}

			FadeInAndOut();

			if (!PlayedSpawnSound) {
				PlayedSpawnSound = true;

				// Common practice regarding spawn sounds for projectiles is to put them into AI, playing sounds in the same place where they are spawned
				// is not multiplayer compatible (either no one will hear it, or only you and not others)
				SoundEngine.PlaySound(SoundID.Item8, Projectile.position);
			}

			Projectile.velocity = Math.Abs(Projectile.velocity.X) < Math.Abs(initialVelocity.X) * 10
								  || Math.Abs(Projectile.velocity.Y) < Math.Abs(initialVelocity.Y) * 10 ?
								  Projectile.velocity * 1.5f :
								  initialVelocity * 10;

			// If the sprite points upwards, this will make it point towards the move direction (for other sprite orientations, change MathHelper.PiOver2)
			Projectile.rotation = Projectile.velocity.ToRotation();
        }
	}
}
