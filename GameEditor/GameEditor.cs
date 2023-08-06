﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Engine;
using Engine.Gui;
using Engine.ListManager;
using Engine.Map;
using Engine.Script;
using Engine.Storage;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GameEditor
{
    public partial class GameEditor : Form
    {
        private RunScript _scriptDialog = new RunScript();
        private VariablesWindow _variablesWindow = new VariablesWindow();
        private LogWindow _log = new LogWindow();
        private FileSystemWatcher _scriptFileWatcher;
        public JxqyGame TheJxqyGame;

        public GameEditor()
        {
            InitializeComponent();
            FunctionRunStateAppendLine("[时间]\t[函数]\t[行数]");
            CenterToScreen();

            //Watch for script file change.
            _scriptFileWatcher = new FileSystemWatcher();
            _scriptFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _scriptFileWatcher.Changed += (sender, args) =>
            {
                var callBack = new SetScriptFileContentEvent(SetScriptFileContent);
                Invoke(callBack, new object[] { _scriptFilePath.Text });
            };
        }

        private void SaveSettings()
        {
            Settings.SaveFormPositionSize(this.Location.X, this.Location.Y, this.Size.Width, this.Size.Height, WindowState == FormWindowState.Maximized);
            Settings.Save();
        }

        private void GameEditor_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveSettings();
            TheJxqyGame.ExitGameImmediately();
        }

        private void GameEditor_Activated(object sender, EventArgs e)
        {

        }

        private void GameEditor_Deactivate(object sender, EventArgs e)
        {

        }

        private void DrawSurface_MouseEnter(object sender, EventArgs e)
        {
            Cursor.Hide();
            Globals.TheGame.IsPaused = false;
            DrawSurface.Select();
        }

        private void DrawSurface_MouseLeave(object sender, EventArgs e)
        {
            Cursor.Show();
            Globals.TheGame.IsPaused = true;
        }

        public void FunctionRunStateAppendLine(string line)
        {
            _functionText.AppendText(line + Environment.NewLine);
        }

        delegate void SetScriptFileContentEvent(string path);

        public void SetScriptFileContent(string path)
        {
            SetScriptFilePath(path);

            var contnet = new StringBuilder();
            var filePathInfo = "【" + path + "】";
            try
            {
                var lines = File.ReadAllLines(path, Globals.LocalEncoding);
                var count = lines.Count();
                for (var i = 0; i < count; i++)
                {
                    contnet.AppendLine((i + 1) + "  " + lines[i]);
                }
            }
            catch (Exception)
            {
                _fileText.Text = (filePathInfo + "  读取失败！");
                return;
            }
            _fileText.Text = contnet.ToString();
        }

        private void SetScriptFilePath(string path)
        {
            var oldPath = _scriptFilePath.Text;
            _scriptFilePath.Text = path;
            TheToolTip.SetToolTip(_scriptFilePath, path);

            if (oldPath != path)
            {
                //Watch new path
                try
                {
                    var fullpath = Path.GetFullPath(path);
                    _scriptFileWatcher.Path = Path.GetDirectoryName(fullpath);
                    _scriptFileWatcher.Filter = Path.GetFileName(fullpath);
                    _scriptFileWatcher.EnableRaisingEvents = true;
                }
                catch (Exception)
                {
                    // Do nothing
                }
            }
        }

        private void _scriptFilePath_Click(object sender, EventArgs e)
        {
            var path = _scriptFilePath.Text;
            if (File.Exists(path))
            {
                Process.Start("explorer", '"' + path + '"');
            }
        }

        private void _fullLifeThewMana_Click(object sender, EventArgs e)
        {
            Globals.ThePlayer.FullLife();
            Globals.ThePlayer.FullMana();
            Globals.ThePlayer.FullThew();
        }

        private void _levelUp_Click(object sender, EventArgs e)
        {
            Globals.ThePlayer.LevelUp();
        }

        private void _addMoney1000_Click(object sender, EventArgs e)
        {
            Globals.ThePlayer.AddMoney(1000);
        }

        private void GameEditor_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = false;
        }

        private void GameEditor_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = false;
        }

        private void GameEditor_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = false;
        }

        private void _allEnemyDie_Click(object sender, EventArgs e)
        {
            NpcManager.AllEnemyDie();
        }

        private void _changePlayerPos_Click(object sender, EventArgs e)
        {
            using (var posDialog = new PlayerPosDialog())
            {
                if (posDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var x = int.Parse(posDialog.MapX.Text);
                        var y = int.Parse(posDialog.MapY.Text);
                        Globals.PlayerKindCharacter.SetPosition(new Vector2(x, y));
                    }
                    catch (Exception)
                    {
                        //Do nothing
                    }
                }
            }
        }

        private void _runScriptMenu_Click(object sender, EventArgs e)
        {
            if (_scriptDialog.ShowDialog() == DialogResult.OK)
            {
                var script = new ScriptParser();
                script.ReadFromLines(_scriptDialog.ScriptContent.Lines, "运行脚本");
                ScriptManager.RunScript(script);
            }
        }

        private void _variablesMenu_Click(object sender, EventArgs e)
        {
            if (_variablesWindow.Visible == false)
            {
                _variablesWindow.Show(this);
            }
        }

        public void OnScriptVariables(Dictionary<string, int> variables)
        {
            var text = new StringBuilder();
            foreach (var variable in variables)
            {
                text.AppendLine(variable.Key + "=" + variable.Value);
            }
            _variablesWindow.VariablesList.Text = text.ToString();
        }

        private void GameEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            _scriptDialog.Dispose();
            _variablesWindow.Dispose();
            _log.Dispose();
            e.Cancel = false;
        }

        private void _logMenu_Click(object sender, EventArgs e)
        {
            if (_log.Visible == false)
            {
                _log.Show(this);
            }
        }

        public void OnLog(string message)
        {
            _log.LogTextCtrl.AppendText(message);
        }

        private void OnLoadGame(int index)
        {
            Loader.LoadGame(index);
            GuiManager.ShowSaveLoad(false);
            GuiManager.ShowTitle(false);
        }

        private void rpg1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OnLoadGame(1);
        }

        private void rpg2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OnLoadGame(2);
        }

        private void rpg3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OnLoadGame(3);
        }

        private void rpg4ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OnLoadGame(4);
        }

        private void rpg5ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OnLoadGame(5);
        }

        private void rpg6ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OnLoadGame(6);
        }

        private void rpg7ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OnLoadGame(7);
        }

        private void rpg1ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Saver.SaveGame(1, Globals.TheGame.TakeSnapShot());
        }

        private void rpg2ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Saver.SaveGame(2, Globals.TheGame.TakeSnapShot());
        }

        private void rpg3ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Saver.SaveGame(3, Globals.TheGame.TakeSnapShot());
        }

        private void rpg4ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Saver.SaveGame(4, Globals.TheGame.TakeSnapShot());
        }

        private void rpg5ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Saver.SaveGame(5, Globals.TheGame.TakeSnapShot());
        }

        private void rpg6ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Saver.SaveGame(6, Globals.TheGame.TakeSnapShot());
        }

        private void rpg7ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Saver.SaveGame(7, Globals.TheGame.TakeSnapShot());
        }

        private void _restartGameMenu_Click(object sender, EventArgs e)
        {
            SaveSettings();
            Program.Restart = true;
            TheJxqyGame.ExitGameImmediately();
        }

        private void _aboutMeun_Click(object sender, EventArgs e)
        {
            MessageBox.Show("By 小试刀剑");
        }

        private void _xiulianMagicLevelUp_Click(object sender, EventArgs e)
        {
            if(Globals.ThePlayer == null) return;
            var info = Globals.ThePlayer.XiuLianMagic;
            if (info == null || info.TheMagic == null) return;
            
            Globals.ThePlayer.AddMagicExp(info, info.TheMagic.LevelupExp - info.Exp + 1);
        }

        private void disableNpcAIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScriptExecuter.DisableNpcAI();
        }

        private void enableNpcAIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScriptExecuter.EnableNpcAI();
        }

        private void _reducePlayerLifeMenu_Click(object sender, EventArgs e)
        {
            Globals.ThePlayer.AddLife(-1000);
        }

        private void emptyManaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Globals.ThePlayer.Mana = 0;
        }

        private void emptyThewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Globals.ThePlayer.Thew = 0;
        }

        private void _levelupCurrentMagicMenuItem_Click(object sender, EventArgs e)
        {
            if (Globals.ThePlayer == null) return;
            var info = Globals.ThePlayer.CurrentMagicInUse;
            if (info == null || info.TheMagic == null) return;

            Globals.ThePlayer.AddMagicExp(info, info.TheMagic.LevelupExp - info.Exp + 1);
        }

        private void _levelDownCurrentMagicMenuItem_Click(object sender, EventArgs e)
        {
            if (Globals.ThePlayer == null) return;
            var info = Globals.ThePlayer.CurrentMagicInUse;
            if (info == null || info.TheMagic == null || info.Level < 2) return;

            info.Exp = info.Level > 2 ? info.TheMagic.GetLevel(info.Level - 2).LevelupExp : 0;
            info.TheMagic = info.TheMagic.GetLevel(info.Level - 1);
            GuiManager.ShowMessage("武功 " + info.TheMagic.Name + " 降级了");
        }

        private void _reloadCurrentMagicMenuItem_Click(object sender, EventArgs e)
        {
            if (Globals.ThePlayer == null) return;
            var index = Globals.ThePlayer.CurrentUseMagicIndex;
            MagicListManager.SavePlayerList(StorageBase.MagicListFilePath);
            MagicListManager.LoadPlayerList(StorageBase.MagicListFilePath);
            Globals.ThePlayer.CurrentUseMagicIndex = index;
            Globals.ThePlayer.XiuLianMagic = MagicListManager.GetItemInfo(
                MagicListManager.XiuLianIndex);
        }

        private void DrawSurface_MouseMove(object sender, MouseEventArgs e)
        {
            var mouseState = Mouse.GetState();
            var mouseScreenPosition = new Vector2(mouseState.X, mouseState.Y);
            var mouseWorldPosition = Globals.TheCarmera.ToWorldPosition(mouseScreenPosition);
            var mouseTilePosition = MapBase.ToTilePosition(mouseWorldPosition);
            toolStripStatusLabel1.Text = string.Format("{0}x{1}", mouseTilePosition.X, mouseTilePosition.Y);
        }

        private void GameEditor_Load(object sender, EventArgs e)
        {

        }

        private void showRangeInRadiusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var posDialog = new RangeRadiusDialog())
            {
                if (posDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var radius = int.Parse(posDialog.RangeRadiusText.Text);
                        var name = posDialog.NpcName.Text;
                        var chracter = Globals.PlayerKindCharacter;
                        if (!string.IsNullOrEmpty(name))
                        {
                            chracter = NpcManager.GetNpc(name);
                        }
                        if (chracter != null)
                        {
                            chracter.ShowRangeRadius(radius);
                        }
                        else
                        {
                            MessageBox.Show("没找到NPC：" + name);
                        }
                    }
                    catch (Exception)
                    {
                        //Do nothing
                    }
                }
            }
        }
    }
}
