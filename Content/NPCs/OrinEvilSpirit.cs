using DimDream.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
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

        public virtual int ProjDamage
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

        public bool HasParent => ParentIndex > -1;
        public int ParentStageHelper { get; set; }

        public float Counter
        {
            get => NPC.localAI[3];
            set => NPC.localAI[3] = value;
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 6;

            NPCID.Sets.DontDoHardmodeScaling[Type] = true;
            NPCID.Sets.CantTakeLunchMoney[Type] = true;
            
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;

            NPCID.Sets.BossBestiaryPriority.Add(Type);

            // If you don't want this NPC to show on the bestiary (if there is no reason to show a boss minion separately)
            // Make sure to remove SetBestiary code as well
            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new NPCID.Sets.NPCBestiaryDrawModifiers() 
            {
                Hide = true // Hides this NPC from the bestiary
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
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
                (!HasParent || (int)parent.localAI[2] != ParentStageHelper || !Main.npc[ParentIndex].active))
            {
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

    internal class OrinEvilSpiritCircleStill : OrinEvilSpirit
    {
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
                float maxSpeed = .2f;
                NPC.velocity = new(Main.rand.NextFloat(-maxSpeed, maxSpeed), Main.rand.NextFloat(-maxSpeed, maxSpeed));
            }

            if (Despawn())
                return;

            if (Counter % 6 == 0)
                CreateDust();


            if (Counter % 60 == 0)
            {
                Vector2 toDestination = player.Center - NPC.Center;
                float offset = toDestination.SafeNormalize(Vector2.UnitY).ToRotation() + Main.rand.NextFloat(-MathHelper.Pi / 40, MathHelper.Pi / 40);
                Circle(NPC.Center, 60, offset, 1f, 8, ModContent.ProjectileType<SpeedUpLargeBallRed>(), 20);
            }
            Counter++;
        }

        public void Circle(Vector2 center, float distance, float offset, float speed, int count, int type, int frameToSpeedUp = 0)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = offset + MathHelper.TwoPi / count * i;
                Vector2 positionOffset = new(center.X + MathF.Sin(angle) * distance, center.Y - MathF.Cos(angle) * distance);
                Vector2 velocity = new Vector2(0, -1).RotatedBy(angle);

                var entitySource = NPC.GetSource_FromAI();

                Projectile.NewProjectile(entitySource, positionOffset, velocity * speed, type, ProjDamage, 0f, Main.myPlayer, 6f, frameToSpeedUp, ParentIndex);
            }
        }
    }
    internal class OrinEvilSpiritCircleThrust : OrinEvilSpiritCircleStill
    {
        public override string Texture => "DimDream/Content/NPCs/OrinEvilSpiritBlue";

        public int FrameToExplode
        {
            get => (int)NPC.ai[0];
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
                ParentStageHelper = (int)Main.npc[ParentIndex].localAI[2];
            }

            if (Despawn())
                return;

            if (Counter == FrameToExplode)
            {
                NPC.active = false;
                NPC.life = 0;
                NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
            }

            if (Counter % 6 == 0)
                CreateDust();

            if (Counter % 30 == 0)
            {
                float offset = NPC.velocity.SafeNormalize(Vector2.UnitY).ToRotation() - MathHelper.Pi / 14;
                Circle(NPC.Center, 20, offset, 1f, 7, ModContent.ProjectileType<SpeedUpLargeBallBlue>(), 0);
            }
            Counter++;
        }
    }

    internal class OrinEvilSpiritBurst : OrinEvilSpirit
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

            if (!Initialized)
            {
                Initialized = true;
                ParentStageHelper = (int)Main.npc[ParentIndex].localAI[2];
            }

            if (Despawn())
                return;

            Counter++;
            if (Counter % 6 == 0)
                CreateDust();


            if (Counter == FrameToExplode && FrameToExplode != 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 toDestination = player.Center - NPC.Center;
                float angle = toDestination.SafeNormalize(Vector2.UnitY).ToRotation();
                BallBurst(NPC.Center, 3, angle, MathHelper.PiOver2);

                NPC.active = false;
                NPC.life = 0;
                NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
            }
        }

        public void BallBurst(Vector2 position, int ballCount, float targetAngle, float totalSpread)
        {
            float individualSpacing = totalSpread / (ballCount - 1);
            float startAngle = targetAngle - totalSpread / 2;
            for (int i = 0; i < ballCount; i++)
            {
                float angle = startAngle + i * individualSpacing;
                Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle)).RotatedByRandom(MathHelper.PiOver2);
                float speed = 7f;
                var entitySource = NPC.GetSource_FromAI();
                int type = ModContent.ProjectileType<BasicLargeBallBlue>();
                Projectile.NewProjectile(entitySource, position, direction * speed, type, ProjDamage, 0f, Main.myPlayer, ai2: ParentIndex);
            }
        }
    }

    internal class OrinEvilSpiritExplode : OrinEvilSpirit
    {
        public override string Texture => "DimDream/Content/NPCs/OrinEvilSpiritBlue";

        public int FrameToExplode
        {
            get => (int)NPC.ai[0];
        }
        public override bool CheckDead()
        {
            NPC.life = 1;
            NPC.dontTakeDamage = true;
            if (Counter < FrameToExplode)
                Counter = FrameToExplode;

            return false;
        }
        public override void AI()
        {
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
            if (Counter % 6 == 0)
                CreateDust();

            if (Counter >= FrameToExplode && FrameToExplode != 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 toDestination = player.Center - NPC.Center;
                float angle = toDestination.SafeNormalize(Vector2.UnitY).ToRotation();
                float speed = 22f;
                for (int i = 0; i < 4; i++)
                    BallExplosion(16, angle, speed - i*4);

                NPC.active = false;
                NPC.life = 0;
                NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
            }
        }

        public void BallExplosion(int ballCount, float offset, float speed)
        {
            for (int i = 0; i < ballCount; i++)
            {
                float angle = offset + MathHelper.TwoPi / ballCount * i;
                Vector2 position = NPC.Center;
                Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                var entitySource = NPC.GetSource_FromAI();
                int type = ModContent.ProjectileType<LargeBallBlue>();
                Projectile.NewProjectile(entitySource, position, direction * speed, type, ProjDamage, 0f, Main.myPlayer, 2, ai2: ParentIndex);
            }
        }

        
    }

    internal class OrinEvilSpiritSpiral : OrinEvilSpirit
    {
        public float OrbitOffset
        {
            get => NPC.localAI[0];
            set => NPC.localAI[0] = value;
        }

        public Vector2 OrbitCenter
        {
            get => new(NPC.localAI[1], NPC.localAI[2]);
            set
            {
                NPC.localAI[1] = value.X;
                NPC.localAI[2] = value.Y;
            }
        }

        public override int ProjDamage
        {
            get
            {
                if (Main.masterMode)
                    return NPC.damage / 12;

                if (Main.expertMode)
                    return NPC.damage / 4;

                return NPC.damage;
            }
        }

        public override void AI()
        {
            if (!Initialized)
            {
                Initialized = true;
                ParentStageHelper = (int)Main.npc[ParentIndex].localAI[2];
            }

            if (Despawn())
                return;

            Counter++;
            if (Counter % 6 == 0)
                CreateDust();

            if (OrbitCenter == Vector2.Zero)
            {
                OrbitCenter = NPC.Center + NPC.velocity; // At the start, velocity is specifically set to be the difference between the npc and the player
                OrbitOffset = .99f;
            }

            float rotationSpeed = MathHelper.Pi / 180f; // Adjust for slower/faster orbit

            Vector2 offset = NPC.Center - OrbitCenter;
            Vector2 offsetNormalized = offset.SafeNormalize(Vector2.UnitY);
            Vector2 rotationMovement = offsetNormalized.RotatedBy(rotationSpeed);


            offsetNormalized *= OrbitOffset;

            if (OrbitOffset <= 1)
                OrbitOffset += .0001f;
            else
                OrbitOffset += .00005f;

            NPC.velocity = rotationMovement * 200 - offsetNormalized * 200;


            if (Main.netMode != NetmodeID.MultiplayerClient && Counter < 100 && Counter % 3 == 0)
            {
                Vector2 toOrbitCenter = (OrbitCenter - NPC.Center).SafeNormalize(Vector2.UnitY);

                SpawnLoneRice(NPC.Center, toOrbitCenter, 300);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && offset.Length() < 8)
            {
                NPC.active = false;
                NPC.life = 0;
                NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
            }
        }

        public void SpawnLoneRice(Vector2 position, Vector2 direction, int frameToSpeedUp)
        {
            float speed = .01f;
            float maxSpeed = 3f;

            var entitySource = NPC.GetSource_FromAI();
            int type = ModContent.ProjectileType<SpeedUpRiceBlue>();
            Projectile p = Projectile.NewProjectileDirect(entitySource, position, direction * speed, type, ProjDamage, 0f, Main.myPlayer, maxSpeed, frameToSpeedUp, ai2: ParentIndex);
            p.timeLeft = 700;
        }
    }

    internal class OrinEvilSpiritRotating : OrinEvilSpiritSpiral
    {
        public override string Texture => "DimDream/Content/NPCs/OrinEvilSpiritBlue";

        public int FrameToExplode
        {
            get => (int)NPC.ai[0];
        }

        public Vector2 OrbitVelocity
        {
            get => new(NPC.ai[1], NPC.ai[2]);
            set 
            {
                NPC.ai[1] = value.X;
                NPC.ai[2] = value.Y;
            }
        }

        public override void AI()
        {
            NPC.dontTakeDamage = true;

            if (!Initialized)
            {
                Initialized = true;
                ParentStageHelper = (int)Main.npc[ParentIndex].localAI[2];
            }

            if (Despawn())
                return;

            Counter++;
            if (Counter % 6 == 0)
                CreateDust();

            if (OrbitCenter == Vector2.Zero)
                OrbitCenter = NPC.Center + NPC.velocity;
            else if (OrbitVelocity != Vector2.Zero)
                OrbitCenter += OrbitVelocity;

            float rotationSpeed = MathHelper.Pi / 20f; // Adjust for slower/faster orbit

            Vector2 offset = NPC.Center - OrbitCenter;
            Vector2 rotationMovement = offset.RotatedBy(rotationSpeed);

            NPC.velocity = rotationMovement - offset + OrbitVelocity;

            if (Main.netMode != NetmodeID.MultiplayerClient && Counter >= FrameToExplode)
            {
                NPC.active = false;
                NPC.life = 0;
                NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
            }
        }
    }
}
