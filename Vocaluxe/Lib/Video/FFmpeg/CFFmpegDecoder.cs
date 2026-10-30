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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;

namespace Vocaluxe.Lib.Video.FFmpeg
{
    enum EFFmpegStreamType
    {
        Video,
        Audio
    }

    unsafe class CFFmpegDecoder : IDisposable
    {
        private const AVPixelFormat _OutputPixelFormat = AVPixelFormat.AV_PIX_FMT_BGRA;
        private const AVSampleFormat _OutputSampleFormat = AVSampleFormat.AV_SAMPLE_FMT_S16;

        private static readonly AVRational _TimeBaseQ = new AVRational { num = 1, den = ffmpeg.AV_TIME_BASE };

        private readonly object _Lock = new object();
        private readonly EFFmpegStreamType _Type;
        private AVFormatContext* _FormatContext;
        private AVCodecContext* _CodecContext;
        private AVPacket* _Packet;
        private AVFrame* _Frame;
        private SwsContext* _SwsContext;
        private SwrContext* _SwrContext;
        private int _StreamIndex = -1;
        private AVRational _TimeBase;
        private bool _EndOfFile;
        private int _BufferCapacity;
        private byte_ptr4 _ImageData;
        private int4 _ImageLineSize;

        public float Length { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public double FramesPerSecond { get; private set; }
        public int SamplesPerSecond { get; private set; }
        public int ChannelCount { get; private set; }
        public double Timecode { get; private set; }
        public IntPtr Buffer { get; private set; }
        public int BufferSize { get; private set; }

        static CFFmpegDecoder()
        {
            DynamicallyLoadedBindings.LibrariesPath = _GetLibrariesPath();
            DynamicallyLoadedBindings.Initialize();
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_QUIET);
        }

        private static string _GetLibrariesPath()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
#if ARCH_X86
            var folder = "ffmpeg86";
#else
            var folder = "ffmpeg64";
#endif
            if (OperatingSystem.IsWindows())
            {
                return Path.Combine(baseDirectory, "libs", "unmanaged", folder);
            }

            var candidates = new[]
            {
                Path.Combine(baseDirectory, "libs", "unmanaged", folder),
                Path.Combine(baseDirectory, "libs", "unmanaged"),
                baseDirectory,
                "/opt/homebrew/opt/ffmpeg@8/lib",
                "/usr/local/opt/ffmpeg@8/lib"
            };
            var library = OperatingSystem.IsMacOS() ? "libavutil.60.dylib" : "libavutil.so.60";
            return candidates.FirstOrDefault(c => File.Exists(Path.Combine(c, library))) ?? "";
        }

        public CFFmpegDecoder(EFFmpegStreamType type)
        {
            _Type = type;
        }

        public bool Open(string fileName)
        {
            lock (_Lock)
            {
                AVFormatContext* formatContext = null;
                if (ffmpeg.avformat_open_input(&formatContext, fileName, null, null) < 0)
                {
                    return false;
                }

                _FormatContext = formatContext;
                if (ffmpeg.avformat_find_stream_info(_FormatContext, null) < 0)
                {
                    _Free();
                    return false;
                }

                Length = _FormatContext->duration > 0 ? _FormatContext->duration * 1000 / ffmpeg.AV_TIME_BASE / 1000f : 0f;

                var mediaType = _Type == EFFmpegStreamType.Video ? AVMediaType.AVMEDIA_TYPE_VIDEO : AVMediaType.AVMEDIA_TYPE_AUDIO;
                for (var i = 0; i < _FormatContext->nb_streams; i++)
                {
                    if (_FormatContext->streams[i]->codecpar->codec_type == mediaType)
                    {
                        _StreamIndex = i;
                        break;
                    }
                }

                if (_StreamIndex < 0 || !_OpenCodec())
                {
                    _Free();
                    return false;
                }

                return true;
            }
        }

        private bool _OpenCodec()
        {
            var stream = _FormatContext->streams[_StreamIndex];
            _TimeBase = stream->time_base;

            var codec = ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id);
            if (codec == null)
            {
                return false;
            }

            _CodecContext = ffmpeg.avcodec_alloc_context3(codec);
            if (_CodecContext == null || ffmpeg.avcodec_parameters_to_context(_CodecContext, stream->codecpar) < 0 ||
                ffmpeg.avcodec_open2(_CodecContext, codec, null) < 0)
            {
                return false;
            }

            _Packet = ffmpeg.av_packet_alloc();
            _Frame = ffmpeg.av_frame_alloc();

            if (_Type == EFFmpegStreamType.Video)
            {
                Width = _CodecContext->width;
                Height = _CodecContext->height;
                if (stream->r_frame_rate.den > 0)
                {
                    FramesPerSecond = ffmpeg.av_q2d(stream->r_frame_rate);
                }

                if (Width <= 0 || Height <= 0)
                {
                    return false;
                }

                _EnsureBuffer(ffmpeg.av_image_get_buffer_size(_OutputPixelFormat, Width, Height, 1));
                BufferSize = _BufferCapacity;
                ffmpeg.av_image_fill_arrays(ref _ImageData, ref _ImageLineSize, (byte*)Buffer, _OutputPixelFormat, Width, Height, 1);
                return true;
            }

            SamplesPerSecond = _CodecContext->sample_rate;
            ChannelCount = _CodecContext->ch_layout.nb_channels;

            AVChannelLayout layout;
            if (_CodecContext->ch_layout.order != AVChannelOrder.AV_CHANNEL_ORDER_UNSPEC)
            {
                ffmpeg.av_channel_layout_copy(&layout, &_CodecContext->ch_layout);
            }
            else
            {
                ffmpeg.av_channel_layout_default(&layout, ChannelCount);
            }

            SwrContext* swrContext = null;
            var result = ffmpeg.swr_alloc_set_opts2(&swrContext, &layout, _OutputSampleFormat, SamplesPerSecond, &layout, _CodecContext->sample_fmt, SamplesPerSecond, 0,
                null);
            ffmpeg.av_channel_layout_uninit(&layout);
            _SwrContext = swrContext;
            return result >= 0 && ffmpeg.swr_init(_SwrContext) >= 0;
        }

        private void _EnsureBuffer(int size)
        {
            if (size <= _BufferCapacity)
            {
                return;
            }

            Buffer = Buffer == IntPtr.Zero ? Marshal.AllocHGlobal(size) : Marshal.ReAllocHGlobal(Buffer, (IntPtr)size);
            _BufferCapacity = size;
        }

        private bool _ReceiveFrame()
        {
            while (true)
            {
                var result = ffmpeg.avcodec_receive_frame(_CodecContext, _Frame);
                if (result >= 0)
                {
                    _UpdateTimecode();
                    return true;
                }

                if (result != ffmpeg.AVERROR(ffmpeg.EAGAIN) || _EndOfFile)
                {
                    return false;
                }

                while (true)
                {
                    if (ffmpeg.av_read_frame(_FormatContext, _Packet) < 0)
                    {
                        _EndOfFile = true;
                        ffmpeg.avcodec_send_packet(_CodecContext, null);
                        break;
                    }

                    var isOwnStream = _Packet->stream_index == _StreamIndex;
                    if (isOwnStream)
                    {
                        ffmpeg.avcodec_send_packet(_CodecContext, _Packet);
                    }

                    ffmpeg.av_packet_unref(_Packet);
                    if (isOwnStream)
                    {
                        break;
                    }
                }
            }
        }

        private void _UpdateTimecode()
        {
            var timestamp = _Frame->best_effort_timestamp;
            if (timestamp != ffmpeg.AV_NOPTS_VALUE)
            {
                Timecode = timestamp * ffmpeg.av_q2d(_TimeBase);
            }
        }

        private bool _ConvertVideoFrame()
        {
            _SwsContext = ffmpeg.sws_getCachedContext(_SwsContext, _Frame->width, _Frame->height, (AVPixelFormat)_Frame->format, Width, Height, _OutputPixelFormat,
                (int)SwsFlags.SWS_BICUBIC, null, null, null);
            if (_SwsContext == null)
            {
                return false;
            }

            return ffmpeg.sws_scale(_SwsContext, _Frame->data.ToArray(), _Frame->linesize.ToArray(), 0, _Frame->height, _ImageData.ToArray(), _ImageLineSize.ToArray()) > 0;
        }

        private bool _ConvertAudioFrame()
        {
            var outSamples = ffmpeg.swr_get_out_samples(_SwrContext, _Frame->nb_samples);
            if (outSamples <= 0)
            {
                BufferSize = 0;
                return true;
            }

            var bytesPerSample = ChannelCount * sizeof(short);
            _EnsureBuffer(outSamples * bytesPerSample);
            var output = (byte*)Buffer;
            var input = _Frame->extended_data;
            var converted = ffmpeg.swr_convert(_SwrContext, &output, outSamples, input, _Frame->nb_samples);
            if (converted < 0)
            {
                return false;
            }

            BufferSize = converted * bytesPerSample;
            return true;
        }

        public bool GetFrame()
        {
            lock (_Lock)
            {
                if (_CodecContext == null || !_ReceiveFrame())
                {
                    return false;
                }

                return _Type == EFFmpegStreamType.Video ? _ConvertVideoFrame() : _ConvertAudioFrame();
            }
        }

        public bool SkipFrames(int count)
        {
            lock (_Lock)
            {
                if (_CodecContext == null)
                {
                    return false;
                }

                for (var i = 0; i < count; i++)
                {
                    if (!_ReceiveFrame())
                    {
                        return false;
                    }
                }

                return _Type != EFFmpegStreamType.Video || _ConvertVideoFrame();
            }
        }

        public bool Seek(bool backward, long targetMilliseconds)
        {
            lock (_Lock)
            {
                if (_CodecContext == null)
                {
                    return false;
                }

                Timecode = targetMilliseconds / 1000.0;
                var position = ffmpeg.av_rescale_q(targetMilliseconds * 1000, _TimeBaseQ, _TimeBase);
                if (ffmpeg.av_seek_frame(_FormatContext, _StreamIndex, position, backward ? ffmpeg.AVSEEK_FLAG_BACKWARD : 0) < 0)
                {
                    return false;
                }

                ffmpeg.avcodec_flush_buffers(_CodecContext);
                if (_SwrContext != null)
                {
                    ffmpeg.swr_init(_SwrContext);
                }

                _EndOfFile = false;
                return true;
            }
        }

        private void _Free()
        {
            if (_SwsContext != null)
            {
                ffmpeg.sws_freeContext(_SwsContext);
                _SwsContext = null;
            }

            if (_SwrContext != null)
            {
                var swrContext = _SwrContext;
                ffmpeg.swr_free(&swrContext);
                _SwrContext = null;
            }

            if (_Frame != null)
            {
                var frame = _Frame;
                ffmpeg.av_frame_free(&frame);
                _Frame = null;
            }

            if (_Packet != null)
            {
                var packet = _Packet;
                ffmpeg.av_packet_free(&packet);
                _Packet = null;
            }

            if (_CodecContext != null)
            {
                var codecContext = _CodecContext;
                ffmpeg.avcodec_free_context(&codecContext);
                _CodecContext = null;
            }

            if (_FormatContext != null)
            {
                var formatContext = _FormatContext;
                ffmpeg.avformat_close_input(&formatContext);
                _FormatContext = null;
            }

            if (Buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Buffer);
                Buffer = IntPtr.Zero;
                _BufferCapacity = 0;
                BufferSize = 0;
            }
        }

        public void Dispose()
        {
            lock (_Lock)
            {
                _Free();
            }
        }
    }
}
