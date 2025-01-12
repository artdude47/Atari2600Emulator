using Atari2600Emulator.Core;

namespace Atari2600Emulator
{
    public partial class Form1 : Form
    {
        private AtariMemory _memory;
        private CPU6507 _cpu;

        public Form1()
        {
            InitializeComponent();
            InitializeEmulator();
        }

        private void InitializeEmulator()
        {
            _memory = new AtariMemory(new byte[4096]);
            _cpu = new CPU6507(_memory);
        }

        private void loadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Atari 2600 ROMs (*.bin;*.a26)|*.bin;*.a26|All Files (*.*)|*.*";
                openFileDialog.Title = "Load ROM";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] romData = File.ReadAllBytes(openFileDialog.FileName);

                        ResetEmulator(romData);

                        Console.WriteLine("ROM loaded successfully!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to load ROM: {ex.Message}", "Error");
                    }
                }
            }
        }

        private void ResetEmulator(byte[] romData)
        {
            //Reset the memory and CPU
            _memory.LoadROM(romData);

            _cpu.Reset();
        }
    }
}
