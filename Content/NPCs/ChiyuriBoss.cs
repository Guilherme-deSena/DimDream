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

namespace DimDream.Content.NPCs
{
	[AutoloadBossHead] // This attribute looks for a texture called "ClassName_Head_Boss" and automatically registers it as the NPC boss head icon
	internal class ChiyuriBoss : ModNPC {
        private int AnimationCount { get; set; } = 0;
		private Vector2 CenterPosition { get; set; }
		private Vector2 MoveOffset {
			get => new Vector2(NPC.ai[1], NPC.ai[2]);
			set {
				NPC.ai[1] = value.X;
				NPC.ai[2] = value.Y;
			}
		}
		private static Asset<Texture2D> MagicCircle { get; set; }
		private bool Moving { get => NPC.velocity.Length() >= 1; }
		private float Pi { get => MathHelper.Pi; }
		private int Stage {
			get {
				if (NPC.life > NPC.lifeMax * .80)
					return 0;

				if (NPC.life > NPC.lifeMax * .50)
					return 1;

				if (NPC.life > NPC.lifeMax * .30)
					return 2;

				return 3;
			}
        }
        // This will be checked to prevent from starting a stage mid-pattern:
        private bool[] TransitionedToStage { get; set; } = new bool[4]; 
        private int ProjDamage
		{
            get
            {
				if (Main.masterMode)
					return NPC.damage / 3;

				if (Main.expertMode)
					return NPC.damage / 2;
				
				return NPC.damage;
            }
        }
		private float Counter {
			get => NPC.ai[3];
			set => NPC.ai[3] = value;
		}

		public void DiagonalAimedSpore(Player player) {
			int side = Counter % 2 == 0 ? 1 : -1;
			Vector2 position = NPC.Top + new Vector2(500 * side + Main.rand.Next(-100, 100), Main.rand.Next(-250, -150));
			Vector2 direction = (player.Center - position).SafeNormalize(Vector2.UnitY);

			float speed = .7f;
			int type = ModContent.ProjectileType<WhiteSpore>();
			int damage = (int)(ProjDamage * .8);
			var entitySource = NPC.GetSource_FromAI();

			Projectile.NewProjectile(entitySource, position, direction * speed, type, damage, 0f, Main.myPlayer);
		}

		// bulletType is the type of projectile the familiar shoots. 0 for white spore, 1 for ring line.
		// bulletCount is how many bullets each burst has (if the projectile uses bursts). Unused for white spore.
		public void AimedFamiliars(Player player, int bulletType = 0) {
			for (int i = 0; i < 2; i++) {
				int side = i == 0 ? 1 : -1;
				Vector2 direction = new Vector2(side, -0.5f);
				float speed = 8f;
				int type = ModContent.ProjectileType<Familiar>();
				int damage = (int)(ProjDamage * .8);
				var entitySource = NPC.GetSource_FromAI();

				Projectile.NewProjectile(entitySource, NPC.Center, direction * speed, type, damage, 0f, Main.myPlayer, bulletType, NPC.whoAmI);
			}
		}

		public void TopRandomSpore() {
			Vector2 position = NPC.Top + new Vector2(Main.rand.Next(-1200, 1200), -1200 + Main.rand.Next(-100, 100));
			Vector2 direction = new Vector2(Main.rand.NextFloat(-1, 1), 1);

			float speed = Main.rand.NextFloat(.2f, .5f);
			int type = ModContent.ProjectileType<WhiteSpore>();
			int damage = ProjDamage / 2;
			var entitySource = NPC.GetSource_FromAI();

			Projectile.NewProjectile(entitySource, position, direction * speed, type, damage, 0f, Main.myPlayer);
		}

		public void CrossedSpores(int bullets, int rotation) {
			for (int j = 0; j < 2; j++) {
				for (int i = 1; i < bullets+1; i++) {
					int xSpacing = 16;
					int ySpacing = 8;
					int side = j == 0 ? 1 : -1;
					float angleOffset = 5;
					Vector2 position = NPC.Center + new Vector2(xSpacing * bullets / 2 - i * xSpacing, ySpacing).RotatedBy(MathHelper.ToRadians(rotation - 45));
					Vector2 direction = new Vector2(1).RotatedBy(MathHelper.ToRadians(rotation - angleOffset * bullets / 2 + i * 5));

					float sideSpeed = side == 1 ? i : bullets + 1 - i;
					float speed = .2f + sideSpeed / 100;
					int type = ModContent.ProjectileType<WhiteSpore>();
					int damage = ProjDamage;
					var entitySource = NPC.GetSource_FromAI();

					Projectile.NewProjectile(entitySource, position, direction*speed, type, damage, 0f, Main.myPlayer, 1f);
				}
			}
		}

		public void PerpendicularSpores(float offset) {
			for (int i = 0; i < 4; i++) {
				Vector2 positionOffset = new Vector2((float)Math.Sin(Pi / 2 * i) * 100, (float)Math.Cos(Pi / 2 * i) * 100);
				Vector2 position = NPC.Center + positionOffset;
				Vector2 direction = positionOffset.SafeNormalize(Vector2.UnitY);
				Vector2 direction2 = direction.RotatedBy(MathHelper.ToRadians(offset));

				float speed = .3f;
				int type = ModContent.ProjectileType<WhiteSpore>();
				int damage = ProjDamage;
				var entitySource = NPC.GetSource_FromAI();

				Projectile.NewProjectile(entitySource, position, direction2 * speed, type, damage, 0f, Main.myPlayer);
			}
		}

		public void BlueLaser() {
			Vector2 position = NPC.Center + new Vector2(Main.rand.Next(-2500, 2500), 1200);
			Vector2 direction = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -1);

			int type = ModContent.ProjectileType<BlueLaser>();
			int damage = ProjDamage;
			var entitySource = NPC.GetSource_FromAI();

			Projectile.NewProjectile(entitySource, position, direction, type, damage, 0f);
		}

		public void DiagonalAimedRingLine(Player player, int bullets, int side) {
			for (int i = 0;  i < bullets; i++) {
				Vector2 position = NPC.Top + new Vector2(500 * side + Main.rand.Next(-50, 50), Main.rand.Next(-300, -280));
				Vector2 direction = (player.Center - position).SafeNormalize(Vector2.UnitY);
				float speed = 4f + (float)i/(bullets/2);
				int type = ModContent.ProjectileType<BlueRing>();
				int damage = (int)(ProjDamage * .8);
				var entitySource = NPC.GetSource_FromAI();

				Projectile.NewProjectile(entitySource, position, direction * speed, type, damage, 0f, Main.myPlayer, 1f);
			}
		}

		public void BlueRing(int bullets) {
			float offset = (Main.rand.NextFloat(0, MathHelper.Pi * 2 / bullets));
			for (int i = 0; i < bullets; i++) {
				Vector2 position = NPC.Center;
				Vector2 direction = new Vector2(1, 0).RotatedBy(MathHelper.Pi * 2 / bullets * i + offset);

				float speed = .55f;
				int type = ModContent.ProjectileType<BlueRing>();
				int damage = ProjDamage;
				var entitySource = NPC.GetSource_FromAI();

				Projectile.NewProjectile(entitySource, position, direction * speed, type, damage, 0f, Main.myPlayer);
			}
			
		}

		public void Stage0(Player player) {
                if (Main.netMode != NetmodeID.MultiplayerClient) { // Only server should spawn bullets
				int frameCount = Main.expertMode ? 20 : 30; // Cooldown between random spores
				if (Counter % frameCount == 0)
					TopRandomSpore();

				if (!Moving) {
					if (Counter > 200 && Counter % 30 == 0)
						BlueLaser();

					if (Counter == 250)
						AimedFamiliars(player);
				}

			}
			if (Counter > 400) {
                Counter = 0;
                TransitionedToStage[0] = true;
            }
				
		}

		public void Stage1() {
			if (Main.netMode != NetmodeID.MultiplayerClient && TransitionedToStage[1]) {
				int frameCount = Main.expertMode ? 20 : 30;
				// Bullet count for each pattern depends on difficulty:
				int ringBullets = Main.expertMode ? 14 : 10;
				int crossedBullets = Main.expertMode ? 12 : 10;

				if (Counter % frameCount == 0)
					TopRandomSpore();

				if (Counter >= 180)
					switch (Counter % 180) { // Spawns crossed spores perpendicularly
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

				if (Counter >= 230 && Counter % 45 == 0)
					BlueRing(ringBullets);

				if (Counter >= 405 && Counter % 15 == 0)
					BlueLaser();
			}
			if (Counter >= 500 || !TransitionedToStage[1])
            {
                Counter = 0;
                TransitionedToStage[1] = true;
            }
        }

		public void Stage2(Player player) {
			if (Main.netMode != NetmodeID.MultiplayerClient && TransitionedToStage[2]) {
				int frameCount = Main.expertMode ? 12 : 20;

				if (Counter % frameCount == 0)
					TopRandomSpore();

				if (Counter % 300 == 0)
					AimedFamiliars(player, 1);

				if (Counter >= 300 && Counter % 15 == 0)
					BlueLaser();
			}
			if (Counter >= 400 || !TransitionedToStage[2])
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

				if (Counter >= 320 && Counter <= 490 && Counter % 40 < 20 && Counter % 5 == 0)
					PerpendicularSpores((Counter % 40 - 10) * 2 * -1);
				else if (Counter >= 320 && Counter % 5 == 0)
					PerpendicularSpores((Counter % 40 - 30) * 2);

				if (Counter == 400)
				{
					for (int i = 0; i <= 270; i += 90)
					{
						CrossedSpores(crossedBullets, i);
					}
				}
			}
			if (Counter >= 500 || !TransitionedToStage[3])
			{
				Counter = 0;
				TransitionedToStage[3] = true;
			}
		}

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 6;

			// Add this in for bosses that have a summon item, requires corresponding code in the item (See MinionBossSummonItem.cs)
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			// Automatically group with other bosses
			NPCID.Sets.BossBestiaryPriority.Add(Type);

			// Specify the debuffs it is immune to. Most NPCs are immune to Confused.
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
		}

		public override void SetDefaults() {
			NPC.width = 72;
			NPC.height = 132;
			NPC.damage = 30;
			NPC.defense = 15;
			NPC.lifeMax = Main.expertMode ? 17500 : 25000;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.DiscourageDespawn(5);
			NPC.value = Item.buyPrice(gold: 5);
			NPC.SpawnWithHigherTime(30);
			NPC.boss = true;
			NPC.npcSlots = 10f; // Take up open spawn slots, preventing random NPCs from spawning during the fight

			// Custom AI, 0 is "bound town NPC" AI which slows the NPC down and changes sprite orientation towards the target
			NPC.aiStyle = -1;

			// The following code assigns a music track to the boss in a simple way.
			if (!Main.dedServ) {
				Music = MusicID.Boss2;
			}
		}

        public override void OnKill()
        {
            // This sets downedChiyuriBoss to true, and if it was false before, it initiates a lantern night
            NPC.SetEventFlagCleared(ref DownedBossSystem.downedChiyuriBoss, -1);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			// Sets the description of this NPC that is listed in the bestiary
			bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement> {
				new MoonLordPortraitBackgroundProviderBestiaryInfoElement(), // Plain black background
				new FlavorTextBestiaryInfoElement("Chiyuri comes from the outside world to do some bamboozling.")
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
			Vector2 offset = new Vector2(113, 113); // offset to position texture correctly around the boss. Usually half the texture's size
			float alphaChange = .1f; // how much is the opacity going to change every frame

			if (Stage == 3 && Counter > 490)
				spriteBatch.Draw(MagicCircle.Value, NPC.Center - Main.screenPosition - offset, drawColor * ((500 - Counter) % 490 * alphaChange));
			else if (Stage == 3 && Counter > 310)
				spriteBatch.Draw(MagicCircle.Value, NPC.Center - Main.screenPosition - offset, drawColor * (Counter % 310 * alphaChange));

			return true;
		}

		public override void AI() {
			// This should almost always be the first code in AI() as it is responsible for finding the proper player target
			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
				NPC.TargetClosest();

			Player player = Main.player[NPC.target];

            if (player.dead)
            {
                // If the targeted player is dead, flee
                NPC.velocity.Y -= 0.04f;
                // This method makes it so when the boss is in "despawn range" (outside of the screen), it despawns in 10 ticks
                NPC.EncourageDespawn(10);
                return;
            }

            float speed = 5f;
			float inertia = 10;
			float slowdownRange = speed * 10;
			Vector2 toDestination = CenterPosition + MoveOffset - NPC.Center;
			Vector2 destNormalized = toDestination.SafeNormalize(Vector2.UnitY);

			if (Counter <= 1 || !Moving)
				Counter++;

			if (Counter == 1) {
                CenterPosition = new Vector2(player.Top.X, player.Top.Y - 320f);
                MoveOffset = new Vector2(Main.rand.Next(-200, 200), Main.rand.Next(-50, 50));
				toDestination = CenterPosition + MoveOffset - NPC.Center;
				destNormalized = toDestination.SafeNormalize(Vector2.UnitY);
				NPC.netUpdate = true;
			}

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
			NPC.frameCounter++;
		}

		public override void FindFrame(int frameHeight) {
			int frameSpeed = 20;
			NPC.frameCounter += 0.5f;
			NPC.frameCounter += NPC.velocity.Length() / 10f; // Make the counter go faster with more movement speed

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
	}
}


/*if (Main.netMode != NetmodeID.MultiplayerClient) {
	/*if (!moving && NPC.HasValidTarget && Counter > 300 && Counter % 3 == 0)
	{ DiagonalAimedSpore(player); }

	if (Counter % 30 == 0) {
		for (int i = 0; i < Main.rand.Next(1, 3); i++) {
			TopRandomSpore();
		}
	}

	if (!moving && Counter % 120 == 0)
	{ BlueRing(8); }

	switch (Counter % 180) {
		case 0:
			CrossedSpores(12, 45);
			break;
		case 30:
			CrossedSpores(12, 135);
			CrossedSpores(12, 315);
			break;
		case 60:
			CrossedSpores(12, 225);
			break;
		}

	if (!moving) {
		if (Counter % 240 <= 120 && Counter % 60 == 0) {
			DiagonalAimedRingLine(player, 10, 1);
			DiagonalAimedRingLine(player, 10, -1);
		}
	}

	if (Counter % 480 == 0)
		BlueLaser();

	if (Counter >= 460 && Counter % 5 == 0)
		PerpendicularSpores(Counter % 460 - 10);
	else if (Counter >= 435 && Counter % 5 == 0)
		PerpendicularSpores((Counter % 435 - 10) * -1);
} */