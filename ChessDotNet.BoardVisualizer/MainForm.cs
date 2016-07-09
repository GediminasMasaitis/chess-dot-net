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
        public MainForm() : this(new ulong[0])
        {
        }

        public MainForm(ulong[] bitboards)
        {
            InitializeComponent();
            var allBitboards = bitboards.ToList();
            while (allBitboards.Count < 3)
            {
                allBitboards.Add(0UL);
            }
            Bitboard1TextBox.Text = allBitboards[0].ToString();
            Bitboard2TextBox.Text = allBitboards[1].ToString();
            Bitboard3TextBox.Text = allBitboards[2].ToString();
            DisplayBitBoards(allBitboards.ToArray());
        }

        private void ShowBitboardButton_Click(object sender, EventArgs e)
        {
            ulong bitboard1;
            if (!ulong.TryParse(Bitboard1TextBox.Text, out bitboard1))
            {
                MessageBox.Show("Invalid bitboard 1");
                return;
            }

            ulong bitboard2;
            if (!ulong.TryParse(Bitboard2TextBox.Text, out bitboard2))
            {
                MessageBox.Show("Invalid bitboard 2");
                return;
            }

            ulong bitboard3;
            if (!ulong.TryParse(Bitboard3TextBox.Text, out bitboard3))
            {
                MessageBox.Show("Invalid bitboard 3");
                return;
            }
            DisplayBitBoards(bitboard1, bitboard2, bitboard3);
        }

        private IEnumerable<bool> BitboardToCells(ulong bitboard)
        {
            for (var i = 0; i < 64; i++)
            {
                var cellExists = (bitboard & (1UL << i)) != 0;
                yield return cellExists;
            }
        }

        private void DisplayBitBoards(params ulong[] bitboards)
        {
            var cells = bitboards.Select(x => BitboardToCells(x).ToList()).ToList();

            var bmp = new Bitmap(MainPictureBox.Width, MainPictureBox.Height);
            var emptyBrush = new SolidBrush(Color.FromArgb(50,50,50));
            var filledBrush1 = new SolidBrush(Color.FromArgb(10, 100, 10));
            var filledBrush2 = new SolidBrush(Color.FromArgb(120, 100, 00));
            var filledBrush3 = new SolidBrush(Color.FromArgb(50, 70, 150));
            var textBrush = new SolidBrush(Color.FromArgb(255,255,255));
            var cellWidth = bmp.Width/8;
            var cellHeight = bmp.Height/8;
            using (var graphics = Graphics.FromImage(bmp))
            {
                for (var i = 0; i < 64; i++)
                {
                    var cellX = 7-(i/8);
                    var cellY = i%8;

                    var brush = emptyBrush;
                    if (cells[0][i])
                    {
                        brush = filledBrush1;
                    }
                    if (cells[1][i])
                    {
                        brush = filledBrush2;
                    }
                    if (cells[2][i])
                    {
                        brush = filledBrush3;
                    }
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
