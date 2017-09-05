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
  public class FisherJob : BlockJobBase, IBlockJobBase, INPCTypeDefiner
  {
    private static float DELAY_JOB = 20.0f;
    private static float DELAY_BAIT = 2.5f;
    private static float DELAY_PULL = 1.8f;
    private static float DELAY_FISH = DELAY_JOB - 0.5f - DELAY_BAIT - DELAY_PULL;

    private enum PROCESS_STATE
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

    // not used actually
    public override float TimeBetweenJobs { get { return DELAY_JOB; } }

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

    public override void OnNPCDoJob (ref NPCBase.NPCState state)
    {
      usedNPC.LookAt ((position + jobdirvec * 3).Vector);
      ushort actualType;
      int waterBlocks = 0;
      for (int depth = 1; depth <= 2; depth++) {
        for (int x = -2; x <= 2; x++) {
          for (int z = 3 - 2; z <= 3 + 2; z++) {
            Vector3Int combinedVec = position + new Vector3Int (-jobdirvec.z * x + jobdirvec.x * z, -depth, jobdirvec.x * x + jobdirvec.z * z);
            if (World.TryGetTypeAt (combinedVec, out actualType) && actualType == BlockTypes.Builtin.BuiltinBlocks.Water) {
              waterBlocks++;
              if (waterBlocks >= 9) {
                break;
              }
            }
          }
        }
      }
      if (waterBlocks < 9) {
        state.SetIndicator (NPCIndicatorType.MissingItem, 8.0f, BlockTypes.Builtin.BuiltinBlocks.Water);
        OverrideCooldown (4.0f);
      } else if (process == PROCESS_STATE.BAITING) {
        bool placedFloat = false;
        for (int depth = 0; depth < 2; depth++) {
          Vector3Int posFloat = position.Add (0, -depth, 0) + jobdirvec * 3;
          if (World.TryGetTypeAt (posFloat, out actualType) && actualType == BlockTypes.Builtin.BuiltinBlocks.Air &&
              World.TryGetTypeAt (posFloat + Vector3Int.down, out actualType) && actualType == BlockTypes.Builtin.BuiltinBlocks.Water) {
            ServerManager.TryChangeBlock (posFloat, itemTypeFloat, ServerManager.SetBlockFlags.DefaultAudio);
            process = PROCESS_STATE.FISHING;
            OverrideCooldown (DELAY_FISH);
            placedFloat = true;
            break;
          }
        }
        if (!placedFloat) {
          state.SetIndicator (NPCIndicatorType.MissingItem, 8.0f, itemTypeFloat);
          OverrideCooldown (8.0f);
        }
      } else if (process == PROCESS_STATE.FISHING) {
        bool foundFloat = false;
        for (int depth = 0; depth < 2; depth++) {
          Vector3Int posFloat = position.Add (0, -depth, 0) + jobdirvec * 3;
          if (World.TryGetTypeAt (posFloat, out actualType) && actualType == itemTypeFloat) {
            ServerManager.TryChangeBlock (posFloat, BlockTypes.Builtin.BuiltinBlocks.Air, ServerManager.SetBlockFlags.Default);
            state.SetIndicator (NPCIndicatorType.Crafted, DELAY_PULL, itemTypeFish);
            ServerManager.SendAudio (position.Vector, FishersModEntries.MOD_PREFIX + "fishing");
            process = PROCESS_STATE.PULLING;
            OverrideCooldown (DELAY_PULL);
            foundFloat = true;
            break;
          }
        }
        if (!foundFloat) {
          Chat.Send (owner, string.Format ("Sam here from {0}, someone stole my fish!", position));
          process = PROCESS_STATE.NONE;
          OverrideCooldown (0.5f);
        }
      } else if (process == PROCESS_STATE.PULLING) {
        state.Inventory.Add (itemTypeFish);
        process = PROCESS_STATE.NONE;
        OverrideCooldown (0.5f);
      } else if (state.Inventory.TryGetOneItem (itemTypeBait)) {
        state.SetIndicator (NPCIndicatorType.Crafted, DELAY_BAIT, itemTypeFloat);
        process = PROCESS_STATE.BAITING;
        OverrideCooldown (DELAY_BAIT);
      } else {
        state.JobIsDone = true;
        needsBait = true;
        OverrideCooldown (0.5f);
      }
    }

    public override void OnNPCDoStockpile (ref NPCBase.NPCState state)
    {
      state.Inventory.TryDump (usedNPC.Colony.UsedStockpile);
      state.JobIsDone = true;
      if (!ToSleep) {
        for (int c = 0; c < 5; c++) {
          if (!usedNPC.Colony.UsedStockpile.TryGetOneItem (itemTypeBait)) {
            break;
          } else {
            state.Inventory.Add (itemTypeBait);
            needsBait = false;
          }
        }
        if (needsBait) {
          state.SetIndicator (NPCIndicatorType.MissingItem, 8.0f, itemTypeBait);
          OverrideCooldown (8.0f);
        } else {
          OverrideCooldown (0.5f);
        }
      } else {
        OverrideCooldown (0.5f);
      }
    }

    public override void OnRemove ()
    {
      ushort actualType;
      for (int depth = 0; depth < 2; depth++) {
        Vector3Int posFloat = position.Add (0, -depth, 0) + jobdirvec * 3;
        if (World.TryGetTypeAt (posFloat, out actualType) && actualType == itemTypeFloat) {
          ServerManager.TryChangeBlock (posFloat, BlockTypes.Builtin.BuiltinBlocks.Air, ServerManager.SetBlockFlags.Default);
          break;
        }
      }
      base.OnRemove ();
    }

    NPCTypeSettings INPCTypeDefiner.GetNPCTypeDefinition ()
    {
      NPCTypeSettings def = NPCTypeSettings.Default;
      def.keyName = NPCTypeKey;
      def.printName = "Fisher";
      def.maskColor1 = new UnityEngine.Color32 (110, 200, 255, 255);
      def.type = NPCTypeID.GetNextID ();
      return def;
    }
  }
}
