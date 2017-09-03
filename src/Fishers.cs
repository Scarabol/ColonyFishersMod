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
  [ModLoader.ModManager]
  public static class FishersModEntries
  {
    public static string MOD_PREFIX = "mods.scarabol.fishers.";
    public static string JOB_ITEM_KEY = MOD_PREFIX + "rod";
    public static string JOB_TOOL_KEY = MOD_PREFIX + "bait";
    public static string FLOAT_TYPE_KEY = MOD_PREFIX + "float";
    private static string AssetsDirectory;
    private static string RelativeTexturesPath;
    private static string RelativeIconsPath;
    private static string RelativeMeshesPath;
    private static string RelativeAudioPath;
    private static Recipe rodRecipe;
    private static Recipe baitRecipe;

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnAssemblyLoaded, "scarabol.fishers.assemblyload")]
    public static void OnAssemblyLoaded (string path)
    {
      AssetsDirectory = Path.Combine (Path.GetDirectoryName (path), "assets");
      ModLocalizationHelper.localize (Path.Combine (AssetsDirectory, "localization"), MOD_PREFIX, false);
      // TODO this is really hacky (maybe better in future ModAPI)
      RelativeTexturesPath = new Uri (MultiPath.Combine (Path.GetFullPath ("gamedata"), "textures", "materials", "blocks", "albedo", "dummyfile")).MakeRelativeUri (new Uri (Path.Combine (AssetsDirectory, "textures"))).OriginalString;
      RelativeIconsPath = new Uri (MultiPath.Combine (Path.GetFullPath ("gamedata"), "textures", "icons", "dummyfile")).MakeRelativeUri (new Uri (Path.Combine (AssetsDirectory, "icons"))).OriginalString;
      RelativeMeshesPath = new Uri (MultiPath.Combine (Path.GetFullPath ("gamedata"), "meshes", "dummyfile")).MakeRelativeUri (new Uri (Path.Combine (AssetsDirectory, "meshes"))).OriginalString;
      RelativeAudioPath = new Uri (MultiPath.Combine (Path.GetFullPath ("gamedata"), "audio", "dummyfile")).MakeRelativeUri (new Uri (Path.Combine (AssetsDirectory, "audio"))).OriginalString;
      ModAudioHelper.IntegrateAudio (Path.Combine (AssetsDirectory, "audio"), MOD_PREFIX, RelativeAudioPath);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterStartup, "scarabol.fishers.registercallbacks")]
    public static void AfterStartup ()
    {
      Pipliz.Log.Write ("Loaded Fishers Mod 1.3 by Scarabol");
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterDefiningNPCTypes, "scarabol.fishers.registerjobs")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.apiprovider.jobs.resolvetypes")]
    public static void AfterDefiningNPCTypes ()
    {
      BlockJobManagerTracker.Register<FisherJob> (JOB_ITEM_KEY);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterAddingBaseTypes, "scarabol.fishers.addrawtypes")]
    public static void AfterAddingBaseTypes ()
    {
      ItemTypes.AddRawType (JOB_ITEM_KEY, new JSONNode ()
        .SetAs ("onPlaceAudio", "woodPlace")
        .SetAs ("onRemoveAudio", "woodDeleteLight")
        .SetAs ("needsBase", true)
        .SetAs ("isSolid", false)
        .SetAs ("npcLimit", 0)
        .SetAs ("icon", Path.Combine (RelativeIconsPath, "rod.png"))
        .SetAs ("sideall", "blackplanks")
        .SetAs ("isRotatable", true)
        .SetAs ("rotatablex+", JOB_ITEM_KEY + "x+")
        .SetAs ("rotatablex-", JOB_ITEM_KEY + "x-")
        .SetAs ("rotatablez+", JOB_ITEM_KEY + "z+")
        .SetAs ("rotatablez-", JOB_ITEM_KEY + "z-")
      );
      foreach (string xz in new string[] { "x+", "x-", "z+", "z-" }) {
        ItemTypes.AddRawType (JOB_ITEM_KEY + xz, new JSONNode ()
          .SetAs ("parentType", JOB_ITEM_KEY)
          .SetAs ("mesh", Path.Combine (RelativeMeshesPath, "rod" + xz + ".obj"))
        );
      }
      ItemTypesServer.AddTextureMapping (FLOAT_TYPE_KEY, new JSONNode ()
        .SetAs ("albedo", MultiPath.Combine (RelativeTexturesPath, "albedo", "float"))
        .SetAs ("normal", "neutral")
        .SetAs ("emissive", "neutral")
        .SetAs ("height", "neutral")
      );
      ItemTypes.AddRawType (FLOAT_TYPE_KEY, new JSONNode ()
        .SetAs ("onPlaceAudio", MOD_PREFIX + "waterSplashSoft")
        .SetAs ("icon", Path.Combine (RelativeIconsPath, "float.png"))
        .SetAs ("isSolid", false)
        .SetAs ("sideall", "SELF")
        .SetAs ("mesh", Path.Combine (RelativeMeshesPath, "float.obj"))
        .SetAs ("onRemove", new JSONNode (NodeType.Array))
      );
      ItemTypes.AddRawType (JOB_TOOL_KEY, new JSONNode ()
        .SetAs ("npcLimit", 1)
        .SetAs ("icon", Path.Combine (RelativeIconsPath, "bait.png"))
        .SetAs ("isPlaceable", false)
      );
      ItemTypes.AddRawType (MOD_PREFIX + "fish", new JSONNode ()
        .SetAs ("icon", Path.Combine (RelativeIconsPath, "fish.png"))
        .SetAs ("isPlaceable", false)
        .SetAs ("nutritionalValue", 4)
      );
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.fishers.loadrecipes")]
    [ModLoader.ModCallbackDependsOn ("pipliz.blocknpcs.loadrecipes")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.apiprovider.registerrecipes")]
    public static void AfterItemTypesDefined ()
    {
      rodRecipe = new Recipe (new InventoryItem ("blackplanks", 2), new InventoryItem (JOB_ITEM_KEY, 1));
      baitRecipe = new Recipe (new InventoryItem ("dirt", 4), new InventoryItem (JOB_TOOL_KEY, 1));
      RecipeManager.AddRecipes ("pipliz.crafter", new List<Recipe> () { rodRecipe, baitRecipe });
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterWorldLoad, "scarabol.fishers.addplayercrafts")]
    public static void AfterWorldLoad ()
    {
      // add recipes here, otherwise they're inserted before vanilla recipes in player crafts
      RecipePlayer.AllRecipes.Add (rodRecipe);
      RecipePlayer.AllRecipes.Add (baitRecipe);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnTryChangeBlockUser, "scarabol.fishers.trychangeblock")]
    public static bool OnTryChangeBlockUser (ModLoader.OnTryChangeBlockUserData userData)
    {
      if (!userData.isPrimaryAction) {
        Vector3Int position = userData.VoxelToChange;
        string itemtypename = ItemTypes.IndexLookup.GetName (userData.typeToBuild);
        string basetypename = TypeHelper.RotatableToBasetype (itemtypename);
        if (basetypename.Equals (JOB_ITEM_KEY)) {
          bool isBlocked = false;
          Vector3Int jobDir = TypeHelper.RotatableToVector (itemtypename);
          for (int c = 1; c <= 3; c++) {
            if (World.TryIsSolid (position + jobDir * c, out isBlocked) && isBlocked) {
              return false;
            }
          }
          for (int c = 2; c <= 3; c++) {
            if (World.TryIsSolid (position + jobDir * c + Vector3Int.up, out isBlocked) && isBlocked) {
              return false;
            }
          }
        }
      }
      return true;
    }
  }

  public class FisherJob : BlockJobBase, IBlockJobBase, INPCTypeDefiner
  {
    private enum PROCESS_STATE
    {
      NONE,
      BAITING,
      FISHING,
      PULLING
    }

    string jobtypename;
    Vector3Int jobdirvec;
    ushort itemBaitType;
    PROCESS_STATE process;
    bool needsBait;

    public override string NPCTypeKey { get { return "scarabol.fisher"; } }

    public override float TimeBetweenJobs { get { return 20.0f; } }

    public override bool NeedsItems { get { return (needsBait); } }

    public ITrackableBlock InitializeOnAdd (Vector3Int position, ushort type, Players.Player player)
    {
      jobtypename = ItemTypes.IndexLookup.GetName (type);
      jobdirvec = TypeHelper.RotatableToVector (jobtypename);
      itemBaitType = ItemTypes.IndexLookup.GetIndex (FishersModEntries.MOD_PREFIX + "bait");
      process = PROCESS_STATE.NONE;
      needsBait = false;
      InitializeJob (player, position, 0);
      return this;
    }

    public override ITrackableBlock InitializeFromJSON (Players.Player player, JSONNode node)
    {
      jobtypename = node.GetAs<string> ("jobtypename");
      jobdirvec = TypeHelper.RotatableToVector (jobtypename);
      itemBaitType = ItemTypes.IndexLookup.GetIndex (FishersModEntries.MOD_PREFIX + "bait");
      process = node.GetAs<PROCESS_STATE> ("process");
      needsBait = node.GetAs<bool> ("needsBait");
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
      ushort itemFloatType = ItemTypes.IndexLookup.GetIndex (FishersModEntries.FLOAT_TYPE_KEY);
      usedNPC.LookAt ((position + jobdirvec * 3).Vector);
      ushort actualType;
      int waterBlocks = 0;
      for (int x = -2; x <= 2; x++) {
        for (int z = 3 - 2; z <= 3 + 2; z++) {
          Vector3Int waterBlockOffset = new Vector3Int (x, -1, z);
          Vector3Int combinedVec = position + new Vector3Int (-jobdirvec.z * waterBlockOffset.x + jobdirvec.x * waterBlockOffset.z, waterBlockOffset.y, jobdirvec.x * waterBlockOffset.x + jobdirvec.z * waterBlockOffset.z);
          if (World.TryGetTypeAt (combinedVec, out actualType) && actualType == BlockTypes.Builtin.BuiltinBlocks.Water) {
            waterBlocks++;
            if (waterBlocks >= 9) {
              break;
            }
          }
        }
        if (waterBlocks >= 9) {
          break;
        }
      }
      if (waterBlocks < 9) {
        state.SetIndicator (NPCIndicatorType.MissingItem, 8.0f, BlockTypes.Builtin.BuiltinBlocks.Water);
        OverrideCooldown (4.0f);
      } else if (process == PROCESS_STATE.BAITING) {
        Vector3Int posFloat = position + jobdirvec * 3;
        if (World.TryGetTypeAt (posFloat, out actualType) && actualType == BlockTypes.Builtin.BuiltinBlocks.Air &&
            World.TryGetTypeAt (posFloat + Vector3Int.down, out actualType) && actualType == BlockTypes.Builtin.BuiltinBlocks.Water) {
          ServerManager.TryChangeBlock (posFloat, itemFloatType, ServerManager.SetBlockFlags.DefaultAudio);
          process = PROCESS_STATE.FISHING;
        } else {
          state.SetIndicator (NPCIndicatorType.MissingItem, 8.0f, ItemTypes.IndexLookup.GetIndex (FishersModEntries.MOD_PREFIX + "float"));
          OverrideCooldown (8.0f);
        }
      } else if (process == PROCESS_STATE.FISHING) {
        if (World.TryGetTypeAt (position + jobdirvec * 3, out actualType) && actualType == itemFloatType) {
          ServerManager.TryChangeBlock (position + jobdirvec * 3, BlockTypes.Builtin.BuiltinBlocks.Air, ServerManager.SetBlockFlags.DefaultAudio);
          state.SetIndicator (NPCIndicatorType.Crafted, 1.8f, ItemTypes.IndexLookup.GetIndex (FishersModEntries.MOD_PREFIX + "fish"));
          ServerManager.SendAudio (position.Vector, FishersModEntries.MOD_PREFIX + "fishing");
          process = PROCESS_STATE.PULLING;
          OverrideCooldown (1.8f);
        } else {
          Chat.Send (owner, string.Format ("Sam here from {0}, someone stole my fish!", position));
          process = PROCESS_STATE.NONE;
          OverrideCooldown (0.5f);
        }
      } else if (process == PROCESS_STATE.PULLING) {
        state.Inventory.Add (ItemTypes.IndexLookup.GetIndex (FishersModEntries.MOD_PREFIX + "fish"));
        process = PROCESS_STATE.NONE;
        OverrideCooldown (0.5f);
      } else if (state.Inventory.TryGetOneItem (itemBaitType)) {
        state.SetIndicator (NPCIndicatorType.Crafted, 2.5f, itemBaitType);
        process = PROCESS_STATE.BAITING;
        OverrideCooldown (2.5f);
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
          if (!usedNPC.Colony.UsedStockpile.TryGetOneItem (itemBaitType)) {
            break;
          } else {
            state.Inventory.Add (itemBaitType);
            needsBait = false;
          }
        }
        if (needsBait) {
          state.SetIndicator (NPCIndicatorType.MissingItem, 8.0f, itemBaitType);
          OverrideCooldown (8.0f);
        } else {
          OverrideCooldown (0.5f);
        }
      }
    }

    public override void OnRemove ()
    {
      Vector3Int posFloat = position + jobdirvec * 3;
      ushort actualType;
      if (World.TryGetTypeAt (posFloat, out actualType) && actualType == ItemTypes.IndexLookup.GetIndex (FishersModEntries.MOD_PREFIX + "float")) {
        ServerManager.TryChangeBlock (posFloat, BlockTypes.Builtin.BuiltinBlocks.Air, ServerManager.SetBlockFlags.Default);
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
