using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;
using OpenSteak_Mines_WPF.Util;

namespace OpenSteak_Mines_WPF
{
    using System.Threading.Tasks;
    using System.Windows;
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MinesWPF _gui;

        public MainWindow()
        {
            InitializeComponent();
            _gui = new MinesWPF(
                MinesGrid,
                this,
                CashOutOrStartButton,
                mineCombo,
                playerBalText,
                payoutMultiplierLbl,
                betAmountTxt);

            betAmountTxt.TextChanged += BetAmountTxt_TextChanged;
            
            // Hide Error Message
            errorMsgLabel.Visibility = Visibility.Hidden;
        }
        
        // Error Handling
        private readonly object _errorLock = new object();
        private bool _isErrorMessageShowing = false;

      
        private void BetAmountTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox betAmountTextBox))
                return;
            
            betAmountTextBox.TextChanged -= BetAmountTxt_TextChanged;

            string text = betAmountTextBox.Text.Replace(".", "").Replace(",", ""); // Make the textbox into a pure number
            bool isMessageError = false;
            
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number))
            {
                // Scale down to 2 decimal places for display
                number /= 100;
                string formattedText = number.ToString("0.00", CultureInfo.InvariantCulture);

                // Update text only if changed
                if (betAmountTextBox.Text != formattedText)
                {
                    betAmountTextBox.Text = formattedText;
                }
            }
            else
            {
                betAmountTextBox.Text = "0.00";
                isMessageError = true;
            }

            // Maintain cursor position
            betAmountTextBox.SelectionStart = betAmountTextBox.Text.Length;

            // Balance validation
            if (decimal.TryParse(betAmountTextBox.Text, out decimal bet) && bet > _gui.GetBalance())
            {
                betAmountTextBox.Text = "0.00";
                isMessageError = true;
            }

            if (isMessageError)
            {
                ShowErrorForFiveSeconds("Bet amount is invalid.");
            }

            // Reattach event
            betAmountTextBox.TextChanged += BetAmountTxt_TextChanged;
        }

        private void ShowErrorForFiveSeconds(string message)
        {
            lock (_errorLock)
            {
                if (_isErrorMessageShowing)
                {
                    return; // Avoid showing multiple errors simultaneously
                }
                _isErrorMessageShowing = true;
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

                lock (_errorLock)
                {
                    _isErrorMessageShowing = false;
                }
            });
        }
        private void CashOutOrStartButton_Click(object sender, RoutedEventArgs e)
        {
            this._gui.StartOrCashout();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            
        }
    }
}
