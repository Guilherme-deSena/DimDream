using Microsoft.Xna.Framework;
using Mono.Cecil;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DimDream.Content.Projectiles
{
    public class ArcaneBomb : ModProjectile
    {
        public bool Exploding { get; set; } = false;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 150;
            Projectile.penetrate = -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            HandleExplosion();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            HandleExplosion();
            return false;
        }

       
        public override void AI()
        {
            if (Exploding)
            {
                if (Projectile.timeLeft % 15 == 0)
                {
                    int fragments = 12;
                    bool isOval = Projectile.timeLeft % 75 != 15;
                    float startingAngle = isOval ? MathHelper.Pi / 4 : 0;
                    if (Projectile.timeLeft % 30 == 0) startingAngle *= -1;

                    SpawnFragments(fragments, isOval, startingAngle);
                }
                return; // Prevent all the remaining AI
            }

            if (Projectile.timeLeft < 90)
                HandleExplosion();

            Visuals();
        }
        public void HandleExplosion()
        {
            if (!Exploding)
            {
                Exploding = true;
                SoundEngine.PlaySound(SoundID.DD2_GoblinBomb, Projectile.position);
                Projectile.timeLeft = 91;
                Projectile.velocity = Vector2.Zero;
                Projectile.friendly = false;
                Projectile.alpha = 255;
            }
        }

        public void SpawnFragments(int fragmentCount, bool isOval, float startingAngle = 0)
        {
            for (int i = 0; i < fragmentCount; i++)
            {
                float instanceAngle = MathHelper.TwoPi / fragmentCount * i;
                Vector2 velocity = new Vector2(0, -1).RotatedBy(instanceAngle);
                if (isOval) velocity.X *= .5f;
                velocity = velocity.RotatedBy(startingAngle);
                float speed = 10f;
                var entitySource = Projectile.GetSource_FromAI();
                int type = ModContent.ProjectileType<ArcaneFragment>();
                Projectile p = Projectile.NewProjectileDirect(entitySource, Projectile.position, velocity * speed, type, Projectile.damage, 2f, Projectile.owner);
                p.rotation = startingAngle;
            }
        }

        public void Visuals()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            int frameSpeed = 15;
            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }
        }

    }
}