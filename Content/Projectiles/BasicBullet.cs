using DimDream.Content.NPCs;
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

namespace DimDream.Content.Projectiles
{
    public class BasicBullet : ModProjectile
    {
        public override string Texture => "DimDream/Content/Projectiles/ErrorTexture";
        public int ParentIndex
        {
            get => (int)Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }
        public bool HasParent => ParentIndex > -1;
        public int ParentStageHelper { get; set; }

        public bool FadedIn
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        public bool Initialized
        {
            get => Projectile.localAI[1] == 1f;
            set => Projectile.localAI[1] = value ? 1f : 0f;
        }

        public int Counter
        {
            get => (int)Projectile.localAI[2];
            set => Projectile.localAI[2] = value;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // When overriding GetAlpha, you usually want to take the projectiles alpha into account. As it is a value between 0 and 255,
            // it's annoying to convert it into a float to multiply. Luckily the Opacity property handles that for us (0f transparent, 1f opaque)
            return Color.White * Projectile.Opacity;
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
            Visuals();

            Counter++;
        }

        public virtual void Visuals()
        {
            // If the sprite points upwards, this will make it point towards the move direction (for other sprite orientations, change MathHelper.PiOver2)
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.spriteDirection = Projectile.direction;

            FadeInAndOut();
        }

        public void FadeInAndOut()
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

        public virtual void Despawn()
        {
            NPC parent = Main.npc[ParentIndex];
            Vector2 arenaCenter = new(parent.localAI[0], parent.localAI[1]);

            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft > 20 &&
                (!HasParent || (parent.dontTakeDamage && parent.localAI[2] >= 1) || (int)parent.localAI[2] != ParentStageHelper || !Main.npc[ParentIndex].active || (arenaCenter - Projectile.Center).Length() > 1000))
            {
                Projectile.timeLeft = 20;
                NetMessage.SendData(MessageID.SyncProjectile, number: Projectile.whoAmI);
            }
        }
    }

    public class BasicLargeBallBlue : BasicBullet
    {
        public override string Texture => "DimDream/Content/Projectiles/LargeBallBlue";

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
    }

    public class BasicLargeBallBlueFriendly : BasicLargeBallBlue
    {
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.alpha = 255;
            Projectile.timeLeft = 300;
            Projectile.friendly = true;
        }

        public override void Despawn() { }
    }

    public class BasicWhiteSpore : BasicBullet
    {
        public override string Texture => "DimDream/Content/Projectiles/WhiteSpore";

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
    }

    public class BasicSpiralBullet : BasicBullet
    {
        public override string Texture => "DimDream/Content/Projectiles/SpiralBullet";
        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            DrawOffsetX = -3;
            DrawOriginOffsetY = -3;
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
    }

    public class BasicSpiralBulletFriendly : BasicSpiralBullet
    {
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.alpha = 255;
            Projectile.timeLeft = 100;
            Projectile.friendly = true;
        }

        public override void Despawn() { }
    }

    public class BasicDonutRed : BasicBullet
    {
        public override string Texture => "DimDream/Content/Projectiles/DonutRed";
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            DrawOffsetX = -20;
            DrawOriginOffsetY = -20;
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
    }

    public class BasicDonutRedFriendly : BasicDonutRed
    {
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            DrawOffsetX = -35;
            DrawOriginOffsetY = -35;
            Projectile.alpha = 255;
            Projectile.timeLeft = 100;
            Projectile.friendly = true;
        }
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            int newSize = 60; // NPC hitbox size (larger than tile hitbox)
            hitbox = new Rectangle(
                (int)(Projectile.Center.X - newSize / 2),
                (int)(Projectile.Center.Y - newSize / 2),
                newSize, newSize
            );
        }
        public override void Despawn() { }
    }
}
