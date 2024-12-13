using System;
using System.Collections.Generic;
using Terraria.ID;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using DimDream.Content.Projectiles;
using DimDream.Common.Systems;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.ItemDropRules;
using DimDream.Content.Items.Weapons;
using DimDream.Content.Items.Consumables;
using System.Diagnostics.Metrics;
using ExampleMod.Content.NPCs.MinionBoss;

namespace DimDream.Content.NPCs
{
    [AutoloadBossHead] // This attribute looks for a texture called "ClassName_Head_Boss" and automatically registers it as the NPC boss head icon
    internal class OrinBoss : ModNPC
    {
        public int ShouldJumpAtFrame { get; set; } = -1;
        private bool Initialized { get; set; } = false;
        private Vector2 ArenaCenter { get; set; }
        private Vector2 AimedPosition { get; set; }
        private Vector2 Destination
        {
            get => new(NPC.ai[2], NPC.ai[3]);
            set
            {
                NPC.ai[2] = value.X;
                NPC.ai[3] = value.Y;
            }
        }
        private bool Moving { get => NPC.velocity.Length() > .5; }
        private static float Pi { get => MathHelper.Pi; }
        private int Stage
        { // Stage is decided by the boss' health percentage
            get
            {
                if (NPC.life > NPC.lifeMax * .85)
                    return 0;

                return 0;
            }
        }
        // This will be checked to prevent starting a stage mid-pattern:
        private bool[] TransitionedToStage { get; set; } = new bool[5];
        private int ProjDamage
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
        private float Counter
        {
            get => NPC.localAI[3];
            set => NPC.localAI[3] = value;
        }


        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 20;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.CantTakeLunchMoney[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 92;
            NPC.height = 78;
            NPC.damage = 45;
            NPC.defense = 22;
            NPC.lifeMax = Main.expertMode ? 27000 : 34000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = Item.buyPrice(gold: 20);
            NPC.SpawnWithHigherTime(30);
            NPC.boss = true;
            NPC.npcSlots = 10f;

            NPC.aiStyle = -1;

            if (!Main.dedServ)
            {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/StrawberryCrisis");
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // All the Classic Mode drops here are based on "not expert", meaning we use .OnSuccess() to add them into the rule, which then gets added
            LeadingConditionRule notExpertRule = new(new Conditions.NotExpert());

            // Notice we use notExpertRule.OnSuccess instead of npcLoot.Add so it only applies in normal mode
            notExpertRule.OnSuccess(ItemDropRule.OneFromOptions(1, [ModContent.ItemType<YumemisCross>(), ModContent.ItemType<RedButton>(), ModContent.ItemType<IcbmLauncher>(), ModContent.ItemType<ArcaneBombBook>()]));
            npcLoot.Add(notExpertRule);

            // Add the treasure bag using ItemDropRule.BossBag (automatically checks for expert mode)
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<YumemiBag>()));
        }

        public override void OnKill()
        {
            // This sets downedYumemiBoss to true, and if it was false before, it initiates a lantern night
            NPC.SetEventFlagCleared(ref DownedBossSystem.downedYumemiBoss, -1);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            // Sets the description of this NPC that is listed in the bestiary
            bestiaryEntry.Info.AddRange([
                new MoonLordPortraitBackgroundProviderBestiaryInfoElement(), // Plain black background
				new FlavorTextBestiaryInfoElement("Captain of the Probability Space Hypervessel, looking to test the magical powers in the world of Terraria so she can write her thesis.")
            ]);
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = ImmunityCooldownID.Bosses; // use the boss immunity cooldown counter, to prevent ignoring boss attacks by taking damage from other sources
            return true;
        }

        public override bool CheckActive()
        {
            foreach (Player player in Main.player)
            {
                if (!player.dead && Vector2.Distance(player.Center, NPC.Center) < 8000)
                    return false;
            }

            return true;
        }

        public override void AI()
        {
            // This should almost always be the first code in AI() as it is responsible for finding the proper player target
            Player player = Main.player[NPC.target];
            if (NPC.target < 0 || NPC.target == 255 || player.dead || !player.active || Vector2.Distance(NPC.Center, player.Center) > 3000f)
                NPC.TargetClosest();

            player = Main.player[NPC.target];

            if (!Initialized) // Initialize tuff that cannot be initialized in SetDefaults()
            {
                Initialized = true;
                NPC.position = player.Center + new Vector2(Main.rand.Next(-500, 500), -1000);
                ArenaCenter = player.Center;
            }

            if (player.dead)
            {
                NPC.velocity.Y -= 0.04f;
                NPC.EncourageDespawn(10);
                return;
            }


            Counter++;

            Vector2 toDestination = Destination - NPC.Center;


            float speed = 15f;
            float minSpeed = 8f;
            float slowdownRange = speed * 40f;
            Vector2 destNormalized = toDestination.SafeNormalize(Vector2.UnitY);
            Vector2 moveTo = toDestination.Length() < slowdownRange ?
                             destNormalized * MathF.Min(MathF.Max(MathF.Pow(toDestination.Length() / slowdownRange, 0.5f) * speed, minSpeed), toDestination.Length())
                             : destNormalized * speed;

            NPC.velocity = moveTo;

            if (Moving)
                NPC.spriteDirection = NPC.velocity.X < 0 ? 1 : -1;

            switch (Stage)
            {
                case 0:
                    Stage1(player); break;
            }


            int arenaRadius = 800; // Actual arena
            int fightRadius = 4000; // How far from the arena center do players have to be in order to be considered out of combat
            ArenaDust(ArenaCenter, arenaRadius);
            PullPlayers(ArenaCenter, arenaRadius, fightRadius);
        }

        public override void FindFrame(int frameHeight)
        {
            int frameSpeed = 6;
            NPC.frameCounter++;

            int firstMovingFrame = 15; // First frame in spritesheet where Orin is moving

            if (Moving)
            {
                switch (NPC.velocity.Length())
                {
                    case > 9:
                        NPC.frame.Y = frameHeight * firstMovingFrame;
                        break;
                    case > 6:
                        NPC.frame.Y = frameHeight * (firstMovingFrame + 1);
                        break;
                    default:
                        NPC.frame.Y = frameHeight * (firstMovingFrame + 2);
                        break;
                }
            } else if (NPC.frameCounter >= frameSpeed)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;

                if (NPC.frame.Y == 15 * frameHeight || NPC.frame.Y >= 20 * frameHeight)
                {
                    NPC.frame.Y = 0;
                }
            }
        }


        public void ArenaDust(Vector2 arenaCenter, int arenaRadius)
        {
            int dustCount = 5;
            for (int i = 0; i < dustCount; i++)
            {
                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                Vector2 position = arenaCenter + new Vector2(0, -arenaRadius).RotatedBy(angle);
                float speed = 5f;
                Vector2 velocity = new Vector2(0, -speed).RotatedBy(angle + Pi / 2) * Main.rand.NextFloat(.1f, 1f);
                int type = DustID.BlueFairy;

                Dust.NewDustPerfect(position, type, velocity, 100, Color.Aqua, 1.5f);
            }
        }

        public void PullPlayers(Vector2 arenaCenter, int pullDistance, int fightDistance)
        {
            float pullStrength = 12f;

            foreach (Player player in Main.player)
            {
                float distance = Vector2.Distance(arenaCenter, player.Center);
                bool isTooDistant = distance > pullDistance && distance < fightDistance;
                if (player.active && !player.dead && isTooDistant)
                {
                    Vector2 directionToArena = arenaCenter - player.Center;

                    directionToArena.Normalize();
                    directionToArena *= pullStrength;

                    player.velocity = directionToArena;
                }
            }
        }

        public void SpawnEvilSpirit(Vector2 position)
        {
            int type = ModContent.NPCType<OrinEvilSpirit>();
            var entitySource = NPC.GetSource_FromAI();

            NPC.NewNPC(entitySource, (int)position.X, (int)position.Y, type, NPC.whoAmI);
        }

        public void RiceCircle(int distance, float offset, int riceCount)
        {
            for (int i = 0; i < riceCount; i++)
            {
                float angle = offset + MathHelper.TwoPi / riceCount * i;
                Vector2 positionOffset = new(NPC.Center.X + MathF.Sin(angle) * distance, NPC.Center.Y - MathF.Cos(angle) * distance);
                Vector2 velocity = new Vector2(0, -1).RotatedBy(angle);
                float speed = .5f;

                int type = ModContent.ProjectileType<Rice>();
                var entitySource = NPC.GetSource_FromAI();
                int damage = ProjDamage;

                Projectile.NewProjectile(entitySource, positionOffset, velocity * speed, type, damage, 0f, Main.myPlayer, 1f);
            }
        }

        public void GoToDefaultPosition()
        {
            Vector2 MoveOffset = new Vector2(0, -350f);
            Destination = ArenaCenter + MoveOffset;
            NPC.netUpdate = true; // Update Destination to every client so they know where the boss should move towards
        }
        public void ShortJump()
        {
            Vector2 direction = new(400 + Main.rand.Next(150), Main.rand.Next(-50, 50));
            if (Main.rand.NextBool() == true)
                direction.X *= -1;
            if (NPC.Center.X - ArenaCenter.X + direction.X > 600 || NPC.Center.X - ArenaCenter.X + direction.X < -600)
                direction.X *= -1;

            Destination = NPC.Center + direction;
            NPC.netUpdate = true;
        }

        public void AltJump()
        {
            Vector2 dest = new Vector2(Main.rand.Next(-2, 2) * 300, -350 + Main.rand.Next(100)) + ArenaCenter;
            Vector2 toDestination = dest - NPC.Center;
            Vector2 toDestinationNormalized = toDestination.SafeNormalize(Vector2.UnitY);
            float travelDistance = Math.Min(Main.rand.Next(400, 500), Vector2.Distance(dest, NPC.Center));

            Destination = toDestinationNormalized * travelDistance + NPC.Center;
            NPC.netUpdate = true;
        }
        public void Stage0(Player player) 
        {
            if (Main.netMode != NetmodeID.MultiplayerClient) // Only server should spawn bullets and change destination
            {
                if (Counter % 101 == 1)
                {
                    if ((NPC.Center - ArenaCenter).Length() > 800)
                        GoToDefaultPosition();
                    else
                        ShortJump();
                }

                if (Counter % 101 == 60 && !Moving)
                {
                    int spiritCount = 0;
                    foreach (var otherNPC in Main.ActiveNPCs)
                    {
                        if (otherNPC.type == ModContent.NPCType<OrinEvilSpirit>())
                            spiritCount++;
                    }

                    /*int projectileCount = 0;
                    foreach (var proj in Main.ActiveProjectiles)
                    {
                        projectileCount++;
                    }
                    Main.NewText($"Projectiles: {projectileCount}, spirits: {spiritCount}");*/

                    if (spiritCount < 12)
                        for (int i = 0; i < 3;  i++)
                        {
                            float angle = MathHelper.TwoPi / 3 * i;
                            Vector2 position = new Vector2(MathF.Sin(angle) * 50, MathF.Cos(angle) * 50) + NPC.Center;
                            SpawnEvilSpirit(position);
                        }
                }
            }

            if (Counter >= 800)
                Counter = 0;
        }

        public void Stage1(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && TransitionedToStage[1])
            {
                if (Counter == 1)
                    GoToDefaultPosition();

                if (Counter > 600 & Counter % 10 == 0)
                {
                    float angle = Counter <= 700 ? 0 : MathF.Sin(Counter - 700) + Main.rand.NextFloat(Pi/20);
                    int distance = (int)Math.Abs(850 - Counter);
                    RiceCircle(distance, angle, 24);
                }
            }

            if (Counter >= 1080 || !TransitionedToStage[1])
            {
                Counter = 0;
                TransitionedToStage[1] = true;
            }
        }
    }
}