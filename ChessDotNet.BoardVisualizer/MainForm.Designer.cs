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
            this.BitboardTextBox = new System.Windows.Forms.TextBox();
            this.BitboardLabel = new System.Windows.Forms.Label();
            this.ShowBitboardButton = new System.Windows.Forms.Button();
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
            // BitboardTextBox
            // 
            this.BitboardTextBox.Location = new System.Drawing.Point(67, 518);
            this.BitboardTextBox.Name = "BitboardTextBox";
            this.BitboardTextBox.Size = new System.Drawing.Size(283, 20);
            this.BitboardTextBox.TabIndex = 1;
            // 
            // BitboardLabel
            // 
            this.BitboardLabel.AutoSize = true;
            this.BitboardLabel.Location = new System.Drawing.Point(12, 521);
            this.BitboardLabel.Name = "BitboardLabel";
            this.BitboardLabel.Size = new System.Drawing.Size(49, 13);
            this.BitboardLabel.TabIndex = 2;
            this.BitboardLabel.Text = "Bitboard:";
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
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(512, 550);
            this.Controls.Add(this.ShowBitboardButton);
            this.Controls.Add(this.BitboardLabel);
            this.Controls.Add(this.BitboardTextBox);
            this.Controls.Add(this.MainPictureBox);
            this.Name = "MainForm";
            this.Text = "Bitboard viewer";
            ((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox MainPictureBox;
        private System.Windows.Forms.TextBox BitboardTextBox;
        private System.Windows.Forms.Label BitboardLabel;
        private System.Windows.Forms.Button ShowBitboardButton;
    }
}

