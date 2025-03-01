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
using Terraria.ModLoader.Default;

namespace DimDream.Content.NPCs
{
    [AutoloadBossHead] // This attribute looks for a texture called "ClassName_Head_Boss" and automatically registers it as the NPC boss head icon
    internal class OrinBossHumanoid : ModNPC
    {
        public int AnimationCount { get; set; } = 0;
        public bool Casting { get; set; } = false;
        public int AnimationDirection { get; set; } = 1;
        public bool Initialized { get; set; } = false;
        public int Inverter { get; set; } = 1;
        public Vector2 ArenaCenter {
            get => new(NPC.localAI[0], NPC.localAI[1]);
            set
            {
                NPC.localAI[0] = value.X;
                NPC.localAI[1] = value.Y;
            }
        }
        public Vector2 Destination
        {
            get => new(NPC.ai[2], NPC.ai[3]);
            set
            {
                NPC.ai[2] = value.X;
                NPC.ai[3] = value.Y;
            }
        }
        public bool Moving { get => NPC.velocity.Length() > .5; }
        public static float Pi { get => MathHelper.Pi; } // Shorter way to write Pi because I'm too lazy to write Mathhelper everytime
        public int Stage
        {
            get
            {
                if (NPC.life > NPC.lifeMax * .5f)
                    return 4;

                return 5;
                /*
                if (StageHelper <= 0)
                    return 0;

                if (StageHelper <= 1)
                    return 1;

                if (NPC.life > NPC.lifeMax * .8f)
                    return 2;

                if (NPC.life > NPC.lifeMax * .5f)
                    return 3;

                return 4;*/
            }
        }
        public int StageLoopCount { get; set; } = 0;
        public int StageHelper // Checked to prevent starting a stage during a pattern, amidst other things
        {
            get => (int)NPC.localAI[2];
            set => NPC.localAI[2] = value;
        }
        public int RevivingIntoStage { get; set; } = 0;
        public float SavedRandom { get; set; } = 0;
        public Vector2 SavedPosition { get; set; }
        public Vector2 CastingPosition
        {
            get => NPC.Center + new Vector2(70, -102);
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
        public int Counter
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
            NPC.lifeMax = Main.expertMode ? 15000 : 20000;
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
            Rectangle sourceRectangle = new(0, 0, npcTexture.Width, frameHeight);
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
                NPC.position = player.Center + new Vector2(Main.rand.Next(-500, 500), -600);
                ArenaCenter = player.Center;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    GoToDefaultPosition();
            }

            if (player.dead)
            {
                NPC.velocity.Y -= 0.04f;
                NPC.EncourageDespawn(10);
                return;
            }


            Counter++;

            Vector2 toDestination = Destination - NPC.Center;


            float speed = 8f;
            float minSpeed = 3f;
            float slowdownRange = speed * 40f;
            Vector2 destNormalized = toDestination.SafeNormalize(Vector2.UnitY);
            Vector2 moveTo = toDestination.Length() < slowdownRange ?
                             destNormalized * MathF.Min(MathF.Max(MathF.Pow(toDestination.Length() / slowdownRange, 0.5f) * speed, minSpeed), toDestination.Length())
                             : destNormalized * speed;

            NPC.velocity = moveTo;

            NPC.spriteDirection = NPC.velocity.X < .2f ? -1 : 1;

            if (NPC.dontTakeDamage && StageHelper > 0 && NPC.life < NPC.lifeMax)
                Revive(RevivingIntoStage);
            else
                switch (Stage)
                {
                    case 0:
                        Stage0(); break;
                    case 1:
                        Stage1(); break;
                    case 2:
                        Stage2(); break;
                    case 3:
                        Stage3(player); break;
                    case 4:
                        Stage4(player); break;
                    case 5:
                        Stage5(player); break;
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

        public void SpawnFairyZombie(Vector2 position, Vector2 velocity)
        {
            int type = ModContent.NPCType<OrinFairyZombie>();
            var entitySource = NPC.GetSource_FromAI();

            NPC npc = NPC.NewNPCDirect(entitySource, (int)position.X, (int)position.Y, type, NPC.whoAmI, 1);
            npc.velocity = new(velocity.X, velocity.Y + 1f);

            OrinFairyZombie spirit = (OrinFairyZombie)npc.ModNPC;
            spirit.ParentIndex = NPC.whoAmI;
        }

        public void SpawnExplodingSpirits(Vector2 center, int distance, int spiritCount, int timeLeft)
        {
            for (int i = 0; i < spiritCount; i++)
            {
                float angle = MathHelper.TwoPi / spiritCount * i;
                Vector2 position = new(center.X + MathF.Sin(angle) * distance, center.Y - MathF.Cos(angle) * distance);
                Vector2 velocity = new(MathF.Cos(angle), MathF.Sin(angle));
                float speed = .07f;
                int type = ModContent.NPCType<OrinEvilSpiritExplode>();
                var entitySource = NPC.GetSource_FromAI();

                NPC spirit = NPC.NewNPCDirect(entitySource, position, type, NPC.whoAmI, timeLeft);
                spirit.velocity = velocity * speed;

                OrinEvilSpiritExplode s = (OrinEvilSpiritExplode)spirit.ModNPC;
                s.ParentIndex = NPC.whoAmI;
            }
        }

        public void SpawnSpiralSpirits(Vector2 center, int distance, int spiritCount)
        {
            for (int i = 0; i < spiritCount; i++)
            {
                float angle = MathHelper.TwoPi / spiritCount * i;
                Vector2 position = new(center.X + MathF.Sin(angle) * distance, center.Y - MathF.Cos(angle) * distance);
                Vector2 velocity = new(-MathF.Sin(angle), MathF.Cos(angle));
                int type = ModContent.NPCType<OrinEvilSpiritSpiral>();
                var entitySource = NPC.GetSource_FromAI();
                float speed = distance;

                NPC spirit = NPC.NewNPCDirect(entitySource, position, type, NPC.whoAmI);
                spirit.velocity = velocity * speed;

                OrinEvilSpiritSpiral s = (OrinEvilSpiritSpiral)spirit.ModNPC;
                s.ParentIndex = NPC.whoAmI;
            }
        }

        public void SpawnThrustingSpirits(Vector2 center, int distance, float speed, int spiritCount, int timeLeft, float offset)
        {
            for (int i = 0; i < spiritCount; i++)
            {
                float angle = (MathHelper.TwoPi / spiritCount * i) + (Pi / 7) + offset;
                Vector2 position = new(center.X + MathF.Sin(angle) * distance, center.Y - MathF.Cos(angle) * distance);
                Vector2 velocity = new(MathF.Sin(angle), -MathF.Cos(angle));
                int type = ModContent.NPCType<OrinEvilSpiritCircleThrust>();
                var entitySource = NPC.GetSource_FromAI();

                NPC spirit = NPC.NewNPCDirect(entitySource, position, type, NPC.whoAmI, timeLeft);
                spirit.velocity = velocity * speed;

                OrinEvilSpiritCircleThrust s = (OrinEvilSpiritCircleThrust)spirit.ModNPC;
                s.ParentIndex = NPC.whoAmI;
            }
        }
        public void SpawnRotatingSpirits(Vector2 center, Vector2 orbitVelocity, int distance, int spiritCount, int timeLeft)
        {
            for (int i = 0; i < spiritCount; i++)
            {
                float angle = MathHelper.TwoPi / spiritCount * i;
                Vector2 position = new(center.X + MathF.Sin(angle) * distance, center.Y - MathF.Cos(angle) * distance);
                Vector2 velocity = new(-MathF.Sin(angle), MathF.Cos(angle));
                int type = ModContent.NPCType<OrinEvilSpiritRotating>();
                var entitySource = NPC.GetSource_FromAI();
                float speed = distance;

                NPC spirit = NPC.NewNPCDirect(entitySource, position, type, NPC.whoAmI, timeLeft, orbitVelocity.X, orbitVelocity.Y);
                spirit.velocity = velocity * speed;

                OrinEvilSpiritRotating s = (OrinEvilSpiritRotating)spirit.ModNPC;
                s.ParentIndex = NPC.whoAmI;
            }
        }

        public void SpawnNuke(Vector2 position, int timeLeft)
        {
            var entitySource = NPC.GetSource_FromAI();
            int type = ModContent.ProjectileType<Nuke>();
            int damage = ProjDamage;

            Projectile p = Projectile.NewProjectileDirect(entitySource, position, Vector2.Zero, type,damage, 0f, Main.myPlayer, .5f, ai2: NPC.whoAmI);
            p.timeLeft = timeLeft;
        }

        public void Circle(Vector2 center, float distance, float offset, float speed, int count, int type, float finalSpeed, int frameToSpeedUp = 0, bool cheapKill = false)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = offset + MathHelper.TwoPi / count * i;
                Vector2 positionOffset = new(center.X + MathF.Sin(angle) * distance, center.Y - MathF.Cos(angle) * distance);
                Vector2 velocity = new Vector2(0, -1).RotatedBy(angle);

                if (!cheapKill && IsPlayerInSquare(positionOffset, distance / 30))
                    return;

                var entitySource = NPC.GetSource_FromAI();
                int damage = ProjDamage;

                Projectile p = Projectile.NewProjectileDirect(entitySource, positionOffset, velocity * speed, type, damage, 0f, Main.myPlayer, finalSpeed, frameToSpeedUp, NPC.whoAmI);
            }
        }

        public void ShootMany(int type, Vector2 position, float angle, float speed, int count)
        {
            for (int i=0; i < count; i++)
            {
                var entitySource = NPC.GetSource_FromAI();
                int damage = ProjDamage;
                Vector2 velocity = new Vector2(0, -1).RotatedBy(angle);

                Projectile.NewProjectile(entitySource, position, velocity * speed, type, damage, 0f, Main.myPlayer, ai2: NPC.whoAmI);
            }
        }

        public bool IsPlayerInSquare(Vector2 center, float halfSize)
        {
            Rectangle squareBounds = new Rectangle(
                (int)(center.X - halfSize),
                (int)(center.Y - halfSize),
                (int)(halfSize * 2),
                (int)(halfSize * 2)
            );

            foreach (Player player in Main.player)
                if (player.active && !player.dead)
                {
                    Rectangle playerHitbox = player.Hitbox;

                    if (squareBounds.Intersects(playerHitbox))
                        return true;
                }

            return false;
        }

        public void GoToDefaultPosition()
        {
            Vector2 MoveOffset = new(0, -350f);
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
            Vector2 offset = new(500 * side, -350);

            Destination = ArenaCenter + offset;
            NPC.netUpdate = true;
        }

        public void RandomJump()
        {
            Vector2 MoveOffset = new(Main.rand.NextFloat(-100, 100), Main.rand.NextFloat(-250, -450));
            Destination = ArenaCenter + MoveOffset;
            NPC.netUpdate = true;
        }

        public void RunFromArena(int side)
        {
            Vector2 direction = new(3000 * side, 0);

            Destination = ArenaCenter + direction;
            NPC.netUpdate = true;
        }

        public void Revive(int stage)
        {
            NPC.life = Math.Min(NPC.life + NPC.lifeMax / 100, NPC.lifeMax);

            if (NPC.life >= NPC.lifeMax)
            {
                NPC.dontTakeDamage = false;
                Counter = 0;
            }
        }

        public void Stage0()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient) // Only server should spawn bullets and change destination
            {
                if (Counter == 1)
                {
                    RandomJump();
                }

                if (Counter > 100)
                {
                    int fairyCount = 0;

                    foreach (var otherNPC in Main.ActiveNPCs)
                        if (otherNPC.type == ModContent.NPCType<OrinFairyZombie>())
                            fairyCount++;


                    if (fairyCount <= 16)
                    {
                        Vector2 velocity = new Vector2(1, 0).RotatedBy(fairyCount * Pi/16) * 5f;

                        SpawnFairyZombie(NPC.Center, velocity);
                    }
                }

                if (Casting && Counter % 5 == 0)
                {
                    float angle = Pi / 125 * (Counter + Main.rand.Next(10));
                    float speed = Main.rand.NextFloat(2f, 4f);
                    ShootMany(ModContent.ProjectileType<BasicSpiralBullet>(), CastingPosition, angle, speed, 2);
                }

                if (Counter % 500 > 100 && Counter % 500 <= 114)
                {
                    int start = Counter % 500 - 100;
                    float angle = start * Pi / 120;
                    float distance = start * 80;
                    int frameToSpeedUp = 100 - start * 2;

                    Circle(CastingPosition, distance, angle, .01f, 24, ModContent.ProjectileType<SpeedUpDiamondBlue>(), 20f, frameToSpeedUp);
                }

                if (Counter % 500 > 200 && Counter % 500 <= 214)
                {
                    int start = Counter % 500 - 200;
                    float angle = -start * Pi / 120;
                    float distance = start * 80;
                    int frameToSpeedUp = 100 - start * 2;

                    Circle(CastingPosition, distance, angle, .01f, 24, ModContent.ProjectileType<SpeedUpDiamondRed>(), 20f, frameToSpeedUp);
                }
            }

            if (Counter == 150)
                Casting = true;

            if (Counter >= 500)
                Counter = 0;
        }

        public void Stage1()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && StageHelper >= 1) // Only server should spawn bullets and change destination
            {
                if (Counter == 1)
                    GoToDefaultPosition();

                if (Casting && Counter % 5 == 0)
                {
                    float angle = Pi / 125 * (Counter + Main.rand.Next(10));
                    float speed = Main.rand.NextFloat(3f, 6f);
                    ShootMany(ModContent.ProjectileType<BasicLargeBallBlue>(), CastingPosition, angle, speed, 2);
                }
            }

            if (Counter == 60)
                Casting = true;

            if (StageHelper < 1)
            {
                StageHelper = 1;
                Counter = 0;
                Casting = false;
            }

            if (Counter >= 500)
                Counter = 0;
        }

        public void Stage2()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && StageHelper >= 2) // Only server should spawn bullets and change destination
            {
                if (Counter % 300 == 1 || Counter % 300 == 100)
                {
                    int side = Counter >= 300 ? -1 : 1;
                    AlternatingJump(Counter % 300 == 100 ? 0 : side);
                }

                if (Counter % 300 == 80)
                {
                    int spiritCount = 5;
                    int distance = 120;

                    SpawnExplodingSpirits(CastingPosition, distance, spiritCount, 170);
                }

                if (Counter % 300 >= 180 && Counter % 3 == 0 && Counter % 300 < 230)
                {
                    int start = Counter % 300 - 180;
                    float angle = Counter >= 300 ? -start * Pi / 400 : start * Pi / 400;
                    float distance = 40 + start * 8;
                    int frameToSpeedUp = 150 - start * 2;

                    Circle(CastingPosition, distance, angle, .01f, 20, ModContent.ProjectileType<SpeedUpDiamondBlue>(), 12f + start / 30, frameToSpeedUp);
                }
            }

            if (Counter == 60)
                Casting = true;

            if (StageHelper < 2)
            {
                StageHelper = 2;
                Counter = 0;
                Casting = false;
            }

            if (Counter >= 600)
                Counter = 0;
        }

        public void Stage3(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && StageHelper >= 3) // Only server should spawn bullets and change destination
            {
                if (Counter == 120)
                {
                    SpawnSpiralSpirits(player.Center, 240, 8);
                    SpawnNuke(player.Center, 400 + 100);
                }

                if (Counter == 240)
                {
                    RandomJump();
                }

            }

            if (Counter >= 100 && Counter <= 210)
                Casting = true;
            else
                Casting = false;

            if (StageHelper < 3)
            {
                StageHelper = 3;
                Counter = 0;
                Casting = false;
            }

            if (Counter >= 450)
                Counter = 0;
        }

        public void Stage4(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && StageHelper >= 4) // Only server should spawn bullets and change destination
            {
                if (Counter == 120)
                {
                    Vector2 defaultPosition = new(0, -350f);
                    float offset = (defaultPosition + ArenaCenter - NPC.Center).ToRotation();
                    SpawnThrustingSpirits(CastingPosition, 80, 4, 7, 180, offset);
                }

                if (Counter == 180)
                    RandomJump();
            }

            if (Counter >= 100)
                Casting = true;

            if (StageHelper < 4)
            {
                StageHelper = 4;
                Counter = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    GoToDefaultPosition();
                Casting = false;
            }

            if (Counter >= 230)
                Counter = 0;
        }

        public void Stage5(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && StageHelper >= 5) // Only server should spawn bullets and change destination
            {
                if (Counter == 1)
                    GoToDefaultPosition();

                if (Counter == 50)
                {
                    for (int i = -3; i < 4; i++)
                    {
                        float toPlayerAngle = (player.Center - NPC.Center).ToRotation() + MathHelper.PiOver2;
                        float angle = toPlayerAngle + Pi / 6 * i;
                        float orbitSpeed = 3f;
                        Vector2 orbitVelocity = new(MathF.Sin(angle) * orbitSpeed, -MathF.Cos(angle) * orbitSpeed);
                        SpawnRotatingSpirits(NPC.Center, orbitVelocity, 100, 5, 500);
                    }
                }
                   
                if (Counter >= 200 && Counter <= 270 && Counter % 5 == 0)
                {
                    int start = Counter - 200;
                    float angle = start * Pi / 160;
                    float distance = 40 + start * 8;
                    int frameToSpeedUp = 180 - start * 2;

                    Circle(CastingPosition, distance, angle, .01f, 14, ModContent.ProjectileType<SpeedUpDiamondBlue>(), 5f, frameToSpeedUp, false);
                } else if (Counter >= 300 && Counter <= 370 && Counter % 5 == 0)
                {
                    int start = Counter - 300;
                    float angle = start * -Pi / 160;
                    float distance = 40 + start * 8;
                    int frameToSpeedUp = 180 - start * 2;

                    Circle(CastingPosition, distance, angle, .01f, 14, ModContent.ProjectileType<SpeedUpDiamondBlue>(), 5f, frameToSpeedUp, false);
                }
            }

            if (Counter == 1)
                Casting = true;

            if (StageHelper < 5)
            {
                StageHelper = 5;
                Counter = 0;
            }

            if (Counter >= 400)
                Counter = 0;
        }
    }
}