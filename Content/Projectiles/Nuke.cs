using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using static Terraria.GameContent.Animations.Actions.Sprites;

namespace DimDream.Content.Projectiles
{
    internal class Nuke : ModProjectile
    {
        public float Scale
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
            Projectile.width = 150;
            Projectile.height = 150;
            DrawOffsetX = -35;
            DrawOriginOffsetY = -45;
            Projectile.alpha = 255;
            Projectile.timeLeft = 100;
            Projectile.friendly = false;
            Projectile.hostile = false;
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

        
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > 150)
            {
                Projectile.hostile = false;
                return false;
            } else
            {
                Projectile.hostile = true;
            }

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawOrigin = texture.Size() / 2f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // Draw projectile with calculated scale
            Main.spriteBatch.Draw(texture, drawPosition, null, Color.White * Projectile.Opacity,
                Projectile.rotation, drawOrigin, Scale, SpriteEffects.None, 0f);

            return false; // Return false to prevent default drawing
        }

        public override void AI()
        {
            if (Projectile.timeLeft > 150)
                return;

            FadeInAndOut();
            Counter++;

            if (!Initialized)
            {
                Initialized = true;
                ParentStageHelper = (int)Main.npc[ParentIndex].localAI[2];
                SoundEngine.PlaySound(SoundID.Item8, Projectile.position);
            }

            Despawn();


            Visuals();
        }

        public bool Despawn()
        {
            NPC parent = Main.npc[ParentIndex];
            if (Main.netMode != NetmodeID.MultiplayerClient && Counter > 20 &&
                (!HasParent || (parent.dontTakeDamage && parent.localAI[2] >= 1) || (int)parent.localAI[2] != ParentStageHelper || !Main.npc[ParentIndex].active))
            {
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 20);
                NetMessage.SendData(MessageID.SyncProjectile, number: Projectile.whoAmI);
                return true;
            }
            return false;
        }
        private void FadeInAndOut()
        {
            // Fade in (we have Projectile.alpha = 255 in SetDefaults which means it spawns transparent)
            int fadeSpeed = 10;
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

        private void Visuals()
        {
            FadeInAndOut();

            if (Scale < 1 && Projectile.timeLeft > 30)
                Scale += .02f;
            else
                Scale -= .02f;
        }
    }
}
