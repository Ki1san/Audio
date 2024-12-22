using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Lame;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;
using VGAudio.Formats;
using System.Linq;

namespace Audio
{
    public partial class Form1 : Form
    {
        private float bassGain = 0f;
        private float midGain = 0f;
        private float trebleGain = 0f;

        private WaveOutEvent waveOut;
        private AudioRecorder audioRecorder;
        private Equalizer equalizer;
        private Effects effects;
        private EffectManager effectManager;
        private Timer countdownTimer;

        private VolumeSampleProvider volumeProvider;
        private Timer timer;

        private WaveInEvent waveIn; // Поток для записи
        private LameMP3FileWriter writer; // Класс для записи в MP3 файл

        public string outputFileName = "output.mp3"; // Имя файла для сохранения
        public AudioFileReader audioFileReader; // Для чтения MP3 файла

        private AudioDeviceManager deviceManager;
        private int selectedDevice;

        private MenuButtonHandler menuButtonHandler;

        public PictureBox PictureBox1 => this.pictureBox1;
        public string OutputFileName => outputFileName;

        private AudioManager audioManager;
        private Selection selection;

        private AudioEditor audioEditor;



        public Form1()
        {
            InitializeComponent();
            deviceManager = new AudioDeviceManager(radioButton1, radioButton2);
            menuButtonHandler = new MenuButtonHandler(this);
            waveOut = new WaveOutEvent();
            selection = new Selection();
            audioEditor = new AudioEditor(pictureBox1);

            countdownTimer = new Timer { Interval = 1000 }; // Update every second
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();

            this.WindowState = FormWindowState.Maximized;

            this.Controls.Add(button6);

            pictureBox1.Paint += PictureBox_Paint;
            pictureBox1.MouseDown += PictureBox_MouseDown;
            pictureBox1.MouseMove += PictureBox_MouseMove;
            pictureBox1.MouseUp += PictureBox_MouseUp;


        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            if (audioFileReader != null && waveOut.PlaybackState == PlaybackState.Playing)
            {
                TimeSpan remainingTime = audioFileReader.TotalTime - audioFileReader.CurrentTime;
                // Update a label or any UI element to show the remaining time
                label18.Text = remainingTime.ToString(@"mm\:ss");

                // Stop the timer when audio finishes
                if (remainingTime.TotalSeconds <= 0)
                {
                    countdownTimer.Stop();
                    label18.Text = "00:00"; // Reset display
                }
            }
        }

        private void InitializeEffects()
        {
            if (audioFileReader != null)
            {
                volumeProvider = new VolumeSampleProvider(audioFileReader);
                effects = new Effects(volumeProvider); // Initialize effects
                effectManager = new EffectManager(effects); // Initialize EffectManager here
            }
            else
            {
                MessageBox.Show("Audio file is not loaded. Please load an audio file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            pictureBox1.Focus();

            // Auto-select first input and output devices
            if (deviceManager.InputDevices.Count > 0)
            {
                selectedDevice = deviceManager.InputDevices[0].Id;
                radioButton1.Checked = true;
            }

            if (deviceManager.OutputDevices.Count > 0)
            {
                selectedDevice = deviceManager.OutputDevices[0].Id;
                radioButton2.Checked = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartRecording();
        }

        private void StartRecording()
        {
            audioRecorder = new AudioRecorder(pictureBox2, outputFileName, selectedDevice);
            radioButton1.Checked = true; // Включаем radioButton1
            audioRecorder.StartRecording();
            MessageBox.Show("Recording started");
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            // Запись данных в файл
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<WaveInEventArgs>(OnDataAvailable), e);
            }
            else
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
                // Отрисовка звуковой волны в pictureBox2
                DrawWaveformFromData(e.Buffer, e.BytesRecorded);
            }
        }

        private void DrawWaveformFromData(byte[] buffer, int bytesRecorded)
        {
            int samples = bytesRecorded / 2; // Поскольку это моно, на каждую выборку 2 байта
            float[] floatBuffer = new float[samples];
            Buffer.BlockCopy(buffer, 0, floatBuffer, 0, bytesRecorded);

            int width = pictureBox2.Width;
            int height = pictureBox2.Height;
            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                int padding = 10;
                int effectiveHeight = height - 2 * padding;

                for (int i = 0; i < samples; i++)
                {
                    float sample = floatBuffer[i];
                    int x = (int)((i / (float)samples) * width); // Пропорциональное значение для ширины
                    int y = (int)((sample + 1) * effectiveHeight / 2); // Нормализация

                    y = Math.Max(0, Math.Min(effectiveHeight, y));
                    bitmap.SetPixel(x, padding + effectiveHeight - y, Color.Blue);
                }
            }
            pictureBox2.Image = bitmap; // Обновляем изображение в pictureBox2
        }

        private void PutData(object sender, WaveInEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<WaveInEventArgs>(PutData), e);
            }
            else
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }

        public void StopRecord(object sender, EventArgs e)
        {
            waveIn?.StopRecording();
            waveOut?.Stop();
            timer?.Stop();
            countdownTimer?.Stop(); // Остановить таймер обратного отсчета
            DisposeWaveInAndWriter();
            MessageBox.Show("Recording stopped");
        }

        private void DisposeWaveInAndWriter()
        {
            waveIn?.Dispose();
            writer?.Close();
            writer?.Dispose();
            waveIn = null;
            writer = null;
            radioButton1.Checked = false; // Сбрасываем состояние radioButton1
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (audioFileReader != null)
            {
                waveOut.Stop();
                audioFileReader.CurrentTime = TimeSpan.Zero;
                PlayAudio(audioFileReader.FileName);
            }
            else
            {
                MessageBox.Show("Please open a file to play.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void PlayAudio(string filePath)
        {
            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                audioFileReader?.Dispose();
                audioFileReader = null;
            }

            try
            {
                audioFileReader = new AudioFileReader(filePath);
                volumeProvider = new VolumeSampleProvider(audioFileReader);
                waveOut = new WaveOutEvent { DeviceNumber = selectedDevice };

                effects = new Effects(volumeProvider);
                InitializeEffects();
                var processedProvider = effects.GetProcessedProvider();

                equalizer = new Equalizer(processedProvider);
                waveOut.Init(equalizer);
                waveOut.Play();

                radioButton2.Checked = true;

                timer = new Timer { Interval = 100 };
                timer.Tick += Timer_Tick;
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing audio: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (audioFileReader != null && waveOut.PlaybackState == PlaybackState.Playing)
            {
                trackBar5.Value = (int)audioFileReader.CurrentTime.TotalSeconds;
                trackBar5.Maximum = (int)audioFileReader.TotalTime.TotalSeconds;
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (volumeProvider != null)
            {
                volumeProvider.Volume = trackBar1.Value / 100f;
            }
        }

        private void trackBar5_Scroll(object sender, EventArgs e)
        {
            if (audioFileReader != null)
            {
                if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    waveOut.Stop();
                }

                audioFileReader.CurrentTime = TimeSpan.FromSeconds(trackBar5.Value);
                waveOut.Play();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (waveOut != null && waveOut.PlaybackState == PlaybackState.Paused)
            {
                waveOut.Play();
                button4.Enabled = true;
                button3.Enabled = false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (waveOut != null && waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Pause();
                button4.Enabled = false;
                button3.Enabled = true;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            SkipBack(5);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SkipForward(5);
        }

        private void SkipBack(int seconds)
        {
            if (audioFileReader != null)
            {
                var newTime = audioFileReader.CurrentTime.TotalSeconds - seconds;
                audioFileReader.CurrentTime = TimeSpan.FromSeconds(Math.Max(0, newTime));
                UpdateTrackBars();
                PlayIfPaused();
            }
        }

        private void SkipForward(int seconds)
        {
            if (audioFileReader != null)
            {
                var newTime = audioFileReader.CurrentTime.TotalSeconds + seconds;
                audioFileReader.CurrentTime = TimeSpan.FromSeconds(Math.Min(audioFileReader.TotalTime.TotalSeconds, newTime));
                UpdateTrackBars();
                PlayIfPaused();
            }
        }

        private void UpdateTrackBars()
        {
            trackBar1.Value = (int)audioFileReader.CurrentTime.TotalSeconds;
            trackBar5.Value = (int)audioFileReader.CurrentTime.TotalSeconds;
        }

        private void PlayIfPaused()
        {
            if (waveOut.PlaybackState == PlaybackState.Paused)
            {
                waveOut.Play();
            }
        }

        public void SelectInputDevice(int deviceId)
        {
            DisposeWaveInAndWriter();
            waveIn = new WaveInEvent
            {
                DeviceNumber = deviceId,
                WaveFormat = new WaveFormat(44100, 1)
            };

            waveIn.DataAvailable += PutData;
            waveIn.RecordingStopped += StopRecord;
            selectedDevice = deviceId;
        }

        public void SelectOutputDevice(int deviceId)
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = new WaveOutEvent { DeviceNumber = deviceId };

            if (equalizer != null)
            {
                waveOut.Init(equalizer);
            }

            selectedDevice = deviceId;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            menuButtonHandler.OpenFile_Click(sender, e);
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            menuButtonHandler.SaveFile_Click(sender, e);
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            menuButtonHandler.ClearPictureBox_Click(sender, e);
        }

        public void ClearAudioAndImage()
        {
            countdownTimer = null;
            pictureBox1.Image = null;
            waveOut?.Stop();
            waveOut?.Dispose();
            audioFileReader?.Dispose();
            trackBar5.Value = 0;
            trackBar5.Maximum = 0;
            trackBar1.Value = 0;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "MP3 Files (*.mp3)|*.mp3|All Files (*.*)|*.*",
                Title = "Save MP3 file as",
                FileName = "exported_output.mp3"
            })
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    menuButtonHandler.ExportAudioFile(saveFileDialog.FileName);
                }
            }
        }

        private void UpdateEqualizerGain(int value, ref float gain, string type)
        {
            if (equalizer != null)
            {
                gain = value;
                switch (type)
                {
                    case "bass":
                        equalizer.UpdateBassGain(gain / 10f);
                        break;
                    case "mid":
                        equalizer.UpdateMidGain(gain / 10f);
                        break;
                    case "treble":
                        equalizer.UpdateTrebleGain(gain / 10f);
                        break;
                }
            }
        }

        private void trackBar2_Scroll_1(object sender, EventArgs e)
        {
            UpdateEqualizerGain(trackBar2.Value, ref bassGain, "bass");
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            UpdateEqualizerGain(trackBar3.Value, ref midGain, "mid");
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            UpdateEqualizerGain(trackBar4.Value, ref trebleGain, "treble");
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "MP3 Files (*.mp3)|*.mp3|All Files (*.*)|*.*",
                Title = "Select MP3 file to import"
            })
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    menuButtonHandler.ImportAudioFile(openFileDialog.FileName);
                }
            }
        }

        private bool isUpdatingEffect = false;
        private float currentRoomSize = 0.5f; // Текущее значение roomSize
        private float currentChorusRate = 0.5f; // Текущее значение rate
        private DateTime lastUpdateTime = DateTime.Now;

        private void UpdatePlayback()
        {
            try
            {
                // Проверка инициализации waveOut
                if (waveOut == null)
                {
                    MessageBox.Show("Audio output is not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Остановка воспроизведения, если оно идет
                if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    waveOut.Stop();
                }

                // Проверка инициализации effectManager
                if (effectManager == null)
                {
                    MessageBox.Show("Effect manager is not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Проверка инициализации effects
                if (effects == null)
                {
                    MessageBox.Show("Effects provider is not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Получаем обработанный провайдер
                var processedProvider = effectManager.UpdateEffects(effects.GetProcessedProvider());
                if (processedProvider == null)
                {
                    MessageBox.Show("Processed audio provider is null.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Инициализируем waveOut с обработанным провайдером
                waveOut.Init(processedProvider);
                waveOut.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Playback error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void trackBar11_Scroll(object sender, EventArgs e)
        {
            try
            {
                if (effectManager == null)
                {
                    MessageBox.Show("Effect manager is not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                float targetRoomSize = Clamp(trackBar11.Value / 10f, 0f, 1f);
                if ((DateTime.Now - lastUpdateTime).TotalMilliseconds < 100) return; // Ограничение частоты

                lastUpdateTime = DateTime.Now;
                currentRoomSize = Lerp(currentRoomSize, targetRoomSize, 0.1f);
                float damp = Clamp(trackBar14.Value / 10f, 0f, 1f);
                float wetDryMix = Clamp(trackBar15.Value / 10f, 0f, 1f);
                effectManager.SetReverb(currentRoomSize, damp, wetDryMix);
                UpdatePlayback();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing room size: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void trackBar7_Scroll(object sender, EventArgs e)
        {
            try
            {
                if (effectManager == null)
                {
                    MessageBox.Show("Effect manager is not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                float saturationAmount = Clamp(trackBar7.Value / 10f, 0f, 1f);
                effectManager.SetSaturation(saturationAmount);
                UpdatePlayback();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing saturation: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void trackBar8_Scroll(object sender, EventArgs e)
        {
            try
            {
                if (effectManager == null)
                {
                    MessageBox.Show("Effect manager is not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if ((DateTime.Now - lastUpdateTime).TotalMilliseconds < 100) return; // Ограничение частоты
                lastUpdateTime = DateTime.Now;

                currentChorusRate = Lerp(currentChorusRate, trackBar8.Value / 10f, 0.1f);
                float depth = trackBar9.Value / 10f;
                effectManager.SetChorus(currentChorusRate, depth, Clamp(trackBar12.Value / 10f, 0f, 1f));
                UpdatePlayback();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing chorus rate: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void trackBar9_Scroll(object sender, EventArgs e)
        {
            try
            {
                if (effectManager == null)
                {
                    MessageBox.Show("Effect manager is not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if ((DateTime.Now - lastUpdateTime).TotalMilliseconds < 100) return; // Ограничение частоты
                lastUpdateTime = DateTime.Now;

                float depth = trackBar9.Value / 10f;
                effectManager.SetChorus(currentChorusRate, depth, Clamp(trackBar12.Value / 10f, 0f, 1f));
                UpdatePlayback();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing chorus depth: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void trackBar10_Scroll(object sender, EventArgs e)
        {
            try
            {
                float feedback = trackBar10.Value / 100f; // Предполагается, что trackBar10 настраивается от 0 до 100
                effectManager.SetDelay(effects.CurrentDelayTime, feedback); // Устанавливаем задержку с текущим временем и feedback
                UpdatePlayback(); // Обновляем воспроизведение с новыми настройками
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении задержки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void trackBar13_Scroll(object sender, EventArgs e)
        {
            try
            {
                if (effectManager == null)
                {
                    MessageBox.Show("Effect manager is not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if ((DateTime.Now - lastUpdateTime).TotalMilliseconds < 100) return; // Ограничение частоты
                lastUpdateTime = DateTime.Now;

                float delayTime = Clamp(trackBar13.Value / 1000f, 0f, 5f);
                float feedback = Clamp(effects.CurrentFeedback, 0f, 1f);
                effectManager.SetDelay(delayTime, feedback);
                UpdatePlayback();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing delay time: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        private float Lerp(float a, float b, float t) => a + (b - a) * t;


        private Random random = new Random(); // Инициализация генератора случайных чисел

        private void ApplyRandomEffect()
        {
            int effectIndex = random.Next(0, 4); // Случайный выбор эффекта

            switch (effectIndex)
            {
                case 0: // Saturation
                    float saturationAmount = random.Next(0, 11) / 10f; // Значение от 0.0 до 1.0
                    effectManager.SetSaturation(saturationAmount);
                    trackBar7.Value = (int)(saturationAmount * 10); // Обновляем ползунок
                    Console.WriteLine($"Applied Saturation: {saturationAmount}");
                    break;

                case 1: // Reverb
                    float roomSize = random.Next(0, 11) / 10f;
                    float damp = random.Next(0, 11) / 10f;
                    float wetDryMix = random.Next(0, 11) / 10f;
                    effectManager.SetReverb(roomSize, damp, wetDryMix);
                    // Обновите ползунки для реверберации здесь
                    trackBar11.Value = (int)(roomSize * 10);
                    trackBar14.Value = (int)(damp * 10);
                    trackBar15.Value = (int)(wetDryMix * 10);
                    Console.WriteLine($"Applied Reverb: RoomSize={roomSize}, Damp={damp}, WetDryMix={wetDryMix}");
                    break;

                case 2: // Chorus
                    float chorusRate = random.Next(0, 11) / 10f;
                    float chorusDepth = random.Next(0, 11) / 10f;
                    effectManager.SetChorus(chorusRate, chorusDepth, Clamp(trackBar12.Value / 10f, 0f, 1f));
                    trackBar8.Value = (int)(chorusRate * 10);
                    trackBar9.Value = (int)(chorusDepth * 10);
                    Console.WriteLine($"Applied Chorus: Rate={chorusRate}, Depth={chorusDepth}");
                    break;

                case 3: // Delay
                    float delayTime = random.Next(0, 5001) / 1000f; // Значение от 0.0 до 5.0
                    float feedback = random.Next(0, 101) / 100f; // Значение от 0.0 до 1.0
                    effectManager.SetDelay(delayTime, feedback);
                    trackBar13.Value = (int)(delayTime * 1000); // Обновляем ползунок
                    trackBar10.Value = (int)(feedback * 100); // Обновляем ползунок
                    Console.WriteLine($"Applied Delay: Time={delayTime}, Feedback={feedback}");
                    break;
            }

            UpdatePlayback(); // Обновляем воспроизведение после применения эффекта
        }



        private void button11_Click(object sender, EventArgs e)
        {
            ApplyRandomEffect();
        }


        private void UpdateWaveform()
        {
            if (audioFileReader != null)
            {
                // Перерисовываем волну на основе текущих аудиоданных
                DrawWaveform(audioFileReader.FileName);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (selection.IsFixed)
            {
                audioEditor.CopySelectedAudio();
            }
        }

        private int sampleRate = 44100;
        private void button9_Click(object sender, EventArgs e)
        {
            // Диалог для выбора места вставки
            var result = MessageBox.Show("Выберите место вставки:\n1. В начало\n2. В середину\n3. В конец",
                                           "Выбор места вставки",
                                           MessageBoxButtons.YesNoCancel);

            int insertionPoint;

            if (result == DialogResult.Yes) // В начало
            {
                insertionPoint = 0; // Вставка в начало
            }
            else if (result == DialogResult.No) // В середину
            {
                // Получаем общее количество выборок
                insertionPoint = sampleRate / 2; // Вставка в середину (можно изменить по необходимости)
            }
            else if (result == DialogResult.Cancel) // В конец
            {
                // Используем общее количество выборок
                insertionPoint = sampleRate; // Вставка в конец
            }
            else
            {
                return; // Если пользователь закрыл диалог, ничего не делаем
            }

            audioEditor.InsertFragment(insertionPoint);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            audioFileReader?.Dispose();
        }

        public void DrawWaveform(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Файл не найден: " + filePath);
                return;
            }

            using (var reader = new AudioFileReader(filePath))
            {

                int width = pictureBox1.Width;
                int height = pictureBox1.Height;
                Bitmap bitmap = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.White);
                    float[] buffer = new float[reader.WaveFormat.SampleRate];
                    int read;
                    int x = 0;
                    int totalSamples = (int)reader.Length / sizeof(float);
                    int samplesPerPixel = totalSamples / width;

                    int padding = 10;
                    int effectiveHeight = height - 2 * padding;

                    while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < read; i++)
                        {
                            int pixelIndex = x / samplesPerPixel;
                            if (pixelIndex >= width) break;

                            float sample = buffer[i];
                            int y = (int)((sample + 1) * effectiveHeight / 2);
                            y = Math.Max(0, Math.Min(effectiveHeight, y));
                            bitmap.SetPixel(pixelIndex, padding + effectiveHeight - y, Color.Red);
                        }
                        x += read;
                    }
                }
                pictureBox1.Image = bitmap;
            }
        }

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            selection.BeginSelection(e.Location);
            pictureBox1.Invalidate();
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (selection.IsActive)
            {
                selection.UpdateSelection(e.Location);
                pictureBox1.Invalidate();
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            selection.UpdateSelection(MousePosition); // Update the selection end position
            selection.FixSelection(); // Fix the selection immediately
            selection.EndSelection(); // End the selection process
            pictureBox1.Invalidate();
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            selection.Draw(e.Graphics);
        }




        private void button6_Click(object sender, EventArgs e)
        {
            if (selection.IsFixed)
            {
                audioEditor.CopySelectedAudio(); // Сначала копируем выделенное
                audioEditor.RemoveSelectedAudio(); // Затем удаляем выделенное
            }
        }
    }
}



