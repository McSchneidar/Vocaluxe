#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Vocaluxe.Base.ThemeSystem;
using VocaluxeLib;
using VocaluxeLib.Log;
using VocaluxeLib.Menu;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base
{
    static class CParty
    {
        private const int _PartyModeSystemVersion = 2;

        private static readonly Dictionary<int, SPartyMode> _PartyModes = new Dictionary<int, SPartyMode>();
        private static int _NextId;
        private static IPartyMode _CurrentPartyMode;

        #region public stuff
        public static bool Init()
        {
            if (_PartyModes.Count > 0)
            {
                return false; //Already initialized
            }

            var pm = new SPartyMode
            {
                Info = new SPartyModeInfos { Author = "Vocaluxe Team", Description = "Normal game", Name = "Normal", TargetAudience = "Just a normal game for everyone" },
                PartyMode = new CPartyModeNormal(-1),
                PartyModeSystemVersion = _PartyModeSystemVersion,
                ScreenFiles = new List<string>()
            };
            _PartyModes.Add(-1, pm);
            _CurrentPartyMode = pm.PartyMode;
            Debug.Assert(_CurrentPartyMode != null && _CurrentPartyMode.Id == -1);

            //load other party modes
            _LoadPartyModes();
            return _CurrentPartyMode.Init();
        }

        public static int CurrentPartyModeId
        {
            get { return _CurrentPartyMode.Id; }
            set
            {
                if (_CurrentPartyMode.Id == value)
                {
                    _CurrentPartyMode.SetDefaults();
                    return;
                }

                if (value != -1 && !_PartyModes.ContainsKey(value))
                {
                    throw new ArgumentException("Partymode with Id=" + value + " does not exist!");
                }

                var pm = _PartyModes[value].PartyMode;
                if (pm.Init())
                {
                    _CurrentPartyMode = pm;
                }
                else
                {
                    CLog.Error("Could not init PartyMode \"" + _PartyModes[value].Info.Name + "\"! Removing...", true);
                    _PartyModes.Remove(value);
                }
            }
        }

        public static int Count
        {
            get { return _PartyModes.Count; }
        }

        public static void ReloadTheme()
        {
            foreach (var pm in _PartyModes.Values)
            {
                pm.PartyMode.ReloadTheme();
            }
        }

        public static void ReloadSkin()
        {
            foreach (var pm in _PartyModes.Values)
            {
                pm.PartyMode.ReloadSkin();
            }
        }

        public static void SaveThemes()
        {
            foreach (var pm in _PartyModes.Values)
            {
                pm.PartyMode.SaveScreens();
            }
        }

        public static List<SPartyModeInfos> GetPartyModeInfos()
        {
            var list = new List<SPartyModeInfos>();
            foreach (var pm in _PartyModes.Values)
            {
                if (pm.PartyMode.Id >= 0)
                {
                    list.Add(pm.Info);
                }
            }

            return list;
        }

        public static void SetNormalGameMode()
        {
            CSongs.ResetPartySongSung();
            CurrentPartyModeId = -1;
        }

        public static void SetPartyMode(int partyModeId)
        {
            CurrentPartyModeId = partyModeId;
        }
        #endregion public stuff

        #region Interface
        public static void UpdateGame()
        {
            _CurrentPartyMode.UpdateGame();
        }

        public static IMenu GetStartScreen()
        {
            return _CurrentPartyMode.GetStartScreen();
        }

        public static SScreenSongOptions GetSongSelectionOptions()
        {
            return _CurrentPartyMode.GetScreenSongOptions();
        }

        public static void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions)
        {
            _CurrentPartyMode.OnSongChange(songIndex, ref screenSongOptions);
        }

        public static void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions)
        {
            _CurrentPartyMode.OnCategoryChange(categoryIndex, ref screenSongOptions);
        }

        public static void SetSearchString(string searchString, bool visible)
        {
            _CurrentPartyMode.SetSearchString(searchString, visible);
        }

        public static void JokerUsed(int teamNr)
        {
            _CurrentPartyMode.JokerUsed(teamNr);
        }

        public static void SongSelected(int songId)
        {
            _CurrentPartyMode.SongSelected(songId);
        }

        public static void FinishedSinging()
        {
            _CurrentPartyMode.FinishedSinging();
        }

        public static void LeavingScore()
        {
            _CurrentPartyMode.LeavingScore();
        }

        public static void LeavingHighscore()
        {
            _CurrentPartyMode.LeavingHighscore();
        }
        #endregion Interface

        #region private stuff
        private static void _LoadPartyModes()
        {
            var files = new List<string>();
            files.AddRange(CHelper.ListFiles(CSettings.FolderNamePartyModes, "*.xml", false, true));

            foreach (var file in files)
            {
                SPartyMode pm;
                if (_LoadPartyMode(file, out pm))
                {
                    _PartyModes.Add(pm.PartyMode.Id, pm);
                }
            }
        }

        private static bool _LoadPartyMode(string filePath, out SPartyMode pm)
        {
            var deser = new CXmlDeserializer();
            try
            {
                pm = deser.Deserialize<SPartyMode>(filePath);
                if (pm.PartyModeSystemVersion != _PartyModeSystemVersion)
                {
                    throw new Exception("Wrong PartyModeSystemVersion " + pm.PartyModeSystemVersion + " expected: " + _PartyModeSystemVersion);
                }

                if (pm.ScreenFiles.Count == 0)
                {
                    throw new Exception("No ScreenFiles found");
                }
            }
            catch (Exception e)
            {
                pm = new SPartyMode();
                CLog.Error("Error loading PartyMode file " + filePath + ": " + e.Message);
                return false;
            }

            var pathToPm = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNamePartyModes, pm.Info.Folder);
            var pathToCode = Path.Combine(pathToPm, CSettings.FolderNamePartyModeCode);

            var filesToCompile = new List<string>();
            filesToCompile.AddRange(CHelper.ListFiles(pathToCode, "*.cs", false, true));

            var output = _CompileFiles(filesToCompile.ToArray());
            if (output == null)
            {
                return false;
            }

            var instance = output.CreateInstance(typeof(IPartyMode).Namespace + "." + pm.Info.Folder + "." + pm.Info.PartyModeFile, false,
                BindingFlags.Public | BindingFlags.Instance, null, new object[] { _NextId++ }, null, null);
            if (instance == null)
            {
                CLog.Error("Error creating Instance of PartyMode file: " + filePath);
                return false;
            }

            try
            {
                pm.PartyMode = (IPartyMode)instance;
            }
            catch (Exception e)
            {
                CLog.Error("Error casting PartyMode file: " + filePath + "; " + e.Message);
                return false;
            }

            if (!CLanguage.LoadPartyLanguageFiles(pm.PartyMode.Id, Path.Combine(pathToPm, CSettings.FolderNamePartyModeLanguages)))
            {
                CLog.Error("Error loading language files for PartyMode: " + filePath);
                return false;
            }

            if (!CThemes.ReadThemesFromFolder(Path.Combine(pathToPm, CSettings.FolderNameThemes), pm.PartyMode.Id))
            {
                return false;
            }

            if (!CThemes.LoadPartymodeTheme(pm.PartyMode.Id))
            {
                return false;
            }

            foreach (var screenfile in pm.ScreenFiles)
            {
                var screen = _GetPartyScreenInstance(output, screenfile, pm.Info.Folder);

                if (screen != null)
                {
                    screen.AssignPartyMode(pm.PartyMode);
                    pm.PartyMode.AddScreen(screen, screenfile);
                }
                else
                {
                    return false;
                }
            }

            pm.PartyMode.LoadTheme();
            pm.Info.ExtInfo = pm.PartyMode;
            return true;
        }

        private static Assembly _CompileFiles(string[] files)
        {
            if (files == null || files.Length == 0)
            {
                return null;
            }

            SyntaxTree[] syntaxTrees;
            try
            {
                syntaxTrees = files.Select(file => CSharpSyntaxTree.ParseText(SourceText.From(File.ReadAllText(file), Encoding.UTF8, SourceHashAlgorithm.Sha256),
                    _ParseOptions, file)).ToArray();
            }
            catch (Exception e)
            {
                CLog.Error("Error Compiling Source (" + CHelper.ListStrings(files) + "): " + e.Message);
                return null;
            }

            var compilation = CSharpCompilation.Create("PartyMode_" + Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(files[0]))) + "_" + Guid.NewGuid().ToString("N"),
                syntaxTrees, _GetCompilationReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
#if DEBUG
                    optimizationLevel: OptimizationLevel.Debug
#else
                    optimizationLevel: OptimizationLevel.Release
#endif
                ));

            using (var peStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(peStream, pdbStream, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));

                if (!emitResult.Success)
                {
                    foreach (var e in emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        var lineSpan = e.Location.GetLineSpan();
                        CLog.Error("Error Compiling Source (" + CHelper.ListStrings(files) + "): " + e.GetMessage() + " in '" + lineSpan.Path + "' (" +
                                   (lineSpan.StartLinePosition.Line + 1) + ")");
                    }

                    return null;
                }

                peStream.Seek(0, SeekOrigin.Begin);
                pdbStream.Seek(0, SeekOrigin.Begin);
                return AssemblyLoadContext.Default.LoadFromStream(peStream, pdbStream);
            }
        }

        private static readonly CSharpParseOptions _ParseOptions = new CSharpParseOptions(LanguageVersion.Latest);

        private static List<MetadataReference> _CompilationReferences;

        private static List<MetadataReference> _GetCompilationReferences()
        {
            if (_CompilationReferences != null)
            {
                return _CompilationReferences;
            }

            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (!string.IsNullOrEmpty(trustedAssemblies))
            {
                paths.UnionWith(trustedAssemblies.Split(Path.PathSeparator).Where(path => !string.IsNullOrEmpty(path)));
            }

            paths.Add(typeof(object).Assembly.Location);
            paths.Add(typeof(IPartyMode).Assembly.Location);

            _CompilationReferences = paths.Where(File.Exists).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToList();
            return _CompilationReferences;
        }

        private static CMenuParty _GetPartyScreenInstance(Assembly assembly, string screenName, string partyModeName)
        {
            if (assembly == null)
            {
                return null;
            }

            var instance = assembly.CreateInstance(typeof(IPartyMode).Namespace + "." + partyModeName + "." + screenName);
            if (instance == null)
            {
                CLog.Error("Error creating Instance of PartyScreen: " + screenName);
                return null;
            }

            CMenuParty screen;
            try
            {
                screen = (CMenuParty)instance;
            }
            catch (Exception e)
            {
                CLog.Error("Error casting PartyScreen: " + screenName + "; " + e.Message);
                return null;
            }

            return screen;
        }
        #endregion private stuff
    }
}