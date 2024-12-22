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
    internal class OrinEvilSpirit : ModNPC
    {
        public override string Texture => "DimDream/Content/NPCs/OrinEvilSpiritRed";
        public bool Initialized { get; set; } = false;

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
            return ModContent.NPCType<OrinBoss>();
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 6;

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
            NPC.width = 38;
            NPC.height = 36;
            NPC.damage = 7;
            NPC.defense = 20;
            NPC.lifeMax = 200;
            NPC.HitSound = SoundID.NPCHit9;
            NPC.DeathSound = SoundID.NPCDeath11;
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

            bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement> {
                new MoonLordPortraitBackgroundProviderBestiaryInfoElement(), // Plain black background
				new FlavorTextBestiaryInfoElement("A vengeful spirit from the Underworld. Orin seems particularly good at making them do her bidding.")
            });
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = ImmunityCooldownID.Bosses; // use the boss immunity cooldown counter, to prevent ignoring boss attacks by taking damage from other sources
            return true;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                int dustType = 264;

                for (int i = 0; i < 20; i++)
                {
                    Vector2 velocity = NPC.velocity + new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f));
                    Dust dust = Dust.NewDustPerfect(NPC.Center, dustType, velocity, 26, Color.White, Main.rand.NextFloat(1.5f, 2.4f));

                    dust.noLight = true;
                    dust.noGravity = true;
                    dust.fadeIn = Main.rand.NextFloat(0.3f, 0.8f);
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            int frameSpeed = 6;
            NPC.frameCounter++;

            if (NPC.frameCounter >= frameSpeed)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;

                if (NPC.frame.Y >= 6 * frameHeight)
                {
                    NPC.frame.Y = 0;
                }
            }
        }

        public override void OnKill()
        {
            SpawnDust(NPC.Center);
        }

        public void CreateDust()
        {
            int dustType = 264;

            Vector2 velocity = NPC.velocity + new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.2f));
            Dust dust = Dust.NewDustPerfect(NPC.Center, dustType, velocity, 26, Color.White, Main.rand.NextFloat(1f, 2f));

            dust.noGravity = true;
            dust.fadeIn = Main.rand.NextFloat(0.3f, 0.8f);
        }

        public bool Despawn()
        {
            NPC parent = Main.npc[ParentIndex];
            if (Main.netMode != NetmodeID.MultiplayerClient &&
                (!HasParent || (parent.dontTakeDamage && parent.localAI[2] >= 1) || !Main.npc[ParentIndex].active || Main.npc[ParentIndex].type != ParentType()))
            {
                // * Not spawned by the boss body (didn't assign a position and parent) or
                // * Parent isn't active or
                // * Parent isn't the body
                // => invalid, kill itself without dropping any items
                NPC.active = false;
                NPC.life = 0;
                NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                return true;
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

    internal class OrinEvilSpiritRed : OrinEvilSpirit
    {
        public override void AI()
        {
            // This should almost always be the first code in AI() as it is responsible for finding the proper player target
            Player player = Main.player[NPC.target];
            if (NPC.target < 0 || NPC.target == 255 || player.dead || !player.active || Vector2.Distance(NPC.Center, player.Center) > 3000f)
                NPC.TargetClosest();

            player = Main.player[NPC.target];

            if (Despawn())
                return;

            if (!Initialized)
            {
                Initialized = true;
                float maxSpeed = .2f;
                NPC.velocity = new(Main.rand.NextFloat(-maxSpeed, maxSpeed), Main.rand.NextFloat(-maxSpeed, maxSpeed));
            }

            if (Counter % 6 == 0)
                CreateDust();


            if (Counter % 60 == 0)
            {
                Vector2 toDestination = player.Center - NPC.Center;
                float offset = toDestination.SafeNormalize(Vector2.UnitY).ToRotation();
                CircleOfBalls(8, offset);
            }
            Counter++;
        }

        public void CircleOfBalls(int ballCount, float offset)
        {
            for (int i = 0; i < ballCount; i++)
            {
                float angle = MathHelper.TwoPi / ballCount * i + offset;
                Vector2 position = new(NPC.Center.X + 60 * MathF.Sin(angle), NPC.Center.Y - 60 * MathF.Cos(angle));
                Vector2 direction = new Vector2(0, -1).RotatedBy(angle);
                float speed = 1f;
                var entitySource = NPC.GetSource_FromAI();
                int type = ModContent.ProjectileType<LargeBallRed>();
                Projectile.NewProjectile(entitySource, position, direction * speed, type, NPC.damage, 2f, Main.myPlayer, 1f, ai2: ParentIndex);
            }
        }
    }

    internal class OrinEvilSpiritBlue : OrinEvilSpirit
    {
        public override string Texture => "DimDream/Content/NPCs/OrinEvilSpiritBlue";
        public int FrameToExplode
        {
            get => (int)NPC.ai[0];
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Counter < FrameToExplode - 120 || Counter % 20 < 10)
                return true;

            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Vector2 drawPosition = NPC.Center - screenPos;
            Color customColor = Color.Red;

            spriteBatch.Draw(
                texture,
                drawPosition,
                NPC.frame,
                customColor,
                NPC.rotation,
                NPC.frame.Size() / 2f,
                NPC.scale,
                SpriteEffects.None,
                0f
            );

            return false; // Return false to prevent the default drawing
        }

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
            if (Counter % 6 == 0)
                CreateDust();


            if (Counter == FrameToExplode && FrameToExplode != 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 toDestination = player.Center - NPC.Center;
                float angle = toDestination.SafeNormalize(Vector2.UnitY).ToRotation();
                BallBurst(3, angle, MathHelper.PiOver2);

                NPC.active = false;
                NPC.life = 0;
                NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
            }
        }

        public void BallBurst(int ballCount, float targetAngle, float totalSpread)
        {
            float individualSpacing = totalSpread / (ballCount - 1);
            float startAngle = targetAngle - totalSpread / 2;
            for (int i = 0; i < ballCount; i++)
            {
                float angle = startAngle + i * individualSpacing;
                Vector2 position = NPC.Center;
                Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle)).RotatedByRandom(MathHelper.PiOver2);
                float speed = 7f;
                var entitySource = NPC.GetSource_FromAI();
                int type = ModContent.ProjectileType<LargeBallBlue>();
                Projectile.NewProjectile(entitySource, position, direction * speed, type, NPC.damage, 2f, Main.myPlayer, ai2: ParentIndex);
            }
        }
    }
}
