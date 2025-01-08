

namespace OpenSteak_Mines_WPF
{
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
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

            // Hide Error Message
            errorMsgLabel.Visibility = Visibility.Hidden;
        }

        private void t1_Click(object sender, RoutedEventArgs e)
        {
            this.api.StartGame();

           
        }

        private bool isMessageError = false;
        private readonly object errorLock = new object();
        private bool isErrorShowing = false;

        private void BetAmountTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            // Detach event to avoid recursive calls
            textBox.TextChanged -= BetAmountTxt_TextChanged;

            string text = textBox.Text.Replace(".", "").Replace(",", ""); // Remove any existing formatting

            bool isMessageError = false;

            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number))
            {
                // Scale down to 2 decimal places for display
                number /= 100;
                string formattedText = number.ToString("0.00", CultureInfo.InvariantCulture);

                // Update text only if changed
                if (textBox.Text != formattedText)
                {
                    textBox.Text = formattedText;
                }
            }
            else
            {
                textBox.Text = "0.00";
                isMessageError = true;
            }

            // Maintain cursor position
            textBox.SelectionStart = textBox.Text.Length;

            // Balance validation
            if (double.TryParse(textBox.Text, out double bet) && bet > this.api.GetBalance())
            {
                textBox.Text = "0.00";
                isMessageError = true;
            }

            if (isMessageError)
            {
                ShowErrorForFiveSeconds("Bet amount is invalid.");
            }

            // Reattach event
            textBox.TextChanged += BetAmountTxt_TextChanged;
        }

        private void ShowErrorForFiveSeconds(string message)
        {
            lock (errorLock)
            {
                if (isErrorShowing)
                {
                    return; // Avoid showing multiple errors simultaneously
                }
                isErrorShowing = true;
            }

            // Display the error message
            Application.Current.Dispatcher.Invoke(() =>
            {
                errorMsgLabel.Visibility = Visibility.Visible;
                errorMsgLabel.Content = message;
            });

            // Hide the message after 5 seconds
            Task.Run(async () =>
            {
                await Task.Delay(2000);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    errorMsgLabel.Visibility = Visibility.Collapsed;
                });

                lock (errorLock)
                {
                    isErrorShowing = false;
                }
            });
        }



        private void Window_Closed(object sender, System.EventArgs e)
        {
            this.api.StartGame();
        }
    }
}
