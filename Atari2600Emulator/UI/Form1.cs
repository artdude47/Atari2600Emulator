using Atari2600Emulator.Core;
using System.Threading;

namespace Atari2600Emulator
{
    public partial class Form1 : Form
    {
        private AtariMemory _memory;
        private CPU6507 _cpu;
        private Bitmap _bitmap;
        private System.Windows.Forms.Timer _frameTimer;

        private const int CyclesPerScanline = 76;
        private const int ScanlinesPerFrame = 262;

        public Form1()
        {
            InitializeComponent();
            InitializeEmulator();
            InitializeVideo();

            _frameTimer = new System.Windows.Forms.Timer();
            _frameTimer.Interval = 1000 / 60;
            _frameTimer.Tick += FrameTimer_Tick;
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

                        _frameTimer.Start();
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

        private void RenderVideo()
        {
            for (int y = 0; y < 192; y++)
            {
                for (int x = 0; x < 160; x++)
                {
                    byte colorVal = _memory.VideoBuffer[y * 160 + x];

                    Color c = Color.FromArgb(colorVal, colorVal, colorVal);
                    _bitmap.SetPixel(x, y, c);
                }
            }

            pictureBox1.Refresh();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void FrameTimer_Tick(object sender, EventArgs e)
        {
            if (_cpu == null || _memory == null) return;

            for (int scanline = 0; scanline < ScanlinesPerFrame; scanline++)
            {
                for (int c = 0; c < CyclesPerScanline && !_cpu.Halted; c++)
                {
                    _cpu.Step();
                }

                DrawScanline(scanline);
            }

            RenderVideo();
        }

        private void DrawScanline(int scanline)
        {
            // Only draw the "visible" scanlines 0..191 (some games use more)
            if (scanline < 0 || scanline >= 192) return;

            // 1) Read TIA registers we need
            //    These addresses match typical docs, but confirm with your code
            byte colubk = _memory.ReadByte(0x0009);  // background color
            byte colupf = _memory.ReadByte(0x0008);  // playfield color
            byte ctrlpf = _memory.ReadByte(0x000A);  // mirroring, etc.

            byte pf0 = _memory.ReadByte(0x000D);     // 4 bits (left) reversed
            byte pf1 = _memory.ReadByte(0x000E);     // 8 bits (middle)
            byte pf2 = _memory.ReadByte(0x000F);     // 8 bits (right)

            // OPTIONAL: Simple players (8 bits each)
            byte grp0 = _memory.ReadByte(0x001B);    // Player 0 bits
            byte grp1 = _memory.ReadByte(0x001C);    // Player 1 bits
            byte colup0 = _memory.ReadByte(0x0006);  // Player 0 color
            byte colup1 = _memory.ReadByte(0x0007);  // Player 1 color

            // 2) Convert PF0, PF1, PF2 into a 20-bit pattern:
            //    The "standard" layout is:
            //      PF0: bits (4..7) => left side, reversed
            //      PF1: bits (0..7) => middle
            //      PF2: bits (0..7) => right side
            //    Then mirrored if ctrlpf's bit 0 (REFP0) or bit 1 is set (depending on score mode, etc.).
            //    For simplicity, let's do standard single-line mirror check:
            bool reflect = (ctrlpf & 0x01) != 0; // If bit 0 of CTRLPF is set => mirror playfield

            // PF0 is only 4 bits: take top 4 bits
            // (on hardware, the 4 bits are reversed when displayed)
            // e.g. if PF0 = 0b11110000 => it represents 0b00001111 on screen for the left side.
            // We'll create a 20-bit integer "playfieldBits" (bits 19..0).
            ushort playfieldBits = 0;

            // Left 4 bits from PF0
            // Real 2600 usage: PF0’s high nibble => left side reversed
            // For a quick approach, shift PF0 >> 4:
            // If PF0 = 0xF0 (1111_0000), then the nibble is 0x0F (0000_1111) = 15 decimal
            byte leftNibble = (byte)((pf0 & 0xF0) >> 4); // 0..15
                                                         // Insert that nibble into the top bits of playfieldBits (bits 19..16)
            playfieldBits |= (ushort)((leftNibble & 0x0F) << 16);

            // Next 8 bits from PF1 go into bits 15..8
            // If PF1 = 0xAA (10101010), that's bits 15..8
            playfieldBits |= (ushort)(pf1 << 8);

            // Final 8 bits from PF2 go into bits 7..0
            playfieldBits |= pf2;

            // 3) For each pixel x in [0..159], decide the color
            for (int x = 0; x < 160; x++)
            {
                // Start with background color
                byte pixelColor = colubk;

                // Convert x into "playfield index" 0..39
                // Each playfield pixel is 4 actual pixels wide => 40 total across 160.
                int playfieldIndex = x / 4; // integer division

                // 0..19 is the left half, 20..39 is the right half
                // If mirror is OFF, bits 0..19 apply to left half, bits 0..19 apply to right half
                // Actually, the 2600 maps bits [19..0] for the left half, repeated or mirrored for the right half.
                // We'll do a simpler approach:
                //   If NOT mirrored, bits 19..0 => the left 20 columns, repeated for the right 20 columns.
                //   If mirrored, bits 19..0 => the left 20 columns, reversed for the right 20 columns.

                // We'll define a helper function:
                bool playfieldBit = GetPlayfieldBit(playfieldBits, playfieldIndex, reflect);

                // If the playfield bit is 1 => draw with colupf
                if (playfieldBit)
                {
                    pixelColor = colupf;
                }

                // OPTIONAL: overlay player 0/1 in a naive way
                // Suppose we just place GRP0's 8 bits at x=0..7, GRP1's at x=8..15, etc.
                // Real hardware uses RESP0/RESP1 to position them. We'll ignore that for simplicity.

                // player 0 is 8 bits in GRP0 => if bit is set, show colup0
                // let's place it at x=0..7
                if (x < 8)
                {
                    // check bit (7 - x) in grp0, because bit 7 is leftmost on screen
                    // (the 2600 actually flips them, but let's do simplest approach)
                    int shift = 7 - x; // so x=0 => we check bit7
                    if (((grp0 >> shift) & 1) == 1)
                    {
                        pixelColor = colup0;
                    }
                }

                // player 1 is 8 bits in GRP1 => place it at x=16..23
                if (x >= 16 && x < 24)
                {
                    int shift = 23 - x; // so x=16 => check bit7
                    if (((grp1 >> shift) & 1) == 1)
                    {
                        pixelColor = colup1;
                    }
                }

                // Finally, store the pixel in the video buffer
                _memory.VideoBuffer[scanline * 160 + x] = pixelColor;
            }
        }

        /// <summary>
        /// Given the combined 20-bit playfield bits (bits [19..0]),
        /// the pixel index 0..39 across a scanline,
        /// and a 'reflect' flag, returns whether that bit is 1 or 0.
        ///
        /// For a typical 2600 arrangement:
        ///  - Bits [19..0] is the "left" half (20 bits),
        ///  - The right half is either repeated or mirrored.
        ///  - If 'reflect' is false: the left 20 bits are repeated to the right side
        ///  - If 'reflect' is true: the left 20 bits are mirrored to the right side
        /// </summary>
        private bool GetPlayfieldBit(ushort playfieldBits, int playfieldIndex, bool reflect)
        {
            // playfieldIndex: 0..39
            // left half: 0..19
            // right half: 20..39

            if (playfieldIndex < 0 || playfieldIndex > 39)
                return false; // out of range

            int half = (playfieldIndex < 20) ? 0 : 1; // left=0, right=1
            int indexInHalf = playfieldIndex % 20;    // 0..19

            if (half == 1) // right side
            {
                if (reflect)
                {
                    // mirror the bits
                    // so if indexInHalf=0 => bit19, indexInHalf=19 => bit0
                    indexInHalf = 19 - indexInHalf;
                }
                // if reflect= false => we use the same indexInHalf
            }

            // So overall bit index = 19..0
            // bit19 is the leftmost, bit0 is the rightmost
            int bitPosition = 19 - indexInHalf; // so indexInHalf=0 => check bit19

            // extract that bit from playfieldBits
            return ((playfieldBits >> bitPosition) & 1) == 1;
        }

    }
}
