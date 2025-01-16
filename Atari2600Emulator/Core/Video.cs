using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atari2600Emulator.Core
{
    internal class Video
    {
        private readonly AtariMemory _memory;
        private Bitmap _bitmap;
        private PictureBox _pictureBox;
        private readonly int _width = 160;
        private readonly int _height = 192;

        public Video(AtariMemory memory, PictureBox pictureBox)
        {
            _memory = memory;
            _pictureBox = pictureBox;
            InitializeBitmap();

            _memory.OnTIARegisterWrite += HandleTIARegisterWrite;
        }

        private void InitializeBitmap()
        {
            _bitmap = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            _pictureBox.Image = _bitmap;
            _pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        private void HandleTIARegisterWrite(ushort address, byte value)
        {
            switch (address)
            {
                case 0x0000: // GRP0 - Playfield graphics fro player 0
                case 0x0001: // GRP1 - Playfield graphics for player 1
                    UpdatePlayfield(address, value);
                    break;

                default:
                    break;
            }

            RenderVideo();
        }

        private void UpdatePlayfield(ushort address, byte value)
        {
            // Determine what playfield is being updated
            int player = (address == 0x0000) ? 0 : 1;

            //Each bit in the value represents a pixel
            for (int i = 0; i < 8; i++)
            {
                bool pixelOn = (value & (1 << i)) != 0;
                int x = player * 80 + i;
                int y = GetCurrentScanLine();

                if (x >= 0 && x < _width && y >= 0 && y < _height)
                {
                    Color color = pixelOn ? Color.White : Color.Black;
                    _bitmap.SetPixel(x, y, color);
                }
            }
        }

        private int GetCurrentScanLine()
        {
            return 0;
        }

        private void RenderVideo()
        {
            if (_pictureBox.InvokeRequired)
            {
                _pictureBox.Invoke(new Action(RenderVideo));
                return;
            }

            _pictureBox.Refresh();
        }

        public void ClearVideoBuffer()
        {
            Graphics g = Graphics.FromImage(_bitmap);
            g.Clear(Color.Black);
            g.Dispose();
            RenderVideo();
        }
    }
}
