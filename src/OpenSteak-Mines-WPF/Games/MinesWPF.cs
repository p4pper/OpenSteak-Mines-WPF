// <copyright file="GameGUI_WPF.cs" company="openSteak">
// Copyright (c) openSteak. All rights reserved.
// </copyright>

using System.Globalization;

namespace OpenSteak_Mines_WPF
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// WPF controller/facade for the Mines game.
    /// - Owns a <see cref="Games.Mines"/> instance (pure logic) and binds it to the WPF controls in <see cref="MainWindow"/>.
    /// - Manages UI state (Start ↔ Cashout), balance persistence, and per-round flow.
    /// - Renders the 5x5 grid of buttons and handles click events.
    ///
    /// This class is intentionally small and self-contained so the WPF app does not depend on the old core library.
    /// </summary>
    public class MinesWPF
    {
        private const decimal DefaultBalance = 5.0m;

        private enum GameState
        {
            Off,
            On,
            Lost,
            Won,
        }

        private Grid minesGrid;
        private MainWindow minesWindow;
        private Button cashOutORStartButton;
        private ComboBox mineCombo;
        private Label balanceLabel;
        private Label payoutMultiplierLabel;
        private TextBox betAmountField;

        private Games.Mines logic;
        private int gridSize;
        private string[] layout;

        private decimal balance;
        private decimal currentBet = 0;
        private GameState gameState = GameState.Off;

        /// <summary>
        /// Initialize the controller by wiring the required <see cref="MainWindow"/> controls.
        /// Loads balance, initializes the mines combo, grid, and labels.
        /// </summary>
        /// <param name="minesGrid">Grid container that will host the 5x5 mine buttons.</param>
        /// <param name="minesWindow">Window providing access to shared styles/resources.</param>
        /// <param name="cashOutOrStartButton">Start/Cashout button that drives the game state.</param>
        /// <param name="mineCombo">ComboBox with selectable mine counts (1..24).</param>
        /// <param name="balanceLabel">Label to display the player's current balance.</param>
        /// <param name="payoutMultiplierLabel">Label to display the current cashout multiplier.</param>
        /// <param name="betAmountField">Textbox where the user enters the bet amount.</param>
        public MinesWPF(
            Grid minesGrid,
            MainWindow minesWindow,
            Button cashOutOrStartButton,
            ComboBox mineCombo,
            Label balanceLabel,
            Label payoutMultiplierLabel,
            TextBox betAmountField)
        {
            this.minesGrid = minesGrid;
            this.minesWindow = minesWindow;
            this.cashOutORStartButton = cashOutOrStartButton;
            this.mineCombo = mineCombo;
            this.balanceLabel = balanceLabel;
            this.payoutMultiplierLabel = payoutMultiplierLabel;
            this.betAmountField = betAmountField;

            this.logic = new Games.Mines();
            this.gridSize = this.logic.GetGridSize();
            this.layout = this.logic.GetLayout();

            Initialize();
        }

        /// <summary>
        /// One-time setup: loads or creates the persisted balance file, populates UI controls,
        /// and sets initial labels and button state.
        /// </summary>
        private void Initialize()
        {
            // Load or initialize balance
            if (File.Exists("balance.txt"))
            {
                decimal.TryParse(File.ReadAllText("balance.txt"), out balance);
            }
            else
            {
                File.WriteAllText("balance.txt", DefaultBalance.ToString(CultureInfo.CurrentCulture));
                balance = DefaultBalance;
            }

            InitializeMinesAmountComboBoxGUI();
            InitializeGridGUI(false);
            UpdateBalanceGUI();
            RestartPayoutMultiplierGUI();
            this.cashOutORStartButton.Content = "Start";
        }

        /// <summary>
        /// Reveals all mines on the grid by disabling the buttons and making the mine images visible.
        /// </summary>
        public static void RevealAllMinesGUI(Grid minesGrid)
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
        /// - When Off: validates bet, debits balance, starts a new round and renders an interactive grid.
        /// - When On (and at least one gem revealed): treats as cashout, pays winnings, and resets to Start.
        /// </summary>
        public void StartOrCashout()
        {
            if (this.gameState == GameState.Off)
            {
                // Parse bet
                if ((decimal.TryParse(this.betAmountField.Text, out this.currentBet) && this.currentBet > 0) || this.currentBet == 0)
                {
                    if (this.currentBet <= this.balance)
                    {
                        this.balance -= this.currentBet;
                        UpdateBalanceGUI();

                        this.logic.RevealedGems = 0;
                        this.logic.MinesCount = GetSelectedMinesAmountGUI();
                        this.logic.Start();

                        this.gameState = GameState.On;
                        SetComponentsToCashoutGUI();
                        InitializeGridGUI(true);
                    }
                    else
                    {
                        RestartBetAmountGUI();
                        this.currentBet = 0;
                    }
                }
                else
                {
                    RestartBetAmountGUI();
                    this.currentBet = 0;
                }
            }
            else if (this.gameState == GameState.On && this.logic.RevealedGems > 0)
            {
                // Cashout
                this.gameState = GameState.Won;
                EndGame();
            }
        }

        public decimal GetBalance()
        {
            balance = Math.Round(balance, 2);
            return balance;
        }

        /// <summary>
        /// Initializes the grid GUI by clearing existing definitions and children, then creating and adding new row and column definitions, and populating the grid with mine buttons.
        /// </summary>
        /// <param name="enableMinesInteraction">Allow mine buttons to be pressed.</param>
        protected void InitializeGridGUI(bool enableMinesInteraction)
        {
            this.minesGrid.RowDefinitions.Clear();
            this.minesGrid.ColumnDefinitions.Clear();
            this.minesGrid.Children.Clear();

            for (int i = 0; i < this.gridSize; i++)
            {
                this.minesGrid.RowDefinitions.Add(new RowDefinition());
                this.minesGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int row = 0; row < this.gridSize; row++)
            {
                for (int col = 0; col < this.gridSize; col++)
                {
                    Button button = this.CreateMineButtonGUI(enableMinesInteraction, row, col);
                    this.minesGrid.Children.Add(button);
                }
            }
        }

        /// <summary>
        /// Initializes the MinesAmountComboBoxGUI by adding integers from 1 to 24 as items.
        /// </summary>
        protected void InitializeMinesAmountComboBoxGUI()
        {
            this.mineCombo.Items.Clear();
            for (int i = 1; i <= 24; i++)
            {
                this.mineCombo.Items.Add(i);
            }
            if (this.mineCombo.Items.Count > 0 && this.mineCombo.SelectedIndex < 0)
            {
                this.mineCombo.SelectedIndex = 1; // default to 2 mines like XAML
            }
        }

        /// <summary>
        /// Changes the GUI components to their start state. This is when the game is restarted/stopped
        /// </summary>
        protected void SetComponentsToStartGUI()
        {
            this.cashOutORStartButton.Content = "Start";
            this.cashOutORStartButton.IsEnabled = true;
            this.mineCombo.IsEnabled = true;
            this.RestartPayoutMultiplierGUI();
        }

        /// <summary>
        /// Changes the GUI components to their cashout state. Note that this happens when game is started
        /// </summary>
        protected void SetComponentsToCashoutGUI()
        {
            this.cashOutORStartButton.Content = "Cashout";
            this.cashOutORStartButton.IsEnabled = false;
            this.mineCombo.IsEnabled = false;
        }

        /// <summary>Updates the balance label in the GUI to reflect the current balance.</summary>
        protected void UpdateBalanceGUI()
        {
            this.balance = Math.Round(this.balance, 2);
            this.balanceLabel.Content = "Balance: $" + this.balance.ToString("0.00");
        }

        /// <summary>
        /// Updates the payout multiplier label with the current cashout multiplier value.
        /// </summary>
        protected void UpdateMultiplierGUI()
        {
            this.payoutMultiplierLabel.Content = this.logic.GetCashoutMultiplier().ToString("0.00") + "x";
        }

        /// <summary>
        /// Enables the cashout GUI components.
        /// </summary>
        protected void EnableCashoutGUI()
        {
            this.cashOutORStartButton.IsEnabled = true;
        }

        private void CheckForWin()
        {
            bool isGameWon = this.logic.RevealedGems == ((this.gridSize * this.gridSize) - this.logic.MinesCount);
            if (isGameWon)
            {
                this.gameState = GameState.Won;
                EndGame();
                return;
            }

            if (this.logic.RevealedGems == 1)
            {
                // Enable cashout button
                this.EnableCashoutGUI();
            }
        }

        private void EndGame()
        {
            RevealAllMinesGUI(minesGrid);

            if (gameState == GameState.Won)
            {
                balance += currentBet * logic.GetCashoutMultiplier();
                balance = Math.Round(balance, 2);
            }

            UpdateBalanceGUI();
            SetComponentsToStartGUI();
            gameState = GameState.Off;
            File.WriteAllText("balance.txt", this.balance.ToString());
        }

        /// <summary>
        /// Retrieves the current bet amount from the GUI.
        /// </summary>
        /// <returns>The current bet amount as a string.</returns>
        protected string GetBetAmountGUI()
        {
            return this.betAmountField.Text;
        }

        /// <summary>
        /// Retrieves the currently selected number of mines from the GUI combo box and returns it as an integer.
        /// </summary>
        /// <returns>returns how </returns>
        protected int GetSelectedMinesAmountGUI()
        {
            return int.Parse(this.mineCombo.SelectedValue.ToString());
        }

        protected void RestartBetAmountGUI()
        {
            this.betAmountField.Text = "0.00";
        }

        protected void RestartPayoutMultiplierGUI()
        {
            this.payoutMultiplierLabel.Content = "0.00x";
        }

        private Button CreateMineButtonGUI(bool enableMines, int row, int col)
        {
            int index = (row * this.gridSize) + col;
            Button button = new Button
            {
                Margin = new Thickness(2),
                Style = (Style)this.minesWindow.Resources["MineButton"],
                IsEnabled = enableMines,
            };

            if (enableMines)
            {
                string iconType = this.layout[index];
                Image iconImage = this.CreateIconImageGUI(iconType);
                button.Content = iconImage;
                button.Tag = iconType;
            }

            button.Click += this.MineButton_Click_Event;
            Grid.SetRow(button, row);
            Grid.SetColumn(button, col);

            return button;
        }

        private Image CreateIconImageGUI(string iconType)
        {
            string imagePath = iconType == "m"
                ? "pack://application:,,,/Resources/mine.png"
                : "pack://application:,,,/Resources/gem.png";

            Image iconImage = new Image
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
            Button clickedButton = sender as Button;
            if (clickedButton == null)
            {
                return;
            }

            if ((string)clickedButton.Tag == "m")
            {
                // Mine clicked: reveal all and end game as lost
                RevealAllMinesGUI(this.minesGrid);
                this.gameState = GameState.Lost;
                EndGame();
            }
            else
            {
                // Gem clicked
                this.logic.RevealedGems++;
                UpdateMultiplierGUI();
                CheckForWin();
            }

            clickedButton.Style = (Style)this.minesWindow.Resources["MineButtonRevealed"];
            Image iconImage = clickedButton.Content as Image;
            iconImage.Visibility = Visibility.Visible;
            clickedButton.IsEnabled = false;
        }
    }
}
