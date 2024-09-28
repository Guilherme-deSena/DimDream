using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using System.Diagnostics.Metrics;

namespace DimDream.Content.Projectiles
{
    internal class ThrownFamiliar : ModProjectile
    {
        public static float Pi { get => MathHelper.Pi; }
        public bool Initialized { get; set; } = false;
        public bool SecondPhase { get => Projectile.timeLeft < 60; }
        //public int ProjectileType { get => (int)Projectile.ai[0]; }
        public NPC ParentNPC { get => Main.npc[(int)Projectile.ai[0]]; }
        public Player Target { get => Main.player[ParentNPC.target]; }
        public Vector2 Destination
        {
            get => new(Projectile.ai[1], Projectile.ai[2]);
            set
            {
                Projectile.ai[1] = value.X;
                Projectile.ai[2] = value.Y;
            }
        }
        public bool FadedIn
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
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

        public override Color? GetAlpha(Color lightColor)
        {
            // When overriding GetAlpha, you usually want to take the projectiles alpha into account. As it is a value between 0 and 255,
            // it's annoying to convert it into a float to multiply. Luckily the Opacity property handles that for us (0f transparent, 1f opaque)
            return Color.White * Projectile.Opacity;
        }

        private void FadeInAndOut()
        {
            // Fade in (we have Projectile.alpha = 255 in SetDefaults which means it spawns transparent)
            int fadeSpeed = 15;
            if (!FadedIn && Projectile.alpha > 0)
            {
                Projectile.alpha -= fadeSpeed;
                if (Projectile.alpha <= 0)
                {
                    FadedIn = true;
                    Projectile.alpha = 0;
                }
            }
        }

        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!Initialized)
                {
                    Initialized = true;
                    Destination = Projectile.velocity;
                }
                if (Projectile.timeLeft <= 1 && Target.active && !Target.dead)
                {
                    Vector2 position = Projectile.Center;
                    Vector2 offset = new Vector2(14, 14); // Necessary to align spawning correctly
                    int type = ModContent.ProjectileType<Cross>();
                    var entitySource = Projectile.GetSource_FromAI();

                    Projectile.NewProjectile(entitySource, position + offset, Vector2.Zero, type, Projectile.damage, 0f, Main.myPlayer);
                    Projectile.Kill();
                }
            }

            Vector2 toDestination = Destination - Projectile.Center;
            Vector2 destNormalized = toDestination.SafeNormalize(Vector2.UnitY);
            float speed = 4f;
            float slowdownRange = speed * 10f;
            Projectile.velocity = toDestination.Length() < slowdownRange ?
                                  destNormalized * (toDestination.Length() / slowdownRange * speed)
                                  : destNormalized * speed;

            if (Projectile.velocity.X < .1f && Projectile.velocity.Y < .1f && Projectile.timeLeft > 60)
            { 
                Projectile.timeLeft = 60; 
            }

            if (!ParentNPC.active)
                Projectile.Kill();


            Visuals();
        }

        private void Visuals()
        {
            int frameSpeed = SecondPhase ? 5 : 20;

            //Projectile.rotation = SecondPhase ? 0 : Projectile.rotation + .01f;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (!SecondPhase && Projectile.frame >= 2)
                    Projectile.frame = 0;
                else if (SecondPhase && Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = 3;
            }

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.78f);
            FadeInAndOut();
        }
    }
}
