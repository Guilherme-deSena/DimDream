using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using System.Diagnostics;
using DimDream.Content.Projectiles;
using DimDream.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using ReLogic.Content;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.ItemDropRules;
using DimDream.Content.Items.Weapons;
using Terraria.UI;
using DimDream.Content.Items.Consumables;

namespace DimDream.Content.NPCs
{
	[AutoloadBossHead] // This attribute looks for a texture called "ClassName_Head_Boss" and automatically registers it as the NPC boss head icon
	internal class ChiyuriBoss : ModNPC {
        private int AnimationCount { get; set; } = 0;
        private bool Initialized { get; set; } = false;
        private Vector2 ArenaCenter { get; set; }
        private Vector2 CenterPosition { get; set; }
		private Vector2 Destination {
			get => new Vector2(NPC.ai[2], NPC.ai[3]);
			set {
				NPC.ai[2] = value.X;
				NPC.ai[3] = value.Y;
			}
		}
		private static Asset<Texture2D> MagicCircle { get; set; }
		private bool Moving { get => NPC.velocity.Length() >= 1; }
		private static float Pi { get => MathHelper.Pi; }
		private int Stage { // Stage is decided by the boss' health percentage
            get {
				if (NPC.life > NPC.lifeMax * .85)
					return 0;

				if (NPC.life > NPC.lifeMax * .50)
					return 1;

				if (NPC.life > NPC.lifeMax * .35)
					return 2;

				return 3;
			}
        }
        // This will be checked to prevent starting a stage mid-pattern:
        private bool[] TransitionedToStage { get; set; } = new bool[4]; 
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

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 6;
            NPCID.Sets.MPAllowedEnemies[Type] = true; // Add this in for bosses that have a summon item, requires corresponding code in the item
            NPCID.Sets.BossBestiaryPriority.Add(Type); // Automatically group with other bosses
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.CantTakeLunchMoney[Type] = true;
        }

		public override void SetDefaults() {
			NPC.width = 72;
			NPC.height = 132;
			NPC.damage = 30;
			NPC.defense = 15;
			NPC.lifeMax = 25000;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.value = Item.buyPrice(gold: 15);
			NPC.SpawnWithHigherTime(30);
			NPC.boss = true;
			NPC.npcSlots = 10f; // Take up open spawn slots, preventing random NPCs from spawning during the fight

            // Custom AI, 0 is "bound town NPC" AI which slows the NPC down and changes sprite orientation towards the target
            NPC.aiStyle = -1;

			// Assigns a music track to the boss in a simple way
			if (!Main.dedServ) {
				Music = MusicID.Boss2;
			}
		}

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            // All the Classic Mode drops here are based on "not expert", meaning we use .OnSuccess() to add them into the rule, which then gets added
            LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());

			// Notice we use notExpertRule.OnSuccess instead of npcLoot.Add so it only applies in normal mode
			notExpertRule.OnSuccess(ItemDropRule.OneFromOptions(1, [ModContent.ItemType<FlowingBow>(), ModContent.ItemType<RippleStaff>()]));
            npcLoot.Add(notExpertRule);

            // Add the treasure bag using ItemDropRule.BossBag (automatically checks for expert mode)
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<ChiyuriBag>()));
        }

        public override void OnKill() {
            // This sets downedChiyuriBoss to true, and if it was false before, it initiates a lantern night
            NPC.SetEventFlagCleared(ref DownedBossSystem.downedChiyuriBoss, -1);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			// Sets the description of this NPC that is listed in the bestiary
			bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement> {
				new MoonLordPortraitBackgroundProviderBestiaryInfoElement(), // Plain black background
				new FlavorTextBestiaryInfoElement("Crew member for the Probability Space Hypervessel, currently helping Yumemi find proof of the existence of magic.")
			});
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
			cooldownSlot = ImmunityCooldownID.Bosses; // use the boss immunity cooldown counter, to prevent ignoring boss attacks by taking damage from other sources
			return true;
		}

		public override void Load() {
			MagicCircle = Mod.Assets.Request<Texture2D>("Assets/Textures/MagicCircle");
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Vector2 offset = new Vector2(113, 113); // Offset to position texture correctly around the boss. Usually half the texture's size
			float alphaChange = .1f; // How much is the opacity going to change every frame during fade-in/fade-out

			if (Stage == 3 && Counter > 390)
				spriteBatch.Draw(MagicCircle.Value, NPC.Center - Main.screenPosition - offset, drawColor * ((500 - Counter) % 390 * alphaChange));
			else if (Stage == 3 && Counter > 210)
				spriteBatch.Draw(MagicCircle.Value, NPC.Center - Main.screenPosition - offset, drawColor * (Counter % 210 * alphaChange));

			return true;
		}

		public override void AI() {
			// This should almost always be the first code in AI() as it is responsible for finding the proper player target
			Player player = Main.player[NPC.target];
			if (NPC.target < 0 || NPC.target == 255 || player.dead || !player.active || Vector2.Distance(NPC.Center, player.Center) > 3000f)
				NPC.TargetClosest();

			player = Main.player[NPC.target];

            if (!Initialized) // Stuff that cannot be initialized in SetDefaults()
            {
                Initialized = true;
                NPC.position = player.Center + new Vector2(Main.rand.Next(-500, 500), -1000);
                ArenaCenter = player.Center;
            }

            if (player.dead)
            {
                // If the targeted player is dead, flee
                NPC.velocity.Y -= 0.04f;
                // This method makes it so when the boss is in "despawn range" (outside of the screen), it despawns in 10 ticks
                NPC.EncourageDespawn(10);
                return;
            }

			if (Counter <= 1 || !Moving)
				Counter++;

			// At the first frame of every stage cycle, change the boss' destination
			if (Counter == 1 && Main.netMode != NetmodeID.MultiplayerClient) {
                CenterPosition = new Vector2(ArenaCenter.X, ArenaCenter.Y - 320f);
                Vector2 MoveOffset = new Vector2(Main.rand.Next(-200, 200), Main.rand.Next(-50, 50));
				Destination = CenterPosition + MoveOffset;
                NPC.netUpdate = true; // Update Destination to every client so they know where the boss should move towards
			}

            float speed = 8f;
            float inertia = 10f;
            float slowdownRange = speed * 10f;
            Vector2 destNormalized;
            Vector2 toDestination = Destination - NPC.Center;
            destNormalized = toDestination.SafeNormalize(Vector2.UnitY);

            Vector2 moveTo = toDestination.Length() < slowdownRange ?
							 destNormalized * (toDestination.Length() / slowdownRange * speed)
							 : destNormalized * speed;

			NPC.velocity = (NPC.velocity * (inertia - 1) + moveTo) / inertia;

			
			switch (Stage) {
				case 0: Stage0(player); break;
				case 1: Stage1(); break;
				case 2: Stage2(player); break;
				case 3: Stage3(); break;
			}

			NPC.rotation = NPC.velocity.X * 0.05f;

            int arenaRadius = 800; // Actual arena
            int fightRadius = 4000; // How far from the arena center do players have to be in order to be considered out of combat
            ArenaDust(ArenaCenter, arenaRadius);
            PullPlayers(ArenaCenter, arenaRadius, fightRadius);
        }

		public override void FindFrame(int frameHeight) {
			int frameSpeed = 10;
			NPC.frameCounter++;

			// Blinking frames only run once every 5 animation cycles
			if (NPC.frameCounter > frameSpeed) {
				NPC.frameCounter = 0;
				NPC.frame.Y += frameHeight;

				if (NPC.frame.Y >= 2 * frameHeight
					&& NPC.frame.Y <= 4 * frameHeight + 1
					&& AnimationCount % 5 != 0)
				{
					NPC.frame.Y = 5 * frameHeight;
				} else if (NPC.frame.Y > 5 * frameHeight) {
					AnimationCount += 1;
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
                if (player.active && !player.dead && Vector2.Distance(arenaCenter, player.Center) > pullDistance)
                {
                    Vector2 directionToNPC = NPC.Center - player.Center;

                    directionToNPC.Normalize();
                    directionToNPC *= pullStrength;

                    player.velocity = directionToNPC;
                }
            }
        }

        // bulletType is the type of projectile the familiar shoots. 0 for white spore, 1 for ring line.
        // bulletCount is how many bullets each burst has (if the projectile uses bursts). Unused for white spore.
        public void AimedFamiliars(Player player, int bulletType = 0)
        {
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? 1 : -1;
                Vector2 direction = new Vector2(side, -0.5f);
                float speed = 8f;
                int type = ModContent.ProjectileType<ShootingFamiliar>();
                int damage = (int)(ProjDamage * .8);
                var entitySource = NPC.GetSource_FromAI();

                Projectile.NewProjectile(entitySource, NPC.Center, direction * speed, type, damage, 0f, Main.myPlayer, bulletType, NPC.whoAmI);
            }
        }

        public void TopRandomSpore()
        {
            Vector2 position = NPC.Top + new Vector2(Main.rand.Next(-1200, 1200), -1200 + Main.rand.Next(-100, 100));
            Vector2 direction = new Vector2(Main.rand.NextFloat(-1, 1), 1);

            float speed = Main.rand.NextFloat(2f, 7f);
            int type = ModContent.ProjectileType<WhiteSpore>();
            int damage = ProjDamage / 2;
            var entitySource = NPC.GetSource_FromAI();

            Projectile.NewProjectile(entitySource, position, direction * speed, type, damage, 0f, Main.myPlayer);
        }

        public void CrossedSpores(int bullets, int rotation)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int i = 1; i < bullets + 1; i++)
                {
                    Color color = Color.LightYellow;
                    int colorNumber = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

                    int xSpacing = 16;
                    int ySpacing = 8;
                    int side = j == 0 ? 1 : -1;
                    float angleOffset = 5;
                    Vector2 position = NPC.Center + new Vector2(xSpacing * bullets / 2 - i * xSpacing, ySpacing).RotatedBy(MathHelper.ToRadians(rotation - 45));
                    Vector2 direction = new Vector2(1).RotatedBy(MathHelper.ToRadians(rotation - angleOffset * bullets / 2 + i * 5));

                    float sideSpeed = side == 1 ? i : bullets + 1 - i;
                    float speed = 2f + sideSpeed / 10;
                    int type = ModContent.ProjectileType<WhiteSpore>();
                    int damage = ProjDamage;
                    var entitySource = NPC.GetSource_FromAI();

                    Projectile.NewProjectile(entitySource, position, direction * speed, type, damage, 0f, Main.myPlayer, 1, colorNumber);
                }
            }
        }

        public void PerpendicularSpores(float offset)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2 positionOffset = new Vector2((float)Math.Sin(Pi / 2 * i) * 100, (float)Math.Cos(Pi / 2 * i) * 100);
                Vector2 position = NPC.Center + positionOffset;
                Vector2 direction = positionOffset.SafeNormalize(Vector2.UnitY);
                Vector2 direction2 = direction.RotatedBy(offset);

                float speed = 6f;
                int type = ModContent.ProjectileType<WhiteSpore>();
                int damage = ProjDamage;
                var entitySource = NPC.GetSource_FromAI();

                Projectile.NewProjectile(entitySource, position, direction2 * speed, type, damage, 0f, Main.myPlayer, 1);
            }
        }

        public void BlueLaser()
        {
            Vector2 position = NPC.Center + new Vector2(Main.rand.Next(-1400, 1400), 1200);
            Vector2 direction = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -1);

            int type = ModContent.ProjectileType<BlueLaser>();
            int damage = ProjDamage;
            var entitySource = NPC.GetSource_FromAI();

            Projectile.NewProjectile(entitySource, position, direction, type, damage, 0f);
        }

        public void BlueRing(int bullets)
        {
            float offset = (Main.rand.NextFloat(0, MathHelper.Pi * 2 / bullets));
            for (int i = 0; i < bullets; i++)
            {
                Vector2 position = NPC.Center;
                Vector2 direction = new Vector2(1, 0).RotatedBy(MathHelper.Pi * 2 / bullets * i + offset);

                float speed = 7f;
                int type = ModContent.ProjectileType<BlueRing>();
                int damage = ProjDamage;
                var entitySource = NPC.GetSource_FromAI();

                Projectile.NewProjectile(entitySource, position, direction * speed, type, damage, 0f, Main.myPlayer);
            }

        }

        public void Stage0(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            { // Only server should spawn bullets
                int frameCount = Main.expertMode ? 20 : 30; // Cooldown between random spores
                if (Counter % frameCount == 0)
                    TopRandomSpore();

                if (Counter > 100 && Counter % 30 == 0)
                    BlueLaser();

                if (Counter == 150)
                    AimedFamiliars(player);

            }
            if (Counter > 300)
            {
                Counter = 0;
                TransitionedToStage[0] = true;
            }

        }

        public void Stage1()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && TransitionedToStage[1])
            {
                int frameCount = Main.expertMode ? 20 : 30;
                // Bullet count for each pattern depends on difficulty:
                int ringBullets = Main.expertMode ? 14 : 10;
                int crossedBullets = Main.expertMode ? 18 : 14;

                if (Counter % frameCount == 0)
                    TopRandomSpore();

                if (Counter >= 115)
                    switch (Counter % 115)
                    { // Spawns crossed spores perpendicularly
                        case 0:
                            CrossedSpores(crossedBullets, 45);
                            break;
                        case 30:
                            CrossedSpores(crossedBullets, 135);
                            CrossedSpores(crossedBullets, 315);
                            break;
                        case 60:
                            CrossedSpores(crossedBullets, 225);
                            break;
                    }

                if (Counter >= 130 && Counter % 45 == 0)
                    BlueRing(ringBullets);

                if (Counter >= 305 && Counter % 15 == 0)
                    BlueLaser();
            }
            if (Counter >= 350 || !TransitionedToStage[1])
            {
                Counter = 0;
                TransitionedToStage[1] = true;
            }
        }

        public void Stage2(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && TransitionedToStage[2])
            {
                int frameCount = Main.expertMode ? 12 : 20;

                if (Counter % frameCount == 0)
                    TopRandomSpore();

                if (Counter % 200 == 0)
                    AimedFamiliars(player, 1);

                if (Counter >= 200 && Counter % 15 == 0)
                    BlueLaser();
            }
            if (Counter >= 300 || !TransitionedToStage[2])
            {
                Counter = 0;
                TransitionedToStage[2] = true;
            }
        }

        public void Stage3()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && TransitionedToStage[3])
            {
                int frameCount = Main.expertMode ? 12 : 20;
                int crossedBullets = Main.expertMode ? 12 : 10;

                if (Counter % frameCount == 0)
                    TopRandomSpore();


                if (Counter == 150)
                {
                    for (int i = 0; i <= 270; i += 90)
                    {
                        CrossedSpores(crossedBullets, i);
                    }
                }

                int start = 220;
                if (Counter >= start && Counter % 6 == 0)
                {
                    int bulletCount = 20;
                    float positionInLine = Counter == start ? 0 : (Counter - start) / 3 % (bulletCount * 2);
                    float maxOffset = Pi / 2;
                    float offset = maxOffset / bulletCount * Math.Abs(bulletCount - positionInLine);
                    PerpendicularSpores(offset - maxOffset / 2);
                }

                if (Counter >= 280 && Counter % 15 == 0)
                    BlueLaser();
            }
            if (Counter >= 400 || !TransitionedToStage[3])
            {
                Counter = 0;
                TransitionedToStage[3] = true;
            }
        }
    }
}