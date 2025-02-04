﻿using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.PSI.Environment;
using Extra.Lib;
using Extra.Lib.Debugger;
using Extra.Lib.Localization;
using ExtraAssetsImporter.Importers;
using Game;
using Game.Modding;
using Game.SceneFlow;
using System.Collections;
using System.IO;
using System.Reflection;

namespace ExtraAssetsImporter
{
	public class EAI : IMod
	{
		private static ILog log = LogManager.GetLogger($"{nameof(ExtraAssetsImporter)}").SetShowsErrorsInUI(false);
#if DEBUG
		internal static Logger Logger = new(log, true);
#else
        internal static Logger Logger = new(log, false);
#endif
        static internal readonly string ELTGameDataPath = $"{EnvPath.kStreamingDataPath}\\Mods\\EAI";                                                                          
		internal static Setting m_Setting;

		internal static string ResourcesIcons { get; private set; }
		public void OnLoad(UpdateSystem updateSystem)
		{
			Logger.Info(nameof(OnLoad));
			ClearData();

			if (!GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset)) return;
			Logger.Info($"Current mod asset at {asset.path}");

            ExtraLocalization.LoadLocalization(Logger, Assembly.GetExecutingAssembly(), false);

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(nameof(ExtraAssetsImporter), m_Setting, new Setting(this));

			m_Setting.dummySettingsToAvoidSettingsBugThanksCO = true;
			m_Setting.ApplyAndSave();

            FileInfo fileInfo = new(asset.path);

			ResourcesIcons = Path.Combine(fileInfo.DirectoryName, "Icons");
			Icons.LoadIcons(fileInfo.DirectoryName);

			string pathToData = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(ExtraAssetsImporter));
			string pathToDataCustomDecals = Path.Combine(pathToData, "CustomDecals");
			string pathToDataCustomSurfaces = Path.Combine(pathToData, "CustomSurfaces");

			if (Directory.Exists(pathToDataCustomDecals)) DecalsImporter.AddCustomDecalsFolder(pathToDataCustomDecals);
			if (Directory.Exists(pathToDataCustomSurfaces)) SurfacesImporter.AddCustomSurfacesFolder(pathToDataCustomSurfaces);

			ExtraLib.AddOnMainMenu(OnMainMenu);

			updateSystem.UpdateAt<sys>(SystemUpdatePhase.MainLoop);

		}

		public void OnDispose()
		{
			Logger.Info(nameof(OnDispose));
			ClearData();
		}

		private void OnMainMenu()
		{
			if (m_Setting.Decals) ExtraLib.extraLibMonoScript.StartCoroutine(DecalsImporter.CreateCustomDecals());
            if (m_Setting.Surfaces) ExtraLib.extraLibMonoScript.StartCoroutine(SurfacesImporter.CreateCustomSurfaces());
			ExtraLib.extraLibMonoScript.StartCoroutine(WaitForCustomStuffToFinish());
		}

		private IEnumerator WaitForCustomStuffToFinish()
		{
			while( (m_Setting.Decals && !DecalsImporter.DecalsLoaded) || ( m_Setting.Surfaces && !SurfacesImporter.SurfacesIsLoaded)) 
			{
				yield return null;
			}
			m_Setting.ResetCompatibility();
			yield break;
		}


        internal static void ClearData()
		{
			if (Directory.Exists(ELTGameDataPath))
			{
				Directory.Delete(ELTGameDataPath, true);
			}
			//if (Directory.Exists(ELTUserDataPath))
			//{
			//	Directory.Delete(ELTUserDataPath, true);
			//}
		}

		public static void LoadCustomAssets(string modPath)
		{
			if (Directory.Exists(modPath + "\\CustomSurfaces")) SurfacesImporter.AddCustomSurfacesFolder(modPath + "\\CustomSurfaces");
			if (Directory.Exists(modPath + "\\CustomDecals")) DecalsImporter.AddCustomDecalsFolder(modPath + "\\CustomDecals");
		}

		public static void UnLoadCustomAssets(string modPath)
		{
			if (Directory.Exists(modPath + "\\CustomSurfaces")) SurfacesImporter.RemoveCustomSurfacesFolder(modPath + "\\CustomSurfaces");
			if (Directory.Exists(modPath + "\\CustomDecals")) DecalsImporter.RemoveCustomDecalsFolder(modPath + "\\CustomDecals");
		}

	}
}
