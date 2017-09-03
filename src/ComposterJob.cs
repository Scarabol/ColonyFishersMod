using System;
using System.IO;
using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Threading;
using Pipliz.APIProvider.Recipes;
using Pipliz.APIProvider.Jobs;
using NPC;

namespace ScarabolMods
{
  public class ComposterJob : BlockJobBase, IBlockJobBase, IRecipeLimitsProvider, INPCTypeDefiner
  {
    private static float CompostValue = 0;
    private static bool ShouldTakeItems = false;

    public override string NPCTypeKey { get { return "scarabol.composter"; } }

    public override float TimeBetweenJobs { get { return 10.0f; } }

    public override bool NeedsItems { get { return (ShouldTakeItems); } }

    public ITrackableBlock InitializeOnAdd (Vector3Int position, ushort type, Players.Player player)
    {
      InitializeJob (player, position, 0);
      return this;
    }

    public override ITrackableBlock InitializeFromJSON (Players.Player player, JSONNode node)
    {
      InitializeJob (player, (Vector3Int)node ["position"], node.GetAs<int> ("npcID"));
      return this;
    }

    public override JSONNode GetJSON ()
    {
      return base.GetJSON ();
    }

    public override void OnNPCDoJob (ref NPCBase.NPCState state)
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
      ushort BaitType = ItemTypes.IndexLookup.GetIndex (FishersModEntries.BAIT_TYPE_KEY);
      if (usedNPC.Colony.UsedStockpile.AmountContained (BaitType) >= RecipeLimits.GetLimit (owner, BaitType)) {
        state.SetIndicator (NPCIndicatorType.SuccessIdle, Pipliz.Random.NextFloat (TimeBetweenJobs, 2 * TimeBetweenJobs));
        state.JobIsDone = false;
      } else if (state.Inventory.GetAmount (BaitType) >= 5) {
        ShouldTakeItems = true;
        OverrideCooldown (0.1);
      } else if (CompostValue >= 1) {
        CompostValue--;
        state.Inventory.Add (new InventoryItem (BaitType, 1));
        state.SetIndicator (NPCIndicatorType.Crafted, TimeBetweenJobs, BaitType);
        state.JobIsDone = false;
      } else {
        ShouldTakeItems = true;
        OverrideCooldown (0.1);
      }
    }

    public override void OnNPCDoStockpile (ref NPCBase.NPCState state)
    {
      state.JobIsDone = true;
      state.Inventory.TryDump (usedNPC.Colony.UsedStockpile);
      if (ToSleep) {
        return;
      }
      ushort GenericCompostType = ItemTypes.IndexLookup.GetIndex (FishersModEntries.COMPOST_TYPE_KEY);
      if (usedNPC.Colony.UsedStockpile.AmountContained (GenericCompostType) >= RecipeLimits.GetLimit (owner, GenericCompostType) || CompostValue >= 1) {
        ShouldTakeItems = false;
        OverrideCooldown (0.1);
        return;
      }
      ushort MostlyType = BlockTypes.Builtin.BuiltinBlocks.Air;
      int MostlyLimit = 0;
      float MaxFactor = 1;
      foreach (Compostable Comp in FishersModEntries.Compostables) {
        int limit = RecipeLimits.GetLimit (owner, Comp.CompostType);
        if (limit > 0) {
          float Factor = ((float)usedNPC.Colony.UsedStockpile.AmountContained (Comp.Type)) / limit;
          if (Factor > MaxFactor) {
            MaxFactor = Factor;
            MostlyType = Comp.Type;
            MostlyLimit = limit;
          }
        }
      }
      if (MostlyType != BlockTypes.Builtin.BuiltinBlocks.Air) {
        while (!state.Inventory.Full && usedNPC.Colony.UsedStockpile.TryRemove (MostlyType, 1) &&
               usedNPC.Colony.UsedStockpile.AmountContained (MostlyType) > MostlyLimit) {
          ShouldTakeItems = false;
          state.Inventory.Add (MostlyType, 1);
        }
        if (state.Inventory.Full && state.Inventory.TryGetOneItem (MostlyType)) {
          usedNPC.Colony.UsedStockpile.Add (MostlyType);
        }
        OverrideCooldown (0.1);
      } else {
        state.SetIndicator (NPCIndicatorType.MissingItem, TimeBetweenJobs, GenericCompostType);
        state.JobIsDone = false;
      }
    }

    public virtual string GetCraftingLimitsIdentifier ()
    {
      return this.NPCTypeKey;
    }

    public virtual Recipe[] GetCraftingLimitsRecipes ()
    {
      Recipe[] result;
      if (RecipeManager.RecipeStorage.TryGetValue (this.NPCTypeKey, out result)) {
        return result;
      }
      return null;
    }

    public virtual List<string> GetCraftingLimitsTriggers ()
    {
      return new List<string> () { FishersModEntries.MOD_PREFIX + "compostmaker" };
    }

    NPCTypeSettings INPCTypeDefiner.GetNPCTypeDefinition ()
    {
      NPCTypeSettings def = NPCTypeSettings.Default;
      def.keyName = NPCTypeKey;
      def.printName = "Composter";
      def.maskColor1 = new UnityEngine.Color32 (200, 255, 110, 255);
      def.type = NPCTypeID.GetNextID ();
      return def;
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
