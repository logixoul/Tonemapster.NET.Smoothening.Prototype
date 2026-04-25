namespace Tonemapster.NET.Smoothening.Prototype
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private Label labelDetailBoost;
        private Panel panelControls;
        private Label labelSigmaColor;
        private Label labelSmoothing;
        private PictureBox pictureBoxPreview;
        private TrackBar trackBarDetailBoost;
        private TrackBar trackBarSigmaColor;
        private TrackBar trackBarSmoothing;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeLoadedImage();
                pictureBoxPreview.Image?.Dispose();
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panelControls = new Panel();
            trackBarSigmaColor = new TrackBar();
            labelSigmaColor = new Label();
            trackBarDetailBoost = new TrackBar();
            labelDetailBoost = new Label();
            trackBarSmoothing = new TrackBar();
            labelSmoothing = new Label();
            pictureBoxPreview = new PictureBox();
            panelControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarSigmaColor).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBarDetailBoost).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBarSmoothing).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).BeginInit();
            SuspendLayout();
            // 
            // panelControls
            // 
            panelControls.Controls.Add(trackBarSigmaColor);
            panelControls.Controls.Add(labelSigmaColor);
            panelControls.Controls.Add(trackBarDetailBoost);
            panelControls.Controls.Add(labelDetailBoost);
            panelControls.Controls.Add(trackBarSmoothing);
            panelControls.Controls.Add(labelSmoothing);
            panelControls.Dock = DockStyle.Bottom;
            panelControls.Location = new Point(0, 290);
            panelControls.Name = "panelControls";
            panelControls.Size = new Size(800, 160);
            panelControls.TabIndex = 0;
            // 
            // trackBarSigmaColor
            // 
            trackBarSigmaColor.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            trackBarSigmaColor.Location = new Point(117, 108);
            trackBarSigmaColor.Maximum = 300;
            trackBarSigmaColor.Name = "trackBarSigmaColor";
            trackBarSigmaColor.Size = new Size(671, 45);
            trackBarSigmaColor.TabIndex = 5;
            trackBarSigmaColor.TickFrequency = 30;
            trackBarSigmaColor.Value = 200;
            trackBarSigmaColor.Scroll += HandleSigmaColorScroll;
            // 
            // labelSigmaColor
            // 
            labelSigmaColor.AutoSize = true;
            labelSigmaColor.Location = new Point(12, 117);
            labelSigmaColor.Name = "labelSigmaColor";
            labelSigmaColor.Size = new Size(93, 15);
            labelSigmaColor.TabIndex = 4;
            labelSigmaColor.Text = "Sigma Color: 1.0";
            // 
            // trackBarDetailBoost
            // 
            trackBarDetailBoost.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            trackBarDetailBoost.Location = new Point(117, 60);
            trackBarDetailBoost.Maximum = 5000;
            trackBarDetailBoost.Name = "trackBarDetailBoost";
            trackBarDetailBoost.Size = new Size(671, 45);
            trackBarDetailBoost.TabIndex = 3;
            trackBarDetailBoost.TickFrequency = 50;
            trackBarDetailBoost.Value = 40;
            trackBarDetailBoost.Scroll += HandleDetailBoostScroll;
            // 
            // labelDetailBoost
            // 
            labelDetailBoost.AutoSize = true;
            labelDetailBoost.Location = new Point(12, 69);
            labelDetailBoost.Name = "labelDetailBoost";
            labelDetailBoost.Size = new Size(91, 15);
            labelDetailBoost.TabIndex = 2;
            labelDetailBoost.Text = "Detail Boost: 4.0";
            // 
            // trackBarSmoothing
            // 
            trackBarSmoothing.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            trackBarSmoothing.Location = new Point(102, 12);
            trackBarSmoothing.Maximum = 100;
            trackBarSmoothing.Name = "trackBarSmoothing";
            trackBarSmoothing.Size = new Size(686, 45);
            trackBarSmoothing.TabIndex = 1;
            trackBarSmoothing.TickFrequency = 10;
            trackBarSmoothing.Scroll += HandleSmoothingStrengthScroll;
            trackBarSmoothing.MouseCaptureChanged += HandleSmoothingStrengthMouseCaptureChanged;
            trackBarSmoothing.MouseUp += HandleSmoothingStrengthMouseUp;
            // 
            // labelSmoothing
            // 
            labelSmoothing.AutoSize = true;
            labelSmoothing.Location = new Point(12, 21);
            labelSmoothing.Name = "labelSmoothing";
            labelSmoothing.Size = new Size(78, 15);
            labelSmoothing.TabIndex = 0;
            labelSmoothing.Text = "Smoothing: 0";
            // 
            // pictureBoxPreview
            // 
            pictureBoxPreview.AllowDrop = true;
            pictureBoxPreview.Dock = DockStyle.Fill;
            pictureBoxPreview.Location = new Point(0, 0);
            pictureBoxPreview.Name = "pictureBoxPreview";
            pictureBoxPreview.Size = new Size(800, 290);
            pictureBoxPreview.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxPreview.TabIndex = 1;
            pictureBoxPreview.TabStop = false;
            pictureBoxPreview.DragDrop += HandleFileDrop;
            pictureBoxPreview.DragEnter += HandleDragEnter;
            // 
            // Form1
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(pictureBoxPreview);
            Controls.Add(panelControls);
            Name = "Form1";
            Text = "Form1";
            DragDrop += HandleFileDrop;
            DragEnter += HandleDragEnter;
            panelControls.ResumeLayout(false);
            panelControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarSigmaColor).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBarDetailBoost).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBarSmoothing).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).EndInit();
            ResumeLayout(false);
        }

        #endregion
    }
}
