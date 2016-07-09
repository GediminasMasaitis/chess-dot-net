using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessDotNet.BoardVisualizer
{
    public partial class MainForm : Form
    {
        public MainForm(ulong bitBoard = 0)
        {
            InitializeComponent();
            DisplayBitBoard(bitBoard);
        }

        private void ShowBitboardButton_Click(object sender, EventArgs e)
        {
            ulong bitboard;
            if (!ulong.TryParse(BitboardTextBox.Text, out bitboard))
            {
                MessageBox.Show("Invalid bitboard");
                return;
            }
            DisplayBitBoard(bitboard);
        }

        private IEnumerable<bool> BitboardToCells(ulong bitboard)
        {
            for (var i = 0; i < 64; i++)
            {
                var cellExists = (bitboard & (1UL << i)) != 0;
                yield return cellExists;
            }
        }

        private void DisplayBitBoard(ulong bitboard)
        {
            var cells = BitboardToCells(bitboard).ToList();

            var bmp = new Bitmap(MainPictureBox.Width, MainPictureBox.Height);
            var emptyBrush = new SolidBrush(Color.FromArgb(50,10,10));
            var filledBrush = new SolidBrush(Color.FromArgb(10, 80, 10));
            var textBrush = new SolidBrush(Color.FromArgb(255,255,255));
            var cellWidth = bmp.Width/8;
            var cellHeight = bmp.Height/8;
            using (var graphics = Graphics.FromImage(bmp))
            {
                for (var i = 0; i < cells.Count; i++)
                {
                    var cellX = 7-(i/8);
                    var cellY = i%8;

                    var brush = cells[i] ? filledBrush : emptyBrush;
                    graphics.FillRectangle(brush, cellY * cellWidth, cellX * cellHeight, cellWidth, cellHeight);

                    var text = (char)(65+(cellY)) + (8-cellX).ToString();
                    graphics.DrawString(text, Font, textBrush, cellY * cellWidth + 28, cellX * cellHeight + 18);
                    graphics.DrawString(i.ToString(), Font, textBrush, cellY * cellWidth + 28, cellX * cellHeight + 32);
                }

                for (var i = 0; i < 8; i++)
                {
                    graphics.DrawLine(Pens.Black, cellWidth*i, 0, cellWidth*i, bmp.Height);
                    graphics.DrawLine(Pens.Black, 0, cellHeight*i, bmp.Width, cellHeight*i);
                }
            }

            MainPictureBox.Image = bmp;
            MainPictureBox.Invalidate();
        }
    }
}
