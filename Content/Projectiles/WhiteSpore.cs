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

		int ColorValue { get => (int)Projectile.ai[1]; }
		public Color Color { get => ColorValue == 0 ? Color.White : 
									new Color((byte)((ColorValue >> 16) & 0xFF), // Red
											  (byte)((ColorValue >> 8) & 0xFF),  // Green
											  (byte)(ColorValue & 0xFF),         // Blue
											  (byte)(ColorValue >> 24) & 0xFF);  // Alpha
        }

		public bool ShouldAccelerate { get => Projectile.ai[0] == 1f; }

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
			Projectile.timeLeft = 600;
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
			return Color * Projectile.Opacity;
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

			/*
			if (ShouldAccelerate)
				Projectile.velocity = Math.Abs(Projectile.velocity.X) < Math.Abs(initialVelocity.X) * 10
									  || Math.Abs(Projectile.velocity.Y) < Math.Abs(initialVelocity.Y) * 10 ?
									  Projectile.velocity * 1.5f :
									  initialVelocity * 10;*/

			// If the sprite points upwards, this will make it point towards the move direction (for other sprite orientations, change MathHelper.PiOver2)
			Projectile.rotation = Projectile.velocity.ToRotation();
        }
	}

    internal class OrinWhiteSpore : ModProjectile
    {
        public override string Texture => "DimDream/Content/Projectiles/WhiteSpore";
        private float Behavior // Set to 1 when creating the proj for speed up behavior
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        public int ParentIndex
        {
            get => (int)Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }
        public bool HasParent => ParentIndex > -1;
        public int ParentStageHelper { get; set; }

        private bool FadedIn
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        private bool Initialized
        {
            get => Projectile.localAI[1] == 1f;
            set => Projectile.localAI[1] = value ? 1f : 0f;
        }

        private int Counter
        {
            get => (int)Projectile.localAI[2];
            set => Projectile.localAI[2] = value;
        }


        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            DrawOffsetX = -4;
            DrawOriginOffsetY = -4;
            Projectile.alpha = 255;
            Projectile.timeLeft = 350;
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
            if (!Initialized)
            {
                Initialized = true;
                ParentStageHelper = (int)Main.npc[ParentIndex].localAI[2];
                SoundEngine.PlaySound(SoundID.Item8, Projectile.position);
            }

            Despawn();

            FadeInAndOut();
            Counter++;


            float maxSpeed = 6f;
            if (Behavior == 1f && Counter > 20 && Projectile.velocity.Length() < maxSpeed)
            {
                float acceleration = .05f;
                Projectile.velocity *= 1f + acceleration / Projectile.velocity.Length();
            }

            // If the sprite points upwards, this will make it point towards the move direction (for other sprite orientations, change MathHelper.PiOver2)
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = Projectile.direction;
        }

        public bool Despawn()
        {
            NPC parent = Main.npc[ParentIndex];
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                (!HasParent || (parent.dontTakeDamage && parent.localAI[2] >= 1) || (int)parent.localAI[2] != ParentStageHelper || !Main.npc[ParentIndex].active))
            {
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 20);
                NetMessage.SendData(MessageID.SyncProjectile, number: Projectile.whoAmI);
                return true;
            }
            return false;
        }
    }
}
