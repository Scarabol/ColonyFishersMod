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
    public static string ROD_TYPE_KEY = MOD_PREFIX + "rod";
    public static string FLOAT_TYPE_KEY = MOD_PREFIX + "float";
    public static string BAIT_TYPE_KEY = MOD_PREFIX + "bait";
    public static string COMPOST_TYPE_KEY = MOD_PREFIX + "compost";

    public static List<Compostable> Compostables = new List<Compostable> () {
      new Compostable ("straw", 10),
      new Compostable ("leavestemperate", 7),
      new Compostable ("grasstemperate", 2),
      new Compostable ("dirt", 4),
      new Compostable ("logtemperate", 3),
      new Compostable ("logtaiga", 3),
      new Compostable ("leavestaiga", 7),
      new Compostable ("grasstaiga", 2),
      new Compostable ("grasstundra", 2),
      new Compostable ("grasssavanna", 2),
      new Compostable ("grassrainforest", 2),
      new Compostable ("berry", 8),
      new Compostable ("flax", 3),
      new Compostable ("sappling", 2),
      new Compostable ("berrybush", 2),
      new Compostable ("cherrysapling", 2),
      new Compostable ("cherryblossom", 6),
      new Compostable ("wheatstage1", 6),
      new Compostable ("wheat", 6)
    };

    private static string AssetsDirectory;
    private static string RelativeTexturesPath;
    private static string RelativeIconsPath;
    private static string RelativeMeshesPath;
    private static string RelativeAudioPath;
    private static Recipe rodRecipe;
    private static Recipe compostMakerRecipe;

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
      Pipliz.Log.Write ("Loaded Fishers Mod 1.4 by Scarabol");
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterDefiningNPCTypes, "scarabol.fishers.registerjobs")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.apiprovider.jobs.resolvetypes")]
    public static void AfterDefiningNPCTypes ()
    {
      BlockJobManagerTracker.Register<FisherJob> (ROD_TYPE_KEY);
      BlockJobManagerTracker.Register<ComposterJob> (MOD_PREFIX + "compostmaker");
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterAddingBaseTypes, "scarabol.fishers.addrawtypes")]
    public static void AfterAddingBaseTypes ()
    {
      ItemTypes.AddRawType (ROD_TYPE_KEY, new JSONNode ()
        .SetAs ("onPlaceAudio", "woodPlace")
        .SetAs ("onRemoveAudio", "woodDeleteLight")
        .SetAs ("needsBase", true)
        .SetAs ("isSolid", false)
        .SetAs ("npcLimit", 0)
        .SetAs ("icon", Path.Combine (RelativeIconsPath, "rod.png"))
        .SetAs ("sideall", "blackplanks")
        .SetAs ("isRotatable", true)
        .SetAs ("rotatablex+", ROD_TYPE_KEY + "x+")
        .SetAs ("rotatablex-", ROD_TYPE_KEY + "x-")
        .SetAs ("rotatablez+", ROD_TYPE_KEY + "z+")
        .SetAs ("rotatablez-", ROD_TYPE_KEY + "z-")
      );
      foreach (string xz in new string[] { "x+", "x-", "z+", "z-" }) {
        ItemTypes.AddRawType (ROD_TYPE_KEY + xz, new JSONNode ()
          .SetAs ("parentType", ROD_TYPE_KEY)
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
      ItemTypes.AddRawType (BAIT_TYPE_KEY, new JSONNode ()
        .SetAs ("icon", Path.Combine (RelativeIconsPath, "bait.png"))
        .SetAs ("isPlaceable", false)
        .SetAs ("npcLimit", 0)
      );
      ItemTypes.AddRawType (MOD_PREFIX + "fish", new JSONNode ()
        .SetAs ("icon", Path.Combine (RelativeIconsPath, "fish.png"))
        .SetAs ("isPlaceable", false)
        .SetAs ("nutritionalValue", 4)
      );
      ItemTypesServer.AddTextureMapping (MOD_PREFIX + "compostMakerSide", new JSONNode ()
        .SetAs ("albedo", MultiPath.Combine (RelativeTexturesPath, "albedo", "compostMakerSide"))
        .SetAs ("normal", "neutral")
        .SetAs ("emissive", "neutral")
        .SetAs ("height", "neutral")
      );
      ItemTypesServer.AddTextureMapping (MOD_PREFIX + "compostMakerTop", new JSONNode ()
        .SetAs ("albedo", MultiPath.Combine (RelativeTexturesPath, "albedo", "compostMakerTop"))
        .SetAs ("normal", "neutral")
        .SetAs ("emissive", "neutral")
        .SetAs ("height", "neutral")
      );
      ItemTypes.AddRawType (MOD_PREFIX + "compostmaker", new JSONNode ()
        .SetAs ("onPlaceAudio", "dirtPlace")
        .SetAs ("onRemoveAudio", "grassDelete")
        .SetAs ("npcLimit", 0)
        .SetAs ("icon", Path.Combine (RelativeIconsPath, "compostMaker.png"))
        .SetAs ("sideall", MOD_PREFIX + "compostMakerSide")
        .SetAs ("sidey+", MOD_PREFIX + "compostMakerTop")
        .SetAs ("sidey-", "dirt")
      );
      ItemTypes.AddRawType (COMPOST_TYPE_KEY, new JSONNode ()
        .SetAs ("icon", Path.Combine (RelativeIconsPath, "compost.png"))
        .SetAs ("isPlaceable", false)
      );
      foreach (Compostable Comp in Compostables) {
        ItemTypes.AddRawType (COMPOST_TYPE_KEY + "." + Comp.TypeName, new JSONNode ()
          .SetAs ("parentType", Comp.TypeName)
          .SetAs ("isPlaceable", false)
          .SetAs ("npcLimit", "2000000000")
        );
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.fishers.loadrecipes")]
    [ModLoader.ModCallbackDependsOn ("pipliz.blocknpcs.loadrecipes")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.apiprovider.registerrecipes")]
    public static void AfterItemTypesDefined ()
    {
      rodRecipe = new Recipe (new InventoryItem ("blackplanks", 2), new InventoryItem (ROD_TYPE_KEY, 1));
      compostMakerRecipe = new Recipe (new List<InventoryItem> () {
        new InventoryItem ("dirt", 1),
        new InventoryItem ("planks", 1)
      }, new InventoryItem (MOD_PREFIX + "compostmaker", 1));
      RecipeManager.AddRecipes ("pipliz.crafter", new List<Recipe> (){ rodRecipe, compostMakerRecipe });
      List<Compostable> CompostablesLookup = new List<Compostable> ();
      foreach (Compostable Comp in Compostables) {
        if (ItemTypes.IndexLookup.TryGetIndex (Comp.TypeName, out Comp.Type) &&
            ItemTypes.IndexLookup.TryGetIndex (COMPOST_TYPE_KEY + "." + Comp.TypeName, out Comp.CompostType)) {
          CompostablesLookup.Add (Comp);
        } else {
          Pipliz.Log.WriteError (string.Format ("Index lookup failed for compostable {0}", Comp.TypeName));
        }
      }
      Compostables = CompostablesLookup;
      List<Recipe> compostRecipes = new List<Recipe> ();
      compostRecipes.Add (new Recipe (new InventoryItem (COMPOST_TYPE_KEY, 1), new InventoryItem (BAIT_TYPE_KEY, 1)));
      foreach (Compostable Comp in Compostables) {
        compostRecipes.Add (new Recipe (new InventoryItem (Comp.Type, Comp.Value), new InventoryItem (Comp.CompostType, 1)));
      }
      RecipeManager.AddRecipes ("scarabol.composter", compostRecipes);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterWorldLoad, "scarabol.fishers.addplayercrafts")]
    public static void AfterWorldLoad ()
    {
      // add recipes here, otherwise they're inserted before vanilla recipes in player crafts
      RecipePlayer.AllRecipes.Add (rodRecipe);
      RecipePlayer.AllRecipes.Add (compostMakerRecipe);
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
