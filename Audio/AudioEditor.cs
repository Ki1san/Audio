using Audio;
using NAudio.Wave;
using SoundFingerprinting.Audio;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

public class AudioEditor
{
    private Form1 mainForm;
    private string audioFilePath;
    private PictureBox pictureBox1;
    private Rectangle selectionRectangle = new Rectangle();
    private byte[] copiedFragment; // Для хранения скопированного фрагмента
    private int bytesPerSample;
    private int sampleRate = 44100; // Частота дискретизации по умолчанию
    private bool isSelecting = false;
    private Point startPoint; // Начальная точка выделения


    private Selection selection;
    private bool isCopied;
    private float[] audioSamples; // Массив аудиоданных
    private float[] clipboardSamples; // Буфер для хранения скопированных аудиоданных

    public AudioEditor(PictureBox pictureBox)
    {
        pictureBox1 = pictureBox;
   
        // Привязываем методы с правильной сигнатурой
       selection = new Selection();
     
        pictureBox1.Focus(); // Устанавливаем фокус на PictureBox
    }

    public void LoadAudioFile(string filePath)
    {
        audioFilePath = filePath;

        using (var audioFileReader = new AudioFileReader(filePath))
        {
            bytesPerSample = audioFileReader.WaveFormat.BitsPerSample / 8 * audioFileReader.WaveFormat.Channels;
            sampleRate = audioFileReader.WaveFormat.SampleRate; // Получаем частоту дискретизации из файла
        }
    }

    private void StartSelection(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) // Проверяем, что нажата левая кнопка мыши
        {
            isSelecting = true;
            startPoint = e.Location; // Сохраняем начальную точку выделения
            selectionRectangle.Location = startPoint; // Устанавливаем начальную точку выделения
            selectionRectangle.Size = new Size(0, 0); // Начальный размер равен 0
        }
    }

    private void UpdateSelection(object sender, MouseEventArgs e)
    {
        if (isSelecting) // Если выделение активно
        {
            int width = e.X - startPoint.X;
            int height = e.Y - startPoint.Y;

            // Обновляем размеры выделения
            selectionRectangle.Size = new Size(width, height);
            pictureBox1.Invalidate(); // Перерисовываем PictureBox
        }
    }

    private void EndSelection(object sender, MouseEventArgs e)
    {
        if (isSelecting)
        {
            UpdateSelection(sender, e); // Обновляем размер выделения
            isSelecting = false; // Завершаем выделение
        }
    }

  
    


    public void CopySelectedAudio()
    {
        int startSampleIndex = (int)(selection.Start.X * audioSamples.Length / pictureBox1.Width);
        int endSampleIndex = (int)(selection.End.X * audioSamples.Length / pictureBox1.Width);

        if (startSampleIndex < endSampleIndex && endSampleIndex < audioSamples.Length)
        {
            int length = endSampleIndex - startSampleIndex;
            clipboardSamples = new float[length];
            Array.Copy(audioSamples, startSampleIndex, clipboardSamples, 0, length);
            isCopied = true; // Устанавливаем флаг, что данные скопированы
        }
    }

    public void InsertFragment(int insertionPoint)
    {
        if (copiedFragment == null || copiedFragment.Length == 0)
        {
            MessageBox.Show("Сначала скопируйте фрагмент.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using (var reader = new AudioFileReader(audioFilePath))
        using (var newFileStream = new MemoryStream())
        {
            var newWaveFormat = reader.WaveFormat;
            var writer = new WaveFileWriter(newFileStream, newWaveFormat);

            byte[] buffer = new byte[4096];
            int bytesRead;

            // Записываем данные до точки вставки
            int currentSample = 0;
            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < bytesRead; i += bytesPerSample)
                {
                    if (currentSample < insertionPoint)
                    {
                        writer.Write(buffer, i, Math.Min(bytesRead - i, bytesPerSample));
                    }
                    currentSample++;
                }

                // Если дошли до точки вставки, вставляем скопированный фрагмент
                if (currentSample == insertionPoint)
                {
                    writer.Write(copiedFragment, 0, copiedFragment.Length);
                }
            }

            // Записываем оставшиеся данные
            writer.Flush();
            writer.Dispose();

            // Сохраняем новый файл
            string newFilePath = Path.Combine(Path.GetDirectoryName(audioFilePath), "inserted_" + Path.GetFileName(audioFilePath));
            File.WriteAllBytes(newFilePath, newFileStream.ToArray());

            MessageBox.Show($"Фрагмент вставлен. Новый файл: {newFilePath}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public void RemoveSelectedAudio()
    {
        int startSampleIndex = (int)(selection.Start.X * audioSamples.Length / pictureBox1.Width);
        int endSampleIndex = (int)(selection.End.X * audioSamples.Length / pictureBox1.Width);
        int cutLength = endSampleIndex - startSampleIndex;

        if (cutLength > 0 && endSampleIndex <= audioSamples.Length)
        {
            float[] newSamples = new float[audioSamples.Length - cutLength];
            Array.Copy(audioSamples, 0, newSamples, 0, startSampleIndex);
            Array.Copy(audioSamples, endSampleIndex, newSamples, startSampleIndex, audioSamples.Length - endSampleIndex);
            audioSamples = newSamples;

            // Сохранение нового аудиофайла без выделенного фрагмента
            string newFilePath = "path_to_your_temp_file.wav"; // Укажите путь к временно сохраненному файлу
            SaveAudioToFile(newFilePath, audioSamples);

            // Перерисовываем волну с новым файлом
            mainForm.DrawWaveform(newFilePath);
        }
    }

   
    private void SaveAudioToFile(string filePath, float[] audioData)
    {
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            using (var waveFileWriter = new WaveFileWriter(fileStream, new NAudio.Wave.WaveFormat(44100, 1))) // Укажите нужные параметры формата
            {
                waveFileWriter.WriteSamples(audioData, 0, audioData.Length);
            }
        }
    }

}
