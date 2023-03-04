using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using SonsOfTheForest.Saves;

namespace SonsOfTheForestCompanionRescue
{
    public partial class Form1 : Form
    {
        private static readonly string _gameProcessName = "SonsOfTheForest";

        private static readonly int _kelvinInternalTypeID = 9;
        private static readonly int _virginiaInternalTypeID = 10;

        private static readonly double[] safePos = { -627, 100, 533 };

        private GameSave _currentlyEditedSave;
        private NPC _kelvin;
        private NPC _virginia;

        public Form1()
        {
            InitializeComponent();
            DisplayStartupWarningDialogue();

            PopulateSaveList();
            gameSavesComboBox_SelectedIndexChanged(null, null);
        }

        /// <summary>
        /// Close the app if the game is still running, or retry.
        /// </summary>
        private void DisplayStartupWarningDialogue()
        {
            if (IsGameRunning())
            {
                var startUpCheckDiagResult = MessageBox.Show("Before using this tool, please make sure you have completely closed the game, as editing the save files while the game is running could corrupt them.", "Game is still running", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                if (startUpCheckDiagResult == DialogResult.Retry)
                {
                    // check again
                    DisplayStartupWarningDialogue();
                }
                else if (startUpCheckDiagResult == DialogResult.Cancel)
                {
                    Load += (sender, evt) => Close();
                }
            }
        }

        /// <summary>
        /// Checks and returns <see cref="true"/> if the game is currently running
        /// </summary>
        /// <returns></returns>
        private bool IsGameRunning()
        {
            return (Process.GetProcessesByName(_gameProcessName).Length > 0);
        }

        private void PopulateSaveList()
        {
            var gameSaves = SaveParser.GetSavesList();
            gameSavesComboBox.DisplayMember = "DisplayName";
            gameSavesComboBox.DataSource = gameSaves;
        }

        private void ResurrectNPC(int npcID)
        {
            var gameSaveData = _currentlyEditedSave.Contents["SaveData.json"];
            //Edit KillStatsList JArray to remove the kill
            var killStatsList = (JArray)gameSaveData.SelectToken("Data.VailWorldSim.KillStatsList");
            var npcKillStat = killStatsList.FirstOrDefault(o => (int)o["TypeId"] == npcID);
            npcKillStat["PlayerKilled"] = 0;

            //Edit the companion data
            var actorList = (JArray)gameSaveData.SelectToken("Data.VailWorldSim.Actors");
            var companionData = actorList.FirstOrDefault(o => (int)o["TypeId"] == npcID);
            companionData["Stats"]["Health"] = 100;
            companionData["State"] = 2;

            //Edit the game's state save to remove kill flags if applicable
            //GameStateSaveData.json -> Data.GameState
            //IsRobbyDead
            //IsVirginiaDead
            var gameStateData = _currentlyEditedSave.Contents["GameStateSaveData.json"].SelectToken("Data.GameState");
            if (npcID == _kelvinInternalTypeID)
            {
                gameStateData["IsRobbyDead"] = false;
            }

            if (npcID == _virginiaInternalTypeID)
            {
                gameStateData["IsVirginiaDead"] = false;
            }
            gameSavesComboBox_SelectedIndexChanged(null, null);
        }

        private void gameSavesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var save = (GameSave)gameSavesComboBox.SelectedItem;
            _currentlyEditedSave = save;
            selectedSavePathTextBox.Text = save.MainDirPath;
            selectedSaveType.Text = save.Type.ToString();
            selectedSaveDateLabel.Text = save.SaveTime.ToString();
            selectedSaveThumbnailPictureBox.ImageLocation = save.MainDirPath + "\\SaveDataThumbnail.png";

            _kelvin = new NPC(_kelvinInternalTypeID, save);
            kelvinStatusLabel.Text = _kelvin.Status.ToString();
            if (_kelvin.Status == CompanionStatus.Alive)
            {
                kelvinStatusLabel.ForeColor = Color.Green;
            }
            else
            {
                kelvinStatusLabel.ForeColor = Color.Red;
            }
            kelvinHealthNumeric.Value = (decimal)_kelvin.Health;
            kelvinPosXNumeric.Value = (decimal)_kelvin.X;
            kelvinPosYNumeric.Value = (decimal)_kelvin.Y;
            kelvinPosZNumeric.Value = (decimal)_kelvin.Z;

            _virginia = new NPC(_virginiaInternalTypeID, save);
            virginiaStatusLabel.Text = _virginia.Status.ToString();
            if (_virginia.Status == CompanionStatus.Alive)
            {
                virginiaStatusLabel.ForeColor = Color.Green;
            }
            else
            {
                virginiaStatusLabel.ForeColor = Color.Red;
            }
            virginiaHealthNumeric.Value = (decimal)_virginia.Health;
            virginiaPosXNumeric.Value = (decimal)_virginia.X;
            virginiaPosYNumeric.Value = (decimal)_virginia.Y;
            virginiaPosZNumeric.Value = (decimal)_virginia.Z;
        }

        private void MoveNPCToLocation(NPC npc, double x, double y, double z)
        {
            npc.X.Value = x;
            npc.Y.Value = y;
            npc.Z.Value = z;
            gameSavesComboBox_SelectedIndexChanged(null, null);
        }

        private void saveChangesButton_Click(object sender, EventArgs e)
        {
            _currentlyEditedSave.WriteChanges();
        }

        private void kelvinHealthNumeric_ValueChanged(object sender, EventArgs e)
        {
            _kelvin.Health.Value = (double)kelvinHealthNumeric.Value;
        }

        private void kelvinPosXNumeric_ValueChanged(object sender, EventArgs e)
        {
            _kelvin.X.Value = (double)kelvinPosXNumeric.Value;
        }

        private void kelvinPosYNumeric_ValueChanged(object sender, EventArgs e)
        {
            _kelvin.Y.Value = (double)kelvinPosYNumeric.Value;
        }

        private void kelvinPosZNumeric_ValueChanged(object sender, EventArgs e)
        {
            _kelvin.Z.Value = (double)kelvinPosZNumeric.Value;
        }

        private void moveKelvinToPlayerButton_Click(object sender, EventArgs e)
        {
            var playerData = (JArray)_currentlyEditedSave.Contents["PlayerStateSaveData.json"].SelectToken("Data.PlayerState._entries");
            var playerPos = playerData.FirstOrDefault(o => ((string)o["Name"]).Contains("player.position"));
            MoveNPCToLocation(_kelvin, (double)(playerPos["FloatArrayValue"][0]), (double)(playerPos["FloatArrayValue"][1]), (double)(playerPos["FloatArrayValue"][2]));
        }

        private void moveVirginiaToPlayerButton_Click(object sender, EventArgs e)
        {
            var playerData = (JArray)_currentlyEditedSave.Contents["PlayerStateSaveData.json"].SelectToken("Data.PlayerState._entries");
            var playerPos = playerData.FirstOrDefault(o => ((string)o["Name"]).Contains("player.position"));
            MoveNPCToLocation(_virginia, (double)(playerPos["FloatArrayValue"][0]), (double)(playerPos["FloatArrayValue"][1]), (double)(playerPos["FloatArrayValue"][2]));
        }

        private void theGreatKelvinUnstuckinatorButton_Click(object sender, EventArgs e)
        {
            MoveNPCToLocation(_kelvin, safePos[0], safePos[1], safePos[2]);
        }

        private void moveVirginiaToFixPosButton_Click(object sender, EventArgs e)
        {
            MoveNPCToLocation(_virginia, safePos[0], safePos[1], safePos[2]);
        }

        private void moveKelvinToPosButton_Click(object sender, EventArgs e)
        {
            MoveNPCToLocation(_kelvin, (double)kelvinPosXNumeric.Value, (double)kelvinPosYNumeric.Value, (double)kelvinPosZNumeric.Value);
        }

        private void moveVirginiaToPosButton_Click(object sender, EventArgs e)
        {
            MoveNPCToLocation(_virginia, (double)virginiaPosXNumeric.Value, (double)virginiaPosYNumeric.Value, (double)virginiaPosZNumeric.Value);
        }

        private void virginiaHealthNumeric_ValueChanged(object sender, EventArgs e)
        {
            _virginia.Health.Value = (double)virginiaHealthNumeric.Value;
        }

        private void virginiaPosXNumeric_ValueChanged(object sender, EventArgs e)
        {
            _virginia.X.Value = (double)virginiaPosXNumeric.Value;
        }

        private void virginiaPosYNumeric_ValueChanged(object sender, EventArgs e)
        {
            _virginia.Y.Value = (double)virginiaPosYNumeric.Value;
        }

        private void virginiaPosZNumeric_ValueChanged(object sender, EventArgs e)
        {
            _virginia.Z.Value = (double)virginiaPosZNumeric.Value;
        }

        private void kelvinResurrectButton_Click(object sender, EventArgs e)
        {
            ResurrectNPC(_kelvinInternalTypeID);
        }

        private void virginiaResurrectButton_Click(object sender, EventArgs e)
        {
            ResurrectNPC(_virginiaInternalTypeID);
        }

        private void repoLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("explorer.exe", repoLinkLabel.Text);
        }
    }
}