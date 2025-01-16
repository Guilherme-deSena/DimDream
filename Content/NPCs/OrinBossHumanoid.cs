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
using ReLogic.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.ItemDropRules;
using DimDream.Content.Items.Weapons;
using DimDream.Content.Items.Consumables;
using System.Diagnostics.Metrics;
using DimDream.Content.BossBars;
using Microsoft.CodeAnalysis.Text;

namespace DimDream.Content.NPCs
{
    [AutoloadBossHead] // This attribute looks for a texture called "ClassName_Head_Boss" and automatically registers it as the NPC boss head icon
    internal class OrinBossHumanoid : ModNPC
    {
        private int AnimationCount { get; set; } = 0;
        private bool Casting { get; set; } = false;
        private int AnimationDirection { get; set; } = 1;
        private bool Initialized { get; set; } = false;
        private int Inverter { get; set; } = 1;
        private Vector2 ArenaCenter { get; set; }
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
        private static float Pi { get => MathHelper.Pi; } // Shorter way to write Pi because I'm too lazy to write Mathhelper everytime
        private int Stage
        { // Stage is decided by the boss' health percentage
            get
            {
                if (StageHelper <= 0)
                    return 0;

                if (StageHelper <= 1)
                    return 1;

                if (NPC.life > NPC.lifeMax * .8f)
                    return 2;

                if (NPC.life > NPC.lifeMax * .5f)
                    return 3;

                return 4;
            }
        }
        private int StageLoopCount
        {
            get => (int)NPC.localAI[1];
            set => NPC.localAI[1] = value;
        }
        private int StageHelper // Checked to prevent starting a stage during a pattern, amidst other things
        {
            get => (int)NPC.localAI[2];
            set => NPC.localAI[2] = value;
        }
        private int RevivingIntoStage { get; set; } = 0;
        private float SavedRandom { get; set; } = 0; // Used to keep a random float between frames
        private Vector2 SavedPosition { get; set; } // Used to keep a position between frames
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
        private int Counter
        {
            get => (int)NPC.localAI[3];
            set => NPC.localAI[3] = value;
        }


        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 27;
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
            NPC.lifeMax = Main.expertMode ? 5000 : 8000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = Item.buyPrice(gold: 20);
            NPC.SpawnWithHigherTime(30);
            NPC.boss = true;
            NPC.npcSlots = 10f;
            NPC.BossBar = ModContent.GetInstance<OrinBossBar>();
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
				new FlavorTextBestiaryInfoElement("Yumemi, Captain of the Probability Space Hypervessel, looking to test the magical powers in the world of Terraria so she can write her thesis.")
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

        public override bool CheckDead()
        {
            if (StageHelper < 10)
            {
                StageHelper = 10;
                NPC.life = 1;
                NPC.dontTakeDamage = true;
                NPC.lifeMax = 60000;
                return false;
            }

            return true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Stage == 4)
            {
                int timeFactor = StageLoopCount > 0 ? 999 : Counter + 100;
                DrawSpellName(spriteBatch, "Cat Sign \"Cat's Walk\"", timeFactor);
            }

            //DrawNPC(spriteBatch, new(Main.miniMapX - Main.miniMapWidth / 2 - 400, Main.miniMapY));

            return true;
        }

        public void DrawSpellName(SpriteBatch spriteBatch, string spellName, int timeFactor)
        {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float textSize = font.MeasureString(spellName).X;
            float heightOffset = timeFactor < 100 ? 500 : MathF.Max(0, 500 - (timeFactor - 100) * 15);
            Vector2 position = new(Main.miniMapX - Main.miniMapWidth / 2 - (textSize + 50), Main.miniMapY + heightOffset);

            spriteBatch.DrawString(font, spellName, position, Color.White);
        }

        public void DrawNPC(SpriteBatch spriteBatch, Vector2 position)
        {
            Texture2D npcTexture = TextureAssets.Npc[NPC.type].Value;
            int frameHeight = npcTexture.Height / Main.npcFrameCount[NPC.type];
            Rectangle sourceRectangle = new Rectangle(0, 0, npcTexture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;

            spriteBatch.Draw(
                npcTexture,
                position,
                sourceRectangle,
                Color.White,
                0,
                origin,
                NPC.scale,
                SpriteEffects.None,
                0f);


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
                NPC.spriteDirection = NPC.velocity.X < 0 ? -1 : 1;

            if (NPC.dontTakeDamage && StageHelper > 0 && NPC.life < NPC.lifeMax)
                Revive(RevivingIntoStage);
            else
                switch (Stage)
                {
                    case 0:
                        Stage0(player); break;
                    case 1:
                        Stage1(player); break;
                    case 2:
                        Stage2(player); break;
                    case 3:
                        Stage3(player); break;
                    case 4:
                        Stage4(player); break;
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

            int firstMoving = 24 * frameHeight;
            int firstCasting = 13 * frameHeight;
            int firstBlinking = 8 * frameHeight;
            int lastFullCasting = 20 * frameHeight;
            int lastFrame = 26 * frameHeight;

            if (NPC.frameCounter >= frameSpeed)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight * AnimationDirection;

                if (Moving)
                {
                    AnimationDirection = 1;
                    if (NPC.frame.Y < firstMoving)
                        NPC.frame.Y = firstMoving;
                    if (NPC.frame.Y > lastFrame)
                        NPC.frame.Y = lastFrame;
                }
                else if (Casting)
                {
                    if (NPC.frame.Y < firstCasting)
                    {
                        NPC.frame.Y = lastFullCasting + frameHeight;
                        AnimationDirection = 1;
                    }
                    else if (NPC.frame.Y == firstCasting || NPC.frame.Y == lastFullCasting)
                        AnimationDirection *= -1;
                    else if (NPC.frame.Y >= firstMoving)
                        NPC.frame.Y = firstCasting + 3 * frameHeight;
                }
                else if (AnimationCount >= 6 && AnimationDirection == 1)
                {
                    if (NPC.frame.Y < firstBlinking)
                        NPC.frame.Y = firstBlinking;
                    
                    if (NPC.frame.Y > firstCasting - frameHeight)
                    {
                        NPC.frame.Y = 5 * frameHeight;
                        AnimationCount = 0;
                    }
                }
                else if (NPC.frame.Y >= firstBlinking - frameHeight || NPC.frame.Y <= 0)
                {
                    AnimationDirection *= -1;
                    AnimationCount++;

                    if (NPC.frame.Y < firstMoving && NPC.frame.Y >= firstCasting)
                    {
                        AnimationDirection = -1;
                        if (NPC.frame.Y == lastFullCasting && AnimationDirection == -1)
                            NPC.frame.Y = frameHeight * 2;
                        else if (NPC.frame.Y <= lastFullCasting)
                            NPC.frame.Y = firstMoving - frameHeight;
                    }
                    else if (NPC.frame.Y >= firstCasting)
                    {
                        AnimationDirection = 1;
                        NPC.frame.Y = frameHeight;
                    }
                }
            }
        }

        public void PrePatternDust(int distance)
        {
            int dustCount = 2;
            for (int i = 0; i < dustCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 position = new(NPC.Center.X + MathF.Sin(angle) * distance, NPC.Center.Y - MathF.Cos(angle) * distance);
                float speed = 20f;
                Vector2 velocity = new(MathF.Sin(angle), -MathF.Cos(angle));
                int type = 285;

                Dust d = Dust.NewDustPerfect(position, type, -velocity * speed, 100, default, 4f);
                d.noGravity = true;
            }
        }

        public void ArenaDust(Vector2 arenaCenter, int arenaRadius)
        {
            int dustCount = 5;
            for (int i = 0; i < dustCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
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

        public void SpawnCircleSpirit(Vector2 position)
        {
            int type = ModContent.NPCType<OrinEvilSpiritRed>();
            var entitySource = NPC.GetSource_FromAI();

            OrinEvilSpiritRed spirit = (OrinEvilSpiritRed)NPC.NewNPCDirect(entitySource, (int)position.X, (int)position.Y, type, NPC.whoAmI).ModNPC;
            spirit.ParentIndex = NPC.whoAmI;
        }

        public void SpawnBurstSpirits(int distance, float offset, int spiritCount, int timeLeft)
        {
            for (int i = 0; i < spiritCount; i++)
            {
                float angle = offset + MathHelper.TwoPi / spiritCount * i;
                Vector2 position = new(NPC.Center.X + MathF.Sin(angle) * distance, NPC.Center.Y - MathF.Cos(angle) * distance);
                Vector2 velocity = new Vector2(0, -1).RotatedBy(angle);
                float speed = .1f;
                int type = ModContent.NPCType<OrinEvilSpiritBlue>();
                var entitySource = NPC.GetSource_FromAI();

                NPC spirit = NPC.NewNPCDirect(entitySource, position, type, NPC.whoAmI, timeLeft);
                spirit.velocity = -velocity * speed;

                OrinEvilSpiritBlue s = (OrinEvilSpiritBlue)spirit.ModNPC;
                s.ParentIndex = NPC.whoAmI;
            }
        }

        public void Circle(Vector2 center, float distance, float offset, float speed, int count, int type, int timeLeft = -1, int color = 0)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = offset + MathHelper.TwoPi / count * i;
                Vector2 positionOffset = new(center.X + MathF.Sin(angle) * distance, center.Y - MathF.Cos(angle) * distance);
                Vector2 velocity = new Vector2(0, -1).RotatedBy(angle);

                var entitySource = NPC.GetSource_FromAI();
                int damage = ProjDamage;

                Projectile p = Projectile.NewProjectileDirect(entitySource, positionOffset, velocity * speed, type, damage, 0f, Main.myPlayer, 1f, color, NPC.whoAmI);

                if (timeLeft >= 0)
                    p.timeLeft = timeLeft;
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

        public void AlternatingJump(int side)
        {
            Vector2 direction = new(500 * side, 100);

            Destination = NPC.Center + direction;
            NPC.netUpdate = true;
        }

        public void RunFromArena(int side)
        {
            Vector2 direction = new(3000 * side, 0);

            Destination = ArenaCenter + direction;
            NPC.netUpdate = true;
        }
        /*
        public void AltJump()
        {
            Vector2 dest = new Vector2(Main.rand.Next(-2, 2) * 300, -350 + Main.rand.Next(100)) + ArenaCenter;
            Vector2 toDestination = dest - NPC.Center;
            Vector2 toDestinationNormalized = toDestination.SafeNormalize(Vector2.UnitY);
            float travelDistance = Math.Min(Main.rand.Next(400, 500), Vector2.Distance(dest, NPC.Center));

            Destination = toDestinationNormalized * travelDistance + NPC.Center;
            NPC.netUpdate = true;
        }*/

        public void Revive(int stage)
        {
            NPC.life = Math.Min(NPC.life + NPC.lifeMax / 100, NPC.lifeMax);

            if (NPC.life >= NPC.lifeMax)
            {
                NPC.dontTakeDamage = false;
                Counter = 0;
            }
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
                        if (otherNPC.type == ModContent.NPCType<OrinEvilSpiritRed>())
                            spiritCount++;
                    }

                    /*int projectileCount = 0;
                    foreach (var proj in Main.ActiveProjectiles)
                    {
                        projectileCount++;
                    }
                    Main.NewText($"Projectiles: {projectileCount}, spirits: {spiritCount}");*/

                    if (spiritCount < 12)
                        for (int i = 0; i < 3; i++)
                        {
                            float angle = MathHelper.TwoPi / 3 * i;
                            Vector2 position = new Vector2(MathF.Sin(angle) * 50, MathF.Cos(angle) * 50) + NPC.Center;
                            SpawnCircleSpirit(position);
                        }
                }

                if (Counter >= 1000)
                    RunFromArena(1);
            }

            if (!NPC.dontTakeDamage)
                NPC.dontTakeDamage = true;

            if (Counter >= 1100)
                StageHelper = 1;
        }

        public void Stage1(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Counter == 1)
                    GoToDefaultPosition();

                if (Counter > 400 && Counter % 10 == 0 && Counter < 900)
                {
                    float angle = Counter <= 500 ? 0 : MathF.Sin(Counter - 500) + Main.rand.NextFloat(Pi / 20);
                    int distance = (int)Math.Abs(650 - Counter);
                    int type = ModContent.ProjectileType<Rice>();

                    Circle(NPC.Center, distance, angle, .5f, 16, type);

                    angle = MathF.Sin(Counter - 400) + Main.rand.NextFloat(Pi / 20);
                    if (Counter < 650 && Counter % 20 == 0)
                        SpawnBurstSpirits(distance, angle, 5, 1200 - Counter);
                }
            }

            if (Counter == 200)
                Casting = true;

            if (Counter == 900)
                Casting = false;

            if (Counter > 300 && Counter < 405)
                PrePatternDust(500 - Counter % 300 * 5);

            if (Counter >= 1080)
            {
                Counter = 0;
                NPC.dontTakeDamage = false;
            }
        }


        public void Stage2(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Counter == 1)
                    GoToDefaultPosition();

                if (Counter >= 200 && Counter % 5 == 0 && Counter <= 320)
                {
                    int start = Counter - 200;
                    float distance = start / 2;
                    int timeLeft = 550 - start * 3;
                    float offset = Pi / 200 * Math.Abs(60 - start) * Inverter;
                    int type = ModContent.ProjectileType<Diamond>();

                    Circle(NPC.Center, distance, offset, .01f, 20, type, timeLeft);
                }
            }

            if (Counter >= 320)
            {
                Counter = 0;
                Inverter *= -1;
            }
        }

        public void Stage3(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && StageHelper >= 11)
            {
                if (Counter == 1)
                    GoToDefaultPosition();

                if (Counter >= 50 && Counter % 5 == 0 && Counter <= 175)
                {
                    int start = Counter - 50;
                    float distance = start;
                    int timeLeft = 550 - start * 3;
                    float speed = .01f;
                    int count = Counter < 150 ? 15 : 30;
                    int type = ModContent.ProjectileType<Diamond>();
                    SavedRandom += Counter < 150 ? Main.rand.NextFloat(-Pi / 30, Pi / 30) + Pi / 100 * Inverter
                        : Pi / 100 * -Inverter;

                    Circle(NPC.Center, distance, SavedRandom, speed, count, type, timeLeft, 1);
                }
            }

            if (StageHelper < 11)
            {
                StageHelper = 11;
                Counter = 0;
                Inverter = 1;
            }

            if (Counter >= 300)
            {
                SavedRandom = 0;
                Counter = 0;
                Inverter *= -1;
            }
        }

        public void Stage4(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && StageHelper >= 12)
            {
                if (Counter <= 1)
                    GoToDefaultPosition();

                if (Counter % 60 == 0 && Counter <= 420)
                {
                    AlternatingJump(Inverter);
                    if (Counter % 120 == 60)
                        Inverter *= -1;
                }

                if (Counter >= 120 && Counter % 10 == 0 && Counter % 60 <= 30 && Counter <= 520)
                {
                    if (Counter % 60 == 0)
                        SavedPosition = new(NPC.Center.X, NPC.Center.Y);

                    int start = Counter % 60;
                    float distance = 50 + start * 5;
                    int timeLeft = 550 - start;
                    float speed = .01f;
                    int count = 18;
                    int type = ModContent.ProjectileType<Diamond>();
                    SavedRandom += Main.rand.NextFloat(-Pi / 30, Pi / 30) + Pi / 100 * Inverter;

                    Circle(SavedPosition, distance, SavedRandom, speed, count, type, timeLeft, 2);
                }

                if (Counter == 480)
                {
                    Inverter *= -1;
                    RunFromArena(Inverter);
                }

                if (Counter >= 640)
                    GoToDefaultPosition();
            }

            if (StageHelper < 12)
            {
                StageHelper = 12;
                StageLoopCount = 0;
                Counter = -100;
                Inverter = -1;
            }

            if (Counter >= 1100)
            {
                StageLoopCount++;
                Counter = 0;
            }
        }
    }
}