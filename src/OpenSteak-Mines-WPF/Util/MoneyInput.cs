//
// Author: @p4pper
//
// Sanitizes numerical inputs for Textbox WPF Component.
//
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenSteak_Mines_WPF.Util
{
    public class MoneyInput
    {
        public static void AttachInput(
            TextBox textbox,
            Func<decimal> getBalance,
            Action<string> showError = null,
            Button actionButton = null
        )
        {
            textbox.PreviewTextInput += TextboxOnPreviewTextInput; 
            textbox.TextChanged += TextboxOnTextChanged;
            DataObject.AddPastingHandler(textbox, Textbox_OnPaste);
            textbox.LostFocus += TextboxOnLostFocus;
        }

        private static void TextboxOnLostFocus(object sender, RoutedEventArgs e)
        {
           
        }

        private static void Textbox_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var pastedText = e.DataObject.GetData(DataFormats.Text) as string;
            
        }

        private static void TextboxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private static void TextboxOnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is TextBox tb)) return;

            // 1) Culture-specific decimal separator
            string sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            // 2) Validate every char in the input chunk (IME can send multiple)
            bool allValidChars = e.Text.All(ch => char.IsDigit(ch) || ch.ToString() == sep);
            if (!allValidChars)
            {
                e.Handled = true; // Block non-digit/non-separator chars
                return;
            }

            // 3) Simulate the pending text after insertion (respect selection)
            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;
            string current = tb.Text ?? string.Empty;
            string pending = current.Remove(selStart, selLen).Insert(selStart, e.Text);

            // 4) Ensure we don’t end up with more than one separator
            int sepCount = 0;
            for (int i = 0; i < pending.Length; i++)
            {
                // Compare as string because separator may be multi-char in some locales
                if (sep.Length == 1)
                {
                    if (pending[i].ToString() == sep) sepCount++;
                }
                else
                {
                    // Rare, but safe: handle multi-char separator
                    if (i + sep.Length <= pending.Length && pending.Substring(i, sep.Length) == sep)
                    {
                        sepCount++;
                        i += sep.Length - 1; // skip ahead
                    }
                }

                if (sepCount > 1)
                {
                    e.Handled = true; // would produce a second separator
                    return;
                }
            }

            // 5) Optional micro-rule: prevent leading separator if you don’t want “.5” while typing
            // If you DO allow “.5”, comment this out.
            // if (string.IsNullOrEmpty(current) && e.Text == sep)
            // {
            //     e.Handled = true; // force user to type leading 0
            // }
        }
    }
    
}