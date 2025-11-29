using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSteak_Mines_WPF.Util
{
    public static class BalanceManager
    {
        /*
         * 
         * Currently the balance system is saved in a text file within the program's directory
         * In the future, a database system could be developed, in that case then protection mechanisms
         * Have to be implemented to prevent balance tampering.
         * 
         * Fun fact, decimals cannot be null, they start at 0.0m         */
        private static decimal balance;
        private const decimal DefaultBalance = 5.0m;

        private static void RoundBalance()
        {
            balance = Math.Round(balance, 2);
        }

        public static void InitializeBalance()
        {
            if (File.Exists("balance.txt"))
            {
                decimal.TryParse(File.ReadAllText("balance.txt"), out balance);
            }
            else
            {
                File.WriteAllText("balance.txt", DefaultBalance.ToString(CultureInfo.CurrentCulture));
                balance = DefaultBalance;
            }
        }

        public static decimal GetBalanceFormatted()
        {
            RoundBalance();
            return balance;
        }

        public static void AddToBalance(decimal amount)
        {
            RoundBalance();
            balance = balance + amount;
        }
        public static void RemoveFromBalance(decimal amount)
        {
            RoundBalance();
            balance = balance - amount;
        }

        public static void UpdateBalanceToDatabase()
        {
            File.WriteAllText("balance.txt", balance.ToString(CultureInfo.CurrentCulture));
        }


    }
}
