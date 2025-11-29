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
            Action<string> showError = null
        )
        {
            textbox.Tag = new MoneyInputContext(getBalance, showError);

            textbox.PreviewTextInput += TextboxOnPreviewTextInput; 
            textbox.TextChanged += TextboxOnTextChanged;
            DataObject.AddPastingHandler(textbox, Textbox_OnPaste);
            textbox.LostFocus += TextboxOnLostFocus;
        }
        
        private sealed class MoneyInputContext
        {
            public Func<decimal> GetBalance { get; }
            public Action<string> ShowError { get; }
            public MoneyInputContext(Func<decimal> getBalance, Action<string> showError)
            {
                GetBalance = getBalance;
                ShowError = showError;
            }
        }

        private static void TextboxOnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox tb)) return;
            var ctx = tb.Tag as MoneyInputContext;

            var culture = CultureInfo.CurrentCulture;
            string dec = culture.NumberFormat.NumberDecimalSeparator;
            string text = tb.Text ?? string.Empty;

            // Normalize: keep digits and one decimal separator
            text = KeepDigitsAndOneDecimal(text, dec);

            if (!decimal.TryParse(text, NumberStyles.Number, culture, out var value))
            {
                value = 0m;
            }

            // Optional: clamp against provided balance
            if (ctx?.GetBalance != null)
            {
                var bal = ctx.GetBalance();
                if (value > bal)
                {
                    value = 0m;
                    ctx.ShowError?.Invoke("Bet amount is invalid.");
                }
            }

            // Format to two decimals in current culture
            tb.Text = value.ToString("N2", culture);
            tb.CaretIndex = tb.Text.Length;
        }

        private static void Textbox_OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!(sender is TextBox tb)) return;
            
            // Handle textual pastes
            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text))
                return;
            
            var pastedText = (e.SourceDataObject.GetData(DataFormats.Text) as string)
                ?? (e.SourceDataObject.GetData(DataFormats.Text) as string)
                ?? string.Empty;
            
            var nfi = CultureInfo.CurrentCulture.NumberFormat;
            string dec = nfi.NumberDecimalSeparator;

            // Normalize NBSP and trim
            pastedText = pastedText.Replace('\u00A0', ' ').Trim();

            // Candidate decimal/grouping characters across locales
            char[] seps = { '.', ',', '\'', ' ', '\u00A0', '’', '٬', '٫' };

            // Determine rightmost separator → treat as decimal, others as grouping
            int lastIdx = -1;
            for (int i = 0; i < pastedText.Length; i++)
                if (seps.Contains(pastedText[i])) lastIdx = i;

            // Build sanitized: keep digits, and map the rightmost sep to current culture decimal
            var sb = new System.Text.StringBuilder(pastedText.Length + 2);
            for (int i = 0; i < pastedText.Length; i++)
            {
                char ch = pastedText[i];
                if (char.IsDigit(ch)) { sb.Append(ch); continue; }
                if (i == lastIdx) sb.Append(dec); // decimal
                // else drop grouping
            }

            string sanitized = sb.ToString();
            if (sanitized.StartsWith(dec)) sanitized = "0" + sanitized; // avoid leading separator

            if (string.IsNullOrEmpty(sanitized)) { e.CancelCommand(); return; }

            // Simulate insertion and validate with current culture
            int selStart = tb.SelectionStart;
            int selLen   = tb.SelectionLength;
            string pending = (tb.Text ?? string.Empty).Remove(selStart, selLen).Insert(selStart, sanitized);

            if (!decimal.TryParse(pending, NumberStyles.Number, CultureInfo.CurrentCulture, out _))
            {
                e.CancelCommand();
                return;
            }

            // Commit and cancel default paste to keep control
            tb.Text = pending;
            tb.SelectionStart = selStart + sanitized.Length;
            tb.SelectionLength = 0;
            e.CancelCommand();
        }

        private static void TextboxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox tb)) return;

            var culture = CultureInfo.CurrentCulture;
            string dec = culture.NumberFormat.NumberDecimalSeparator;

            // Allow only digits and a single decimal separator while typing.
            // Keep caret friendly: only rewrite if something illegal slipped in.
            string before = tb.Text ?? string.Empty;
            string after = KeepDigitsAndOneDecimal(before, dec);

            // Optional: constrain to two fractional digits while typing (can be relaxed if desired)
            int di = after.IndexOf(dec, StringComparison.Ordinal);
            if (di >= 0 && di + dec.Length < after.Length)
            {
                int fracLen = after.Length - (di + dec.Length);
                if (fracLen > 2)
                {
                    after = after.Substring(0, di + dec.Length + 2);
                }
            }

            if (!ReferenceEquals(before, after) && before != after)
            {
                int caret = tb.CaretIndex;
                tb.TextChanged -= TextboxOnTextChanged;
                tb.Text = after;
                // Place caret near the end to avoid complex offset math for multi-char dec separators
                tb.CaretIndex = Math.Min(after.Length, caret);
                tb.TextChanged += TextboxOnTextChanged;
            }
        }
        private static string KeepDigitsAndOneDecimal(string s, string dec)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            var sb = new System.Text.StringBuilder(s.Length);
            bool hadDec = false;

            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (char.IsDigit(ch))
                {
                    sb.Append(ch);
                }
                else
                {
                    if (!hadDec && MatchesAt(s, i, dec))
                    {
                        sb.Append(dec);
                        hadDec = true;
                        i += dec.Length - 1; // skip the rest of the decimal token
                    }
                    // ignore all other characters
                }
            }

            return sb.ToString();
        }
        private static bool MatchesAt(string s, int index, string token)
        {
            if (index < 0 || index + token.Length > s.Length) return false;
            for (int i = 0; i < token.Length; i++)
            {
                if (s[index + i] != token[i]) return false;
            }
            return true;
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
            string current = tb.Text;
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