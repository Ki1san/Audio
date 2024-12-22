using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Lame;

namespace Audio
{
    public class AudioRecorder
    {
        private WaveInEvent waveIn;
        private LameMP3FileWriter writer;
        private string outputFileName;
        private PictureBox pictureBox2;
        private int selectedDevice;

        public AudioRecorder(PictureBox pictureBox2, string outputFileName, int selectedDevice)
        {
            this.pictureBox2 = pictureBox2;
            this.outputFileName = outputFileName;
            this.selectedDevice = selectedDevice;
        }

        public void StartRecording()
        {
            isRecording = true;

            waveIn = new WaveInEvent
            {
                DeviceNumber = selectedDevice,
                WaveFormat = new WaveFormat(44100, 1)
            };

            waveIn.DataAvailable += PutData;
            waveIn.RecordingStopped += (sender, e) => StopRecording(); // Изменение здесь

            writer = new LameMP3FileWriter(outputFileName, waveIn.WaveFormat, LAMEPreset.STANDARD);
            waveIn.StartRecording();
        }

        private void PutData(object sender, WaveInEventArgs e)
        {
            if (writer != null)
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
                DrawWaveform(e.Buffer, e.BytesRecorded);
            }
        }

        private bool isRecording = false; // Флаг для отслеживания состояния записи

        private void DrawWaveform(byte[] buffer, int bytesRecorded)
        {
            int samples = bytesRecorded / 2; // Поскольку это моно
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

                // Рисуем ось
                g.DrawLine(Pens.Black, 0, padding + effectiveHeight / 2, width, padding + effectiveHeight / 2);

                // Если запись началась, рисуем звуковую волну или линию
                if (isRecording)
                {
                    for (int i = 1; i < samples; i++)
                    {
                        float sample1 = floatBuffer[i - 1];
                        float sample2 = floatBuffer[i];

                        int x1 = (int)((i - 1) / (float)samples * width);
                        int y1 = (int)((sample1 + 1) * effectiveHeight / 2);
                        y1 = Math.Max(0, Math.Min(effectiveHeight, y1));

                        int x2 = (int)(i / (float)samples * width);
                        int y2 = (int)((sample2 + 1) * effectiveHeight / 2);
                        y2 = Math.Max(0, Math.Min(effectiveHeight, y2));

                        g.DrawLine(Pens.Blue, x1, padding + effectiveHeight - y1, x2, padding + effectiveHeight - y2);
                    }
                }
                else
                {
                    // Если запись не началась, рисуем линию
                    g.DrawLine(Pens.Blue, 0, padding + effectiveHeight / 2, width, padding + effectiveHeight / 2);
                }
            }

            pictureBox2.Image = bitmap; // Обновляем изображение в pictureBox
        }

        public void StopRecording()
        {
            isRecording = false;
            waveIn?.StopRecording();
        }

        public void Dispose()
        {
            waveIn?.Dispose();
            writer?.Close();
            writer?.Dispose();
            waveIn = null;
            writer = null;
        }
    }
}

