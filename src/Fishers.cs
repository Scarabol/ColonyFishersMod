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
    private static ushort rodType;

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
      Pipliz.Log.Write ("Loaded Fishers Mod 1.0 by Scarabol");
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
      ItemTypesServer.AddTextureMapping (JOB_ITEM_KEY, new JSONNode ()
        .SetAs ("albedo", "blackplanks")
        .SetAs ("normal", "neutral")
        .SetAs ("emissive", "neutral")
        .SetAs ("height", "neutral")
      );
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
      rodType = ItemTypes.IndexLookup.GetIndex (JOB_ITEM_KEY);
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
    ushort jobtype;
    string jobtypename;
    Vector3Int jobdirvec;
    ushort itemBaitType;
    string process = "";
    bool needsBait = false;

    // FIXME load and save from JSON

    public override string NPCTypeKey { get { return "scarabol.fisher"; } }

    public override float TimeBetweenJobs { get { return 20.0f; } }

    public override bool NeedsItems { get { return (needsBait); } }

    public ITrackableBlock InitializeOnAdd (Vector3Int position, ushort type, Players.Player player)
    {
      jobtype = type;
      jobtypename = ItemTypes.IndexLookup.GetName (type);
      jobdirvec = TypeHelper.RotatableToVector (jobtypename);
      itemBaitType = ItemTypes.IndexLookup.GetIndex (FishersModEntries.MOD_PREFIX + "bait");
      InitializeJob (player, position, 0);
      return this;
    }

    public override void OnNPCDoJob (ref NPCBase.NPCState state)
    {
      ushort itemFloatType = ItemTypes.IndexLookup.GetIndex (FishersModEntries.FLOAT_TYPE_KEY);
      usedNPC.LookAt ((position + jobdirvec * 3).Vector);
      // FIXME check for water otherwise, set indicator no water
      // FIXME use enumerable for process control flow
      if (process.Equals ("Baiting")) {
        Vector3Int posFloat = position + jobdirvec * 3;
        ushort actualType;
        if (World.TryGetTypeAt (posFloat, out actualType) && actualType == BlockTypes.Builtin.BuiltinBlocks.Air) {
          ServerManager.TryChangeBlock (posFloat, itemFloatType, ServerManager.SetBlockFlags.DefaultAudio);
          process = "Fishing";
        } else {
          state.SetIndicator (NPCIndicatorType.MissingItem, 8.0f, ItemTypes.IndexLookup.GetIndex (FishersModEntries.MOD_PREFIX + "float"));
          process = "";
          OverrideCooldown (8.0f);
        }
      } else if (process.Equals ("Fishing")) {
        ushort actualType;
        if (World.TryGetTypeAt (position + jobdirvec * 3, out actualType) && actualType == itemFloatType) {
          ServerManager.TryChangeBlock (position + jobdirvec * 3, BlockTypes.Builtin.BuiltinBlocks.Air, ServerManager.SetBlockFlags.DefaultAudio);
          int timeToPull = Pipliz.Random.Next (5, 10);
          state.SetIndicator (NPCIndicatorType.Crafted, timeToPull, ItemTypes.IndexLookup.GetIndex (FishersModEntries.MOD_PREFIX + "fish"));
          process = "Pulling";
          OverrideCooldown (timeToPull);
        } else {
          Chat.Send (owner, string.Format ("Sam here from {0}, someone stole my fish!", position));
          process = "";
          OverrideCooldown (0.5f);
        }
      } else if (process.Equals ("Pulling")) {
        state.Inventory.Add (ItemTypes.IndexLookup.GetIndex (FishersModEntries.MOD_PREFIX + "fish"));
        state.Inventory.Remove (new InventoryItem (itemBaitType, 1)); // needs to be done here to avoid empty state inventory
        process = "";
        OverrideCooldown (0.5f);
      } else if (state.Inventory.TryGetOneItem (itemBaitType)) {
        state.SetIndicator (NPCIndicatorType.Crafted, 2.5f, itemBaitType);
        process = "Baiting";
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
