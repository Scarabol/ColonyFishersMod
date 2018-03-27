using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Mods.APIProvider.Jobs;
using NPC;
using Server.NPCs;
using BlockTypes.Builtin;

namespace ScarabolMods
{
  public class FisherJob : BlockJobBase, IBlockJobBase, IRecipeLimitsProvider, INPCTypeDefiner
  {
    static float DELAY_JOB = 20.0f;
    static float DELAY_BAIT = 2.5f;
    static float DELAY_PULL = 1.8f;
    static float DELAY_FISH = DELAY_JOB - 0.5f - DELAY_BAIT - DELAY_PULL;

    enum PROCESS_STATE
    {
      NONE,
      BAITING,
      FISHING,
      PULLING
    }

    string jobtypename;
    Vector3Int jobdirvec;
    ushort itemTypeFloat;
    ushort itemTypeBait;
    ushort itemTypeFish;
    PROCESS_STATE process;
    bool needsBait;

    public override string NPCTypeKey { get { return "scarabol.fisher"; } }

    public override bool NeedsItems { get { return (needsBait); } }

    protected void Init ()
    {
      jobdirvec = TypeHelper.RotatableToVector (jobtypename);
      itemTypeFloat = ItemTypes.IndexLookup.GetIndex (FishersModEntries.FLOAT_TYPE_KEY);
      itemTypeBait = ItemTypes.IndexLookup.GetIndex (FishersModEntries.BAIT_TYPE_KEY);
      itemTypeFish = ItemTypes.IndexLookup.GetIndex (FishersModEntries.FISH_TYPE_KEY);
    }

    public ITrackableBlock InitializeOnAdd (Vector3Int position, ushort type, Players.Player player)
    {
      jobtypename = ItemTypes.IndexLookup.GetName (type);
      process = PROCESS_STATE.NONE;
      needsBait = false;
      Init ();
      InitializeJob (player, position, 0);
      return this;
    }

    public override ITrackableBlock InitializeFromJSON (Players.Player player, JSONNode node)
    {
      jobtypename = node.GetAs<string> ("jobtypename");
      process = node.GetAs<PROCESS_STATE> ("process");
      needsBait = node.GetAs<bool> ("needsBait");
      Init ();
      InitializeJob (player, (Vector3Int)node ["position"], node.GetAs<int> ("npcID"));
      return this;
    }

    public override JSONNode GetJSON ()
    {
      return base.GetJSON ()
        .SetAs ("jobtypename", jobtypename)
        .SetAs ("process", process)
        .SetAs ("needsBait", needsBait);
    }

    public override void OnNPCAtJob (ref NPCBase.NPCState state)
    {
      usedNPC.LookAt ((position + jobdirvec * 3).Vector);
      ushort actualType;
      int waterBlocks = 0;
      for (int depth = 1; depth <= 2; depth++) {
        for (int x = -2; x <= 2; x++) {
          for (int z = 3 - 2; z <= 3 + 2; z++) {
            Vector3Int combinedVec = position + new Vector3Int (-jobdirvec.z * x + jobdirvec.x * z, -depth, jobdirvec.x * x + jobdirvec.z * z);
            if (World.TryGetTypeAt (combinedVec, out actualType) && actualType == BuiltinBlocks.Water) {
              waterBlocks++;
              if (waterBlocks >= 9) {
                break;
              }
            }
          }
        }
      }
      if (waterBlocks < 9) {
        state.SetIndicator (new Shared.IndicatorState (8.0f, BuiltinBlocks.Water, true, false));
        state.SetCooldown (4.0f);
      } else if (process == PROCESS_STATE.BAITING) {
        bool placedFloat = false;
        for (int depth = 0; depth < 2; depth++) {
          Vector3Int posFloat = position.Add (0, -depth, 0) + jobdirvec * 3;
          if (World.TryGetTypeAt (posFloat, out actualType) && actualType == BuiltinBlocks.Air &&
              World.TryGetTypeAt (posFloat + Vector3Int.down, out actualType) && actualType == BuiltinBlocks.Water) {
            ServerManager.TryChangeBlock (posFloat, itemTypeFloat, Owner);
            process = PROCESS_STATE.FISHING;
            state.SetCooldown (DELAY_FISH);
            placedFloat = true;
            break;
          }
        }
        if (!placedFloat) {
          state.SetIndicator (new Shared.IndicatorState (8.0f, itemTypeFloat, true, false));
          state.SetCooldown (8.0f);
        }
      } else if (process == PROCESS_STATE.FISHING) {
        bool foundFloat = false;
        for (int depth = 0; depth < 2; depth++) {
          Vector3Int posFloat = position.Add (0, -depth, 0) + jobdirvec * 3;
          if (World.TryGetTypeAt (posFloat, out actualType) && actualType == itemTypeFloat) {
            ServerManager.TryChangeBlock (posFloat, BuiltinBlocks.Air, Owner);
            state.SetIndicator (new Shared.IndicatorState (DELAY_PULL, itemTypeFish));
            ServerManager.SendAudio (position.Vector, FishersModEntries.MOD_PREFIX + "fishing");
            process = PROCESS_STATE.PULLING;
            state.SetCooldown (DELAY_PULL);
            foundFloat = true;
            break;
          }
        }
        if (!foundFloat) {
          Chat.Send (Owner, string.Format ("Sam here from {0}, someone stole my fish!", position));
          process = PROCESS_STATE.NONE;
          state.SetCooldown (0.5f);
        }
      } else if (process == PROCESS_STATE.PULLING) {
        state.Inventory.Add (itemTypeFish);
        process = PROCESS_STATE.NONE;
        state.SetCooldown (0.5f);
      } else if (Stockpile.GetStockPile (Owner).AmountContained (itemTypeFish) >= RecipeStorage.GetPlayerStorage (Owner).GetRecipeSetting (FishersModEntries.FISH_TYPE_KEY + ".recipe").Limit) {
        state.SetIndicator (new Shared.IndicatorState (8.0f, NPCIndicatorType.None));
        state.SetCooldown (8.0f);
      } else if (state.Inventory.TryGetOneItem (itemTypeBait)) {
        state.SetIndicator (new Shared.IndicatorState (DELAY_BAIT, itemTypeFloat));
        process = PROCESS_STATE.BAITING;
        state.SetCooldown (DELAY_BAIT);
      } else {
        state.JobIsDone = true;
        needsBait = true;
        state.SetCooldown (0.5f);
      }
    }

    public override void OnNPCAtStockpile (ref NPCBase.NPCState state)
    {
      state.Inventory.Dump (usedNPC.Colony.UsedStockpile);
      state.JobIsDone = true;
      if (!ToSleep) {
        for (int c = 0; c < 5; c++) {
          if (!usedNPC.Colony.UsedStockpile.TryRemove (itemTypeBait)) {
            break;
          }
          state.Inventory.Add (itemTypeBait);
          needsBait = false;
        }
        if (needsBait) {
          state.SetIndicator (new Shared.IndicatorState (8.0f, itemTypeBait, true, false));
          state.SetCooldown (8.0f);
        } else {
          state.SetCooldown (0.5f);
        }
      } else {
        state.SetCooldown (0.5f);
      }
    }

    public override void OnRemove ()
    {
      ushort actualType;
      for (int depth = 0; depth < 2; depth++) {
        Vector3Int posFloat = position.Add (0, -depth, 0) + jobdirvec * 3;
        if (World.TryGetTypeAt (posFloat, out actualType) && actualType == itemTypeFloat) {
          ServerManager.TryChangeBlock (posFloat, BuiltinBlocks.Air, Owner);
          break;
        }
      }
      base.OnRemove ();
    }

    public virtual string GetCraftingLimitsType ()
    {
      return NPCTypeKey;
    }

    public virtual IList<Recipe> GetCraftingLimitsRecipes ()
    {
      return new List<Recipe> { new Recipe (FishersModEntries.FISH_TYPE_KEY + ".recipe",
          new InventoryItem (FishersModEntries.BAIT_TYPE_KEY, 1),
          new InventoryItem (FishersModEntries.FISH_TYPE_KEY, 1)
        )
      };
    }

    public virtual List<string> GetCraftingLimitsTriggers ()
    {
      return new List<string>  {
        FishersModEntries.ROD_TYPE_KEY + "x+",
        FishersModEntries.ROD_TYPE_KEY + "x-",
        FishersModEntries.ROD_TYPE_KEY + "z+",
        FishersModEntries.ROD_TYPE_KEY + "z-"
      };
    }

    NPCTypeStandardSettings INPCTypeDefiner.GetNPCTypeDefinition ()
    {
      return new NPCTypeStandardSettings {
        keyName = NPCTypeKey,
        printName = "Fisher",
        maskColor1 = new UnityEngine.Color32 (110, 200, 255, 255),
        type = NPCTypeID.GetNextID ()
      };
    }
  }
}
