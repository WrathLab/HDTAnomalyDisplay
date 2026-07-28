using System;
using System.Linq;
using System.Threading.Tasks;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using Hearthstone_Deck_Tracker.Utility.Logging;
using System.Windows.Controls;
using System.Windows.Media;
using Hearthstone_Deck_Tracker.Controls;
// Core is ambiguous between Hearthstone_Deck_Tracker.Core (static
// class) and Hearthstone_Deck_Tracker.API.Core (which has the
// OverlayCanvas). We resolve by using the full API.Core path for
// anything that's only on the API side, and the static Core for
// things like Game / GameEntity.
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace HDTAnomalyDisplay
{
    public class AnomalyDisplay
    {
        public CardImage CardImage;
        public static MoveCardManager MoveManager;

        public AnomalyDisplay()
        {
        }

        // Wait for the GAME entity to expose the anomaly DbfId. The
        // original code waited for 2 hero entities with the
        // BACON_HERO_CAN_BE_DRAFTED tag — that's the INITIAL hero
        // select (one-time per BG run), not the per-match anomaly
        // detection we actually need. The wait timed out for every
        // normal BG match and the plugin never got to read the
        // anomaly. We just spin up to ~3s polling the game entity
        // directly, which is the real signal we want.
        public async Task AwaitGameEntity()
        {
            const int maxAttempts = 12;        // 12 * 250ms = 3s
            const int delayBetweenAttempts = 250;

            for (var i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(delayBetweenAttempts);

                if (Core.Game == null || Core.Game.GameEntity == null)
                    continue;

                int? anomalyId = BattlegroundsUtils.GetBattlegroundsAnomalyDbfId(Core.Game.GameEntity);
                if (anomalyId.HasValue)
                {
                    Log.Info($"[HDTAnomalyDisplay] Game entity exposed anomaly DbfId={anomalyId.Value} on attempt {i + 1}");
                    return;
                }
            }
        }

        public void InitializeView(int cardDbfId)
        {
            // Do not recreate card if it already exists via a double call to HandleGameStart() (cf OnLoad)
            if (CardImage == null)
            {
                CardImage = new CardImage();

                Core.OverlayCanvas.Children.Add(CardImage);
                Canvas.SetTop(CardImage, Settings.Default.AnomalyCardTop);
                Canvas.SetLeft(CardImage, Settings.Default.AnomalyCardLeft);
                CardImage.Visibility = System.Windows.Visibility.Visible;

                MoveManager = new MoveCardManager(CardImage, SettingsView.IsUnlocked);
                Settings.Default.PropertyChanged += SettingsChanged;
                SettingsChanged(null, null);
            }

            CardImage.SetCardIdFromCard(Database.GetCardFromDbfId(cardDbfId, false));
        }

        // On scaling change update the card
        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            CardImage.RenderTransform = new ScaleTransform(Settings.Default.AnomalyCardScale / 100, Settings.Default.AnomalyCardScale / 100);
            Canvas.SetTop(CardImage, Settings.Default.AnomalyCardTop);
            Canvas.SetLeft(CardImage, Settings.Default.AnomalyCardLeft);
        }

        public async void HandleGameStart()
        {
            // Entire flow wrapped in try/catch so a single null entity
            // or a moved API doesn't take the plugin down silently. The
            // original code threw NREs that left the user with no
            // anomaly overlay and no log to debug.
            try
            {
                if (Core.Game == null || Core.Game.CurrentGameMode != GameMode.Battlegrounds)
                {
                    Log.Info("[HDTAnomalyDisplay] HandleGameStart: not a Battlegrounds game, ignoring.");
                    return;
                }

                await AwaitGameEntity();

                if (Core.Game == null || Core.Game.GameEntity == null)
                {
                    Log.Warn("[HDTAnomalyDisplay] HandleGameStart: GameEntity still null after wait, aborting.");
                    return;
                }

                int? anomalyDbfId = BattlegroundsUtils.GetBattlegroundsAnomalyDbfId(Core.Game.GameEntity);

                if (anomalyDbfId.HasValue)
                {
                    Log.Info($"[HDTAnomalyDisplay] Anomaly DbfId found: {anomalyDbfId.Value}");
                    InitializeView(anomalyDbfId.Value);
                }
                else
                {
                    Log.Warn("[HDTAnomalyDisplay] No anomaly DbfId found — this BG match has no active anomaly.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[HDTAnomalyDisplay] HandleGameStart failed");
            }
        }

        public void ClearCard()
        {
            try
            {
                if (CardImage != null)
                {
                    CardImage.SetCardIdFromCard(null);
                    if (Core.OverlayCanvas != null)
                        Core.OverlayCanvas.Children.Remove(CardImage);
                }
                CardImage = null;

                Log.Info("[HDTAnomalyDisplay] Destroying the MoveManager...");
                if (MoveManager != null)
                {
                    MoveManager.Dispose();
                    MoveManager = null;
                }

                Settings.Default.PropertyChanged -= SettingsChanged;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[HDTAnomalyDisplay] ClearCard failed");
            }
        }
    }
}
