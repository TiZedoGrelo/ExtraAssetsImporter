﻿using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using Game.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Experimental.Rendering;
using UnityEngine;
using Colossal.AssetPipeline;
using Unity.Mathematics;
using Extra.Lib;
using Extra.Lib.UI;
using System.Collections;
using Colossal.PSI.Common;
using Colossal.Json;
using Unity.Entities;
using Game.SceneFlow;
using Colossal.Localization;
using System.Linq;

namespace ExtraAssetsImporter.Importers;

public class JSONDecalsMaterail
{
	public Dictionary<string, float> Float = [];
	public Dictionary<string, Vector4> Vector = [];
	public List<PrefabIdentifierInfo> prefabIdentifierInfos = [];
}

internal class DecalsImporter
{
	internal static List<string> FolderToLoadDecals = [];
	internal static Dictionary<PrefabBase, string> DecalsDataBase = [];
	private static bool DecalsLoading = false;
	internal static bool DecalsLoaded = false;

    //private static readonly List<string> validName = ["_BaseColorMap.png", "_NormalMap.png", "_MaskMap.png"];

    //internal static void SearchForCustomDecalsFolder(string ModsFolderPath)
    //{
    //    foreach (DirectoryInfo directory in new DirectoryInfo(ModsFolderPath).GetDirectories())
    //    {
    //        if (File.Exists($"{directory.FullName}\\CustomDecals.zip"))
    //        {
    //            if (Directory.Exists($"{directory.FullName}\\CustomDecals")) Directory.Delete($"{directory.FullName}\\CustomDecals", true);
    //            ZipFile.ExtractToDirectory($"{directory.FullName}\\CustomDecals.zip", directory.FullName);
    //            File.Delete($"{directory.FullName}\\CustomDecals.zip");
    //        }
    //        if (Directory.Exists($"{directory.FullName}\\CustomDecals")) AddCustomDecalsFolder($"{directory.FullName}\\CustomDecals");
    //    }
    //}

    internal static void LoadLocalization()
	{

		Dictionary<string, string> csLocalisation = [];

		foreach (string folder in FolderToLoadDecals)
		{
			foreach (string decalsCat in Directory.GetDirectories(folder))
			{

                //if (!csLocalisation.ContainsKey($"SubServices.NAME[{new DirectoryInfo(decalsCat).Name} Decals]"))
                //{
                //	csLocalisation.Add($"SubServices.NAME[{new DirectoryInfo(decalsCat).Name} Decals]", $"{new DirectoryInfo(decalsCat).Name} Decals");
                //}

                //if (!csLocalisation.ContainsKey($"Assets.SUB_SERVICE_DESCRIPTION[{new DirectoryInfo(decalsCat).Name} Decals]"))
                //{
                //	csLocalisation.Add($"Assets.SUB_SERVICE_DESCRIPTION[{new DirectoryInfo(decalsCat).Name} Decals]", $"{new DirectoryInfo(decalsCat).Name} Decals");
                //}

                


                foreach (string filePath in Directory.GetDirectories(decalsCat))
				{
                    FileInfo[] fileInfos = new DirectoryInfo(folder).Parent.GetFiles(".dll");
                    string modName = fileInfos.Length > 0 ? fileInfos[0].Name.Split('_')[0] : new DirectoryInfo(folder).Parent.Name.Split('_')[0];
                    string decalName = $"{modName} {new DirectoryInfo(decalsCat).Name} {new DirectoryInfo(filePath).Name} Decal";

					if (!csLocalisation.ContainsKey($"Assets.NAME[{decalName}]") && !GameManager.instance.localizationManager.activeDictionary.ContainsID($"Assets.NAME[{decalName}]")) csLocalisation.Add($"Assets.NAME[{decalName}]", new DirectoryInfo(filePath).Name);
					if (!csLocalisation.ContainsKey($"Assets.DESCRIPTION[{decalName}]") && !GameManager.instance.localizationManager.activeDictionary.ContainsID($"Assets.DESCRIPTION[{decalName}]")) csLocalisation.Add($"Assets.DESCRIPTION[{decalName}]", new DirectoryInfo(filePath).Name);
				}
			}
		}

		foreach (string localeID in GameManager.instance.localizationManager.GetSupportedLocales())
		{
            GameManager.instance.localizationManager.AddSource(localeID, new MemorySource(csLocalisation));
        }
	}

	public static void AddCustomDecalsFolder(string path)
	{
		if (FolderToLoadDecals.Contains(path)) return;
		FolderToLoadDecals.Add(path);
		Icons.LoadIcons(new DirectoryInfo(path).Parent.FullName);
	}

    public static void RemoveCustomDecalsFolder(string path)
    {
        if (!FolderToLoadDecals.Contains(path)) return;
        FolderToLoadDecals.Remove(path);
        Icons.UnLoadIcons(new DirectoryInfo(path).Parent.FullName);
    }

    internal static IEnumerator CreateCustomDecals()
	{
		if (DecalsLoading || FolderToLoadDecals.Count <= 0) yield break;

        DecalsLoading = true;

		int numberOfDecals = 0;
		int ammoutOfDecalsloaded = 0;
		int failedDecals = 0;

		var notificationInfo = ExtraLib.m_NotificationUISystem.AddOrUpdateNotification(
			$"{nameof(ExtraAssetsImporter)}.{nameof(EAI)}.{nameof(CreateCustomDecals)}",
			title: "EAI, Importing the custom decals.",
			progressState: ProgressState.Indeterminate,
			thumbnail: $"{Icons.COUIBaseLocation}/Icons/NotificationInfo/Decals.svg",
			progress: 0
		);

		foreach (string folder in FolderToLoadDecals)
			foreach (string catFolder in Directory.GetDirectories(folder))
				foreach (string decalsFolder in Directory.GetDirectories(catFolder))
					numberOfDecals++;

		ExtraAssetsMenu.AssetCat assetCat = ExtraAssetsMenu.GetOrCreateNewAssetCat("Decals", $"{Icons.COUIBaseLocation}/Icons/UIAssetCategoryPrefab/Decals.svg");

        Dictionary<string, string> csLocalisation = [];

        foreach (string folder in FolderToLoadDecals)
		{
			foreach (string catFolder in Directory.GetDirectories(folder))
			{
				foreach (string decalsFolder in Directory.GetDirectories(catFolder))
				{
                    string decalName = new DirectoryInfo(decalsFolder).Name;
                    notificationInfo.progressState = ProgressState.Progressing;
					notificationInfo.progress = (int)(ammoutOfDecalsloaded / (float)numberOfDecals * 100);
                    notificationInfo.text = $"Loading : {decalName}";
					try
					{
                        string catName = new DirectoryInfo(catFolder).Name;
                        FileInfo[] fileInfos = new DirectoryInfo(folder).Parent.GetFiles("*.dll");
                        string modName = fileInfos.Length > 0 ? Path.GetFileNameWithoutExtension(fileInfos[0].Name).Split('_')[0] : new DirectoryInfo(folder).Parent.Name.Split('_')[0];
                        string fullDecalName = $"{modName} {catName} {decalName} Decal";
                        CreateCustomDecal(decalsFolder, decalName, catName, modName, fullDecalName, assetCat);
                        if (!csLocalisation.ContainsKey($"Assets.NAME[{fullDecalName}]") && !GameManager.instance.localizationManager.activeDictionary.ContainsID($"Assets.NAME[{fullDecalName}]")) csLocalisation.Add($"Assets.NAME[{fullDecalName}]", decalName);
                        if (!csLocalisation.ContainsKey($"Assets.DESCRIPTION[{fullDecalName}]") && !GameManager.instance.localizationManager.activeDictionary.ContainsID($"Assets.DESCRIPTION[{fullDecalName}]")) csLocalisation.Add($"Assets.DESCRIPTION[{fullDecalName}]", decalName);
                    }
					catch (Exception e)
					{
						failedDecals++;
						EAI.Logger.Error($"Failed to load the custom decal at {decalsFolder} | ERROR : {e}");
					}
					ammoutOfDecalsloaded++;
                    yield return null;
				}
			}
		}

        foreach (string localeID in GameManager.instance.localizationManager.GetSupportedLocales())
        {
            GameManager.instance.localizationManager.AddSource(localeID, new MemorySource(csLocalisation));
        }

        ExtraLib.m_NotificationUISystem.RemoveNotification(
			identifier: notificationInfo.id,
			delay: 5f,
			text: $"Complete, {numberOfDecals - failedDecals} Loaded, {failedDecals} failed.",
			progressState: ProgressState.Complete,
			progress: 100
		);

		//LoadLocalization();
		DecalsLoaded = true;
    }

	private static void CreateCustomDecal(string folderPath, string decalName, string catName, string modName, string fullDecalName, ExtraAssetsMenu.AssetCat assetCat)
	{

		// RenderPrefab DecalRenderPrefab = (RenderPrefab)DecalPrefab.m_Meshes[0].m_Mesh;
		// SpawnableObject DecalSpawnableObjectPrefab = DecalPrefab.GetComponent<SpawnableObject>();
		// DecalProperties DecalPropertiesPrefab = DecalRenderPrefab.GetComponent<DecalProperties>();

		//string fullDecalName = $"{modName} {catName} {decalName} Decal";

		StaticObjectPrefab decalPrefab = (StaticObjectPrefab)ScriptableObject.CreateInstance("StaticObjectPrefab");
		decalPrefab.name = fullDecalName;

		Surface decalSurface = new(decalName, "DefaultDecal");

		if (File.Exists(folderPath + "\\decal.json"))
		{
			JSONDecalsMaterail jSONMaterail = Decoder.Decode(File.ReadAllText(folderPath + "\\decal.json")).Make<JSONDecalsMaterail>();
			foreach (string key in jSONMaterail.Float.Keys) { decalSurface.AddProperty(key, jSONMaterail.Float[key]); }
			foreach (string key in jSONMaterail.Vector.Keys) { decalSurface.AddProperty(key, jSONMaterail.Vector[key]); }

            VersionCompatiblity(jSONMaterail, catName, decalName);
            if (jSONMaterail.prefabIdentifierInfos.Count > 0)
			{
                ObsoleteIdentifiers obsoleteIdentifiers = decalPrefab.AddComponent<ObsoleteIdentifiers>();
				obsoleteIdentifiers.m_PrefabIdentifiers = [.. jSONMaterail.prefabIdentifierInfos];
			}
		}

		// if(!decalSurface.floats.ContainsKey("_DrawOrder")) decalSurface.AddProperty("_DrawOrder", 0f);

		byte[] fileData;

		fileData = File.ReadAllBytes(folderPath + "\\_BaseColorMap.png");
		Texture2D texture2D_BaseColorMap_Temp = new(1, 1);
		if (!texture2D_BaseColorMap_Temp.LoadImage(fileData)) { UnityEngine.Debug.LogError($"[EAI] Failed to Load the BaseColorMap image for the {decalName} decal."); return; }

		Texture2D texture2D_BaseColorMap = new(texture2D_BaseColorMap_Temp.width, texture2D_BaseColorMap_Temp.height, GraphicsFormat.R8G8B8A8_SRGB, texture2D_BaseColorMap_Temp.mipmapCount, TextureCreationFlags.MipChain)
		{
			name = $"{decalName}_BaseColorMap"
		};

		for (int i = 0; i < texture2D_BaseColorMap_Temp.mipmapCount; i++)
		{
			texture2D_BaseColorMap.SetPixels(texture2D_BaseColorMap_Temp.GetPixels(i), i);
		}
		texture2D_BaseColorMap.Apply();
		if (!File.Exists(folderPath + "\\icon.png")) texture2D_BaseColorMap.ResizeTexture(128).SaveTextureAsPNG(folderPath + "\\icon.png");//ELT.ResizeTexture(texture2D_BaseColorMap_Temp, 128, folderPath + "\\icon.png");
		TextureImporter.Texture textureImporterBaseColorMap = new($"{decalName}_BaseColorMap", folderPath + "\\" + "_BaseColorMap.png", texture2D_BaseColorMap);
		//textureImporterBaseColorMap.CompressBC(1);

		decalSurface.AddProperty("_BaseColorMap", textureImporterBaseColorMap);

		if (File.Exists(folderPath + "\\_NormalMap.png"))
		{
			fileData = File.ReadAllBytes(folderPath + "\\_NormalMap.png");
			Texture2D texture2D_NormalMap_temp = new(1, 1)
			{
				name = $"{decalName}_NormalMap"
			};
			if (texture2D_NormalMap_temp.LoadImage(fileData))
			{
				Texture2D texture2D_NormalMap = new(texture2D_NormalMap_temp.width, texture2D_NormalMap_temp.height, GraphicsFormat.R8G8B8A8_SRGB, texture2D_NormalMap_temp.mipmapCount, TextureCreationFlags.None)
				{
					name = $"{decalName}_NormalMap"
				};

				for (int i = 0; i < texture2D_NormalMap_temp.mipmapCount; i++)
				{
					texture2D_NormalMap.SetPixels(texture2D_NormalMap_temp.GetPixels(i), i);
				}
				texture2D_NormalMap.Apply();
                TextureImporter.Texture textureImporterNormalMap = new($"{decalName}_NormalMap", folderPath + "\\" + "_NormalMap.png", texture2D_NormalMap);
				textureImporterNormalMap.CompressBC(1, Colossal.AssetPipeline.Native.NativeTextures.BlockCompressionFormat.BC5);
				decalSurface.AddProperty("_NormalMap", textureImporterNormalMap);
            };
		}

		if (File.Exists(folderPath + "\\_MaskMap.png"))
		{
			fileData = File.ReadAllBytes(folderPath + "\\_MaskMap.png");
			Texture2D texture2D_MaskMap_temp = new(1, 1);
			if (texture2D_MaskMap_temp.LoadImage(fileData))
			{
				Texture2D texture2D_MaskMap = new(texture2D_MaskMap_temp.width, texture2D_MaskMap_temp.height, GraphicsFormat.R8G8B8A8_SRGB, texture2D_MaskMap_temp.mipmapCount, TextureCreationFlags.None)
				{
					name = $"{decalName}_MaskMap"
				};

				for (int i = 0; i < texture2D_MaskMap_temp.mipmapCount; i++)
				{
					texture2D_MaskMap.SetPixels(texture2D_MaskMap_temp.GetPixels(i), i);
				}
				texture2D_MaskMap.Apply();
				TextureImporter.Texture textureImporterMaskMap = new($"{decalName}_MaskMap", folderPath + "\\" + "_MaskMap.png", texture2D_MaskMap);
				//textureImporterMaskMap.CompressBC(1);
				decalSurface.AddProperty("_MaskMap", textureImporterMaskMap);

			};
		}

		if (File.Exists(folderPath + "\\icon.png"))
		{
			fileData = File.ReadAllBytes(folderPath + "\\icon.png");
			Texture2D texture2D_Icon = new(1, 1);
			if (texture2D_Icon.LoadImage(fileData))
			{
				if (texture2D_Icon.width > 128 || texture2D_Icon.height > 128)
				{
					//ELT.ResizeTexture(texture2D_Icon, 128, folderPath + "\\icon.png");
					texture2D_Icon.ResizeTexture(128).SaveTextureAsPNG(folderPath + "\\icon.png");
				}
			}
		}

        AssetDataPath assetDataPath = AssetDataPath.Create($"Mods/EAI/CustomDecals/{modName}/{catName}/{decalName}", "SurfaceAsset");
		SurfaceAsset surfaceAsset = new()
		{
			guid = Guid.NewGuid(), //DecalRenderPrefab.surfaceAssets.ToArray()[0].guid, //
			database = AssetDatabase.game //DecalRenderPrefab.surfaceAssets.ToArray()[0].database,
		};
		surfaceAsset.database.AddAsset<SurfaceAsset>(assetDataPath, surfaceAsset.guid);
		surfaceAsset.SetData(decalSurface);
		surfaceAsset.Save(force: false, saveTextures: true, vt: false);

		Vector4 MeshSize = decalSurface.GetVectorProperty("colossal_MeshSize");
		Vector4 TextureArea = decalSurface.GetVectorProperty("colossal_TextureArea");
		Mesh[] meshes = [ConstructMesh(MeshSize.x, MeshSize.y, MeshSize.z)];
		GeometryAsset geometryAsset = new()
		{
			guid = Guid.NewGuid(),
			database = AssetDatabase.game //DecalRenderPrefab.geometryAsset.database
		};

		AssetDataPath assetDataPath2 = AssetDataPath.Create($"Mods/EAI/CustomDecals/{modName}/{catName}/{decalName}", "GeometryAsset");
		geometryAsset.database.AddAsset<GeometryAsset>(assetDataPath2, geometryAsset.guid);
		geometryAsset.SetData(meshes);
		geometryAsset.Save(true);

		RenderPrefab renderPrefab = (RenderPrefab)ScriptableObject.CreateInstance("RenderPrefab");
		renderPrefab.name = $"{fullDecalName}_RenderPrefab";
		renderPrefab.geometryAsset = new AssetReference<GeometryAsset>(geometryAsset.guid);
		renderPrefab.surfaceAssets = [surfaceAsset];
		renderPrefab.bounds = new(new(-MeshSize.x * 0.5f, -MeshSize.y * 0.5f, -MeshSize.z * 0.5f), new(MeshSize.x * 0.5f, MeshSize.y * 0.5f, MeshSize.z * 0.5f));
		renderPrefab.meshCount = 1;
		renderPrefab.vertexCount = geometryAsset.GetVertexCount(0);
		renderPrefab.indexCount = 1;
		renderPrefab.manualVTRequired = false;
        geometryAsset.Unload();
        surfaceAsset.Unload();

        DecalProperties decalProperties = renderPrefab.AddComponent<DecalProperties>();
		decalProperties.m_TextureArea = new(new(TextureArea.x, TextureArea.y), new(TextureArea.z, TextureArea.w));
		decalProperties.m_LayerMask = (DecalLayers)decalSurface.GetFloatProperty("colossal_DecalLayerMask");
		decalProperties.m_RendererPriority = (int)(decalSurface.HasProperty("_DrawOrder") ? decalSurface.GetFloatProperty("_DrawOrder") : 0);
		decalProperties.m_EnableInfoviewColor = false;//DecalPropertiesPrefab.m_EnableInfoviewColor;

		ObjectMeshInfo objectMeshInfo = new()
		{
			m_Mesh = renderPrefab,
			m_Position = float3.zero,
			m_RequireState = Game.Objects.ObjectState.None
		};

		decalPrefab.m_Meshes = [objectMeshInfo];

		StaticObjectPrefab placeholder = (StaticObjectPrefab)ScriptableObject.CreateInstance("StaticObjectPrefab");
		placeholder.name = $"{fullDecalName}_Placeholders";
		placeholder.m_Meshes = [objectMeshInfo];
		placeholder.AddComponent<PlaceholderObject>();

		SpawnableObject spawnableObject = decalPrefab.AddComponent<SpawnableObject>();
		spawnableObject.m_Placeholders = [placeholder];

		UIObject decalPrefabUI = decalPrefab.AddComponent<UIObject>();
		decalPrefabUI.m_IsDebugObject = false;
		decalPrefabUI.m_Icon = File.Exists(folderPath + "\\icon.png") ? $"{Icons.COUIBaseLocation}/CustomDecals/{catName}/{decalName}/icon.png" : Icons.DecalPlaceholder;
		decalPrefabUI.m_Priority = (int)(decalSurface.HasProperty("UiPriority") ? decalSurface.GetFloatProperty("UiPriority") : -1);
		decalPrefabUI.m_Group = ExtraAssetsMenu.GetOrCreateNewUIAssetCategoryPrefab(catName, Icons.GetIcon, assetCat);

        decalSurface.Dispose();
        //decalPrefab.AddComponent<CustomDecal>();

        ExtraLib.m_PrefabSystem.AddPrefab(decalPrefab);

	}

	internal static Mesh ConstructMesh(float length, float height, float width)
	{
		Mesh mesh = new();

		//3) Define the co-ordinates of each Corner of the cube 
		Vector3[] c =
		[
			new Vector3(-length * .5f, -height * .5f, width * .5f),
		new Vector3(length * .5f, -height * .5f, width * .5f),
		new Vector3(length * .5f, -height * .5f, -width * .5f),
		new Vector3(-length * .5f, -height * .5f, -width * .5f),
		new Vector3(-length * .5f, height * .5f, width * .5f),
		new Vector3(length * .5f, height * .5f, width * .5f),
		new Vector3(length * .5f, height * .5f, -width * .5f),
		new Vector3(-length * .5f, height * .5f, -width * .5f),
	];


		//4) Define the vertices that the cube is composed of:
		//I have used 16 vertices (4 vertices per side). 
		//This is because I want the vertices of each side to have separate normals.
		//(so the object renders light/shade correctly) 
		Vector3[] vertices =
		[
			c[0], c[1], c[2], c[3], // Bottom
			c[7], c[4], c[0], c[3], // Left
			c[4], c[5], c[1], c[0], // Front
			c[6], c[7], c[3], c[2], // Back
			c[5], c[6], c[2], c[1], // Right
			c[7], c[6], c[5], c[4]  // Top
		];


		//5) Define each vertex's Normal
		Vector3 up = Vector3.up;
		Vector3 down = Vector3.down;
		Vector3 forward = Vector3.forward;
		Vector3 back = Vector3.back;
		Vector3 left = Vector3.left;
		Vector3 right = Vector3.right;


		Vector3[] normals =
		[
			down, down, down, down,             // Bottom
			left, left, left, left,             // Left
			forward, forward, forward, forward,	// Front
			back, back, back, back,             // Back
			right, right, right, right,         // Right
			up, up, up, up                      // Top
		];

		//6) Define each vertex's UV co-ordinates
		Vector2 uv00 = new(0f, 0f);
		Vector2 uv10 = new(1f, 0f);
		Vector2 uv01 = new(0f, 1f);
		Vector2 uv11 = new(1f, 1f);

		Vector2[] uvs =
		[
			uv11, uv01, uv00, uv10, // Bottom
			uv11, uv01, uv00, uv10, // Left
			uv11, uv01, uv00, uv10, // Front
			uv11, uv01, uv00, uv10, // Back	        
			uv11, uv01, uv00, uv10, // Right 
			uv11, uv01, uv00, uv10  // Top
		];


		//7) Define the Polygons (triangles) that make up the our Mesh (cube)
		//IMPORTANT: Unity uses a 'Clockwise Winding Order' for determining front-facing polygons.
		//This means that a polygon's vertices must be defined in 
		//a clockwise order (relative to the camera) in order to be rendered/visible.
		int[] triangles =
		[
			3, 1, 0,        3, 2, 1,        // Bottom	
			7, 5, 4,        7, 6, 5,        // Left
			11, 9, 8,       11, 10, 9,      // Front
			15, 13, 12,     15, 14, 13,     // Back
			19, 17, 16,     19, 18, 17,	    // Right
			23, 21, 20,     23, 22, 21,	    // Top
		];


		//8) Build the Mesh
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.normals = normals;
		mesh.uv = uvs;
		mesh.Optimize();
		// mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		mesh.RecalculateBounds();

		return mesh;
	}

    private static void VersionCompatiblity(JSONDecalsMaterail jSONDecalsMaterail, string catName, string decalName)
    {
		if (EAI.m_Setting.CompatibilityDropDown == EAICompatibility.LocalAsset)
		{
            PrefabIdentifierInfo prefabIdentifierInfo = new()
            {
                m_Name = $"ExtraAssetsImporter {catName} {decalName} Decal",
                m_Type = "StaticObjectPrefab"
            };
            jSONDecalsMaterail.prefabIdentifierInfos.Insert(0, prefabIdentifierInfo);
        }
        if (EAI.m_Setting.CompatibilityDropDown == EAICompatibility.ELT3)
        {
            PrefabIdentifierInfo prefabIdentifierInfo = new()
            {
                m_Name = $"ExtraLandscapingTools_mods_{catName}_{decalName}",
                m_Type = "StaticObjectPrefab"
            };
            jSONDecalsMaterail.prefabIdentifierInfos.Insert(0, prefabIdentifierInfo);
        }
    }
}
internal class CustomDecal : ComponentBase
{
    public override void GetArchetypeComponents(HashSet<ComponentType> components) { }
    public override void GetPrefabComponents(HashSet<ComponentType> components) { }
}
