// <copyright file="GameGUI_WPF.cs" company="openSteak">
// Copyright (c) openSteak. All rights reserved.
// </copyright>

using OpenSteak_Mines_WPF.Util;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OpenSteak_Mines_WPF.Games
{
    /// <summary>
    /// WPF controller/facade for the Mines game.
    /// - Owns a <see cref="Games.Mines"/> instance (pure logic) and binds it to the WPF controls in <see cref="MainWindow"/>.
    /// - Manages UI state (Start ↔ Cashout), balance persistence, and per-round flow.
    /// - Renders the 5x5 grid of buttons and handles click events.
    ///
    /// This class is intentionally small and self-contained, so the WPF app does not depend on the old core library.
    /// </summary>
    public class MinesWpf
    {
        private const decimal DefaultBalance = 5.0m;

        private enum GameState
        {
            Off,
            On,
            Lost,
            Won,
        }

        private readonly Grid _minesGrid;
        private readonly Button _cashOutOrStartButton;
        private readonly ComboBox _mineCombo;
        private readonly Label _balanceLabel;
        private readonly Label _payoutMultiplierLabel;
        private readonly TextBox _betAmountField;

        private readonly Label _betReturnLabelBeauty;
        private readonly Label _betReturnTotalMoneyBeauty;
        private readonly Border _popupBorder;

        private readonly Mines _logic;
        private readonly int _gridSize;
        private readonly string[] _layout;

        private decimal _currentBet;
        private GameState _gameState = GameState.Off;


        /// <summary>
        /// Initialize the controller by wiring the required <see cref="MainWindow"/> controls.
        /// Loads balance, initializes the mine combo, grid, and labels.
        /// </summary>
        /// <param name="minesGrid">Grid container that will host the 5x5 mine buttons.</param>
        /// <param name="cashOutOrStartButton">Start/Cashout button that drives the game state.</param>
        /// <param name="mineCombo">ComboBox with selectable mine counts (1..24).</param>
        /// <param name="balanceLabel">Label to display the player's current balance.</param>
        /// <param name="payoutMultiplierLabel">Label to display the current cashout multiplier.</param>
        /// <param name="betAmountField">Textbox where the user enters the bet amount.</param>
        /// <param name="betReturnLabelBeauty"></param>
        /// <param name="betReturnTotalMoneyBeauty"></param>
        /// <param name="popupBorder"></param>
        public MinesWpf(
            Grid minesGrid,
            Button cashOutOrStartButton,
            ComboBox mineCombo,
            Label balanceLabel,
            Label payoutMultiplierLabel,
            TextBox betAmountField,
            Label betReturnLabelBeauty,
            Label betReturnTotalMoneyBeauty,
            Border popupBorder
            )
        {
            _minesGrid = minesGrid;
            _cashOutOrStartButton = cashOutOrStartButton;
            _mineCombo = mineCombo;
            _balanceLabel = balanceLabel;
            _payoutMultiplierLabel = payoutMultiplierLabel;
            _betAmountField = betAmountField;
            _betReturnLabelBeauty = betReturnLabelBeauty;
            _betReturnTotalMoneyBeauty = betReturnTotalMoneyBeauty;
            _popupBorder = popupBorder;
            
            _logic = new Mines();
            _gridSize = _logic.GetGridSize();
            _layout = _logic.GetLayout();

            Initialize();
        }

        /// <summary>
        /// One-time setup: loads or creates the persisted balance file, populates UI controls,
        /// and sets initial labels and button state.
        /// </summary>
        private void Initialize()
        {
            InitializeMinesAmountComboBoxGui();
            InitializeGridGui(false);

            BalanceManager.InitializeBalance();
            BalanceManager.GetBalanceFormatted();
            UpdateBalanceGui();

            RestartPayoutMultiplierGui();
            this._cashOutOrStartButton.Content = "Start";
        }

        /// <summary>
        /// Reveals all mines on the grid by disabling the buttons and making the mine images visible.
        /// </summary>
        private static void RevealAllMinesGui(Grid minesGrid)
        {
            foreach (var child in minesGrid.Children)
            {
                if (child is Button btn)
                {
                    btn.IsEnabled = false;
                    ((Image)btn.Content).Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Primary button action.
        /// - When Off: validates bet, debits balance, starts a new round, and renders an interactive grid.
        /// - When On (and at least one gem revealed): treats as cashout, pays winnings, and resets to Start.
        /// </summary>
        public void StartOrCashout()
        {
            switch (_gameState)
            {
                // Parse bet
                case GameState.Off when (decimal.TryParse(_betAmountField.Text, out _currentBet) && _currentBet > 0) || _currentBet == 0:
                {
                    if (_currentBet <= BalanceManager.GetBalanceFormatted())
                    {
                        _betAmountField.IsEnabled = false;
                        BalanceManager.RemoveFromBalance(_currentBet);
                        UpdateBalanceGui();

                        _logic.RevealedGems = 0;
                        _logic.MinesCount = GetSelectedMinesAmountGui();
                        _logic.Start();

                        TransitionTo(GameState.On);
                        SetComponentsToCashoutGui();
                        InitializeGridGui(true);
                    }
                    else
                    {
                        RestartBetAmountGui();
                        _currentBet = 0;
                    }

                    break;
                }
                case GameState.Off:
                    RestartBetAmountGui();
                    _currentBet = 0;
                    break;
                case GameState.On when _logic.RevealedGems > 0:
                {
                    // Cashout
                    var multiplier = _logic.GetCashoutMultiplier();
                    var amount = Math.Round(_currentBet * multiplier, 2);
                    ShowCashoutPopup(multiplier, amount);

                    TransitionTo(GameState.Won);
                    EndGame();
                    break;
                }
            }
        }

        private void TransitionTo(GameState newState)
        {
            _gameState = newState;
        }

        private void ShowCashoutPopup(decimal multiplier, decimal totalAmount)
        {
            // Update popup labels
            _betReturnLabelBeauty.Content = multiplier.ToString("0.00") + "x";
            _betReturnTotalMoneyBeauty.Content = "$" + totalAmount.ToString("0.00");

            // Show and auto-hide (Collapsed to trigger the hide animation in XAML)
            _popupBorder.Visibility = Visibility.Visible;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.8) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                _popupBorder.Visibility = Visibility.Collapsed;
            };
            timer.Start();
        }

        /// <summary>
        /// Initializes the grid GUI by clearing existing definitions and children, then creating and adding new row and column definitions, and populating the grid with mine buttons.
        /// </summary>
        /// <param name="enableMinesInteraction">Allow mine buttons to be pressed.</param>
        private void InitializeGridGui(bool enableMinesInteraction)
        {
            _minesGrid.RowDefinitions.Clear();
            _minesGrid.ColumnDefinitions.Clear();
            _minesGrid.Children.Clear();

            for (int i = 0; i < _gridSize; i++)
            {
                _minesGrid.RowDefinitions.Add(new RowDefinition());
                _minesGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int row = 0; row < _gridSize; row++)
            {
                for (int col = 0; col < _gridSize; col++)
                {
                    Button button = CreateMineButtonGui(enableMinesInteraction, row, col);
                    _minesGrid.Children.Add(button);
                }
            }
        }

        /// <summary>
        /// Initializes the MinesAmountComboBoxGUI by adding integers from 1 to 24 as items.
        /// </summary>
        private void InitializeMinesAmountComboBoxGui()
        {
            _mineCombo.Items.Clear();
            for (int i = 1; i <= 24; i++)
            {
                _mineCombo.Items.Add(i);
            }
            if (_mineCombo.Items.Count > 0 && _mineCombo.SelectedIndex < 0)
            {
                _mineCombo.SelectedIndex = 1; // default to 2 mines like XAML
            }
        }

        /// <summary>
        /// Changes the GUI components to their start state. This is when the game is restarted/stopped
        /// </summary>
        private void SetComponentsToStartGui()
        {
            _cashOutOrStartButton.Content = "Start";
            _cashOutOrStartButton.IsEnabled = true;
            _mineCombo.IsEnabled = true;
            _betAmountField.IsEnabled = true;
            RestartPayoutMultiplierGui();
        }

        /// <summary>
        /// Changes the GUI components to their cashout state. Note that this happens when the game is started
        /// </summary>
        private void SetComponentsToCashoutGui()
        {
            _cashOutOrStartButton.Content = "Cashout";
            _cashOutOrStartButton.IsEnabled = true; // keep enabled; logic blocks premature cashout
            _mineCombo.IsEnabled = false;
            _betAmountField.IsEnabled = false;
        }

        /// <summary>Updates the balance label in the GUI to reflect the current balance.</summary>
        private void UpdateBalanceGui()
        {
            _balanceLabel.Content = "Balance: $" + BalanceManager.GetBalanceFormatted().ToString("0.00");
        }

        /// <summary>
        /// Updates the payout multiplier label with the current cashout multiplier value.
        /// </summary>
        private void UpdateMultiplierGui()
        {
            _payoutMultiplierLabel.Content = _logic.GetCashoutMultiplier().ToString("0.00") + "x";
        }
        private void CheckForWin()
        {
            var isGameWon = _logic.RevealedGems == ((_gridSize * _gridSize) - _logic.MinesCount);
            if (!isGameWon) return; // The game isn't won yet.
            
            TransitionTo(GameState.Won);
            EndGame();
        }

        public event Action<decimal> SessionEnded;

        private void EndGame()
        {
            RevealAllMinesGui(_minesGrid);

            decimal sessionDelta;
            if (_gameState == GameState.Won)
            {
                // Settle winnings at the multiplier at the time of cashout
                var winAmount = Math.Round(_currentBet * _logic.GetCashoutMultiplier(), 2);
                BalanceManager.AddToBalance(winAmount);
                sessionDelta = winAmount - _currentBet; // net profit for this session
            }
            else
            {
                sessionDelta = -_currentBet; // lost the bet
            }

            // Notify listeners (e.g., for chart updates)
            try { SessionEnded?.Invoke(sessionDelta); } catch { /* swallow to not break game flow */ }

            UpdateBalanceGui();
            SetComponentsToStartGui();
            BalanceManager.UpdateBalanceToDatabase();

            TransitionTo(GameState.Off);
        }

        /// <summary>
        /// Retrieves the current bet amount from the GUI.
        /// </summary>
        /// <returns>The current bet amount as a string.</returns>
        protected string GetBetAmountGui()
        {
            return _betAmountField.Text;
        }

        /// <summary>
        /// Retrieves the currently selected number of mines from the GUI combo box and returns it as an integer.
        /// </summary>
        /// <returns>returns how </returns>
        private int GetSelectedMinesAmountGui()
        {
            return int.Parse(_mineCombo.SelectedValue.ToString());
        }

        private void RestartBetAmountGui()
        {
            _betAmountField.Text = "0.00";
        }

        private void RestartPayoutMultiplierGui()
        {
            _payoutMultiplierLabel.Content = "0.00x";
        }

        private Button CreateMineButtonGui(bool enableMines, int row, int col)
        {
            var index = (row * _gridSize) + col;
            var button = new Button
            {
                Margin = new Thickness(2),
                Style = (Style)Application.Current.Resources["MineButton"],
                IsEnabled = enableMines,
            };

            if (enableMines)
            {
                var iconType = _layout[index];
                var iconImage = CreateIconImageGui(iconType);
                button.Content = iconImage;
                button.Tag = iconType;
            }

            button.Click += MineButton_Click_Event;
            Grid.SetRow(button, row);
            Grid.SetColumn(button, col);

            return button;
        }

        private Image CreateIconImageGui(string iconType)
        {
            var imagePath = iconType == "m"
                ? "pack://application:,,,/Src/Resources/mine.png"
                : "pack://application:,,,/Src/Resources/gem.png";

            var iconImage = new Image
            {
                Source = new BitmapImage(new Uri(imagePath)),
                Width = 20,
                Height = 20,
                Visibility = Visibility.Collapsed,
                RenderTransform = new ScaleTransform(3, 3),
                RenderTransformOrigin = new Point(0.5, 0.5),
            };

            return iconImage;
        }

        private void MineButton_Click_Event(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button clickedButton))
                return;
            

            if ((string)clickedButton.Tag == "m")
            {
                // Mine clicked: reveal all and end game as lost
                _cashOutOrStartButton.Style = (Style)Application.Current.Resources["BetButtonLost"];
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(0.7)
                };
                
                timer.Tick += (s, v) =>
                {
                    timer.Stop();
                    _cashOutOrStartButton.Style = (Style)Application.Current.Resources["BetButton"];
                };
                timer.Start();

                RevealAllMinesGui(_minesGrid);
                TransitionTo(GameState.Lost);
                EndGame();
            }
            else
            {
                // Gem clicked
                _logic.RevealedGems++;
                UpdateMultiplierGui();
                CheckForWin();
            }

            clickedButton.Style = (Style)Application.Current.Resources["MineButtonRevealed"];
            if (clickedButton.Content is Image iconImage) 
                iconImage.Visibility = Visibility.Visible;
            clickedButton.IsEnabled = false;
        }

        //
        // I don't have any fucking idea why MoneyInput needs dynamic decimal..
        // Perhaps I was too drunk when I wrote MoneyInput.cs
        //
        // God help.
        //
        public Func<decimal> GetBalanceDynamically()
        {
            Func<decimal> dynamicBal = () => BalanceManager.GetBalanceFormatted();
            return dynamicBal;
        }
    }
}
