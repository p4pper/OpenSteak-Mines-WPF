

namespace OpenSteak_Mines_WPF
{
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using OpenSteakMines;
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WPFApi gui;
        private GameAPI api;

        public MainWindow()
        {
            this.InitializeComponent();
            this.api = new GameAPI();
            this.gui = new WPFApi(
                this.api,
                this.api.GetGridSize(),
                this.api.GetLayout(),
                this.MinesGrid,
                this,
                this.cashOutORStartBtn,
                this.mineCombo,
                this.playerBalText,
                this.payoutMultiplierLbl,
                this.betAmountTxt);

            this.api.InitializeSelf(this.gui);
            this.betAmountTxt.TextChanged += this.BetAmountTxt_TextChanged;
        }

        private void t1_Click(object sender, RoutedEventArgs e)
        {
            this.api.StartGame();

           
        }

        private void BetAmountTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            textBox.TextChanged -= this.BetAmountTxt_TextChanged;

            string text = textBox.Text;
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number))
            {
                textBox.Text = number.ToString("0.00", CultureInfo.InvariantCulture);
                textBox.SelectionStart = textBox.Text.Length;
            }
            else
            {
                textBox.Text = "0.00";
            }

            textBox.TextChanged += this.BetAmountTxt_TextChanged;

            if (double.TryParse(textBox.Text, out double bet) && bet > this.api.GetBalance())
            {
                textBox.Text = "0.00";
            }
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            this.api.StartGame();
        }
    }
}
