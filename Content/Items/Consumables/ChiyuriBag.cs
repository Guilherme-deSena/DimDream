using DimDream.Content.NPCs;
using DimDream.Content.Items.Weapons;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace DimDream.Content.Items.Consumables
{
    // Basic code for a boss treasure bag
    public class ChiyuriBag : ModItem
    {
        public override void SetStaticDefaults()
        {
            // This set is one that every boss bag should have.
            // It will create a glowing effect around the item when dropped in the world.
            // It will also let our boss bag drop dev armor.
            ItemID.Sets.BossBag[Type] = true;
            // ItemID.Sets.PreHardmodeLikeBossBag[Type] = true; // This should only be set for pre-hardmode bosses

            Item.ResearchUnlockCount = 3;
        }

        public override void SetDefaults()
        {
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.width = 24;
            Item.height = 24;
            Item.rare = ItemRarityID.Purple;
            Item.expert = true; // This makes sure that "Expert" displays in the tooltip and the item name color changes
        }

        public override bool CanRightClick()
        {
            return true;
        }

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.OneFromOptions(1, [ModContent.ItemType<FlowingBow>(),
                                                         ModContent.ItemType<RippleStaff>()]));
            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<ChiyuriBoss>()));
        }
    }
}