namespace Tonemapster.NET.Smoothening.Prototype
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private Panel panelControls;
        private Label labelSmoothing;
        private PictureBox pictureBoxPreview;
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
            components = new System.ComponentModel.Container();
            panelControls = new Panel();
            labelSmoothing = new Label();
            trackBarSmoothing = new TrackBar();
            pictureBoxPreview = new PictureBox();
            panelControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarSmoothing).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).BeginInit();
            SuspendLayout();

            panelControls.Controls.Add(trackBarSmoothing);
            panelControls.Controls.Add(labelSmoothing);
            panelControls.Dock = DockStyle.Bottom;
            panelControls.Location = new Point(0, 390);
            panelControls.Name = "panelControls";
            panelControls.Size = new Size(800, 60);
            panelControls.TabIndex = 0;

            labelSmoothing.AutoSize = true;
            labelSmoothing.Location = new Point(12, 21);
            labelSmoothing.Name = "labelSmoothing";
            labelSmoothing.Size = new Size(84, 15);
            labelSmoothing.TabIndex = 0;
            labelSmoothing.Text = "Smoothing: 0";

            trackBarSmoothing.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            trackBarSmoothing.Location = new Point(102, 12);
            trackBarSmoothing.Maximum = 100;
            trackBarSmoothing.Name = "trackBarSmoothing";
            trackBarSmoothing.Size = new Size(686, 45);
            trackBarSmoothing.TabIndex = 1;
            trackBarSmoothing.TickFrequency = 10;
            trackBarSmoothing.MouseCaptureChanged += HandleSmoothingStrengthMouseCaptureChanged;
            trackBarSmoothing.MouseUp += HandleSmoothingStrengthMouseUp;
            trackBarSmoothing.Scroll += HandleSmoothingStrengthScroll;

            pictureBoxPreview.AllowDrop = true;
            pictureBoxPreview.Dock = DockStyle.Fill;
            pictureBoxPreview.Location = new Point(0, 0);
            pictureBoxPreview.Name = "pictureBoxPreview";
            pictureBoxPreview.Size = new Size(800, 390);
            pictureBoxPreview.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxPreview.TabIndex = 1;
            pictureBoxPreview.TabStop = false;
            pictureBoxPreview.DragDrop += HandleFileDrop;
            pictureBoxPreview.DragEnter += HandleDragEnter;

            AllowDrop = true;
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
            ((System.ComponentModel.ISupportInitialize)trackBarSmoothing).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).EndInit();
            ResumeLayout(false);
        }

        #endregion
    }
}
