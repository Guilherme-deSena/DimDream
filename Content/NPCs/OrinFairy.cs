using DimDream.Content.NPCs;
using DimDream.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace DimDream.Content.NPCs
{
    // Don't call this class. Use one of its child classes below it instead.
    internal class OrinFairy : ModNPC
    {
        public bool Initialized { get; set; } = false;
        private bool SlowMoving { get => Math.Abs(NPC.velocity.X) > .2f && Math.Abs(NPC.velocity.X) < .6f; }

        public int ParentIndex
        {
            get => (int)NPC.ai[3] - 1;
            set => NPC.ai[3] = value + 1;
        }

        public bool HasParent => ParentIndex > -1;
        public int ParentStageHelper { get; set; }

        public float Counter
        {
            get => NPC.localAI[3];
            set => NPC.localAI[3] = value;
        }

        public int ProjDamage
        {
            get
            {
                if (Main.masterMode)
                    return NPC.damage / 4;

                if (Main.expertMode)
                    return NPC.damage / 2;

                return NPC.damage;
            }
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 16;

            NPCID.Sets.DontDoHardmodeScaling[Type] = true;
            NPCID.Sets.CantTakeLunchMoney[Type] = true;

            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;

            NPCID.Sets.BossBestiaryPriority.Add(Type);

            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Hide = true // Hides this NPC from the bestiary
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
        }

        public override void SetDefaults()
        {
            NPC.width = 52;
            NPC.height = 44;
            NPC.damage = 7;
            NPC.defense = 20;
            NPC.lifeMax = 500;
            NPC.HitSound = SoundID.NPCHit5;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.netAlways = true;
            NPC.aiStyle = -1;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = ImmunityCooldownID.Bosses; // use the boss immunity cooldown counter, to prevent ignoring boss attacks by taking damage from other sources
            return true;
        }


        public override void OnKill()
        {
            SpawnDust(NPC.Center);
        }

        public override bool CheckDead()
        {
            NPC.dontTakeDamage = true;
            NPC.life = 1;
            NPC.velocity = Vector2.Zero;
            Counter = 1;

            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            int frameSpeed = 6;
            NPC.frameCounter++;

            int firstMoving = 8 * frameHeight;
            int lastFrame = 14 * frameHeight;
            int deadFrame = 15 * frameHeight;

            if (NPC.dontTakeDamage)
                NPC.frame.Y = deadFrame;
            else if (SlowMoving)
                switch (NPC.velocity.X)
                {
                    case > .4f:
                        NPC.frame.Y = firstMoving + frameHeight;
                        break;
                    case > .2f:
                        NPC.frame.Y = firstMoving + frameHeight * 2;
                        break;
                    default:
                        NPC.frame.Y = firstMoving + frameHeight * 3;
                        break;
                }
            else if (NPC.frameCounter >= frameSpeed)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;

                if (Math.Abs(NPC.velocity.X) > .5f)
                {
                    if (NPC.frame.Y < firstMoving)
                        NPC.frame.Y = firstMoving;
                    if (NPC.frame.Y > lastFrame)
                        NPC.frame.Y = firstMoving + 2 * frameHeight;
                }
                else if (NPC.frame.Y >= firstMoving - 1)
                {
                    if (NPC.frame.Y > lastFrame || NPC.frame.Y == firstMoving)
                        NPC.frame.Y = frameHeight;
                    else
                        NPC.frame.Y -= frameHeight * 2;
                }
            }
        }

        public override void AI()
        {
            if (!Initialized)
            {
                Initialized = true;
                ParentStageHelper = (int)Main.npc[ParentIndex].localAI[2];
            }

            Despawn();

            Counter++;

            if (Counter == 2 && NPC.dontTakeDamage && Main.netMode != NetmodeID.MultiplayerClient)
            {
                MoveOnDeath();
                NPC.netUpdate = true;
            }
            else if (Counter > 60 && NPC.velocity != Vector2.Zero)
            {
                NPC.velocity *= .95f;
                if (Math.Abs(NPC.velocity.X) < .1f && Math.Abs(NPC.velocity.Y) < .1f)
                    NPC.velocity = Vector2.Zero;
            }

            if (NPC.dontTakeDamage)
            {
                NPC.velocity *= .95f;
                if (Math.Abs(NPC.velocity.X) < .1f && Math.Abs(NPC.velocity.Y) < .1f)
                    NPC.velocity = Vector2.Zero;
            }

            NPC.friendly = NPC.dontTakeDamage;
            NPC.alpha = NPC.dontTakeDamage ? 150 : 0;
            NPC.spriteDirection = NPC.velocity.X < 0 ? 1 : -1;
        }

        public virtual bool Despawn()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC parent = Main.npc[ParentIndex];
                OrinBossHumanoid boss = (OrinBossHumanoid)parent.ModNPC;
                if (boss.StageHelper == 1)
                {
                    int type = ModContent.NPCType<OrinFairyZombieSpores>();
                    var entitySource = parent.GetSource_FromAI();
                    NPC npc = NPC.NewNPCDirect(entitySource, (int)NPC.Center.X, (int)NPC.Center.Y, type, NPC.whoAmI);
                    npc.damage = NPC.damage;
                    OrinFairyZombieSpores fairy = (OrinFairyZombieSpores)npc.ModNPC;
                    fairy.ParentIndex = ParentIndex;

                    NPC.active = false;
                    NPC.life = 0;
                    NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                    return true;
                }

                if (!HasParent || !Main.npc[ParentIndex].active)
                {
                    NPC.active = false;
                    NPC.life = 0;
                    NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                    return true;
                }
            }
            return false;
        }

        public void MoveOnDeath()
        {
            NPC.velocity = new(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-18f, 18f));
        }

        public void SpawnDust(Vector2 position)
        {
            int dustCount = 6;
            for (int i = 0; i < dustCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float speed = 5f;
                Vector2 velocity = new(MathF.Sin(angle), -MathF.Cos(angle));
                int type = 245;

                Dust.NewDustPerfect(position, type, -velocity * speed, 100, default, 2f);
            }
        }
    }

    internal class OrinFairyZombieSpores : OrinFairy
    {
        public override string Texture => "DimDream/Content/NPCs/OrinFairyZombie";
        public override void AI()
        {
            // This should almost always be the first code in AI() as it is responsible for finding the proper player target
            Player player = Main.player[NPC.target];
            if (NPC.target < 0 || NPC.target == 255 || player.dead || !player.active || Vector2.Distance(NPC.Center, player.Center) > 3000f)
                NPC.TargetClosest();

            player = Main.player[NPC.target];

            if (!Initialized)
            {
                Initialized = true;
                ParentStageHelper = (int)Main.npc[ParentIndex].localAI[2];
            }

            if (Despawn())
                return;

            Counter++;
            if (Counter == 1)
            {
                NPC.dontTakeDamage = true;
                NPC.life = 2;
                NPC.velocity = Vector2.Zero;

                NPC.netUpdate = true;
            }
            else if (Counter == 2 && NPC.life == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                ShootSpores(12, NPC.Center);
                MoveOnDeath();
                NPC.netUpdate = true;
            }
            else if (Counter >= 60 && Counter % 6 == 0 && NPC.dontTakeDamage)
                CreateDust();
            else if (Counter >= 180 && NPC.dontTakeDamage)
                Revive();


            if (!NPC.dontTakeDamage)
            {
                Vector2 toPlayer = player.Center - NPC.Center;
                float angle = toPlayer.SafeNormalize(Vector2.UnitY).ToRotation();
                float speed = 2.5f;
                NPC.velocity = new(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
            } else if (NPC.velocity != Vector2.Zero)
            {
                NPC.velocity *= .95f;
                if (Math.Abs(NPC.velocity.X) < .1f && Math.Abs(NPC.velocity.Y) < .1f)
                    NPC.velocity = Vector2.Zero;
            }

            NPC.friendly = NPC.dontTakeDamage;
            NPC.alpha = NPC.dontTakeDamage ? 150 : 0;
            NPC.spriteDirection = NPC.velocity.X < 0 ? 1 : -1;
        }

        public override bool Despawn()
        {
            NPC parent = Main.npc[ParentIndex];

            if (Main.netMode != NetmodeID.MultiplayerClient &&
                ((NPC.alpha >= 255 && parent.localAI[2] > ParentStageHelper) ||
                    !HasParent ||
                    !Main.npc[ParentIndex].active))
            {
                NPC.life = 0;
                NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);

                return true;
            }

            if (parent.localAI[2] > ParentStageHelper)
            {
                NPC.alpha += 15;

                return true;
            }

            return false;
        }

        public void CreateDust()
        {
            int dustType = 264;

            Vector2 velocity = NPC.velocity + new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.2f));
            Dust dust = Dust.NewDustPerfect(NPC.Center, dustType, velocity, 26, Color.White, Main.rand.NextFloat(1f, 2f));

            dust.noGravity = true;
            dust.fadeIn = Main.rand.NextFloat(0.3f, 0.8f);
        }

        public void Revive()
        {
            NPC.dontTakeDamage = false;
            NPC.life = NPC.lifeMax;
        }

        public void ShootSpores(int sporeCount, Vector2 position)
        {
            for (int i = 0; i < sporeCount; i++)
            {
                Vector2 velocity = new Vector2(1, 0).RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
                float speed = Main.rand.NextFloat(1.5f, 3f);
                var entitySource = NPC.GetSource_FromAI();
                int type = ModContent.ProjectileType<BasicWhiteSpore>();

                Projectile.NewProjectile(entitySource, position, velocity * speed, type, ProjDamage, 0f, Main.myPlayer, ai2: ParentIndex);
            }
        }
    }
    
    internal class OrinFairyZombieBalls : OrinFairyZombieSpores
    {
        public float StartCounter
        {
            get => NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public bool IsGonnaThrowDonuts
        {
            get => NPC.localAI[0] > 0;
            set => NPC.localAI[0] = value ? 1 : 0;
        }
        public override void AI()
        {
            // This should almost always be the first code in AI() as it is responsible for finding the proper player target
            Player player = Main.player[NPC.target];
            if (NPC.target < 0 || NPC.target == 255 || player.dead || !player.active || Vector2.Distance(NPC.Center, player.Center) > 3000f)
                NPC.TargetClosest();

            player = Main.player[NPC.target];

            if (!Initialized)
            {
                Initialized = true;
                NPC.dontTakeDamage = true;
                Counter = StartCounter;
                ParentStageHelper = (int)Main.npc[ParentIndex].localAI[2];
            }

            if (Despawn())
                return;

            Counter++;

            if (Counter == 2 && NPC.life == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                MoveOnDeath();
                NPC.netUpdate = true;
            } else if (Counter >= 60 && Counter % 6 == 0 && NPC.dontTakeDamage)
            {
                CreateDust();
            } else if (Counter >= 180 && NPC.dontTakeDamage)
            {
                Revive();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float angle = (player.Center - NPC.Center).Y > 0 ? MathHelper.Pi : 0;
                    ThrowSmallBall(NPC.Center, angle, 4f, 120);
                }
            } else if (Counter >= 500 && IsGonnaThrowDonuts)
            {
                Counter = -100;
                IsGonnaThrowDonuts = false;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float offset = Main.rand.NextFloat(MathHelper.TwoPi);
                    ThrowDonuts(NPC.Center, 8, 4f, offset);
                    NPC.netUpdate = true;
                }
            }

            if (Counter > 500 && (player.Center - NPC.Center).Length() < 450)
            {
                Counter = 460;
                IsGonnaThrowDonuts = true;
            }

            if (!NPC.dontTakeDamage && !IsGonnaThrowDonuts && Counter > 1)
            {
                Vector2 toPlayer = player.Center - NPC.Center;
                float angle = toPlayer.SafeNormalize(Vector2.UnitY).ToRotation();
                float speed = 2.5f;
                NPC.velocity = new(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
            }
            else if (NPC.velocity != Vector2.Zero)
            {
                NPC.velocity *= .95f;
                if (Math.Abs(NPC.velocity.X) < .1f && Math.Abs(NPC.velocity.Y) < .1f)
                    NPC.velocity = Vector2.Zero;
            }


            NPC.friendly = NPC.dontTakeDamage;
            NPC.alpha = NPC.dontTakeDamage ? 150 : 0;
            NPC.spriteDirection = NPC.velocity.X < 0 ? 1 : -1;
        }

        public void ThrowSmallBall(Vector2 position, float angle, float maxSpeed, int frameToSpeedUp)
        {
            Vector2 velocity = new Vector2(0, -1).RotatedBy(angle);
            float speed = .01f;
            var entitySource = NPC.GetSource_FromAI();
            int type = ModContent.ProjectileType<SpeedUpLargeBallBlue>();

            Projectile.NewProjectile(entitySource, position, velocity * speed, type, ProjDamage, 0f, Main.myPlayer, maxSpeed, frameToSpeedUp, ai2: ParentIndex);
        }

        public void ThrowDonuts(Vector2 position, int count, float speed, float offset)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = offset + MathHelper.TwoPi / count * i;
                Vector2 velocity = new Vector2(0, -1).RotatedBy(angle);

                var entitySource = NPC.GetSource_FromAI();
                int type = ModContent.ProjectileType<BasicDonutRed>();

                Projectile.NewProjectile(entitySource, position, velocity * speed, type, ProjDamage, 0f, Main.myPlayer, ai2: ParentIndex);
            }
        }
    }
}
