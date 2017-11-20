using System;
using System.IO;
using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Threading;
using Pipliz.APIProvider.Jobs;
using NPC;
using Server.NPCs;
using BlockTypes.Builtin;

namespace ScarabolMods
{
  public class ComposterJob : CraftingJobBase, IBlockJobBase, INPCTypeDefiner
  {
    private static float CompostValue = 0;
    private static ushort itemTypeBait;
    private static ushort itemTypeCompost;

    public override string NPCTypeKey { get { return "scarabol.composter"; } }

    public override float CraftingCooldown { get { return 10.0f; } }

    public override int MaxRecipeCraftsPerHaul { get { return 1; } }

    public override ITrackableBlock InitializeOnAdd (Vector3Int position, ushort type, Players.Player player)
    {
      itemTypeBait = ItemTypes.IndexLookup.GetIndex (FishersModEntries.BAIT_TYPE_KEY);
      itemTypeCompost = ItemTypes.IndexLookup.GetIndex (FishersModEntries.COMPOST_TYPE_KEY);
      base.InitializeOnAdd (position, type, player);
      return this;
    }

    public override ITrackableBlock InitializeFromJSON (Players.Player player, JSONNode node)
    {
      itemTypeBait = ItemTypes.IndexLookup.GetIndex (FishersModEntries.BAIT_TYPE_KEY);
      itemTypeCompost = ItemTypes.IndexLookup.GetIndex (FishersModEntries.COMPOST_TYPE_KEY);
      base.InitializeFromJSON (player, node);
      return this;
    }

    public override void OnNPCAtJob (ref NPCBase.NPCState state)
    {
      state.JobIsDone = true;
      this.usedNPC.LookAt (this.position.Vector);
      if (!state.Inventory.IsEmpty) {
        foreach (Compostable Comp in FishersModEntries.Compostables) {
          while (state.Inventory.TryGetOneItem (Comp.Type)) {
            CompostValue += 1.0f / Comp.Value;
          }
        }
      }
      if (usedNPC.Colony.UsedStockpile.AmountContained (itemTypeBait) >= RecipeStorage.GetPlayerStorage (owner).GetRecipeSetting (FishersModEntries.BAIT_TYPE_KEY + ".recipe").Limit) {
        state.SetIndicator (NPCIndicatorType.SuccessIdle, Pipliz.Random.NextFloat (CraftingCooldown, 2 * CraftingCooldown));
        state.JobIsDone = false;
      } else if (state.Inventory.GetAmount (itemTypeBait) >= 5) {
        shouldTakeItems = true;
        state.SetCooldown (0.1);
      } else if (CompostValue >= 1) {
        CompostValue--;
        state.Inventory.Add (itemTypeBait, 1);
        state.SetIndicator (NPCIndicatorType.Crafted, CraftingCooldown, itemTypeBait);
        state.JobIsDone = false;
      } else {
        shouldTakeItems = true;
        state.SetCooldown (0.1);
      }
    }

    public override void OnNPCAtStockpile (ref NPCBase.NPCState state)
    {
      state.JobIsDone = true;
      state.Inventory.TryDump (usedNPC.Colony.UsedStockpile);
      if (ToSleep) {
        return;
      }
      if (CompostValue >= 1) {
        shouldTakeItems = false;
        state.SetCooldown (0.1);
        return;
      }
      ushort MostlyType = BuiltinBlocks.Air;
      int MostlyLimit = 0;
      float MaxFactor = 1;
      foreach (Compostable Comp in FishersModEntries.Compostables) {
        int limit = RecipeStorage.GetPlayerStorage (owner).GetRecipeSetting (Comp.TypeName + ".recipe").Limit;
        if (limit > 0) {
          float Factor = ((float)usedNPC.Colony.UsedStockpile.AmountContained (Comp.Type)) / limit;
          if (Factor > MaxFactor) {
            MaxFactor = Factor;
            MostlyType = Comp.Type;
            MostlyLimit = limit;
          }
        }
      }
      if (MostlyType != BuiltinBlocks.Air) {
        while (state.Inventory.UsedCapacity < 50 && usedNPC.Colony.UsedStockpile.TryRemove (MostlyType, 1) &&
               usedNPC.Colony.UsedStockpile.AmountContained (MostlyType) > MostlyLimit) {
          shouldTakeItems = false;
          state.Inventory.Add (MostlyType, 1);
        }
        if (state.Inventory.Full && state.Inventory.TryGetOneItem (MostlyType)) {
          usedNPC.Colony.UsedStockpile.Add (MostlyType);
        }
        state.SetCooldown (0.1);
      } else {
        state.SetIndicator (NPCIndicatorType.MissingItem, CraftingCooldown, itemTypeCompost);
        state.JobIsDone = false;
      }
    }

    public override List<string> GetCraftingLimitsTriggers ()
    {
      return new List<string> () { FishersModEntries.COMPOSTMAKER_TYPE_KEY };
    }

    public override IList<Recipe> GetCraftingLimitsRecipes ()
    {
      return new List<Recipe> ();
    }

    NPCTypeStandardSettings INPCTypeDefiner.GetNPCTypeDefinition ()
    {
      return new NPCTypeStandardSettings () {
        keyName = NPCTypeKey,
        printName = "Composter",
        maskColor1 = new UnityEngine.Color32 (200, 255, 110, 255),
        type = NPCTypeID.GetNextID ()
      };
    }
  }

  public class Compostable
  {
    public string TypeName;
    public int Value;
    public ushort Type;
    public ushort CompostType;

    public Compostable (string typeName, int value)
    {
      this.TypeName = typeName;
      this.Value = value;
    }
  }
}
