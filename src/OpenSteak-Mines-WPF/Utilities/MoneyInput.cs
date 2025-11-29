using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace OpenSteak_Mines_WPF.Utilities
{
    /// <summary>
    /// Reusable helper that wires culture-aware money input behavior to a TextBox.
    /// - Filters typing and paste to plausible numeric content.
    /// - Validates on TextChanged with custom rules (non-negative, max 2 decimals, <= balance).
    /// - Formats to 0.00 on LostFocus.
    /// - Optionally toggles an action button enabled state.
    /// - Surfaces errors via a provided callback (e.g., a transient banner in your window).
    /// </summary>
    public static class MoneyInput
    {
        /// <summary>
        /// Attach money-input behavior to a TextBox.
        /// </summary>
        /// <param name="textBox">Target TextBox (e.g., bet amount box).</param>
        /// <param name="getBalance">Delegate returning current balance as decimal.</param>
        /// <param name="showError">Callback to show a user-friendly error (e.g., banner). Optional, can be null.</param>
        /// <param name="actionButton">Optional button (e.g., Start/Cashout) to enable/disable based on validity.</param>
        public static void Attach(TextBox textBox, Func<decimal> getBalance, Action<string> showError = null, Button actionButton = null)
        {
            if (textBox == null) throw new ArgumentNullException(nameof(textBox));
            if (getBalance == null) throw new ArgumentNullException(nameof(getBalance));

            // Remove duplicates if Attach is called more than once
            DataObject.RemovePastingHandler(textBox, OnPaste);
            textBox.PreviewTextInput -= OnPreviewTextInput;
            textBox.TextChanged -= OnTextChanged;
            textBox.LostFocus -= OnLostFocus;

            // Store delegates on the TextBox via Tag or attached property; use Tag tuple to avoid extra class
            textBox.Tag = new BehaviorState(getBalance, showError, actionButton);

            DataObject.AddPastingHandler(textBox, OnPaste);
            textBox.PreviewTextInput += OnPreviewTextInput;
            textBox.TextChanged += OnTextChanged;
            textBox.LostFocus += OnLostFocus;

            // Initial formatting/validation pass
            Validate(textBox);
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb) return;
            var culture = CultureInfo.CurrentCulture;
            string sep = culture.NumberFormat.NumberDecimalSeparator;

            // Only allow digits and at most one separator
            bool validChars = e.Text.All(ch => char.IsDigit(ch) || ch.ToString() == sep);
            if (!validChars)
            {
                e.Handled = true;
                return;
            }

            // Simulate pending text post-insert to check separator count
            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;
            string pending = tb.Text.Remove(selStart, selLen).Insert(selStart, e.Text);
            if (e.Text == sep && pending.Count(c => c.ToString() == sep) > 1)
            {
                e.Handled = true;
            }
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string pasteText = (string)e.DataObject.GetData(DataFormats.Text);
            var culture = CultureInfo.CurrentCulture;

            // Quick gate: must parse as a number at all
            if (!decimal.TryParse(pasteText, NumberStyles.Number, culture, out _))
            {
                e.CancelCommand();

                if (tb.Tag is BehaviorState st && st.ShowError != null)
                {
                    st.ShowError("Invalid number");
                }
            }
        }

        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            Validate(tb, format: false); // validate while typing, but do not reformat
        }

        private static void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            Validate(tb, format: true); // final normalization to 0.00
        }

        private static void Validate(TextBox tb, bool format = false)
        {
            if (tb.Tag is not BehaviorState st) return;

            var culture = CultureInfo.CurrentCulture;
            string text = tb.Text;

            // Treat empty as 0 while typing
            if (string.IsNullOrWhiteSpace(text))
            {
                st.SetActionButtonEnabled(false);
                if (format)
                {
                    tb.Text = 0m.ToString("0.00", culture);
                    tb.SelectionStart = tb.Text.Length;
                }
                return;
            }

            if (!decimal.TryParse(text, NumberStyles.Number, culture, out var bet))
            {
                st.ShowError?.Invoke("Enter a valid number");
                st.SetActionButtonEnabled(false);
                return;
            }

            // Enforce max 2 decimals
            int[] bits = decimal.GetBits(bet);
            int scale = (bits[3] >> 16) & 0xFF;
            if (scale > 2)
            {
                st.ShowError?.Invoke("Max 2 decimal places");
                st.SetActionButtonEnabled(false);
                return;
            }

            if (bet < 0)
            {
                st.ShowError?.Invoke("Bet cannot be negative");
                st.SetActionButtonEnabled(false);
                return;
            }

            decimal balance = st.GetBalance();
            if (bet > balance)
            {
                st.ShowError?.Invoke("Insufficient balance");
                st.SetActionButtonEnabled(false);
                return;
            }

            // Valid input at this point
            st.SetActionButtonEnabled(bet > 0m);

            if (format)
            {
                string formatted = bet.ToString("0.00", culture);
                if (!string.Equals(tb.Text, formatted, StringComparison.Ordinal))
                {
                    tb.Text = formatted;
                    tb.SelectionStart = tb.Text.Length;
                }
            }
        }

        private sealed class BehaviorState
        {
            public BehaviorState(Func<decimal> getBalance, Action<string> showError, Button actionButton)
            {
                GetBalance = getBalance;
                ShowError = showError;
                ActionButton = actionButton;
            }

            public Func<decimal> GetBalance { get; }
            public Action<string> ShowError { get; }
            public Button ActionButton { get; }

            public void SetActionButtonEnabled(bool enabled)
            {
                if (ActionButton != null)
                {
                    ActionButton.IsEnabled = enabled;
                }
            }
        }
    }
}
