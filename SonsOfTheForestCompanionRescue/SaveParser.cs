using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SonsOfTheForest.Saves
{
    public enum SaveReadMode
    {
        /// <summary>
        /// Represents the three core files: PlayerStateSaveData, SaveData, GameStateSaveData.
        /// </summary>
        Core,
        /// <summary>
        /// Represents all files in the given save.
        /// </summary>
        Extended
    }

    public enum GameSaveType
    {
        Multiplayer,
        Singleplayer,
        MultiplayerClient
    }

    public static class SaveParser
    {
        /// <summary>
        /// The root Saves folder where all the saves are located.
        /// </summary>
        private static readonly string _savesRoot = @$"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))}Low\Endnight\SonsOfTheForest\Saves";

        /// <summary>
        /// Determines how many files <see cref="GameSave"/> instances read. 
        /// By default it is set to <see cref="SaveReadMode.Core", and only reads the three core files: PlayerStateSaveData, SaveData, GameStateSaveData.
        /// Setting this to <see cref="SaveReadMode.Extended"/> reads all files in the given save.</see>/>
        /// </summary>
        public static SaveReadMode ReadMode = SaveReadMode.Core;

        /// <summary>
        /// Gets and returns a list of <see cref="GameSave"/>s. Ignores MultiplayerClient saves by default.
        /// </summary>
        /// <param name="includeClientSaves">If set to <see langword="true"/>, the retured list also includes <see cref="GameSaveType.MultiplayerClient"/> saves.</param>
        /// <returns></returns>
        public static List<GameSave> GetSavesList(bool includeClientSaves = false)
        {
            var saveIdPath = Directory.GetDirectories(_savesRoot)[0];
            List<GameSave> saves = new List<GameSave>();

            if (Directory.Exists(saveIdPath + "\\Multiplayer"))
            {
                var multiplayerSavesPaths = Directory.GetDirectories(saveIdPath + "\\Multiplayer");
                foreach (var multiplayerSavePath in multiplayerSavesPaths)
                {
                    saves.Add(new GameSave(multiplayerSavePath));
                }
            }
            if (Directory.Exists(saveIdPath + "\\Singleplayer"))
            {
                var singleplayerSavesPaths = Directory.GetDirectories(saveIdPath + "\\Singleplayer");
                foreach (var singleplayerSavePath in singleplayerSavesPaths)
                {
                    saves.Add(new GameSave(singleplayerSavePath));
                }
            }
            if (includeClientSaves == true && Directory.Exists(saveIdPath + "\\MultiplayerClient"))
            {
                var clientSavesPaths = Directory.GetDirectories(saveIdPath + "\\MultiplayerClient");
                foreach (var clientSavePath in clientSavesPaths)
                {
                    saves.Add(new GameSave(clientSavePath));
                }
            }
            return saves;
        }

        /// <summary>
        /// Custom parser to correctly deserialise the faulty JSON format the saves use.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static JObject ReadFile(string path)
        {
            string fileString = File.ReadAllText(path);
            JObject json = JObject.Parse(fileString);
            JObject saveData = (JObject)json["Data"];

            /*
            The Data object's key values are stored as strings, to this loops through all of them
            and explicitly parses them as an object
            */

            foreach (var kvp in saveData)
            {
                string stringEntry = kvp.Value.ToString();
                saveData[kvp.Key] = JObject.Parse(stringEntry);
            }

            return json;
        }

        /// <summary>
        /// Custom parser to correctly serialise to the faulty JSON format the saves use.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="contents"></param>
        public static void WriteFile(string path, JObject contents)
        {
            JObject save = new JObject(contents);
            JObject saveData = (JObject)save["Data"];

            /*
            The Data object's key values are stored as strings, to this loops through all of them
            and converts back to string
            */

            foreach (var kvp in saveData)
            {
                string stringEntry = kvp.Value.ToString(Newtonsoft.Json.Formatting.None);
                saveData[kvp.Key] = stringEntry;
            }

            File.WriteAllText(path, save.ToString(Newtonsoft.Json.Formatting.None));
        }
    }

    /// <summary>
    /// Represents a game save and its contents.
    /// </summary>
    public class GameSave
    {
        /// <summary>
        /// The root directory of the save.
        /// </summary>
        public string MainDirPath { get; private set; }
        /// <summary>
        /// The internal ID of the save.
        /// </summary>
        public string ID { get; private set; }
        /// <summary>
        /// A quick at-glance identifier of the save.
        /// </summary>
        public string DisplayName { get; private set; }
        /// <summary>
        /// The host type of the save.
        /// </summary>
        public GameSaveType Type { get; private set; }
        /// <summary>
        /// The time this game was last saved at.
        /// </summary>
        public DateTime SaveTime { get; private set; }
        /// <summary>
        /// The last write time of this save's main file.
        /// </summary>
        public DateTime LastEditTime { get; private set; }
        /// <summary>
        /// Contains the list and contents of the save files within this save, dependent on <seealso cref="SaveParser.ReadMode"/>.
        /// </summary>
        public Dictionary<string, JObject> Contents = new Dictionary<string, JObject>();

        public GameSave(string savePath)
        {
            MainDirPath = savePath;
            ID = Path.GetFileName(savePath);
            Type = GetSaveType();
            LastEditTime = File.GetLastWriteTime(MainDirPath + "\\PlayerStateSaveData.json");
            LoadSaveFiles();
            var saveTimeString = Contents["GameStateSaveData.json"].SelectToken("Data.GameState.SaveTime").ToString();
            SaveTime = DateTime.Parse(saveTimeString);
            DisplayName = $"[{Type}] [{SaveTime}] - {ID}";
        }

        /// <summary>
        /// Saves and overwrites the contents of the approriate savefiles with the data in <see cref="Contents"/>.
        /// </summary>
        public void WriteChanges()
        {
            foreach (var file in Contents)
            {
                var filePath = Path.Combine(MainDirPath, file.Key);
                SaveParser.WriteFile(filePath, file.Value);
            }
        }

        private GameSaveType GetSaveType()
        {
            if (MainDirPath.Contains("MultiplayerClient"))
            {
                return GameSaveType.MultiplayerClient;
            }
            else if (MainDirPath.Contains("Singleplayer"))
            {
                return GameSaveType.Singleplayer;
            }
            else
            {
                return GameSaveType.Multiplayer;
            }
        }

        /// <summary>
        /// If <see cref="SaveParser.ReadMode"/> is set to <see cref="SaveReadMode.Core"/>, loads the three core files,
        /// otherwise loads the entire save contents.
        /// </summary>
        private void LoadSaveFiles()
        {
            if (SaveParser.ReadMode == SaveReadMode.Extended)
            {
                var files = Directory.GetFiles(MainDirPath);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    if (fileName.EndsWith(".png") != true)
                    {
                        Contents.Add(fileName, SaveParser.ReadFile(file));
                    }
                }
            }
            else
            {
                var playerStateSaveDataPath = $"{MainDirPath}\\PlayerStateSaveData.json";
                var gameStateSaveDataPath = $"{MainDirPath}\\GameStateSaveData.json";
                var saveDataPath = $"{MainDirPath}\\SaveData.json";

                Contents.Add(Path.GetFileName(saveDataPath), SaveParser.ReadFile(saveDataPath));
                Contents.Add(Path.GetFileName(gameStateSaveDataPath), SaveParser.ReadFile(gameStateSaveDataPath));
                Contents.Add(Path.GetFileName(playerStateSaveDataPath), SaveParser.ReadFile(playerStateSaveDataPath));
            }
        }
    }
}
