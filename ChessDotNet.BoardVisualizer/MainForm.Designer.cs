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
            this.ShowBitboardButton = new System.Windows.Forms.Button();
            this.Bitboard0Label = new System.Windows.Forms.Label();
            this.Bitboard0TextBox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // MainPictureBox
            // 
            this.MainPictureBox.Location = new System.Drawing.Point(0, 0);
            this.MainPictureBox.Name = "MainPictureBox";
            this.MainPictureBox.Size = new System.Drawing.Size(513, 513);
            this.MainPictureBox.TabIndex = 0;
            this.MainPictureBox.TabStop = false;
            // 
            // ShowBitboardButton
            // 
            this.ShowBitboardButton.Location = new System.Drawing.Point(528, 478);
            this.ShowBitboardButton.Name = "ShowBitboardButton";
            this.ShowBitboardButton.Size = new System.Drawing.Size(236, 23);
            this.ShowBitboardButton.TabIndex = 3;
            this.ShowBitboardButton.Text = "Show";
            this.ShowBitboardButton.UseVisualStyleBackColor = true;
            this.ShowBitboardButton.Click += new System.EventHandler(this.ShowBitboardButton_Click);
            // 
            // Bitboard0Label
            // 
            this.Bitboard0Label.AutoSize = true;
            this.Bitboard0Label.Location = new System.Drawing.Point(525, 13);
            this.Bitboard0Label.Name = "Bitboard0Label";
            this.Bitboard0Label.Size = new System.Drawing.Size(58, 13);
            this.Bitboard0Label.TabIndex = 23;
            this.Bitboard0Label.Text = "Bitboard 0:";
            // 
            // Bitboard0TextBox
            // 
            this.Bitboard0TextBox.Location = new System.Drawing.Point(589, 10);
            this.Bitboard0TextBox.Name = "Bitboard0TextBox";
            this.Bitboard0TextBox.Size = new System.Drawing.Size(175, 20);
            this.Bitboard0TextBox.TabIndex = 22;
            this.Bitboard0TextBox.Text = "0";
            // 
            // MainForm
            // 
            this.AcceptButton = this.ShowBitboardButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(775, 513);
            this.Controls.Add(this.Bitboard0Label);
            this.Controls.Add(this.Bitboard0TextBox);
            this.Controls.Add(this.ShowBitboardButton);
            this.Controls.Add(this.MainPictureBox);
            this.Name = "MainForm";
            this.Text = "Bitboard viewer";
            ((System.ComponentModel.ISupportInitialize)(this.MainPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox MainPictureBox;
        private System.Windows.Forms.Button ShowBitboardButton;
        private System.Windows.Forms.Label Bitboard0Label;
        private System.Windows.Forms.TextBox Bitboard0TextBox;
    }
}

