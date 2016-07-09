namespace ChessDotNet.BoardVisualizer
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.MainPictureBox = new System.Windows.Forms.PictureBox();
            this.Bitboard1TextBox = new System.Windows.Forms.TextBox();
            this.Bitboard1Label = new System.Windows.Forms.Label();
            this.ShowBitboardButton = new System.Windows.Forms.Button();
            this.Bitboard2Label = new System.Windows.Forms.Label();
            this.Bitboard2TextBox = new System.Windows.Forms.TextBox();
            this.Bitboard3Label = new System.Windows.Forms.Label();
            this.Bitboard3TextBox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // MainPictureBox
            // 
            this.MainPictureBox.Location = new System.Drawing.Point(0, 0);
            this.MainPictureBox.Name = "MainPictureBox";
            this.MainPictureBox.Size = new System.Drawing.Size(512, 512);
            this.MainPictureBox.TabIndex = 0;
            this.MainPictureBox.TabStop = false;
            // 
            // Bitboard1TextBox
            // 
            this.Bitboard1TextBox.Location = new System.Drawing.Point(76, 518);
            this.Bitboard1TextBox.Name = "Bitboard1TextBox";
            this.Bitboard1TextBox.Size = new System.Drawing.Size(274, 20);
            this.Bitboard1TextBox.TabIndex = 1;
            this.Bitboard1TextBox.Text = "0";
            // 
            // Bitboard1Label
            // 
            this.Bitboard1Label.AutoSize = true;
            this.Bitboard1Label.Location = new System.Drawing.Point(12, 521);
            this.Bitboard1Label.Name = "Bitboard1Label";
            this.Bitboard1Label.Size = new System.Drawing.Size(58, 13);
            this.Bitboard1Label.TabIndex = 2;
            this.Bitboard1Label.Text = "Bitboard 1:";
            // 
            // ShowBitboardButton
            // 
            this.ShowBitboardButton.Location = new System.Drawing.Point(356, 516);
            this.ShowBitboardButton.Name = "ShowBitboardButton";
            this.ShowBitboardButton.Size = new System.Drawing.Size(151, 23);
            this.ShowBitboardButton.TabIndex = 3;
            this.ShowBitboardButton.Text = "Show";
            this.ShowBitboardButton.UseVisualStyleBackColor = true;
            this.ShowBitboardButton.Click += new System.EventHandler(this.ShowBitboardButton_Click);
            // 
            // Bitboard2Label
            // 
            this.Bitboard2Label.AutoSize = true;
            this.Bitboard2Label.Location = new System.Drawing.Point(12, 547);
            this.Bitboard2Label.Name = "Bitboard2Label";
            this.Bitboard2Label.Size = new System.Drawing.Size(58, 13);
            this.Bitboard2Label.TabIndex = 5;
            this.Bitboard2Label.Text = "Bitboard 2:";
            // 
            // Bitboard2TextBox
            // 
            this.Bitboard2TextBox.Location = new System.Drawing.Point(76, 544);
            this.Bitboard2TextBox.Name = "Bitboard2TextBox";
            this.Bitboard2TextBox.Size = new System.Drawing.Size(274, 20);
            this.Bitboard2TextBox.TabIndex = 4;
            this.Bitboard2TextBox.Text = "0";
            // 
            // Bitboard3Label
            // 
            this.Bitboard3Label.AutoSize = true;
            this.Bitboard3Label.Location = new System.Drawing.Point(12, 573);
            this.Bitboard3Label.Name = "Bitboard3Label";
            this.Bitboard3Label.Size = new System.Drawing.Size(58, 13);
            this.Bitboard3Label.TabIndex = 7;
            this.Bitboard3Label.Text = "Bitboard 3:";
            // 
            // Bitboard3TextBox
            // 
            this.Bitboard3TextBox.Location = new System.Drawing.Point(76, 570);
            this.Bitboard3TextBox.Name = "Bitboard3TextBox";
            this.Bitboard3TextBox.Size = new System.Drawing.Size(274, 20);
            this.Bitboard3TextBox.TabIndex = 6;
            this.Bitboard3TextBox.Text = "0";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(512, 600);
            this.Controls.Add(this.Bitboard3Label);
            this.Controls.Add(this.Bitboard3TextBox);
            this.Controls.Add(this.Bitboard2Label);
            this.Controls.Add(this.Bitboard2TextBox);
            this.Controls.Add(this.ShowBitboardButton);
            this.Controls.Add(this.Bitboard1Label);
            this.Controls.Add(this.Bitboard1TextBox);
            this.Controls.Add(this.MainPictureBox);
            this.Name = "MainForm";
            this.Text = "Bitboard viewer";
            ((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox MainPictureBox;
        private System.Windows.Forms.TextBox Bitboard1TextBox;
        private System.Windows.Forms.Label Bitboard1Label;
        private System.Windows.Forms.Button ShowBitboardButton;
        private System.Windows.Forms.Label Bitboard2Label;
        private System.Windows.Forms.TextBox Bitboard2TextBox;
        private System.Windows.Forms.Label Bitboard3Label;
        private System.Windows.Forms.TextBox Bitboard3TextBox;
    }
}

