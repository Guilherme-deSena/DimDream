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

namespace DimDream.Content.NPCs
{
    [AutoloadBossHead] // This attribute looks for a texture called "ClassName_Head_Boss" and automatically registers it as the NPC boss head icon
    internal class YumemiBoss : ModNPC
    {
        private int AnimationCount { get; set; } = 0;
        private Vector2 CenterPosition { get; set; }
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
        private bool ShouldDrawLine { get; set; }
        private bool Moving { get => NPC.velocity.Length() >= 1; }
        private static float Pi { get => MathHelper.Pi; }
        private int Stage
        { // Stage is decided by the boss' health percentage
            get
            {
                if (NPC.life > NPC.lifeMax * .85)
                    return 0;

                if (NPC.life > NPC.lifeMax * .60)
                    return 1;

                if (NPC.life > NPC.lifeMax * .45)
                    return 2;

                if (NPC.life > NPC.lifeMax * .20)
                    return 3;

                return 4;
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

        public void SpawnCross(Player player)
        {
            Vector2 position = player.Center;

            int type = ModContent.ProjectileType<SpawnedFamiliar>();
            int damage = (int)ProjDamage;
            var entitySource = NPC.GetSource_FromAI();

            Projectile.NewProjectile(entitySource, position, Vector2.Zero, type, damage, 0f, Main.myPlayer, NPC.whoAmI);
        }

        // bulletType: 0 for ReceptacleBullet, 1 for WhiteSpore
        public void PerpendicularBullets(int bulletType)
        {

            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    float side = (Pi / 2) * j;
                    float offset = bulletType == 1 ? 0 : Pi / 4;
                    Vector2 position = NPC.Center + new Vector2(0, -20).RotatedBy(side + offset);
                    float rotation = (MathHelper.Pi / 2) * i;
                    float randomOffset = Main.rand.NextFloat(-Pi / 30, Pi / 30);
                    Vector2 direction = new Vector2(0, -1).RotatedBy(rotation + offset + randomOffset);
                    float speed = Main.rand.NextFloat(3f, 5f);

                    int type = bulletType == 0 ? ModContent.ProjectileType<ReceptacleBullet>() : ModContent.ProjectileType<WhiteSpore>();
                    int damage = (int)(ProjDamage * .6);
                    var entitySource = NPC.GetSource_FromAI();

                    Projectile.NewProjectile(entitySource, position, direction * speed, type, damage, 0f, Main.myPlayer);
                }
            }
        }

        public void RainBullets()
        {
            for ( int i = -2; i < 2; i++ )
            {
                float rotation = (MathHelper.Pi / 10) * i;
                Vector2 direction = new Vector2(0, -1).RotatedBy(rotation);
                float speed = Main.rand.NextFloat(4f, 6f);

                int type = ModContent.ProjectileType<ReceptacleBullet>();
                int damage = (int)(ProjDamage * .65);
                var entitySource = NPC.GetSource_FromAI();

                Projectile.NewProjectile(entitySource, NPC.Center, direction * speed, type, damage, 0f, Main.myPlayer, 1f);
            }
        }

        public void CircularBomb(int bullets, Vector2 position, Color color, int bulletType = 0, float damagePercent = .7f)
        {
            float directionOffset = Main.rand.NextFloat(0, 1);
            for (int j = 0; j < bullets; j++)
            {
                int colorNumber = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
                Vector2 direction = new Vector2(1, 0).RotatedBy(MathHelper.Pi * 2 / bullets * j + directionOffset);

                float speed = 3f;
                int type = bulletType == 0 ? ModContent.ProjectileType<WhiteSpore>() : ModContent.ProjectileType<ReceptacleBullet>();
                int damage = (int)(NPC.damage * damagePercent);
                var entitySource = NPC.GetSource_FromAI();

                Projectile.NewProjectile(entitySource, position, direction * speed, type, damage, 0f, Main.myPlayer, 0f, colorNumber);
            }
        }

        public void SpiralCircle(float speed, float offset)
        {
            for (int i = 0; i < 18; i++)
            {
                float rotation = (Pi / 9) * i;
                Vector2 direction = new Vector2(0, -1).RotatedBy(rotation + offset);

                int type = ModContent.ProjectileType<ReceptacleBullet>();
                int damage = (int)ProjDamage;
                var entitySource = NPC.GetSource_FromAI();

                Projectile.NewProjectile(entitySource, NPC.Center, direction * speed, type, damage, 0f, Main.myPlayer);
            }
        }

        public void ThrowStuff(Vector2 destination, int bulletType, int bulletCount, float speed = 4f)
        {
            for (int i = 0; i < bulletCount; i++)
            {
                Vector2 position = NPC.Center;
                Vector2 toDestination = destination - position;
                float offset = -(Pi/10) + Pi/5 * i/Math.Max(bulletCount - 1, 1);
                Vector2 direction =  new Vector2(1, 0).RotatedBy(toDestination.SafeNormalize(Vector2.UnitY).ToRotation() + offset);

                int damage = (int)(ProjDamage * .8);
                var entitySource = NPC.GetSource_FromAI();
                Projectile.NewProjectile(entitySource, position, direction * speed, bulletType, damage, 0f, Main.myPlayer);
            }
        }

        public void ShootingFamiliar(float speed, int side)
        {
            Vector2 position = NPC.Center;
            Vector2 direction = new Vector2(side, -1f);

            int damage = (int)(ProjDamage * .9);
            int type = ModContent.ProjectileType<ShootingFamiliar>();
            var entitySource = NPC.GetSource_FromAI();
            Projectile.NewProjectile(entitySource, position, direction * speed, type, damage, 0f, Main.myPlayer, 0f, 0f, 1f);
        }

        public void TopRandomSpore()
        {
            Vector2 position = NPC.Top + new Vector2(Main.rand.Next(-1200, 1200), -1200 + Main.rand.Next(-100, 100));
            Vector2 direction = new(Main.rand.NextFloat(-1, 1), 1);

            float speed = Main.rand.NextFloat(2f, 5f);
            int type = ModContent.ProjectileType<WhiteSpore>();
            int damage = ProjDamage / 2;
            var entitySource = NPC.GetSource_FromAI();

            Projectile.NewProjectile(entitySource, position, direction * speed, type, damage, 0f, Main.myPlayer);
        }

        public void SimpleMovement(Player player)
        {
            CenterPosition = new Vector2(player.Top.X, player.Top.Y - 320f);
            Vector2 MoveOffset = new Vector2(Main.rand.Next(-200, 200), Main.rand.Next(-50, 50));
            Destination = CenterPosition + MoveOffset;
            NPC.netUpdate = true; // Update Destination to every client so they know where the boss should move towards
        }

        public void StrafeMovement(Player player, int side)
        {
            CenterPosition = new Vector2(player.Top.X, player.Top.Y - 200f);
            Vector2 MoveOffset = new(400 * side, Main.rand.Next(-150, 150));
            Destination = CenterPosition + MoveOffset;
            NPC.netUpdate = true;
        }

        public void Stage0(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Counter < 250 && Counter % 60 == 1)
                {
                    int side = Counter % 120 == 1 ? 1 : -1;
                    StrafeMovement(player, side);
                }
                /*
                int frameCount = Main.expertMode ? 20 : 28;
                if (Counter % frameCount == 0)
                    TopRandomSpore();*/

                if (Counter == 240)
                    SimpleMovement(player);

                if (Counter >= 60 && Counter <= 180 && Counter % 30 == 0)
                    SpawnCross(player);

                if (Counter >= 450 && Counter % 10 == 0)
                {
                    int bombBullets = Main.expertMode ? 24 : 16;
                    float offset1 = Counter <= 470 ? 0 : Pi / 3;
                    float offset2 = Counter % 30 / 10;
                    Vector2 positionOffset = new((float)Math.Sin(offset1 + Pi / 1.5 * offset2) * 100, (float)Math.Cos(offset1 + Pi / 1.5 * offset2) * 100);
                    Vector2 position = NPC.Center + positionOffset;
                    Color color = offset1 == 0 ? Color.GreenYellow : Color.LightSkyBlue;
                    CircularBomb(bombBullets, position, color);
                }
            }

            ShouldDrawLine = Counter < 200 ? true : false;

            if (Counter >= 500)
                Counter = 0;
        }

        public void Stage1(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && TransitionedToStage[1]) // Only server should spawn bullets
            {   
                if (Counter % 240 == 1)
                {
                    int side = Counter % 480 == 0 ? 1 : -1;
                    StrafeMovement(player, side);
                }
                /*
                int frameCount = Main.expertMode ? 12 : 20;
                if (Counter % frameCount == 0)
                    TopRandomSpore();*/

                if (Counter >= 60 && Counter <= 180 && Counter % 30 == 0)
                    SpawnCross(player);


                if (Counter >= 240 && Counter % 240 < 120)
                {
                    if (Counter % 20 == 0)
                        PerpendicularBullets(0);

                    if (Counter % 5 == 0)
                        RainBullets();

                    if (Counter % 20 == 10)
                        PerpendicularBullets(1);
                }
            }

            ShouldDrawLine = Counter < 200 ? true : false;

            if (Counter >= 900 || !TransitionedToStage[1])
            {
                Counter = 0;
                TransitionedToStage[1] = true;
            }
        }

        public void Stage2(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && TransitionedToStage[2])
            {
                if (Counter == 90)
                    SimpleMovement(player);
                /*
                int frameCount = Main.expertMode ? 20 : 28;
                if (Counter % frameCount == 0)
                    TopRandomSpore();*/

                if (Counter >= 120 && Counter <= 240 && Counter % 30 == 0)
                    SpawnCross(player);

                if (Counter == 1)
                    AimedPosition = player.Center;

                if (Counter % 10 == 0 && Counter <= 90)
                {
                    int bulletType = Counter % 20 == 0 ? ModContent.ProjectileType<ReceptacleBullet>() : ModContent.ProjectileType<WhiteSpore>();
                    int minBullets = Main.expertMode ? 4 : 2;
                    int bulletCount = minBullets + 2 * ((int)Counter % 30 / 5);
                    ThrowStuff(AimedPosition, bulletType, bulletCount, 10f);
                }

                if (Counter >= 440 && Counter % 10 == 0)
                {
                    int bombBullets = Main.expertMode ? 32 : 24;
                    float offset1 = Counter < 470 ? 0 : Pi / 3;
                    float offset2 = Counter % 30 / 10;
                    Vector2 positionOffset = new((float)Math.Sin(offset1 + Pi / 1.5 * offset2) * 100, (float)Math.Cos(offset1 + Pi / 1.5 * offset2) * 100);
                    Vector2 position = NPC.Center + positionOffset;

                    CircularBomb(bombBullets, position, Color.White, 1, .9f);
                }
            }

            ShouldDrawLine = Counter < 250 ? true : false;

            if (Counter >= 500 || !TransitionedToStage[2])
            {
                Counter = 0;
                TransitionedToStage[2] = true;
            }
        }

        public void Stage3(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && TransitionedToStage[3])
            {
                if (Counter == 1)
                    SimpleMovement(player);
                /*
                int frameCount = Main.expertMode ? 12 : 20;
                if (Counter % frameCount == 0)
                    TopRandomSpore();*/
                
                if (Counter == 190)
                    AimedPosition = player.Center;

                if (Counter >= 210 && Counter % 10 == 0 && Counter <= 270)
                {
                    int bulletType = Counter % 20 == 0 ? ModContent.ProjectileType<ReceptacleBullet>() : ModContent.ProjectileType<WhiteSpore>();
                    int minBullets = Main.expertMode ? 4 : 2;
                    int bulletCount = minBullets + 2 * ((int)Counter % 30 / 10);
                    ThrowStuff(AimedPosition, bulletType, bulletCount, 12f);
                }

                if (Counter >= 300 && Counter % 5 == 0)
                {
                    float frame = (Counter % 300) / 10;
                    float speed = 2f + frame * 1f;
                    float offset = (Pi / 36) * frame;
                    SpiralCircle(speed, offset);
                }
            }

            ShouldDrawLine = Counter < 190 ? true : false;

            if (Counter >= 350 || !TransitionedToStage[3])
            {
                Counter = 0;
                TransitionedToStage[3] = true;
            }
        }

        public void Stage4(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && TransitionedToStage[4])
            {
                if (Counter == 1)
                    SimpleMovement(player);
                /*
                int frameCount = Main.expertMode ? 12 : 20;
                if (Counter % frameCount == 0)
                    TopRandomSpore();*/

                if (Counter >= 60 && Counter <= 210 && Counter % 30 == 0)
                    SpawnCross(player);

                if (Counter == 300)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        float speed = 20f;
                        int side = i % 2 == 0 ? 1 : -1;
                        ShootingFamiliar(speed, side);
                    }
                }
            }

            ShouldDrawLine = Counter < 210 ? true : false;

            if (Counter >= 600 || !TransitionedToStage[4])
            {
                Counter = 0;
                TransitionedToStage[4] = true;
            }
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 11;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 112;
            NPC.height = 142;
            NPC.damage = 45;
            NPC.defense = 22;
            NPC.lifeMax = 34000;
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
                Music = MusicID.Boss2;
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

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (ShouldDrawLine)
            {
                Vector2 npcPosition = NPC.Center - Main.screenPosition;
                Vector2 playerPosition = Main.player[NPC.target].Center - Main.screenPosition;
                DrawLine(spriteBatch, npcPosition, playerPosition, Color.Aqua, 4);
            }

            return true;
        }

        private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness)
        {
            // Calculate the distance and angle between the start and end points
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);

            // Create a rectangle to represent the line
            Rectangle rectangle = new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness);

            // Draw the line using a white 1x1 texture and applying the color
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, null, color, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
        }

        public override void AI()
        {
            // This should almost always be the first code in AI() as it is responsible for finding the proper player target
            Player player = Main.player[NPC.target];
            if (NPC.target < 0 || NPC.target == 255 || player.dead || !player.active || Vector2.Distance(NPC.Center, player.Center) > 3000f)
                NPC.TargetClosest();

            player = Main.player[NPC.target];

            if (player.dead)
            {
                NPC.velocity.Y -= 0.04f;
                NPC.EncourageDespawn(10);
                return;
            }

            Counter++;

            float speed = 10f;
            float inertia = 10;
            float slowdownRange = speed * 10;
            Vector2 toDestination = Destination - NPC.Center;
            Vector2 destNormalized = toDestination.SafeNormalize(Vector2.UnitY);

            Vector2 moveTo = toDestination.Length() < slowdownRange ?
                             destNormalized * (toDestination.Length() / slowdownRange * speed)
                             : destNormalized * speed;

            NPC.velocity = (NPC.velocity * (inertia - 1) + moveTo) / inertia;
            NPC.rotation = NPC.velocity.X * 0.05f;

            switch (Stage)
            {
                case 0: Stage0(player); break;
                case 1: Stage1(player); break;
                case 2: Stage2(player); break;
                case 3: Stage3(player); break;
                case 4: Stage4(player); break;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            int frameSpeed = 10;
            NPC.frameCounter++;

            // Blinking frames only run once every 5 animation cycles
            if (NPC.frameCounter >= frameSpeed)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;

                

                if (NPC.frame.Y >= 6 * frameHeight
                    && NPC.frame.Y < 7 * frameHeight
                    && AnimationCount % 3 != 0)
                {
                    NPC.frame.Y = 0;
                    AnimationCount += 1;
                }

                if (NPC.frame.Y >= 11 * frameHeight)
                {
                    NPC.frame.Y = 0;
                    AnimationCount += 1;
                }
            }
        }
    }
}