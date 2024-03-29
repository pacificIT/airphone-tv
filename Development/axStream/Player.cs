using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;

namespace axStream
{
    class Player
    {
        public class ErrorEventArgs : EventArgs
        {
            public enum ERRORNUMBER
            {
                ERRORCONNECTING = 0,
                ERRORRECORDING = 1,
                ERRORSENDING = 2
            }
            public ERRORNUMBER Error;
            public Exception Exception;
        }

        private WaveIn waveInStream;
        //private byte[] m_RecBuffer;
        private RAOPClient at;
        private string ip;
        private Double Volume = -144;

        //This appears to be an AirTunes magic number... Although maybe it is more that it needs to be a power of 2?
        //Anyway this works.
        private const int BufferSize = 16384; // Default 16384

        public delegate void OnConnectEventHandler(object sender, EventArgs e);
        public event OnConnectEventHandler OnConnect;
        public delegate void OnDisconnectEventHandler(object sender, EventArgs e);
        public event OnDisconnectEventHandler OnDisconnect;
        public delegate void OnErrorEventHandler(object sender, ErrorEventArgs e);
        public event OnErrorEventHandler OnError;

        public Player(string ip, double volume)
        {
            this.Volume = volume;
            this.ip = ip;
        }

        protected virtual void ConnectedEvent(EventArgs e)
        {
            if (OnConnect != null)
                OnConnect(this, e);
        }

        protected virtual void DisconnectedEvent(EventArgs e)
        {
            if (OnDisconnect != null)
                OnDisconnect(this, e);
        }

        protected virtual void ErrorEvent(ErrorEventArgs e)
        {
            if (OnError != null)
                OnError(this, e);
        }

        private void DataArrived(object sender, WaveInEventArgs e)
        {

            //if (m_RecBuffer == null || m_RecBuffer.Length < e.BytesRecorded)
            //    m_RecBuffer = new byte[size];
            //System.Runtime.InteropServices.Marshal.Copy(data, m_RecBuffer, 0, size);

            byte[] alac = EncodeALAC(e.Buffer);

            try
            {
                at.SendSample(alac, 0, alac.Length);
            }
            catch (Exception ex)
            {
                ErrorEventArgs err = new ErrorEventArgs();
                err.Error = ErrorEventArgs.ERRORNUMBER.ERRORSENDING;
                err.Exception = ex;
                ErrorEvent(err);

                Stop();
            }

        }

        public void SetVolume(double volume)
        {
            at.Volume = volume;
        }

        public void Stop()
        {
            if (waveInStream != null)
                try
                {
                    waveInStream.StopRecording();
                    waveInStream.Dispose();
                    
                }
                finally
                {
                    waveInStream = null;
                }
            if (at != null)
            {
                try
                {
                    at.Disconnect();
                }
                finally
                {
                    at = null;
                }

                DisconnectedEvent(EventArgs.Empty);
            }
        }

        public void Start()
        {
            Stop();
            try
            {
                // Start Connection to Airport Express
                at = new RAOPClient(ip);
                at.Volume = Volume;

                try
                {
                    at.Connect();
                }
                catch (Exception ex)
                {
                    at = null;

                    ErrorEventArgs e = new ErrorEventArgs();
                    e.Error = ErrorEventArgs.ERRORNUMBER.ERRORCONNECTING;
                    e.Exception = ex;
                    ErrorEvent(e);

                    //Console.WriteLine("Connect failed: {0}", e.Message);
                    return;
                }


                string s = String.Format("JackStatus: {0}{1}JackType: {2}{3}",
                    at.JackStatus == RAOPClient.JACK_STATUS_CONNECTED ?
                    "connected" : "disconnected", Environment.NewLine,
                    at.JackType == RAOPClient.JACK_TYPE_DIGITAL ?
                    "digital" : "analog", Environment.NewLine);
                //Console.WriteLine(s);


                // Start recorder
                //NAudio.Wave.WaveFormat fmt = new NAudio.Wave.WaveFormat(44100, 16, 2);
                //Player.BufferSize = fmt.AverageBytesPerSecond;
                //m_Recorder = new WaveLib.WaveInRecorder(-1, fmt, BufferSize, 3, new WaveLib.BufferDoneEventHandler(DataArrived));
                waveInStream = new WaveIn();
                waveInStream.NumberOfBuffers = 3;
                waveInStream.BufferSize = Player.BufferSize;
                waveInStream.PreferredBufferType = WaveIn.BufferType.Size;
                waveInStream.WaveFormat = new NAudio.Wave.WaveFormat(44100, 16, 2);
                waveInStream.DataAvailable += new EventHandler<WaveInEventArgs>(DataArrived);
                waveInStream.StartRecording();

                ConnectedEvent(EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ErrorEventArgs e = new ErrorEventArgs();
                e.Error = ErrorEventArgs.ERRORNUMBER.ERRORRECORDING;
                e.Exception = ex;
                ErrorEvent(e);

                at = null;
                Stop();
                //throw;
            }
        }

        public bool Connected
        {
            get
            {
                if (at == null) return false;
                return at.JackStatus == RAOPClient.JACK_STATUS_CONNECTED;
            }
        }

        private static byte[] EncodeALAC(byte[] buffer)
        {
            // Frame size is set as 4096 samples, stereo
            //BitBuffer bitbuf = new BitBuffer((4096 * 2 * 2) + 3);
            BitBuffer bitbuf = new BitBuffer(Player.BufferSize + 3);

            bitbuf.WriteBits(1, 3);  // channels -- 0 mono, 1 stereo
            bitbuf.WriteBits(0, 4);  // unknown
            bitbuf.WriteBits(0, 12); // unknown
            bitbuf.WriteBits(0, 1);  // 'has size' flag
            bitbuf.WriteBits(0, 2);  // unknown
            bitbuf.WriteBits(1, 1);  // 'no compression' flag

            for (int i = 0; i < buffer.Length; i += 2)
            {
                // endian swap 16 bit samples
                bitbuf.WriteBits(buffer[i + 1], 8);
                bitbuf.WriteBits(buffer[i], 8);
            }

            return bitbuf.Buffer;
        }
    }

    class BitBuffer
    {
        byte[] buffer;

        byte[] masks =
    {
        0x01, 0x03, 0x07, 0x0F,
        0x1F, 0x3F, 0x7F, 0xFF
    };

        int bitOffset = 0;
        int byteOffset = 0;

        public byte[] Buffer
        {
            get
            {
                return this.buffer;
            }
        }

        public BitBuffer(int length)
        {
            buffer = new byte[length];
        }

        public void WriteBits(int data, int numbits)
        {
            if (bitOffset != 0 && bitOffset + numbits > 8)
            {
                int numwritebits = 8 - bitOffset;
                byte bitstowrite = (byte)((data >> (numbits - numwritebits)) <<
                                   (8 - bitOffset - numwritebits));
                buffer[byteOffset] |= bitstowrite;
                numbits -= numwritebits;
                bitOffset = 0;
                byteOffset++;
            }

            while (numbits >= 8)
            {
                byte bitstowrite = (byte)((data >> (numbits - 8)) & 0xFF);
                buffer[byteOffset] |= bitstowrite;
                numbits -= 8;
                bitOffset = 0;
                byteOffset++;
            }

            if (numbits > 0)
            {
                byte bitstowrite = (byte)((data & masks[numbits]) <<
                                   (8 - bitOffset - numbits));
                buffer[byteOffset] |= bitstowrite;
                bitOffset += numbits;
                if (bitOffset == 8)
                {
                    byteOffset++;
                    bitOffset = 0;
                }
            }
        }
    }
}
