namespace ServerTest
{
    partial class StreamForm
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
            this.components = new System.ComponentModel.Container();
            this.StreamBox = new System.Windows.Forms.PictureBox();
            this.FpsCounter = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.StreamBox)).BeginInit();
            this.SuspendLayout();
            // 
            // StreamBox
            // 
            this.StreamBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StreamBox.Location = new System.Drawing.Point(0, 0);
            this.StreamBox.Name = "StreamBox";
            this.StreamBox.Size = new System.Drawing.Size(800, 450);
            this.StreamBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.StreamBox.TabIndex = 0;
            this.StreamBox.TabStop = false;
            // 
            // FpsCounter
            // 
            this.FpsCounter.Interval = 1000;
            this.FpsCounter.Tick += new System.EventHandler(this.FpsCounter_Tick);
            // 
            // StreamForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.StreamBox);
            this.Name = "StreamForm";
            this.Text = "StreamForm";
            this.Load += new System.EventHandler(this.StreamForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.StreamBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox StreamBox;
        private System.Windows.Forms.Timer FpsCounter;
    }
}