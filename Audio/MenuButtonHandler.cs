using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using VGAudio.Formats;

namespace Audio
{
    public class MenuButtonHandler
    {
        private Form1 mainForm;
        private AudioEditor audioEditor;

        public MenuButtonHandler(Form1 form)
        {
            this.mainForm = form;
            this.audioEditor = new AudioEditor(form.PictureBox1);

        }

        public void OpenFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav|All Files (*.*)|*.*";
                openFileDialog.Title = "Выберите аудиофайл для открытия";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    // Обновляем интерфейс, чтобы отобразить загруженные данные
                    mainForm.DrawWaveform(filePath);
                    mainForm.PlayAudio(filePath);
                }
            }
        }



        public void SaveFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "MP3 Files (*.mp3)|*.mp3|All Files (*.*)|*.*",
                Title = "Сохраните MP3 файл как",
                FileName = "output.mp3" // Установите имя по умолчанию
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string outputFilePath = saveFileDialog.FileName;

                try
                {
                    // Предполагаем, что у вас есть переменная outputFileName в Form1
                    if (File.Exists(mainForm.OutputFileName))
                    {
                        File.Copy(mainForm.OutputFileName, outputFilePath, true);
                        MessageBox.Show($"Файл сохранен как '{outputFilePath}'.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Файл для сохранения не существует.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void ClearPictureBox_Click(object sender, EventArgs e)
        {
            mainForm.ClearAudioAndImage(); // Вызов метода из Form1
        }

        // Метод для импорта аудиофайлов
        public void ImportAudioFile(string filePath)
        {
            try
            {
                // Логика для импорта файла (например, просто открыть файл)
                if (File.Exists(filePath))
                {
                    MessageBox.Show($"Аудиофайл '{filePath}' импортирован.", "Импорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    mainForm.DrawWaveform(filePath);
                    mainForm.PlayAudio(filePath);
                }
                else
                {
                    MessageBox.Show("Файл не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Метод для экспорта аудиофайлов
        public void ExportAudioFile(string outputPath)
        {
            try
            {
                // Здесь можно добавить логику для экспорта (например, просто копирование)
                if (!string.IsNullOrEmpty(mainForm.OutputFileName) && File.Exists(mainForm.OutputFileName))
                {
                    File.Copy(mainForm.OutputFileName, outputPath, true);
                    MessageBox.Show($"Аудиофайл экспортирован в '{outputPath}'.", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Нет доступного аудиофайла для экспорта.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}