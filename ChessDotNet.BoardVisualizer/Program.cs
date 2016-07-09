using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessDotNet.BoardVisualizer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            ulong bitboard;
            if (args.Length == 0)
            {
                bitboard = 0;
            }
            else if (!ulong.TryParse(args[0], out bitboard))
            {
                MessageBox.Show("Invalid bitboard");
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(bitboard));
        }
    }
}
