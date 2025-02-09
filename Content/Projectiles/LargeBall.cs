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
    internal class LargeBallRed : ModProjectile
    {
        public override string Texture => "DimDream/Content/Projectiles/LargeBallRed";
        private float Behavior // 1 for speed up, 2 for slow down
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
            Projectile.width = 10;
            Projectile.height = 10;
            DrawOffsetX = -5;
            DrawOriginOffsetY = -5;
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
            else if (Behavior == 2f && Counter == 6)
            {
                Projectile.velocity /= 2;
            }

        // If the sprite points upwards, this will make it point towards the move direction (for other sprite orientations, change MathHelper.PiOver2)
        Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = Projectile.direction;
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
    }

    internal class LargeBallBlue : LargeBallRed
    {
        public override string Texture => "DimDream/Content/Projectiles/LargeBallBlue";
    }
}
