using System;
using System.IO;
using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Threading;
using Pipliz.Mods.APIProvider.Jobs;
using NPC;
using BlockTypes.Builtin;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class FishersModEntries
  {
    public static string MOD_PREFIX = "mods.scarabol.fishers.";
    public static string ROD_TYPE_KEY = MOD_PREFIX + "rod";
    public static string FLOAT_TYPE_KEY = MOD_PREFIX + "float";
    public static string BAIT_TYPE_KEY = MOD_PREFIX + "bait";
    public static string FISH_TYPE_KEY = MOD_PREFIX + "fish";
    public static string COMPOST_TYPE_KEY = MOD_PREFIX + "compost";
    public static string COMPOSTMAKER_TYPE_KEY = MOD_PREFIX + "compostmaker";
    public static string COMPOST_PREFIX = COMPOST_TYPE_KEY + ".";

    public static List<Compostable> Compostables = new List<Compostable> () {
      new Compostable ("straw", 10),
      new Compostable ("leavestemperate", 7),
      new Compostable ("grasstemperate", 2),
      new Compostable ("dirt", 4),
      new Compostable ("logtemperate", 4),
      new Compostable ("logtaiga", 4),
      new Compostable ("leavestaiga", 7),
      new Compostable ("grasstaiga", 2),
      new Compostable ("grasstundra", 2),
      new Compostable ("grasssavanna", 2),
      new Compostable ("grassrainforest", 2),
      new Compostable ("berry", 8),
      new Compostable ("flax", 6),
      new Compostable ("flaxstage1", 2),
      new Compostable ("sappling", 2),
      new Compostable ("berrybush", 2),
      new Compostable ("cherrysapling", 2),
      new Compostable ("cherryblossom", 6),
      new Compostable ("wheatstage1", 6),
      new Compostable ("wheat", 6)
    };

    private static string AssetsDirectory;
    private static Recipe rodRecipe;
    private static Recipe compostMakerRecipe;

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnAssemblyLoaded, "scarabol.fishers.assemblyload")]
    public static void OnAssemblyLoaded (string path)
    {
      AssetsDirectory = Path.Combine (Path.GetDirectoryName (path), "assets");
      ModLocalizationHelper.localize (Path.Combine (AssetsDirectory, "localization"), MOD_PREFIX, false);
      Dictionary<string, string> prefixesCompost = new Dictionary<string, string> ();
      string[] prefixFiles = Directory.GetFiles (Path.Combine (AssetsDirectory, "localization"), "prefixes.json", SearchOption.AllDirectories);
      foreach (string filepath in prefixFiles) {
        try {
          JSONNode jsonPrefixes;
          if (Pipliz.JSON.JSON.Deserialize (filepath, out jsonPrefixes, false)) {
            string locName = Directory.GetParent (filepath).Name;
            string compostPrefix;
            if (jsonPrefixes.TryGetAs ("compost", out compostPrefix)) {
              prefixesCompost [locName] = compostPrefix;
            } else {
              Pipliz.Log.Write ("Prefix key 'compost' not found in '{0}' file", locName);
            }
          }
        } catch (Exception exception) {
          Pipliz.Log.WriteError (string.Format ("Exception reading localization from {0}; {1}", filepath, exception.Message));
        }
      }
      Dictionary<string, JSONNode> compostsLocalizations = new Dictionary<string, JSONNode> ();
      foreach (Compostable compostable in Compostables) {
        foreach (KeyValuePair<string, string> locPrefix in prefixesCompost) {
          JSONNode locNode;
          if (!compostsLocalizations.TryGetValue (locPrefix.Key, out locNode)) {
            locNode = new JSONNode ();
            compostsLocalizations.Add (locPrefix.Key, locNode);
          }
          string vanillaPath = MultiPath.Combine ("gamedata", "localization", locPrefix.Key, "types.json");
          JSONNode jsonVanilla;
          if (Pipliz.JSON.JSON.Deserialize (vanillaPath, out jsonVanilla, false)) {
            string localizedTypename;
            if (!jsonVanilla.TryGetAs (compostable.TypeName, out localizedTypename)) {
              localizedTypename = compostable.TypeName;
            }
            locNode.SetAs (compostable.TypeName, string.Format ("{0} ({1})", locPrefix.Value, localizedTypename));
          }
        }
      }
      foreach (KeyValuePair<string, JSONNode> locEntry in compostsLocalizations) {
        try {
          ModLocalizationHelper.localize (locEntry.Key, "types.json", locEntry.Value, COMPOST_PREFIX, false);
        } catch (Exception exception) {
          Pipliz.Log.WriteError (string.Format ("Exception while localization of {0}; {1}", locEntry.Key, exception.Message));
        }
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterStartup, "scarabol.fishers.registercallbacks")]
    public static void AfterStartup ()
    {
      Pipliz.Log.Write ("Loaded Fishers Mod 3.0 by Scarabol");
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.fishers.registerjobs")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.apiprovider.jobs.resolvetypes")]
    public static void RegisterJobs ()
    {
      BlockJobManagerTracker.Register<FisherJob> (ROD_TYPE_KEY);
      BlockJobManagerTracker.Register<ComposterJob> (COMPOSTMAKER_TYPE_KEY);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterAddingBaseTypes, "scarabol.fishers.addrawtypes")]
    public static void AfterAddingBaseTypes (Dictionary<string, ItemTypesServer.ItemTypeRaw> itemTypes)
    {
      itemTypes.Add (ROD_TYPE_KEY, new ItemTypesServer.ItemTypeRaw (ROD_TYPE_KEY, new JSONNode ()
        .SetAs ("onPlaceAudio", "woodPlace")
        .SetAs ("onRemoveAudio", "woodDeleteLight")
        .SetAs ("needsBase", true)
        .SetAs ("isSolid", false)
        .SetAs ("npcLimit", 0)
        .SetAs ("icon", MultiPath.Combine (AssetsDirectory, "icons", "rod.png"))
        .SetAs ("sideall", "blackplanks")
        .SetAs ("isRotatable", true)
        .SetAs ("rotatablex+", ROD_TYPE_KEY + "x+")
        .SetAs ("rotatablex-", ROD_TYPE_KEY + "x-")
        .SetAs ("rotatablez+", ROD_TYPE_KEY + "z+")
        .SetAs ("rotatablez-", ROD_TYPE_KEY + "z-")
      ));
      foreach (string xz in new string[] { "x+", "x-", "z+", "z-" }) {
        itemTypes.Add (ROD_TYPE_KEY + xz, new ItemTypesServer.ItemTypeRaw (ROD_TYPE_KEY + xz, new JSONNode ()
          .SetAs ("parentType", ROD_TYPE_KEY)
          .SetAs ("mesh", MultiPath.Combine (AssetsDirectory, "meshes", "rod" + xz + ".obj"))
        ));
      }
      itemTypes.Add (FLOAT_TYPE_KEY, new ItemTypesServer.ItemTypeRaw (FLOAT_TYPE_KEY, new JSONNode ()
        .SetAs ("onPlaceAudio", MOD_PREFIX + "waterSplashSoft")
        .SetAs ("icon", MultiPath.Combine (AssetsDirectory, "icons", "float.png"))
        .SetAs ("isSolid", false)
        .SetAs ("sideall", "SELF")
        .SetAs ("mesh", MultiPath.Combine (AssetsDirectory, "meshes", "float.obj"))
        .SetAs ("onRemove", new JSONNode (NodeType.Array))
      ));
      itemTypes.Add (BAIT_TYPE_KEY, new ItemTypesServer.ItemTypeRaw (BAIT_TYPE_KEY, new JSONNode ()
        .SetAs ("icon", MultiPath.Combine (AssetsDirectory, "icons", "bait.png"))
        .SetAs ("isPlaceable", false)
        .SetAs ("npcLimit", 0)
      ));
      itemTypes.Add (FISH_TYPE_KEY, new ItemTypesServer.ItemTypeRaw (FISH_TYPE_KEY, new JSONNode ()
        .SetAs ("icon", MultiPath.Combine (AssetsDirectory, "icons", "fish.png"))
        .SetAs ("isPlaceable", false)
        .SetAs ("nutritionalValue", 4)
      ));
      itemTypes.Add (COMPOSTMAKER_TYPE_KEY, new ItemTypesServer.ItemTypeRaw (COMPOSTMAKER_TYPE_KEY, new JSONNode ()
        .SetAs ("onPlaceAudio", "dirtPlace")
        .SetAs ("onRemoveAudio", "grassDelete")
        .SetAs ("npcLimit", 0)
        .SetAs ("icon", MultiPath.Combine (AssetsDirectory, "icons", "compostMaker.png"))
        .SetAs ("sideall", MOD_PREFIX + "compostMakerSide")
        .SetAs ("sidey+", MOD_PREFIX + "compostMakerTop")
        .SetAs ("sidey-", "dirt")
      ));
      itemTypes.Add (COMPOST_TYPE_KEY, new ItemTypesServer.ItemTypeRaw (COMPOST_TYPE_KEY, new JSONNode ()
        .SetAs ("icon", MultiPath.Combine (AssetsDirectory, "icons", "compost.png"))
        .SetAs ("isPlaceable", false)
      ));
      foreach (Compostable Comp in Compostables) {
        itemTypes.Add (COMPOST_PREFIX + Comp.TypeName, new ItemTypesServer.ItemTypeRaw (COMPOST_PREFIX + Comp.TypeName, new JSONNode ()
          .SetAs ("parentType", Comp.TypeName)
          .SetAs ("isPlaceable", false)
          .SetAs ("npcLimit", "2000000000")
        ));
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.fishers.loadrecipes")]
    [ModLoader.ModCallbackDependsOn ("pipliz.server.loadresearchables")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.server.loadsortorder")]
    public static void LoadRecipes ()
    {
      rodRecipe = new Recipe (ROD_TYPE_KEY + ".recipe", new InventoryItem (BuiltinBlocks.BlackPlanks, 2), new InventoryItem (ROD_TYPE_KEY, 1));
      compostMakerRecipe = new Recipe (COMPOSTMAKER_TYPE_KEY + ".recipe", new List<InventoryItem> () {
        new InventoryItem (BuiltinBlocks.Dirt, 1),
        new InventoryItem (BuiltinBlocks.Planks, 1)
      }, new InventoryItem (COMPOSTMAKER_TYPE_KEY, 1));
      RecipeStorage.AddDefaultLimitTypeRecipe ("pipliz.crafter", rodRecipe);
      RecipeStorage.AddDefaultLimitTypeRecipe ("pipliz.crafter", compostMakerRecipe);
      List<Compostable> CompostablesLookup = new List<Compostable> ();
      foreach (Compostable Comp in Compostables) {
        if (ItemTypes.IndexLookup.TryGetIndex (Comp.TypeName, out Comp.Type) &&
            ItemTypes.IndexLookup.TryGetIndex (COMPOST_PREFIX + Comp.TypeName, out Comp.CompostType)) {
          CompostablesLookup.Add (Comp);
        } else {
          Pipliz.Log.WriteError (string.Format ("Index lookup failed for compostable {0}", Comp.TypeName));
        }
      }
      Compostables = CompostablesLookup;
      RecipeStorage.AddDefaultLimitTypeRecipe ("scarabol.composter", new Recipe (BAIT_TYPE_KEY + ".recipe", new InventoryItem (COMPOST_TYPE_KEY, 1), new InventoryItem (BAIT_TYPE_KEY, 1)));
      foreach (Compostable Comp in Compostables) {
        RecipeStorage.AddDefaultLimitTypeRecipe ("scarabol.composter", new Recipe (Comp.TypeName + ".recipe", new InventoryItem (Comp.Type, Comp.Value), new InventoryItem (Comp.CompostType, 1)));
      }
      RecipePlayer.AddDefaultRecipe (rodRecipe);
      RecipePlayer.AddDefaultRecipe (compostMakerRecipe);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterSelectedWorld, "scarabol.fishers.registertexturemappings")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.server.registertexturemappingtextures")]
    public static void AfterSelectedWorld ()
    {
      ItemTypesServer.SetTextureMapping (FLOAT_TYPE_KEY, new ItemTypesServer.TextureMapping (
        MultiPath.Combine (FishersModEntries.AssetsDirectory, "textures", "albedo", "float.png")
      ));
      ItemTypesServer.SetTextureMapping (MOD_PREFIX + "compostMakerSide", new ItemTypesServer.TextureMapping (
        MultiPath.Combine (FishersModEntries.AssetsDirectory, "textures", "albedo", "compostMakerSide.png"),
        MultiPath.Combine (FishersModEntries.AssetsDirectory, "textures", "normal", "compostMakerSide.png"),
        null,
        MultiPath.Combine (FishersModEntries.AssetsDirectory, "textures", "heightSmoothnessSpecularity", "compostMakerSide.png")
      ));
      ItemTypesServer.SetTextureMapping (MOD_PREFIX + "compostMakerTop", new ItemTypesServer.TextureMapping (
        MultiPath.Combine (FishersModEntries.AssetsDirectory, "textures", "albedo", "compostMakerTop.png"),
        MultiPath.Combine (FishersModEntries.AssetsDirectory, "textures", "normal", "compostMakerTop.png"),
        null,
        MultiPath.Combine (FishersModEntries.AssetsDirectory, "textures", "heightSmoothnessSpecularity", "compostMakerTop.png")
      ));
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnTryChangeBlockUser, "scarabol.fishers.trychangeblock")]
    public static bool OnTryChangeBlockUser (ModLoader.OnTryChangeBlockUserData userData)
    {
      if (!userData.isPrimaryAction) {
        Vector3Int position = userData.VoxelToChange;
        string itemtypename = ItemTypes.IndexLookup.GetName (userData.typeToBuild);
        string basetypename = TypeHelper.RotatableToBasetype (itemtypename);
        if (basetypename.Equals (ROD_TYPE_KEY)) {
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
}
