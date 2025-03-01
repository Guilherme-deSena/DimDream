using DimDream.Content.NPCs;
using DimDream.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace DimDream.Content.NPCs
{
    // Don't call this class. Use one of its child classes below it instead.
    internal class OrinFairyZombie : ModNPC
    {
        public bool Initialized { get; set; } = false;
        private bool SlowMoving { get => Math.Abs(NPC.velocity.X) > .2f && Math.Abs(NPC.velocity.X) < .6f; }
        public int Behavior { get => (int)NPC.ai[0]; }

        // This is a neat trick that uses the fact that NPCs have all NPC.ai[] values set to 0f on spawn (if not otherwise changed).
        // We set ParentIndex to a number in the body after spawning it. If we set ParentIndex to 3, NPC.ai[0] will be 4. If NPC.ai[0] is 0, ParentIndex will be -1.
        // Now combine both facts, and the conclusion is that if this NPC spawns by other means (not from the body), ParentIndex will be -1, allowing us to distinguish
        // between a proper spawn and an invalid/"cheated" spawn
        public int ParentIndex
        {
            get => (int)NPC.ai[3] - 1;
            set => NPC.ai[3] = value + 1;
        }

        public bool HasParent => ParentIndex > -1;

        public float Counter
        {
            get => NPC.localAI[3];
            set => NPC.localAI[3] = value;
        }

        public static int ParentType()
        {
            return ModContent.NPCType<OrinBossHumanoid>();
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 16;

            NPCID.Sets.DontDoHardmodeScaling[Type] = true;
            NPCID.Sets.CantTakeLunchMoney[Type] = true;

            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;

            NPCID.Sets.BossBestiaryPriority.Add(Type);
            // Optional: If you don't want this NPC to show on the bestiary (if there is no reason to show a boss minion separately)
            // Make sure to remove SetBestiary code as well
            // NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new NPCID.Sets.NPCBestiaryDrawModifiers() {
            //	Hide = true // Hides this NPC from the bestiary
            // };
            // NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
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

            float maxSpeed = .2f;
            NPC.velocity = new(Main.rand.NextFloat(-maxSpeed, maxSpeed), Main.rand.NextFloat(-maxSpeed, maxSpeed));
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            // Makes it so whenever you beat the boss associated with it, it will also get unlocked immediately
            int associatedNPCType = ParentType();
            bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[associatedNPCType], quickUnlock: true);

            bestiaryEntry.Info.AddRange([
                new MoonLordPortraitBackgroundProviderBestiaryInfoElement(), // Plain black background
				new FlavorTextBestiaryInfoElement("A vengeful spirit from the Underworld. Orin seems particularly good at making them do her bidding.")
            ]);
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
            Counter = 0;

            NPC.netUpdate = true;
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
            Despawn();

            Counter++;

            if (Counter == 1 && NPC.dontTakeDamage)
                NPC.velocity = new(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-9f, 9f));
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
                    int type = ModContent.NPCType<OrinFairyZombieGhostly>();
                    var entitySource = parent.GetSource_FromAI();
                    OrinFairyZombieGhostly fairy = (OrinFairyZombieGhostly)NPC.NewNPCDirect(entitySource, (int)NPC.Center.X, (int)NPC.Center.Y, type, NPC.whoAmI).ModNPC;
                    fairy.ParentIndex = ParentIndex;
                    fairy.Counter = -1;

                    NPC.active = false;
                    NPC.life = 0;
                    NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                    return true;
                }

                if (!HasParent || (parent.dontTakeDamage && boss.StageHelper >= 2) || !Main.npc[ParentIndex].active || Main.npc[ParentIndex].type != ParentType())
                {
                    NPC.active = false;
                    NPC.life = 0;
                    NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                    return true;
                }
            }
            return false;
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

    internal class OrinFairyZombieGhostly : OrinFairyZombie
    {
        public override string Texture => "DimDream/Content/NPCs/OrinFairyZombieGhostly";
        public override void AI()
        {
            // This should almost always be the first code in AI() as it is responsible for finding the proper player target
            Player player = Main.player[NPC.target];
            if (NPC.target < 0 || NPC.target == 255 || player.dead || !player.active || Vector2.Distance(NPC.Center, player.Center) > 3000f)
                NPC.TargetClosest();

            player = Main.player[NPC.target];

            if (Despawn())
                return;

            Counter++;
            if (Counter == 0)
            {
                NPC.dontTakeDamage = true;
                NPC.life = 2;
                NPC.velocity = Vector2.Zero;

                NPC.netUpdate = true;
            }
            else if (Counter == 1 && NPC.life == 1)
            {
                ShootSpores(12, NPC.Center);
                NPC.velocity = new(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-9f, 9f));
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
                (!HasParent || (parent.dontTakeDamage && parent.localAI[2] >= 1) || !Main.npc[ParentIndex].active || Main.npc[ParentIndex].type != ParentType()))
            {
                NPC.life = 0;
                NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
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

                Projectile.NewProjectile(entitySource, position, velocity * speed, type, NPC.damage, 0f, Main.myPlayer, ai2: ParentIndex);
            }
        }
    }
}
