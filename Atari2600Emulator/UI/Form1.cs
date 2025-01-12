using Atari2600Emulator.Core;

namespace Atari2600Emulator
{
    public partial class Form1 : Form
    {
        private AtariMemory _memory;
        private CPU6507 _cpu;
        private Bitmap _bitmap;

        public Form1()
        {
            InitializeComponent();
            RedirectConsoleOutput();
            InitializeEmulator();
            InitializeVideo();
        }

        private void RedirectConsoleOutput()
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true,
            });
            Console.SetError(Console.Out);
        }

        private void InitializeEmulator()
        {
            _memory = new AtariMemory(new byte[4096]);
            _cpu = new CPU6507(_memory);
        }

        private void InitializeVideo()
        {
            _bitmap = new Bitmap(160, 192);
            pictureBox1.Image = _bitmap;
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
                         MessageBox.Show($"Success!", "Success");


                         ExecuteROM();
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

        private void ExecuteROM()
        {
            const int cyclesPerScanline = 76;
            const int scanlinesPerFrame = 262;

            for (int i = 0; i < scanlinesPerFrame; i++)
            {
                for (int j = 0; j < cyclesPerScanline && !_cpu.Halted; j++)
                {
                    _cpu.Step();
                }
            }
            RenderVideo();
        }

        private byte[] GenerateTestROM()
        {
            return new byte[]
            {
        // Instructions (Program starts at $F000)
        0xA9, 0x10,       // LDA #$10
        0x8D, 0x00, 0x10, // STA $1000
        0xA5, 0x10,       // LDA $10
        0x69, 0x05,       // ADC #$05
        0xE9, 0x03,       // SBC #$03
        0xF0, 0x02,       // BEQ $F00F
        0xA9, 0x01,       // LDA #$01
        0xD0, 0x02,       // BNE $F013
        0xA9, 0x00,       // LDA #$00
        0x90, 0x02,       // BCC $F017
        0xA9, 0xFF,       // LDA #$FF
        0x30, 0x02,       // BMI $F01B
        0xA9, 0x80,       // LDA #$80
        0x10, 0x02,       // BPL $F01F
        0xA9, 0x7F,       // LDA #$7F
        0x00,             // BRK

        // Padding (Fill to $FFFC)
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,

        // Reset Vector (Points to $F000)
        0x00, 0xF0
            };
        }

        private void RenderVideo()
        {
            for (int y = 0; y < 192; y++)
            {
                for (int x = 0; x < 160; x++)
                {
                    byte color = _memory.VideoBuffer[y * 160 + x];
                    Color pixelColor = Color.FromArgb(color, color, color);
                    _bitmap.SetPixel(x, y, pixelColor);
                }
            }

            pictureBox1.Refresh();
        }

    }
}
