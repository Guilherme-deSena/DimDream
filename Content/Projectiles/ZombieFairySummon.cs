using DimDream.Content.Buffs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using System.Diagnostics.Metrics;

namespace DimDream.Content.Projectiles
{
    public class ZombieFairySummon : ModProjectile
    {
        private bool SlowMoving { get => Math.Abs(Projectile.velocity.X) > .2f && Math.Abs(Projectile.velocity.X) < .6f; }
        public int Counter
        {
            get => (int)Projectile.localAI[2];

            set => Projectile.localAI[2] = value;
        }
        public override string Texture => "DimDream/Content/NPCs/OrinFairyZombie";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 16;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

            Main.projPet[Projectile.type] = true;

            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public sealed override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 44;
            Projectile.tileCollide = false;

            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
        }


        public override bool? CanCutTiles()
        {
            return false;
        }

        public override void AI()
        {
            Counter++;

            Player owner = Main.player[Projectile.owner];

            if (!CheckActive(owner))
            {
                return;
            }

            GeneralBehavior(owner, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition);
            SearchForTargets(owner, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter);
            Movement(foundTarget, distanceFromTarget, targetCenter, distanceToIdlePosition, vectorToIdlePosition);
            Attack(foundTarget, distanceFromTarget, targetCenter);
            Visuals();
        }

        // This is the "active check", makes sure the minion is alive while the player is alive, and despawns if not
        private bool CheckActive(Player owner)
        {
            if (owner.dead || !owner.active)
            {
                owner.ClearBuff(ModContent.BuffType<ZombieFairyBuff>());

                return false;
            }

            if (owner.HasBuff(ModContent.BuffType<ZombieFairyBuff>()))
            {
                Projectile.timeLeft = 2;
            }

            return true;
        }

        private void GeneralBehavior(Player owner, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition)
        {
            Vector2 idlePosition = owner.Center;
            idlePosition.Y -= 48f; // Go up 48 coordinates (three tiles from the center of the player)

            // If your minion doesn't aimlessly move around when it's idle, you need to "put" it into the line of other summoned minions
            // The index is projectile.minionPos
            float minionPositionOffsetX = (10 + Projectile.minionPos * 40) * -owner.direction;
            idlePosition.X += minionPositionOffsetX; // Go behind the player

            // All of this code below this line is adapted from Spazmamini code (ID 388, aiStyle 66)

            // Teleport to player if distance is too big
            vectorToIdlePosition = idlePosition - Projectile.Center;
            distanceToIdlePosition = vectorToIdlePosition.Length();

            if (Main.myPlayer == owner.whoAmI && distanceToIdlePosition > 2000f)
            {
                Projectile.position = idlePosition;
                Projectile.velocity *= 0.1f;
                Projectile.netUpdate = true;
            }

            // If your minion is flying, you want to do this independently of any conditions
            float overlapVelocity = 0.04f;

            // Fix overlap with other minions
            foreach (var other in Main.ActiveProjectiles)
            {
                if (other.whoAmI != Projectile.whoAmI && other.owner == Projectile.owner && Math.Abs(Projectile.position.X - other.position.X) + Math.Abs(Projectile.position.Y - other.position.Y) < Projectile.width)
                {
                    if (Projectile.position.X < other.position.X)
                    {
                        Projectile.velocity.X -= overlapVelocity;
                    }
                    else
                    {
                        Projectile.velocity.X += overlapVelocity;
                    }

                    if (Projectile.position.Y < other.position.Y)
                    {
                        Projectile.velocity.Y -= overlapVelocity;
                    }
                    else
                    {
                        Projectile.velocity.Y += overlapVelocity;
                    }
                }
            }
        }

        private void SearchForTargets(Player owner, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter)
        {
            // Starting search distance
            distanceFromTarget = 700f;
            targetCenter = Projectile.position;
            foundTarget = false;

            // This code is required if your minion weapon has the targeting feature
            if (owner.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[owner.MinionAttackTargetNPC];
                float between = Vector2.Distance(npc.Center, Projectile.Center);

                if (between < 2000f)
                {
                    distanceFromTarget = between;
                    targetCenter = npc.Center;
                    foundTarget = true;
                }
            }

            if (!foundTarget)
            {
                // This code is required either way, used for finding a target
                foreach (var npc in Main.ActiveNPCs)
                {
                    if (npc.CanBeChasedBy())
                    {
                        float between = Vector2.Distance(npc.Center, Projectile.Center);
                        bool closest = Vector2.Distance(Projectile.Center, targetCenter) > between;
                        bool inRange = between < distanceFromTarget;
                        bool lineOfSight = Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height);
                        // Additional check for this specific minion behavior, otherwise it will stop attacking once it dashed through an enemy while flying though tiles afterwards
                        // The number depends on various parameters seen in the movement code below. Test different ones out until it works alright
                        bool closeThroughWall = between < 100f;

                        if (((closest && inRange) || !foundTarget) && (lineOfSight || closeThroughWall))
                        {
                            distanceFromTarget = between;
                            targetCenter = npc.Center;
                            foundTarget = true;
                        }
                    }
                }
            }
        }

        private void Movement(bool foundTarget, float distanceFromTarget, Vector2 targetCenter, float distanceToIdlePosition, Vector2 vectorToIdlePosition)
        {
            if (Counter < 0)
            {
                Projectile.velocity *= Projectile.velocity.Length() > .2f ? .9f : 0;
                return;
            }

            float speed = 8f;
            float inertia = 20f;

            if (foundTarget)
            {
                if (distanceFromTarget > 200f)
                {
                    Vector2 direction = targetCenter - Projectile.Center;
                    direction.Normalize();
                    direction *= speed;

                    Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction) / inertia;
                } else
                {
                    Counter = -100;
                }
            }
            else
            {
                // Minion doesn't have a target: return to player and idle
                if (distanceToIdlePosition > 600f)
                {
                    // Speed up the minion if it's away from the player
                    speed = 12f;
                    inertia = 60f;
                }
                else
                {
                    // Slow down the minion if closer to the player
                    speed = 4f;
                    inertia = 80f;
                }

                if (distanceToIdlePosition > 20f)
                {
                    // The immediate range around the player (when it passively floats about)

                    // This is a simple movement formula using the two parameters and its desired direction to create a "homing" movement
                    vectorToIdlePosition.Normalize();
                    vectorToIdlePosition *= speed;
                    Projectile.velocity = (Projectile.velocity * (inertia - 1) + vectorToIdlePosition) / inertia;
                }
                else if (Projectile.velocity == Vector2.Zero)
                {
                    // If there is a case where it's not moving at all, give it a little "poke"
                    Projectile.velocity.X = -0.15f;
                    Projectile.velocity.Y = -0.05f;
                }
            }
        }

        private void Attack(bool foundTarget, float distanceFromTarget, Vector2 targetCenter)
        {
            if (Main.myPlayer == Projectile.owner && Counter == -80 && foundTarget && distanceFromTarget < 1400f)
            {
                Vector2 direction = targetCenter - Projectile.Center;
                ShootDonuts(Projectile.Center, 6f, direction.ToRotation(), 8);
            }
        }

        private void ShootDonuts(Vector2 position, float speed, float offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = offset + MathHelper.TwoPi / count * i;
                Vector2 direction = new Vector2(0, -1).RotatedBy(angle);

                var entitySource = Projectile.GetSource_FromAI();
                int type = ModContent.ProjectileType<BasicDonutRedFriendly>();
                Projectile.NewProjectile(entitySource, position, direction * speed, type, Projectile.damage, Projectile.knockBack, Main.myPlayer);
            }
        }

        private void Visuals()
        {
            int frameSpeed = 6;

            int firstMoving = 8;
            int lastFrame = 14;
            int deadFrame = 15;

            Projectile.spriteDirection = Projectile.velocity.X >= 0 ? 1 : -1;
            Projectile.alpha = Counter < 0 && Counter > -80 ? 150 : 0;

            Projectile.frameCounter++;

            if (Counter < 0 && Counter >= -80)
                Projectile.frame = deadFrame;
            else if (SlowMoving)
                switch (Projectile.velocity.X)
                {
                    case > .5f:
                        Projectile.frame = firstMoving + 2;
                        break;
                    case > .3f:
                        Projectile.frame = firstMoving + 1;
                        break;
                    default:
                        Projectile.frame = firstMoving;
                        break;
                }
            else if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Math.Abs(Projectile.velocity.X) > .5f)
                {
                    if (Projectile.frame < firstMoving)
                        Projectile.frame = firstMoving;
                    if (Projectile.frame > lastFrame)
                        Projectile.frame = firstMoving + 2;
                }
                else if (Projectile.frame >= firstMoving)
                {
                    if (Projectile.frame > lastFrame || Projectile.frame == firstMoving)
                        Projectile.frame = 0;
                    else
                        Projectile.frame -= 2;
                }
            }

            if (Counter > 0 || Counter < -80)
                Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.78f);
            else if (Counter % 6 == 0)
                CreateDust();
        }

        private void CreateDust()
        {
            int dustType = 264;

            Vector2 velocity = Projectile.velocity + new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.2f));
            Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 26, Color.White, Main.rand.NextFloat(1f, 2f));

            dust.noGravity = true;
            dust.fadeIn = Main.rand.NextFloat(0.3f, 0.8f);
        }
    }
}