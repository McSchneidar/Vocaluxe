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
using System.Runtime.InteropServices;

namespace PortAudioSharp
{
    static class PortAudio
    {
        private const string _Lib = "portaudio";

        #region types
        public enum PaError
        {
            paNoError = 0,
            paNotInitialized = -10000,
            paUnanticipatedHostError,
            paInvalidChannelCount,
            paInvalidSampleRate,
            paInvalidDevice,
            paInvalidFlag,
            paSampleFormatNotSupported,
            paBadIODeviceCombination,
            paInsufficientMemory,
            paBufferTooBig,
            paBufferTooSmall,
            paNullCallback,
            paBadStreamPtr,
            paTimedOut,
            paInternalError,
            paDeviceUnavailable,
            paIncompatibleHostApiSpecificStreamInfo,
            paStreamIsStopped,
            paStreamIsNotStopped,
            paInputOverflowed,
            paOutputUnderflowed,
            paHostApiNotFound,
            paInvalidHostApi,
            paCanNotReadFromACallbackStream,
            paCanNotWriteToACallbackStream,
            paCanNotReadFromAnOutputOnlyStream,
            paCanNotWriteToAnInputOnlyStream,
            paIncompatibleStreamHostApi,
            paBadBufferPtr
        }

        public enum PaHostApiTypeId
        {
            paInDevelopment = 0,
            paDirectSound = 1,
            paMME = 2,
            paASIO = 3,
            paSoundManager = 4,
            paCoreAudio = 5,
            paOSS = 7,
            paALSA = 8,
            paAL = 9,
            paBeOS = 10,
            paWDMKS = 11,
            paJACK = 12,
            paWASAPI = 13,
            paAudioScienceHPI = 14
        }

        [Flags]
        public enum PaSampleFormat : uint
        {
            paFloat32 = 0x00000001,
            paInt32 = 0x00000002,
            paInt24 = 0x00000004,
            paInt16 = 0x00000008,
            paInt8 = 0x00000010,
            paUInt8 = 0x00000020,
            paCustomFormat = 0x00010000,
            paNonInterleaved = 0x80000000
        }

        [Flags]
        public enum PaStreamFlags : uint
        {
            paNoFlag = 0,
            paClipOff = 0x00000001,
            paDitherOff = 0x00000002,
            paNeverDropInput = 0x00000004,
            paPrimeOutputBuffersUsingStreamCallback = 0x00000008,
            paPlatformSpecificFlags = 0xFFFF0000
        }

        [Flags]
        public enum PaStreamCallbackFlags : uint
        {
            paInputUnderflow = 0x00000001,
            paInputOverflow = 0x00000002,
            paOutputUnderflow = 0x00000004,
            paOutputOverflow = 0x00000008,
            paPrimingOutput = 0x00000010
        }

        public enum PaStreamCallbackResult
        {
            paContinue = 0,
            paComplete = 1,
            paAbort = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PaHostApiInfo
        {
            public int structVersion;
            public PaHostApiTypeId type;
            [MarshalAs(UnmanagedType.LPStr)]
            public string name;
            public int deviceCount;
            public int defaultInputDevice;
            public int defaultOutputDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PaHostErrorInfo
        {
            public PaHostApiTypeId hostApiType;
            public int errorCode;
            [MarshalAs(UnmanagedType.LPStr)]
            public string errorText;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PaDeviceInfo
        {
            public int structVersion;
            [MarshalAs(UnmanagedType.LPStr)]
            public string name;
            public int hostApi;
            public int maxInputChannels;
            public int maxOutputChannels;
            public double defaultLowInputLatency;
            public double defaultLowOutputLatency;
            public double defaultHighInputLatency;
            public double defaultHighOutputLatency;
            public double defaultSampleRate;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PaStreamParameters
        {
            public int device;
            public int channelCount;
            public PaSampleFormat sampleFormat;
            public double suggestedLatency;
            public IntPtr hostApiSpecificStreamInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PaStreamCallbackTimeInfo
        {
            public double inputBufferAdcTime;
            public double currentTime;
            public double outputBufferDacTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PaStreamInfo
        {
            public int structVersion;
            public double inputLatency;
            public double outputLatency;
            public double sampleRate;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate PaStreamCallbackResult PaStreamCallbackDelegate(IntPtr input, IntPtr output, uint frameCount, ref PaStreamCallbackTimeInfo timeInfo,
            PaStreamCallbackFlags statusFlags, IntPtr userData);
        #endregion

        #region native
        [DllImport(_Lib, EntryPoint = "Pa_Initialize", CallingConvention = CallingConvention.Cdecl)]
        public static extern PaError Pa_Initialize();

        [DllImport(_Lib, EntryPoint = "Pa_Terminate", CallingConvention = CallingConvention.Cdecl)]
        public static extern PaError Pa_Terminate();

        [DllImport(_Lib, EntryPoint = "Pa_GetErrorText", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _Pa_GetErrorText(PaError errorCode);

        [DllImport(_Lib, EntryPoint = "Pa_GetHostApiCount", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_GetHostApiCount();

        [DllImport(_Lib, EntryPoint = "Pa_GetDefaultHostApi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_GetDefaultHostApi();

        [DllImport(_Lib, EntryPoint = "Pa_GetHostApiInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _Pa_GetHostApiInfo(int hostApi);

        [DllImport(_Lib, EntryPoint = "Pa_GetLastHostErrorInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _Pa_GetLastHostErrorInfo();

        [DllImport(_Lib, EntryPoint = "Pa_GetDeviceCount", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_GetDeviceCount();

        [DllImport(_Lib, EntryPoint = "Pa_GetDeviceInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _Pa_GetDeviceInfo(int device);

        [DllImport(_Lib, EntryPoint = "Pa_OpenStream", CallingConvention = CallingConvention.Cdecl)]
        private static extern PaError _Pa_OpenStream(out IntPtr stream, IntPtr inputParameters, IntPtr outputParameters, double sampleRate, uint framesPerBuffer,
            PaStreamFlags streamFlags, PaStreamCallbackDelegate streamCallback, IntPtr userData);

        [DllImport(_Lib, EntryPoint = "Pa_CloseStream", CallingConvention = CallingConvention.Cdecl)]
        public static extern PaError Pa_CloseStream(IntPtr stream);

        [DllImport(_Lib, EntryPoint = "Pa_StartStream", CallingConvention = CallingConvention.Cdecl)]
        public static extern PaError Pa_StartStream(IntPtr stream);

        [DllImport(_Lib, EntryPoint = "Pa_StopStream", CallingConvention = CallingConvention.Cdecl)]
        public static extern PaError Pa_StopStream(IntPtr stream);

        [DllImport(_Lib, EntryPoint = "Pa_AbortStream", CallingConvention = CallingConvention.Cdecl)]
        public static extern PaError Pa_AbortStream(IntPtr stream);

        [DllImport(_Lib, EntryPoint = "Pa_IsStreamStopped", CallingConvention = CallingConvention.Cdecl)]
        private static extern int _Pa_IsStreamStopped(IntPtr stream);

        [DllImport(_Lib, EntryPoint = "Pa_GetStreamInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _Pa_GetStreamInfo(IntPtr stream);
        #endregion

        #region managed helpers
        public static string Pa_GetErrorText(PaError errorCode)
        {
            return Marshal.PtrToStringAnsi(_Pa_GetErrorText(errorCode));
        }

        public static PaHostApiInfo Pa_GetHostApiInfo(int hostApi)
        {
            return _PtrToStruct<PaHostApiInfo>(_Pa_GetHostApiInfo(hostApi));
        }

        public static PaHostErrorInfo Pa_GetLastHostErrorInfo()
        {
            return _PtrToStruct<PaHostErrorInfo>(_Pa_GetLastHostErrorInfo());
        }

        public static PaDeviceInfo Pa_GetDeviceInfo(int device)
        {
            return _PtrToStruct<PaDeviceInfo>(_Pa_GetDeviceInfo(device));
        }

        public static PaStreamInfo Pa_GetStreamInfo(IntPtr stream)
        {
            return _PtrToStruct<PaStreamInfo>(_Pa_GetStreamInfo(stream));
        }

        public static PaError Pa_IsStreamStopped(IntPtr stream)
        {
            var result = _Pa_IsStreamStopped(stream);
            switch (result)
            {
                case 1:
                    return PaError.paStreamIsStopped;
                case 0:
                    return PaError.paStreamIsNotStopped;
                default:
                    return (PaError)result;
            }
        }

        public static PaError Pa_OpenStream(out IntPtr stream, ref PaStreamParameters? inputParameters, ref PaStreamParameters? outputParameters, double sampleRate,
            uint framesPerBuffer, PaStreamFlags streamFlags, PaStreamCallbackDelegate streamCallback, IntPtr userData)
        {
            var input = _StructToPtr(inputParameters);
            var output = _StructToPtr(outputParameters);
            try
            {
                return _Pa_OpenStream(out stream, input, output, sampleRate, framesPerBuffer, streamFlags, streamCallback, userData);
            }
            finally
            {
                if (input != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(input);
                }

                if (output != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(output);
                }
            }
        }

        private static T _PtrToStruct<T>(IntPtr ptr) where T : struct
        {
            return ptr == IntPtr.Zero ? default(T) : Marshal.PtrToStructure<T>(ptr);
        }

        private static IntPtr _StructToPtr(PaStreamParameters? parameters)
        {
            if (!parameters.HasValue)
            {
                return IntPtr.Zero;
            }

            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<PaStreamParameters>());
            Marshal.StructureToPtr(parameters.Value, ptr, false);
            return ptr;
        }
        #endregion
    }
}
