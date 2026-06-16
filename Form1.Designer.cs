namespace Recordaudio
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            layoutRoot = new TableLayoutPanel();
            grpCapture = new GroupBox();
            lblStep1 = new Label();
            lblClient = new Label();
            txtClient = new TextBox();
            chkIncludeMic = new CheckBox();
            lblMicrophone = new Label();
            cmbMicrophones = new ComboBox();
            btnStart = new Button();
            btnStop = new Button();
            splitMain = new SplitContainer();
            grpHistory = new GroupBox();
            lblStep2 = new Label();
            lstRecordings = new ListBox();
            grpPreview = new GroupBox();
            lblStep3 = new Label();
            txtPreparationPreview = new TextBox();
            panelBottom = new Panel();
            lblStep4 = new Label();
            btnPrepareTranscription = new Button();
            btnOpenFolder = new Button();
            lblStatusCaption = new Label();
            lblStatus = new Label();
            prgWorkflow = new ProgressBar();
            layoutRoot.SuspendLayout();
            grpCapture.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            grpHistory.SuspendLayout();
            grpPreview.SuspendLayout();
            panelBottom.SuspendLayout();
            SuspendLayout();
            // 
            // layoutRoot
            // 
            layoutRoot.ColumnCount = 1;
            layoutRoot.ColumnStyles.Add(new ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            layoutRoot.Controls.Add(grpCapture, 0, 0);
            layoutRoot.Controls.Add(splitMain, 0, 1);
            layoutRoot.Controls.Add(panelBottom, 0, 2);
            layoutRoot.Dock = DockStyle.Fill;
            layoutRoot.Location = new System.Drawing.Point(0, 0);
            layoutRoot.Name = "layoutRoot";
            layoutRoot.Padding = new Padding(20, 18, 20, 18);
            layoutRoot.RowCount = 3;
            layoutRoot.RowStyles.Add(new RowStyle(System.Windows.Forms.SizeType.Absolute, 178F));
            layoutRoot.RowStyles.Add(new RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            layoutRoot.RowStyles.Add(new RowStyle(System.Windows.Forms.SizeType.Absolute, 118F));
            layoutRoot.Size = new System.Drawing.Size(1280, 780);
            layoutRoot.TabIndex = 0;
            // 
            // grpCapture
            // 
            grpCapture.Controls.Add(lblStep1);
            grpCapture.Controls.Add(lblClient);
            grpCapture.Controls.Add(txtClient);
            grpCapture.Controls.Add(chkIncludeMic);
            grpCapture.Controls.Add(lblMicrophone);
            grpCapture.Controls.Add(cmbMicrophones);
            grpCapture.Controls.Add(btnStart);
            grpCapture.Controls.Add(btnStop);
            grpCapture.Dock = DockStyle.Fill;
            grpCapture.Location = new System.Drawing.Point(23, 21);
            grpCapture.Name = "grpCapture";
            grpCapture.Padding = new Padding(16, 14, 16, 14);
            grpCapture.Size = new System.Drawing.Size(1234, 172);
            grpCapture.TabIndex = 0;
            grpCapture.TabStop = false;
            grpCapture.Text = "Préparer l’appel";
            // 
            // lblStep1
            // 
            lblStep1.AutoSize = true;
            lblStep1.Location = new System.Drawing.Point(18, 30);
            lblStep1.Name = "lblStep1";
            lblStep1.Size = new System.Drawing.Size(580, 20);
            lblStep1.TabIndex = 0;
            lblStep1.Text = "Étape 1. Renseigne le nom de l’appel, active ton micro si besoin, puis démarre l’enregistrement.";
            // 
            // lblClient
            // 
            lblClient.AutoSize = true;
            lblClient.Location = new System.Drawing.Point(18, 63);
            lblClient.Name = "lblClient";
            lblClient.Size = new System.Drawing.Size(183, 20);
            lblClient.TabIndex = 1;
            lblClient.Text = "Nom du client / de l’appel";
            // 
            // txtClient
            // 
            txtClient.Location = new System.Drawing.Point(18, 87);
            txtClient.Name = "txtClient";
            txtClient.PlaceholderText = "Ex. : Appel découverte - Société Martin";
            txtClient.Size = new System.Drawing.Size(708, 27);
            txtClient.TabIndex = 2;
            // 
            // chkIncludeMic
            // 
            chkIncludeMic.AutoSize = true;
            chkIncludeMic.Location = new System.Drawing.Point(18, 129);
            chkIncludeMic.Name = "chkIncludeMic";
            chkIncludeMic.Size = new System.Drawing.Size(186, 24);
            chkIncludeMic.TabIndex = 3;
            chkIncludeMic.Text = "Inclure mon micro voix";
            chkIncludeMic.UseVisualStyleBackColor = true;
            chkIncludeMic.CheckedChanged += chkIncludeMic_CheckedChanged;
            // 
            // lblMicrophone
            // 
            lblMicrophone.AutoSize = true;
            lblMicrophone.Location = new System.Drawing.Point(258, 130);
            lblMicrophone.Name = "lblMicrophone";
            lblMicrophone.Size = new Size(142, 20);
            lblMicrophone.TabIndex = 4;
            lblMicrophone.Text = "Périphérique micro";
            // 
            // cmbMicrophones
            // 
            cmbMicrophones.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMicrophones.FormattingEnabled = true;
            cmbMicrophones.Location = new System.Drawing.Point(406, 126);
            cmbMicrophones.Name = "cmbMicrophones";
            cmbMicrophones.Size = new System.Drawing.Size(320, 28);
            cmbMicrophones.TabIndex = 5;
            // 
            // btnStart
            // 
            btnStart.Location = new System.Drawing.Point(782, 83);
            btnStart.Name = "btnStart";
            btnStart.Size = new System.Drawing.Size(230, 44);
            btnStart.TabIndex = 6;
            btnStart.Text = "🎙 Démarrer l’enregistrement";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new System.Drawing.Point(1028, 83);
            btnStop.Name = "btnStop";
            btnStop.Size = new System.Drawing.Size(176, 44);
            btnStop.TabIndex = 7;
            btnStop.Text = "⏹ Arrêter";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.Location = new System.Drawing.Point(23, 199);
            splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(grpHistory);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(grpPreview);
            splitMain.Size = new System.Drawing.Size(1234, 442);
            splitMain.SplitterDistance = 372;
            splitMain.TabIndex = 1;
            // 
            // grpHistory
            // 
            grpHistory.Controls.Add(lblStep2);
            grpHistory.Controls.Add(lstRecordings);
            grpHistory.Dock = DockStyle.Fill;
            grpHistory.Location = new System.Drawing.Point(0, 0);
            grpHistory.Name = "grpHistory";
            grpHistory.Padding = new Padding(14, 14, 14, 14);
            grpHistory.Size = new System.Drawing.Size(372, 442);
            grpHistory.TabIndex = 0;
            grpHistory.TabStop = false;
            grpHistory.Text = "Historique des appels";
            // 
            // lblStep2
            // 
            lblStep2.AutoSize = true;
            lblStep2.Location = new System.Drawing.Point(16, 30);
            lblStep2.Name = "lblStep2";
            lblStep2.Size = new System.Drawing.Size(285, 20);
            lblStep2.TabIndex = 0;
            lblStep2.Text = "Étape 2. Sélectionne un appel à consulter.";
            // 
            // lstRecordings
            // 
            lstRecordings.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstRecordings.FormattingEnabled = true;
            lstRecordings.ItemHeight = 20;
            lstRecordings.Location = new System.Drawing.Point(16, 60);
            lstRecordings.Name = "lstRecordings";
            lstRecordings.Size = new System.Drawing.Size(338, 344);
            lstRecordings.TabIndex = 1;
            lstRecordings.SelectedIndexChanged += lstRecordings_SelectedIndexChanged;
            lstRecordings.DrawItem += lstRecordings_DrawItem;
            // 
            // grpPreview
            // 
            grpPreview.Controls.Add(lblStep3);
            grpPreview.Controls.Add(txtPreparationPreview);
            grpPreview.Dock = DockStyle.Fill;
            grpPreview.Location = new System.Drawing.Point(0, 0);
            grpPreview.Name = "grpPreview";
            grpPreview.Padding = new Padding(14, 14, 14, 14);
            grpPreview.Size = new System.Drawing.Size(858, 442);
            grpPreview.TabIndex = 0;
            grpPreview.TabStop = false;
            grpPreview.Text = "Analyse / aperçu";
            // 
            // lblStep3
            // 
            lblStep3.AutoSize = true;
            lblStep3.Location = new System.Drawing.Point(16, 30);
            lblStep3.Name = "lblStep3";
            lblStep3.Size = new System.Drawing.Size(425, 20);
            lblStep3.TabIndex = 0;
            lblStep3.Text = "Étape 3. Consulte ici le résumé, l’analyse et les informations utiles.";
            // 
            // txtPreparationPreview
            // 
            txtPreparationPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtPreparationPreview.Location = new System.Drawing.Point(16, 60);
            txtPreparationPreview.Multiline = true;
            txtPreparationPreview.Name = "txtPreparationPreview";
            txtPreparationPreview.ReadOnly = true;
            txtPreparationPreview.ScrollBars = ScrollBars.Vertical;
            txtPreparationPreview.Size = new System.Drawing.Size(824, 344);
            txtPreparationPreview.TabIndex = 1;
            // 
            // panelBottom
            // 
            panelBottom.Controls.Add(lblStep4);
            panelBottom.Controls.Add(btnPrepareTranscription);
            panelBottom.Controls.Add(btnOpenFolder);
            panelBottom.Controls.Add(lblStatusCaption);
            panelBottom.Controls.Add(lblStatus);
            panelBottom.Controls.Add(prgWorkflow);
            panelBottom.Dock = DockStyle.Fill;
            panelBottom.Location = new System.Drawing.Point(23, 647);
            panelBottom.Name = "panelBottom";
            panelBottom.Size = new System.Drawing.Size(1234, 112);
            panelBottom.TabIndex = 2;
            // 
            // lblStep4
            // 
            lblStep4.AutoSize = true;
            lblStep4.Location = new System.Drawing.Point(18, 12);
            lblStep4.Name = "lblStep4";
            lblStep4.Size = new System.Drawing.Size(425, 20);
            lblStep4.TabIndex = 0;
            lblStep4.Text = "Étape 4. Lance l’analyse d’un appel sélectionné ou ouvre son dossier.";
            // 
            // btnPrepareTranscription
            // 
            btnPrepareTranscription.Location = new System.Drawing.Point(1002, 48);
            btnPrepareTranscription.Name = "btnPrepareTranscription";
            btnPrepareTranscription.Size = new System.Drawing.Size(200, 46);
            btnPrepareTranscription.TabIndex = 1;
            btnPrepareTranscription.Text = "⚡ Générer l’analyse";
            btnPrepareTranscription.UseVisualStyleBackColor = true;
            btnPrepareTranscription.Click += btnPrepareTranscription_Click;
            // 
            // btnOpenFolder
            // 
            btnOpenFolder.Location = new System.Drawing.Point(790, 48);
            btnOpenFolder.Name = "btnOpenFolder";
            btnOpenFolder.Size = new System.Drawing.Size(194, 46);
            btnOpenFolder.TabIndex = 2;
            btnOpenFolder.Text = "📁 Ouvrir le dossier";
            btnOpenFolder.UseVisualStyleBackColor = true;
            btnOpenFolder.Click += btnOpenFolder_Click;
            // 
            // lblStatusCaption
            // 
            lblStatusCaption.AutoSize = true;
            lblStatusCaption.Location = new System.Drawing.Point(18, 48);
            lblStatusCaption.Name = "lblStatusCaption";
            lblStatusCaption.Size = new System.Drawing.Size(55, 20);
            lblStatusCaption.TabIndex = 3;
            lblStatusCaption.Text = "Statut :";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new System.Drawing.Point(79, 48);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(35, 20);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Prêt";
            // 
            // prgWorkflow
            // 
            prgWorkflow.Location = new System.Drawing.Point(18, 82);
            prgWorkflow.Name = "prgWorkflow";
            prgWorkflow.Size = new System.Drawing.Size(744, 12);
            prgWorkflow.TabIndex = 5;
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1280, 780);
            Controls.Add(layoutRoot);
            MinimumSize = new System.Drawing.Size(1180, 720);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Recordaudio - Assistant CMD";
            layoutRoot.ResumeLayout(false);
            grpCapture.ResumeLayout(false);
            grpCapture.PerformLayout();
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            grpHistory.ResumeLayout(false);
            grpHistory.PerformLayout();
            grpPreview.ResumeLayout(false);
            grpPreview.PerformLayout();
            panelBottom.ResumeLayout(false);
            panelBottom.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layoutRoot;
        private GroupBox grpCapture;
        private Label lblStep1;
        private Label lblClient;
        private TextBox txtClient;
        private CheckBox chkIncludeMic;
        private Label lblMicrophone;
        private ComboBox cmbMicrophones;
        private Button btnStart;
        private Button btnStop;
        private SplitContainer splitMain;
        private GroupBox grpHistory;
        private Label lblStep2;
        private ListBox lstRecordings;
        private GroupBox grpPreview;
        private Label lblStep3;
        private TextBox txtPreparationPreview;
        private Panel panelBottom;
        private Label lblStep4;
        private Button btnPrepareTranscription;
        private Button btnOpenFolder;
        private Label lblStatusCaption;
        private Label lblStatus;
        private ProgressBar prgWorkflow;
    }
}