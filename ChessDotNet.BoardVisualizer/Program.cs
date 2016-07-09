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
            var bitboards = new ulong[args.Length];
            for (var i = 0; i < args.Length; i++)
            {
                if (!ulong.TryParse(args[i], out bitboards[i]))
                {
                    MessageBox.Show("Invalid bitboard");
                    return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(bitboards));
        }
    }
}
