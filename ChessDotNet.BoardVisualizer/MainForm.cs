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
        private IList<TextBox> BitboardsTextBoxes { get; }
        private IList<SolidBrush> Brushes { get;}
        private SolidBrush EmptyBrush { get; }
        private int BitBoardCount { get; }

        public MainForm() : this(new ulong[0])
        {
        }

        public MainForm(ulong[] bitboards)
        {
            InitializeComponent();
            var allBitboards = bitboards.ToList();

            BitBoardCount = 13;
            BitboardsTextBoxes = new TextBox[BitBoardCount];
            BitboardsTextBoxes[0] = Bitboard0TextBox;

            var colors = new List<Color>
            {
                Color.FromArgb(100, 0, 0),
                Color.FromArgb(0, 100, 0),
                Color.FromArgb(40, 70, 170),
                Color.FromArgb(100, 100, 0),
                Color.FromArgb(100, 0, 100),
                Color.FromArgb(0, 100, 100),
                Color.FromArgb(170, 70, 0),
                Color.FromArgb(0, 170, 100),
                Color.FromArgb(70, 30, 0),
                Color.FromArgb(180, 0, 100),
                Color.FromArgb(180, 150, 50),
                Color.FromArgb(120, 120, 120),
                Color.FromArgb(170, 170, 170),
            };

            var rng = new Random(0);
            while (colors.Count < BitBoardCount)
            {
                var red = rng.Next(0, 256);
                var green = rng.Next(0, 256);
                var blue = rng.Next(0, 256);
                var color = Color.FromArgb(red, green, blue);
                colors.Add(color);
            }

            Brushes = colors.Select(x => new SolidBrush(x)).ToList();
            EmptyBrush = new SolidBrush(Color.FromArgb(80,80,80));


            var offset = 30;

            for (var i = 1; i < BitBoardCount; i++)
            {
                var newTextBox = new TextBox();
                newTextBox.Parent = this;
                newTextBox.Location = new Point(Bitboard0TextBox.Location.X, Bitboard0TextBox.Location.Y + offset * i);
                newTextBox.Size = Bitboard0TextBox.Size;

                var newLabel = new Label();
                newLabel.Parent = this;
                newLabel.Location = new Point(Bitboard0Label.Location.X, Bitboard0Label.Location.Y + offset*i);
                newLabel.Text = $"Bitboard {i}:";

                BitboardsTextBoxes[i] = newTextBox;
            }

            for (var i = 0; i < BitboardsTextBoxes.Count; i++)
            {
                if (allBitboards.Count <= i)
                {
                    allBitboards.Add(0UL);
                }
                //BitboardsTextBoxes[i].BackColor = EmptyBrush.Color;
                BitboardsTextBoxes[i].ForeColor = Brushes[i].Color;
                BitboardsTextBoxes[i].Font = new Font(FontFamily.GenericMonospace, 10, FontStyle.Bold);
            }

            for (var i = 0; i < allBitboards.Count; i++)
            {
                if (i >= BitboardsTextBoxes.Count)
                {
                    break;
                }
                BitboardsTextBoxes[i].Text = allBitboards[i].ToString();
            }

            DisplayBitBoards(allBitboards.ToArray());
        }

        private void ShowBitboardButton_Click(object sender, EventArgs e)
        {
            var bitboards = new ulong[BitboardsTextBoxes.Count];
            for (var i = 0; i < BitboardsTextBoxes.Count; i++)
            {
                if (!ulong.TryParse(BitboardsTextBoxes[i].Text, out bitboards[i]))
                {
                    MessageBox.Show($"Invalid bitboard {i}");
                    return;
                }
            }

            DisplayBitBoards(bitboards);
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

            var textBrush = new SolidBrush(Color.FromArgb(255,255,255));
            var cellWidth = bmp.Width/8;
            var cellHeight = bmp.Height/8;

            var font = new Font(Font, FontStyle.Bold);

            var borderIncrement = (cellWidth/2-5)/BitboardsTextBoxes.Count;

            using (var graphics = Graphics.FromImage(bmp))
            {
                for (var i = 0; i < 64; i++)
                {
                    var cellX = 7-(i/8);
                    var cellY = i%8;

                    graphics.FillRectangle(EmptyBrush, cellY * cellWidth, cellX * cellHeight, cellWidth, cellHeight);

                    var borderWidth = 0;
                    for (var j = 0; j < cells.Count; j++)
                    {
                        if (cells[j][i])
                        {
                            graphics.FillRectangle(Brushes[j], cellY * cellWidth + borderWidth + 1, cellX * cellHeight + borderWidth + 1, cellWidth - 2*borderWidth - 1, cellHeight - 2*borderWidth - 1);
                            borderWidth += borderIncrement;
                        }
                    }

                    var text = (char)(65+(cellY)) + (8-cellX).ToString();
                    graphics.DrawString(text, font, textBrush, cellY * cellWidth + cellWidth/2 - 8, cellX * cellHeight + cellHeight/2 - 10);
                    graphics.DrawString(i.ToString().PadLeft(2, '0'), font, textBrush, cellY * cellWidth + cellWidth / 2 - 8, cellX * cellHeight + cellHeight/2 + 2);
                }

                for (var i = 0; i < 9; i++)
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
