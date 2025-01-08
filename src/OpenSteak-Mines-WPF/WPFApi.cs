// <copyright file="GameGUI_WPF.cs" company="openSteak">
// Copyright (c) openSteak. All rights reserved.
// </copyright>

namespace OpenSteak_Mines_WPF
{
    using OpenSteakMines;
    using System;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// This class connects GameAPI with the WPF Module using the GameGUI_Bridge.
    /// Of course, you can use MinesAPI and GameGUI_Bridge to connect it with other GUI frameworks
    /// </summary>
    public class WPFApi : GameGUI_Bridge
    {
        private Grid minesGrid;
        private MainWindow minesWindow;
        private Button cashOutORStartButton;
        private ComboBox mineCombo;
        private Label balanceLabel;
        private Label payoutMultiplierLabel;
        private TextBox betAmountField;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameGUI_WPF"/> class.
        /// </summary>
        /// <param name="api">Initialized GameAPI Class</param>
        /// <param name="gridSize">Grid size in one-dimension, for example 5x5, should be 5.</param>
        /// <param name="layout">Generated string layout array from GameLogic.</param>
        /// <param name="minesGrid">The GUI Component for the Mines Grid, contains the mines buttons.</param>
        /// <param name="minesWindow">Main Window, needed for the resources.</param>
        /// <param name="cashOutORStartButton">Main start/cashout button.</param>
        /// <param name="mineCombo">The combobox item containing numbered mines.</param>
        /// <param name="balanceLabel">The Balance label field.</param>
        /// <param name="payoutMultiplierLabel">The payout multiplier label field.</param>
        /// <param name="betAmountField">The bet amount field.</param>
        public WPFApi(
            GameAPI api,
            int gridSize,
            string[] layout,
            Grid minesGrid,
            MainWindow minesWindow,
            Button cashOutORStartButton,
            ComboBox mineCombo,
            Label balanceLabel,
            Label payoutMultiplierLabel,
            TextBox betAmountField)
        : base(api)
        {
            this.minesGrid = minesGrid;
            this.minesWindow = minesWindow;
            this.cashOutORStartButton = cashOutORStartButton;
            this.mineCombo = mineCombo;
            this.balanceLabel = balanceLabel;
            this.payoutMultiplierLabel = payoutMultiplierLabel;
            this.betAmountField = betAmountField;
        }

        /// <summary>
        /// Reveals all mines on the grid by disabling the buttons and making the mine images visible.
        /// </summary>
        protected override void RevealAllMinesGUI()
        {
            foreach (var child in this.minesGrid.Children)
            {
                if (child is Button btn)
                {
                    btn.IsEnabled = false;
                    ((Image)btn.Content).Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Initializes the grid GUI by clearing existing definitions and children, then creating and adding new row and column definitions, and populating the grid with mine buttons.
        /// </summary>
        /// <param name="enableMinesInteraction">Allow mine buttons to be pressed.</param>
        protected override void InitializeGridGUI(bool enableMinesInteraction)
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
        protected override void InitializeMinesAmountComboBoxGUI()
        {
            for (int i = 1; i <= 24; i++)
            {
                this.mineCombo.Items.Add(i);
            }
        }

        /// <summary>
        /// Changes the GUI components to their start state. This is when the game is restarted/stopped
        /// </summary>
        protected override void SetComponentsToStartGUI()
        {
            this.cashOutORStartButton.Content = "Start";
            this.cashOutORStartButton.IsEnabled = true;
            this.mineCombo.IsEnabled = true;
            this.RestartPayoutMultiplierGUI();
        }

        /// <summary>
        /// Changes the GUI components to their cashout state. Note that this happens when game is started
        /// </summary>
        protected override void SetComponentsToCashoutGUI()
        {
            this.cashOutORStartButton.Content = "Cashout";
            this.cashOutORStartButton.IsEnabled = false;
            this.mineCombo.IsEnabled = false;
        }

        /// <summary>Updates the balance label in the GUI to reflect the current balance.</summary>
        protected override void UpdateBalanceGUI()
        {
            this.balanceLabel.Content = "Balance: " + Math.Round(this.api.GetBalance(), 2).ToString("0.00");
        }

        /// <summary>
        /// Updates the payout multiplier label with the current cashout multiplier value.
        /// </summary>
        protected override void UpdateMultiplierGUI()
        {
            this.payoutMultiplierLabel.Content = this.api.getCashoutMultiplier().ToString("0.00") + "x";
        }

        /// <summary>
        /// Enables the cashout GUI components.
        /// </summary>
        protected override void EnableCashoutGUI()
        {
            this.cashOutORStartButton.IsEnabled = true;
        }

        /// <summary>
        /// Retrieves the current bet amount from the GUI.
        /// </summary>
        /// <returns>The current bet amount as a string.</returns>
        protected override string GetBetAmountGUI()
        {
            return this.betAmountField.Text;
        }

        /// <summary>
        /// Retrieves the currently selected number of mines from the GUI combo box and returns it as an integer.
        /// </summary>
        /// <returns>returns how </returns>
        protected override int GetSelectedMinesAmountGUI()
        {
            return int.Parse(this.mineCombo.SelectedValue.ToString());
        }

        protected override void RestartBetAmountGUI()
        {
            this.betAmountField.Text = "0.00";
        }

        protected override void RestartPayoutMultiplierGUI()
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
                Task.Delay(new Random().Next(2000, 4000));
                this.api.MineButtonClickedIsMine();
            }
            else
            {
                this.api.MineButtonClickedIsGem();
            }

            clickedButton.Style = (Style)this.minesWindow.Resources["MineButtonRevealed"];
            Image iconImage = clickedButton.Content as Image;
            iconImage.Visibility = Visibility.Visible;
            clickedButton.IsEnabled = false;
        }
    }
}
