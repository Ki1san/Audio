using System.Collections.Generic;
using NAudio.Wave;
using System.Windows.Forms; // Importing Windows Forms namespace

public class AudioDevice
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class AudioDeviceManager
{
    public List<AudioDevice> InputDevices { get; private set; }
    public List<AudioDevice> OutputDevices { get; private set; }

    public AudioDeviceManager(RadioButton radioButton1, RadioButton radioButton2)
    {
        InputDevices = new List<AudioDevice>();
        OutputDevices = new List<AudioDevice>();

        // Populate input devices
        for (int i = 0; i < WaveIn.DeviceCount; i++)
        {
            var deviceInfo = WaveIn.GetCapabilities(i);
            InputDevices.Add(new AudioDevice { Id = i, Name = deviceInfo.ProductName });
        }

        // Populate output devices
        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            var deviceInfo = WaveOut.GetCapabilities(i);
            OutputDevices.Add(new AudioDevice { Id = i, Name = deviceInfo.ProductName });
        }

        // Set default device names in radio buttons
        if (InputDevices.Count > 0)
            radioButton1.Text = InputDevices[0].Name;

        if (OutputDevices.Count > 0)
            radioButton2.Text = OutputDevices[0].Name;
    }
}