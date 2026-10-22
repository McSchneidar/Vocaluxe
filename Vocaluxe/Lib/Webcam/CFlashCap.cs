#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FlashCap;
using Vocaluxe.Base;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Webcam
{
    class CFlashCap : IWebcam
    {
        private readonly List<SWebcamDevice> _Devices = new List<SWebcamDevice>();
        private readonly Dictionary<string, CaptureDeviceDescriptor> _Descriptors = new Dictionary<string, CaptureDeviceDescriptor>();
        private bool _Paused;
        private CaptureDeviceDescriptor _Descriptor;
        private VideoCharacteristics _Characteristics;
        private CaptureDevice _Webcam;
        private SWebcamConfig _Config;
        private byte[] _Data;
        private static readonly object _MutexData = new object();
        private int _Width, _Height;
        private bool _NewFrameAvailable;
        private bool _IsCapturing;

        public bool Init()
        {
            try
            {
                var devices = new CaptureDevices();
                foreach (var descriptor in devices.EnumerateDescriptors())
                {
                    var characteristics = _GetUsableCharacteristics(descriptor);
                    if (characteristics.Length == 0)
                    {
                        continue;
                    }

                    var moniker = descriptor.Identity?.ToString() ?? descriptor.Name;
                    if (_Descriptors.ContainsKey(moniker))
                    {
                        continue;
                    }

                    var device = new SWebcamDevice
                    {
                        Name = descriptor.Name,
                        MonikerString = moniker,
                        Capabilities = characteristics.Select(c => new SCapabilities
                        {
                            Framerate = _GetFramerate(c),
                            Height = c.Height,
                            Width = c.Width
                        }).ToList()
                    };

                    _Descriptors.Add(moniker, descriptor);
                    _Devices.Add(device);
                }
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error enumerating webcams");
            }

            return true;
        }

        public void Close()
        {
            DeSelect();
            _Devices.Clear();
            _Descriptors.Clear();
        }

        public void DeSelect()
        {
            if (_Descriptor == null)
            {
                return;
            }

            _CloseDevice();
            lock (_MutexData)
            {
                _IsCapturing = false;
                _Data = null;
                _Descriptor = null;
                _Characteristics = null;
            }
        }

        #region ConfigSelection
        private static VideoCharacteristics[] _GetUsableCharacteristics(CaptureDeviceDescriptor descriptor)
        {
            return descriptor.Characteristics.Where(c => c.PixelFormat != PixelFormats.Unknown).ToArray();
        }

        private static int _GetFramerate(VideoCharacteristics characteristics)
        {
            var fps = characteristics.FramesPerSecond;
            return fps.Denominator == 0 ? 0 : (int)Math.Round((double)fps.Numerator / fps.Denominator);
        }

        private static float _GetScore(int value, int valueRequested)
        {
            if (valueRequested <= 0)
            {
                return 1;
            }

            return 1.0f - (float)Math.Abs(value - valueRequested) / Math.Max(value, valueRequested);
        }

        private static VideoCharacteristics _SelectWebcamConfig(VideoCharacteristics[] capabilities, SWebcamConfig config)
        {
            var configTaken = 0;
            if (config.Framerate != 0 && config.Height != 0 && config.Width != 0)
            {
                float bestMatchScore = 0;
                for (var i = 0; i < capabilities.Length; i++)
                {
                    var score = _GetScore(_GetFramerate(capabilities[i]), config.Framerate);
                    score += _GetScore(capabilities[i].Height, config.Height);
                    score += _GetScore(capabilities[i].Width, config.Width);
                    if (score >= bestMatchScore)
                    {
                        // Take the config with the best score, Higher indizes should indicate a better resolution
                        // so choose the last one with best score if 2 have equal scores
                        bestMatchScore = score;
                        configTaken = i;
                    }
                }
            }

            return capabilities[configTaken];
        }
        #endregion

        public bool Select(SWebcamConfig config)
        {
            //Close old camera connection
            DeSelect();

            if (_Devices.Count < 1)
            {
                return false;
            }

            var moniker = _Devices[0].MonikerString;
            if (!string.IsNullOrEmpty(config.MonikerString) && _Descriptors.ContainsKey(config.MonikerString))
            {
                moniker = config.MonikerString;
            }

            _Descriptor = _Descriptors[moniker];
            _Characteristics = _SelectWebcamConfig(_GetUsableCharacteristics(_Descriptor), config);

            _Config.Framerate = _GetFramerate(_Characteristics);
            _Config.Height = _Characteristics.Height;
            _Config.Width = _Characteristics.Width;
            _Config.MonikerString = moniker;

            return true;
        }

        public void Start()
        {
            if (_Descriptor == null)
            {
                return;
            }

            if (_IsCapturing)
            {
                _Paused = false;
                return;
            }

            try
            {
                _Webcam = _Descriptor.OpenAsync(_Characteristics, _OnFrame).GetAwaiter().GetResult();
                _Paused = false;
                _Webcam.StartAsync().GetAwaiter().GetResult();
                _IsCapturing = true;
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error starting webcam " + _Descriptor.Name);
                _CloseDevice();
            }
        }

        public void Pause()
        {
            if (_Webcam != null && !_Paused)
            {
                lock (_MutexData) //Make sure all events are finished or not yet startet for consistency
                {
                    _Paused = true;
                }
            }
        }

        public void Stop()
        {
            if (_Webcam != null)
            {
                lock (_MutexData) //Make sure all events are finished or not yet startet for consistency
                {
                    _IsCapturing = false;
                }

                _CloseDevice();
            }
        }

        private void _CloseDevice()
        {
            var webcam = _Webcam;
            _Webcam = null;
            if (webcam == null)
            {
                return;
            }

            try
            {
                webcam.StopAsync().GetAwaiter().GetResult();
                webcam.Dispose();
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error stopping webcam");
            }
        }

        public bool GetFrame(ref CTextureRef frame)
        {
            lock (_MutexData)
            {
                if (_Data != null && _Data.Length == _Width * _Height * 4 && _NewFrameAvailable)
                {
                    if (frame == null)
                    {
                        frame = CDraw.AddTexture(_Width, _Height, _Data);
                    }
                    else
                    {
                        CDraw.UpdateTexture(frame, _Width, _Height, _Data);
                    }

                    _NewFrameAvailable = false;
                    return true;
                }

                return false;
            }
        }

        public Bitmap GetBitmap()
        {
            lock (_MutexData)
            {
                if (_Data != null && _Data.Length == _Width * _Height * 4)
                {
                    var bmp = new Bitmap(_Width, _Height);
                    var bitmapdata = bmp.LockBits(new Rectangle(0, 0, _Width, _Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(_Data, 0, bitmapdata.Scan0, _Data.Length);
                    bmp.UnlockBits(bitmapdata);
                    return bmp;
                }

                return null;
            }
        }

        private void _OnFrame(PixelBufferScope bufferScope)
        {
            if (_Paused || !IsCapturing())
            {
                return;
            }

            var image = bufferScope.Buffer.ExtractImage();
            bufferScope.ReleaseNow();

            Bitmap frame;
            try
            {
                frame = new Bitmap(new MemoryStream(image));
            }
            catch (ArgumentException)
            {
                return;
            }

            using (frame)
            {
                if (frame.Width == 0 || frame.Height == 0)
                {
                    return;
                }

                lock (_MutexData)
                {
                    if (!IsCapturing())
                    {
                        return;
                    }

                    if (_Data == null || _Data.Length != frame.Width * frame.Height * 4)
                    {
                        _Data = new byte[frame.Width * frame.Height * 4];
                    }

                    _Width = frame.Width;
                    _Height = frame.Height;
                    var bitmapdata = frame.LockBits(new Rectangle(0, 0, _Width, _Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(bitmapdata.Scan0, _Data, 0, _Data.Length);
                    frame.UnlockBits(bitmapdata);
                    _NewFrameAvailable = true;
                }
            }
        }

        public SWebcamDevice[] GetDevices()
        {
            return _Devices.ToArray();
        }

        public bool IsDeviceAvailable()
        {
            return _Descriptor != null;
        }

        public bool IsCapturing()
        {
            return _IsCapturing && !_Paused;
        }

        public SWebcamConfig GetConfig()
        {
            return _Config;
        }
    }
}
