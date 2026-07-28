using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;
using System;
using System.Windows.Controls;

namespace HDTAnomalyDisplay
{
    public class AnomalyDisplayPlugin : IPlugin
    {
        public string Name => "HDT Anomaly Display";

        public string Description => "Displays the current Battlegrounds anomaly on your overlay.";

        public string ButtonText => "SETTINGS";

        public string Author => "Mouchoir & Tignus (patched)";

        public Version Version => new Version(1, 3, 0);

        public MenuItem MenuItem => CreateMenu();

        private MenuItem CreateMenu()
        {
            MenuItem settingsMenuItem = new MenuItem { Header = "Anomaly Display Settings" };

            settingsMenuItem.Click += (sender, args) =>
            {
                try
                {
                    SettingsView.Flyout.IsOpen = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[HDTAnomalyDisplay] Failed to open settings flyout");
                }
            };

            return settingsMenuItem;
        }
        public AnomalyDisplay anomalyDisplay;

        public void OnButtonPress() => SettingsView.Flyout.IsOpen = true;

        public void OnLoad()
        {
            try
            {
                Log.Info("[HDTAnomalyDisplay] OnLoad");
                anomalyDisplay = new AnomalyDisplay();
                GameEvents.OnGameStart.Add(anomalyDisplay.HandleGameStart);
                GameEvents.OnGameEnd.Add(anomalyDisplay.ClearCard);

                // Processing GameStart logic in case plugin was loaded/unloaded after starting a game without restarting HDT
                anomalyDisplay.HandleGameStart();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[HDTAnomalyDisplay] OnLoad failed");
            }
        }

        public void OnUnload()
        {
            try
            {
                Log.Info("[HDTAnomalyDisplay] OnUnload");
                Settings.Default.Save();
                anomalyDisplay?.ClearCard();
                anomalyDisplay = null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[HDTAnomalyDisplay] OnUnload failed");
            }
        }

        public void OnUpdate()
        {
        }
    }
}
