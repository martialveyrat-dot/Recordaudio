using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Recordaudio.Helpers;
using Recordaudio.Models;
using Recordaudio.Services;

namespace Recordaudio
{
    public partial class Form1 : Form
    {
        private readonly RecordingService recordingService = new();
        private readonly RecordingCatalogService catalogService = new();
        private readonly AudioChunkingService audioChunkingService = new();
        private readonly BackendChunkTranscriptionService backendChunkTranscriptionService = new();
        private readonly SimpleTranscriptAnonymizer simpleTranscriptAnonymizer = new();
        private readonly BackendCmdAnalysisService backendCmdAnalysisService = new();
        private readonly CmdAnalysisFormatter cmdAnalysisFormatter = new();
        private readonly CmdAnalysisDocxExporter cmdAnalysisDocxExporter = new();

        private bool isUiBusy = false;
        private AppUiState currentState = AppUiState.Ready;
        private List<RecordingItem> recordingItems = new();

        private enum AppUiState
        {
            Ready,
            Recording,
            Finalizing,
            PreparingChunks,
            Transcribing,
            Error
        }

        public Form1()
        {
            InitializeComponent();

            ApplyUiTheme();

            recordingService.StatusChanged += RecordingService_StatusChanged;
            recordingService.RecordingCompleted += RecordingService_RecordingCompleted;
            recordingService.RecordingCompletedDetailed += RecordingService_RecordingCompletedDetailed;
            recordingService.ErrorOccurred += RecordingService_ErrorOccurred;

            LoadMicrophones();
            LoadRecordingCatalog();
            SetUiState(AppUiState.Ready);
        }

        private void ApplyUiTheme()
        {
            BackColor = UiTheme.AppBackground;
            Font = UiTheme.DefaultFont;

            grpCapture.Font = UiTheme.TitleFont;
            grpHistory.Font = UiTheme.TitleFont;
            grpPreview.Font = UiTheme.TitleFont;

            lblStep1.ForeColor = UiTheme.MutedText;
            lblStep2.ForeColor = UiTheme.MutedText;
            lblStep3.ForeColor = UiTheme.MutedText;
            lblStep4.ForeColor = UiTheme.MutedText;

            txtPreparationPreview.Font = UiTheme.DefaultFont;
            txtPreparationPreview.BackColor = UiTheme.CardBackground;
            txtPreparationPreview.ForeColor = UiTheme.StrongText;
            txtPreparationPreview.BorderStyle = BorderStyle.FixedSingle;

            lstRecordings.Font = UiTheme.DefaultFont;
            lstRecordings.DrawMode = DrawMode.OwnerDrawFixed;
            lstRecordings.ItemHeight = 42;
            lstRecordings.BackColor = UiTheme.CardBackground;
            lstRecordings.BorderStyle = BorderStyle.FixedSingle;

            txtClient.BackColor = Color.White;
            txtClient.ForeColor = UiTheme.StrongText;

            cmbMicrophones.BackColor = Color.White;
            cmbMicrophones.ForeColor = UiTheme.StrongText;

            btnStart.BackColor = UiTheme.Primary;
            btnStart.ForeColor = Color.White;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.FlatAppearance.BorderSize = 0;

            btnStop.BackColor = UiTheme.Error;
            btnStop.ForeColor = Color.White;
            btnStop.FlatStyle = FlatStyle.Flat;
            btnStop.FlatAppearance.BorderSize = 0;

            btnPrepareTranscription.BackColor = UiTheme.Success;
            btnPrepareTranscription.ForeColor = Color.White;
            btnPrepareTranscription.FlatStyle = FlatStyle.Flat;
            btnPrepareTranscription.FlatAppearance.BorderSize = 0;

            btnOpenFolder.BackColor = Color.White;
            btnOpenFolder.ForeColor = UiTheme.StrongText;
            btnOpenFolder.FlatStyle = FlatStyle.Flat;
            btnOpenFolder.FlatAppearance.BorderColor = UiTheme.Border;
            btnOpenFolder.FlatAppearance.BorderSize = 1;

            lblStatusCaption.ForeColor = UiTheme.MutedText;
            lblStatus.ForeColor = UiTheme.StrongText;

            Text = "Recordaudio - Assistant CMD";
        }

        private void LoadMicrophones()
        {
            cmbMicrophones.Items.Clear();

            var microphones = recordingService.GetAvailableMicrophones();

            foreach (var mic in microphones)
            {
                cmbMicrophones.Items.Add(mic);
            }

            if (cmbMicrophones.Items.Count > 0)
            {
                cmbMicrophones.SelectedIndex = 0;
            }

            AppLogger.Log($"Chargement microphones : {cmbMicrophones.Items.Count} trouvé(s).");
        }

        private void LoadRecordingCatalog(string? selectedId = null)
        {
            recordingItems = catalogService.LoadAll();
            RefreshRecordingList(selectedId);
        }

        private void RefreshRecordingList(string? selectedId = null)
        {
            lstRecordings.Items.Clear();

            RecordingItem? selectedItem = null;

            foreach (var item in recordingItems)
            {
                lstRecordings.Items.Add(item);

                if (!string.IsNullOrWhiteSpace(selectedId) && item.Id == selectedId)
                {
                    selectedItem = item;
                }
            }

            if (selectedItem != null)
            {
                lstRecordings.SelectedItem = selectedItem;
            }

            lstRecordings.Refresh();
            btnPrepareTranscription.Enabled = !isUiBusy && lstRecordings.SelectedItem is RecordingItem;
        }

        private void chkIncludeMic_CheckedChanged(object sender, EventArgs e)
        {
            UpdateMicControls();
        }

        private void UpdateMicControls()
        {
            bool enabled = chkIncludeMic.Checked && currentState == AppUiState.Ready;
            cmbMicrophones.Enabled = enabled;
            lblMicrophone.Enabled = enabled;
        }

        private void SetUiState(AppUiState state, string? statusText = null)
        {
            currentState = state;

            switch (state)
            {
                case AppUiState.Ready:
                    btnStart.Enabled = !isUiBusy;
                    btnStop.Enabled = false;
                    btnOpenFolder.Enabled = !isUiBusy;
                    btnPrepareTranscription.Enabled = !isUiBusy && lstRecordings.SelectedItem is RecordingItem;

                    txtClient.Enabled = !isUiBusy;
                    chkIncludeMic.Enabled = !isUiBusy;
                    UpdateMicControls();

                    lblStatus.Text = statusText ?? "Prêt";
                    lblStatus.ForeColor = UiTheme.Success;

                    prgWorkflow.Style = ProgressBarStyle.Blocks;
                    prgWorkflow.Value = 100;

                    lblStep1.Text = "Étape 1. Renseigne le nom de l’appel, active ton micro si besoin, puis démarre l’enregistrement.";
                    lblStep2.Text = "Étape 2. Sélectionne un appel existant pour relire ou lancer l’analyse.";
                    lblStep3.Text = "Étape 3. Consulte ici le résumé, l’analyse et les informations utiles.";
                    lblStep4.Text = "Étape 4. Lance l’analyse d’un appel sélectionné ou ouvre son dossier.";
                    break;

                case AppUiState.Recording:
                    btnStart.Enabled = false;
                    btnStop.Enabled = !isUiBusy;
                    btnOpenFolder.Enabled = false;
                    btnPrepareTranscription.Enabled = false;

                    txtClient.Enabled = false;
                    chkIncludeMic.Enabled = false;
                    cmbMicrophones.Enabled = false;
                    lblMicrophone.Enabled = false;

                    lblStatus.Text = statusText ?? "Enregistrement en cours...";
                    lblStatus.ForeColor = UiTheme.Error;

                    prgWorkflow.Style = ProgressBarStyle.Marquee;

                    lblStep1.Text = "Enregistrement en cours. Clique sur « Arrêter » à la fin de l’appel.";
                    break;

                case AppUiState.Finalizing:
                    btnStart.Enabled = false;
                    btnStop.Enabled = false;
                    btnOpenFolder.Enabled = false;
                    btnPrepareTranscription.Enabled = false;

                    txtClient.Enabled = false;
                    chkIncludeMic.Enabled = false;
                    cmbMicrophones.Enabled = false;
                    lblMicrophone.Enabled = false;

                    lblStatus.Text = statusText ?? "Finalisation en cours...";
                    lblStatus.ForeColor = UiTheme.Warning;

                    prgWorkflow.Style = ProgressBarStyle.Marquee;

                    lblStep4.Text = "Patiente pendant la finalisation de l’audio et la préparation des fichiers.";
                    break;

                case AppUiState.PreparingChunks:
                    btnStart.Enabled = false;
                    btnStop.Enabled = false;
                    btnOpenFolder.Enabled = false;
                    btnPrepareTranscription.Enabled = false;

                    txtClient.Enabled = false;
                    chkIncludeMic.Enabled = false;
                    cmbMicrophones.Enabled = false;
                    lblMicrophone.Enabled = false;

                    lblStatus.Text = statusText ?? "Préparation des chunks en cours...";
                    lblStatus.ForeColor = UiTheme.Warning;

                    prgWorkflow.Style = ProgressBarStyle.Marquee;

                    lblStep4.Text = "Préparation audio en cours : découpage intelligent avant transcription.";
                    break;

                case AppUiState.Transcribing:
                    btnStart.Enabled = false;
                    btnStop.Enabled = false;
                    btnOpenFolder.Enabled = false;
                    btnPrepareTranscription.Enabled = false;

                    txtClient.Enabled = false;
                    chkIncludeMic.Enabled = false;
                    cmbMicrophones.Enabled = false;
                    lblMicrophone.Enabled = false;

                    lblStatus.Text = statusText ?? "Analyse CMD en cours...";
                    lblStatus.ForeColor = UiTheme.Warning;

                    prgWorkflow.Style = ProgressBarStyle.Marquee;

                    lblStep4.Text = "Transcription, analyse commerciale et génération du livrable en cours.";
                    break;

                case AppUiState.Error:
                    btnStart.Enabled = !isUiBusy;
                    btnStop.Enabled = false;
                    btnOpenFolder.Enabled = !isUiBusy;
                    btnPrepareTranscription.Enabled = !isUiBusy && lstRecordings.SelectedItem is RecordingItem;

                    txtClient.Enabled = !isUiBusy;
                    chkIncludeMic.Enabled = !isUiBusy;
                    UpdateMicControls();

                    lblStatus.Text = statusText ?? "Erreur";
                    lblStatus.ForeColor = UiTheme.Error;

                    prgWorkflow.Style = ProgressBarStyle.Blocks;
                    prgWorkflow.Value = 0;

                    lblStep4.Text = "Une erreur est survenue. Corrige le problème puis relance l’action.";
                    break;
            }
        }

        private bool ValidateStartInputs(out string? validationMessage)
        {
            validationMessage = null;

            if (string.IsNullOrWhiteSpace(txtClient.Text))
            {
                validationMessage = "Veuillez renseigner un nom de client ou d’appel.";
                return false;
            }

            if (chkIncludeMic.Checked)
            {
                if (cmbMicrophones.SelectedItem is not AudioDeviceItem selectedMic)
                {
                    validationMessage = "Le micro est activé, mais aucun périphérique n’est sélectionné.";
                    return false;
                }

                var currentMics = recordingService.GetAvailableMicrophones();
                bool exists = false;

                foreach (var mic in currentMics)
                {
                    if (mic.DeviceNumber == selectedMic.DeviceNumber)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    validationMessage = "Le microphone sélectionné n’est plus disponible. Recharge la liste et réessaie.";
                    return false;
                }
            }

            return true;
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (isUiBusy || recordingService.IsRecording)
                return;

            if (!ValidateStartInputs(out string? validationMessage))
            {
                AppLogger.Log($"Validation démarrage refusée : {validationMessage}");
                MessageBox.Show(validationMessage ?? "Validation impossible.");
                SetUiState(AppUiState.Error, validationMessage);
                return;
            }

            try
            {
                isUiBusy = true;

                var options = new RecordingSessionOptions
                {
                    ClientName = txtClient.Text.Trim(),
                    IncludeMic = chkIncludeMic.Checked,
                    MicrophoneDeviceNumber = chkIncludeMic.Checked && cmbMicrophones.SelectedItem is AudioDeviceItem mic
                        ? mic.DeviceNumber
                        : null
                };

                AppLogger.Log($"Démarrage demandé. Client={options.ClientName}, IncludeMic={options.IncludeMic}, MicDevice={options.MicrophoneDeviceNumber}");

                SetUiState(AppUiState.Recording, "Enregistrement de l’appel en cours...");
                await recordingService.StartAsync(options);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur UI au démarrage : {ex.Message}");
                SetUiState(AppUiState.Error, "Erreur au démarrage");
            }
            finally
            {
                isUiBusy = false;

                if (!recordingService.IsRecording && currentState != AppUiState.Error)
                {
                    SetUiState(AppUiState.Ready);
                }
                else if (recordingService.IsRecording)
                {
                    SetUiState(AppUiState.Recording, lblStatus.Text);
                }
            }
        }

        private async void btnStop_Click(object sender, EventArgs e)
        {
            if (isUiBusy || !recordingService.IsRecording)
                return;

            try
            {
                isUiBusy = true;
                AppLogger.Log("Arrêt demandé par l’utilisateur.");
                SetUiState(AppUiState.Finalizing, "Finalisation de l’enregistrement...");

                await recordingService.StopAsync();
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur UI à l’arrêt : {ex.Message}");
                SetUiState(AppUiState.Error, "Erreur à l’arrêt");
            }
            finally
            {
                isUiBusy = false;

                if (!recordingService.IsRecording && currentState != AppUiState.Error)
                {
                    SetUiState(AppUiState.Ready);
                }
            }
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            if (isUiBusy)
                return;

            if (lstRecordings.SelectedItem is not RecordingItem item)
            {
                MessageBox.Show("Sélectionne d’abord un appel dans l’historique.");
                return;
            }

            try
            {
                string folderToOpen =
                    !string.IsNullOrWhiteSpace(item.VisibleFolderPath) ? item.VisibleFolderPath :
                    !string.IsNullOrWhiteSpace(item.WorkFolderPath) ? item.WorkFolderPath :
                    Path.GetDirectoryName(item.AudioFilePath) ?? recordingService.GetRecordingFolder();

                Directory.CreateDirectory(folderToOpen);
                Process.Start("explorer.exe", folderToOpen);
                AppLogger.Log($"Ouverture dossier : {folderToOpen}");
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur ouverture dossier : {ex.Message}");
                MessageBox.Show($"Impossible d’ouvrir le dossier : {ex.Message}");
            }
        }

        private async void btnPrepareTranscription_Click(object sender, EventArgs e)
        {
            if (isUiBusy)
                return;

            if (lstRecordings.SelectedItem is not RecordingItem item)
            {
                MessageBox.Show("Sélectionne d’abord un enregistrement.");
                return;
            }

            try
            {
                isUiBusy = true;

                if (string.IsNullOrWhiteSpace(item.ChunkManifestPath) || !File.Exists(item.ChunkManifestPath))
                {
                    SetUiState(AppUiState.PreparingChunks, "Préparation audio et découpage en cours...");

                    var chunkResult = await audioChunkingService.PrepareChunksAsync(item);

                    if (!chunkResult.Success)
                    {
                        item.Status = RecordingStatus.Error;
                        item.LastError = chunkResult.ErrorMessage;
                        catalogService.Update(item);
                        LoadRecordingCatalog(item.Id);

                        MessageBox.Show($"Préparation des chunks impossible : {chunkResult.ErrorMessage}");
                        SetUiState(AppUiState.Error, "Erreur de préparation");
                        return;
                    }

                    item.Status = RecordingStatus.ReadyForTranscription;
                    item.ChunkFolderPath = chunkResult.ChunkFolderPath;
                    item.ChunkManifestPath = chunkResult.ChunkManifestPath;
                    item.ChunkCount = chunkResult.Chunks.Count;
                    item.LastError = "";

                    catalogService.Update(item);
                    LoadRecordingCatalog(item.Id);
                    txtPreparationPreview.Text = chunkResult.PreviewText;
                }

                SetUiState(AppUiState.Transcribing, "Transcription, analyse commerciale et génération DOCX en cours...");

                item.Status = RecordingStatus.Transcribing;
                item.LastError = "";
                catalogService.Update(item);
                LoadRecordingCatalog(item.Id);

                var transcriptionResult = await backendChunkTranscriptionService.TranscribeChunksViaBackendAsync(item);

                if (!transcriptionResult.Success)
                {
                    item.Status = RecordingStatus.Error;
                    item.LastError = transcriptionResult.ErrorMessage;
                    catalogService.Update(item);
                    LoadRecordingCatalog(item.Id);

                    MessageBox.Show($"Transcription impossible : {transcriptionResult.ErrorMessage}");
                    SetUiState(AppUiState.Error, "Erreur de transcription");
                    return;
                }

                string anonymizedTranscript = simpleTranscriptAnonymizer.Anonymize(
                    transcriptionResult.TranscriptText,
                    item.ClientName);

                string baseFolder = !string.IsNullOrWhiteSpace(item.WorkFolderPath)
                    ? Path.Combine(item.WorkFolderPath, "analysis")
                    : item.ChunkFolderPath;

                if (string.IsNullOrWhiteSpace(baseFolder))
                {
                    baseFolder = Path.GetDirectoryName(item.AudioFilePath) ?? recordingService.GetRecordingFolder();
                }

                Directory.CreateDirectory(baseFolder);

                string anonymizedTranscriptPath = Path.Combine(
                    baseFolder,
                    $"{Path.GetFileNameWithoutExtension(item.AudioFilePath)}.anonymized.transcript.txt"
                );

                File.WriteAllText(anonymizedTranscriptPath, anonymizedTranscript);

                CmdAnalysisResult analysis = await backendCmdAnalysisService.AnalyzeTranscriptAsync(
                    item.ClientName,
                    anonymizedTranscript);

                string analysisJsonPath = Path.Combine(
                    baseFolder,
                    $"{Path.GetFileNameWithoutExtension(item.AudioFilePath)}.cmd-analysis.json"
                );

                string analysisTextPath = Path.Combine(
                    baseFolder,
                    $"{Path.GetFileNameWithoutExtension(item.AudioFilePath)}.cmd-analysis.txt"
                );

                string checklistPath = Path.Combine(
                    baseFolder,
                    $"{Path.GetFileNameWithoutExtension(item.AudioFilePath)}.checklist.txt"
                );

                string analysisJson = JsonSerializer.Serialize(analysis, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                string analysisText = cmdAnalysisFormatter.BuildAnalysisReport(analysis);
                string checklistText = cmdAnalysisFormatter.BuildChecklist(analysis);

                File.WriteAllText(analysisJsonPath, analysisJson);
                File.WriteAllText(analysisTextPath, analysisText);
                File.WriteAllText(checklistPath, checklistText);

                string docxPath = cmdAnalysisDocxExporter.Export(analysis, item);

                item.Status = RecordingStatus.Transcribed;
                item.TranscriptFilePath = transcriptionResult.TranscriptFilePath;
                item.TranscriptText = transcriptionResult.TranscriptText;

                item.AnonymizedTranscriptFilePath = anonymizedTranscriptPath;
                item.AnonymizedTranscriptText = anonymizedTranscript;

                item.SummaryFilePath = analysisTextPath;
                item.SummaryText = analysisText;

                item.AnalysisJsonFilePath = analysisJsonPath;
                item.ChecklistFilePath = checklistPath;
                item.DocxFilePath = docxPath;
                item.FinalDocumentPath = docxPath;

                item.LastError = transcriptionResult.ErrorMessage ?? "";

                catalogService.Update(item);
                LoadRecordingCatalog(item.Id);

                txtPreparationPreview.Text = BuildReadablePreview(item);

                AppLogger.Log($"Transcription + analyse CMD + DOCX réussis : {item.AudioFilePath}");
                MessageBox.Show(
                    string.IsNullOrWhiteSpace(transcriptionResult.ErrorMessage)
                        ? "Analyse CMD terminée."
                        : $"Analyse CMD terminée avec avertissement.\n\n{transcriptionResult.ErrorMessage}");

                SetUiState(AppUiState.Ready, "Analyse terminée avec succès.");
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur UI analyse CMD : {ex}");
                SetUiState(AppUiState.Error, "Erreur d’analyse");
                MessageBox.Show($"Erreur d’analyse : {ex.Message}");
            }
            finally
            {
                isUiBusy = false;

                if (currentState != AppUiState.Error)
                {
                    SetUiState(AppUiState.Ready, lblStatus.Text);
                }
            }
        }

        private void lstRecordings_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnPrepareTranscription.Enabled = !isUiBusy && lstRecordings.SelectedItem is RecordingItem;

            if (lstRecordings.SelectedItem is RecordingItem item)
            {
                txtPreparationPreview.Text = BuildReadablePreview(item);
            }
            else
            {
                txtPreparationPreview.Text = "";
            }
        }

        private string BuildReadablePreview(RecordingItem item)
        {
            string summary = !string.IsNullOrWhiteSpace(item.SummaryText) ? item.SummaryText : item.TranscriptText;

            return
$@"APPEL SÉLECTIONNÉ

Client / appel : {item.ClientName}
Date : {item.CreatedAt:yyyy-MM-dd HH:mm:ss}
Statut : {item.Status}
Durée : {item.DurationSeconds:0} sec
Nombre de chunks : {item.ChunkCount}

FICHIERS
Audio final : {item.AudioFilePath}
Dossier visible : {item.VisibleFolderPath}
Dossier de travail : {item.WorkFolderPath}
Transcript : {item.TranscriptFilePath}
Transcript anonymisé : {item.AnonymizedTranscriptFilePath}
Analyse : {item.SummaryFilePath}
Analyse JSON : {item.AnalysisJsonFilePath}
Checklist : {item.ChecklistFilePath}
Document final : {item.FinalDocumentPath}
Erreur : {item.LastError}

----------------------------------------
APERÇU
----------------------------------------

{summary}";
        }

        private void lstRecordings_DrawItem(object? sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0 || e.Index >= lstRecordings.Items.Count)
                return;

            if (lstRecordings.Items[e.Index] is not RecordingItem item)
                return;

            string icon = item.Status switch
            {
                RecordingStatus.Recorded => "🟡",
                RecordingStatus.ReadyForTranscription => "🟠",
                RecordingStatus.Transcribing => "🟠",
                RecordingStatus.Transcribed => "🟢",
                RecordingStatus.Error => "🔴",
                _ => "⚪"
            };

            string line1 = $"{icon}  {item.CreatedAt:dd/MM HH:mm}";
            string line2 = item.ClientName;

            using var strongBrush = new SolidBrush(UiTheme.StrongText);
            using var mutedBrush = new SolidBrush(UiTheme.MutedText);

            e.Graphics.DrawString(line1, UiTheme.DefaultFont, mutedBrush, e.Bounds.Left + 8, e.Bounds.Top + 4);
            e.Graphics.DrawString(line2, UiTheme.TitleFont, strongBrush, e.Bounds.Left + 8, e.Bounds.Top + 20);

            e.DrawFocusRectangle();
        }

        private void RecordingService_StatusChanged(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(RecordingService_StatusChanged), message);
                return;
            }

            AppLogger.Log($"StatusChanged: {message}");

            if (message.Contains("Finalisation", StringComparison.OrdinalIgnoreCase))
            {
                SetUiState(AppUiState.Finalizing, message);
            }
            else if (recordingService.IsRecording)
            {
                SetUiState(AppUiState.Recording, message);
            }
            else
            {
                lblStatus.Text = message;
            }
        }

        private void RecordingService_RecordingCompleted(string filePath)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(RecordingService_RecordingCompleted), filePath);
                return;
            }

            AppLogger.Log($"Enregistrement terminé : {filePath}");
            SetUiState(AppUiState.Ready, $"Fichier enregistré : {Path.GetFileName(filePath)}");
            MessageBox.Show($"Enregistrement terminé.\n\nFichier : {filePath}");
        }

        private void RecordingService_RecordingCompletedDetailed(string filePath, string clientName)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string>(RecordingService_RecordingCompletedDetailed), filePath, clientName);
                return;
            }

            double duration = 0;

            try
            {
                using var reader = new NAudio.Wave.AudioFileReader(filePath);
                duration = reader.TotalTime.TotalSeconds;
            }
            catch
            {
            }

            var folder = Path.GetDirectoryName(filePath) ?? "";

            var item = new RecordingItem
            {
                ClientName = clientName,
                CreatedAt = DateTime.Now,
                AudioFilePath = filePath,
                DurationSeconds = duration,
                Status = RecordingStatus.Recorded,
                VisibleFolderPath = folder
            };

            catalogService.Add(item);
            LoadRecordingCatalog(item.Id);
        }

        private void RecordingService_ErrorOccurred(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(RecordingService_ErrorOccurred), message);
                return;
            }

            AppLogger.Log($"Erreur service : {message}");
            SetUiState(AppUiState.Error, message);
            MessageBox.Show(message);
        }

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            if (recordingService.IsRecording || currentState == AppUiState.Finalizing || isUiBusy)
            {
                var result = MessageBox.Show(
                    "Un enregistrement, une finalisation ou une analyse est en cours. Voulez-vous fermer l’application proprement ?",
                    "Fermeture",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            try
            {
                isUiBusy = true;
                SetUiState(AppUiState.Finalizing, "Fermeture et finalisation en cours...");
                AppLogger.Log("Fermeture de l’application demandée.");

                await recordingService.CleanupOnCloseAsync();
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur pendant la fermeture : {ex.Message}");
            }

            base.OnFormClosing(e);
        }
    }
}