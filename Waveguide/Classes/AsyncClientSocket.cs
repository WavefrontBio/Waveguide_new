using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TcpTools
{



    public class SimpleAsyncClient : IDisposable
    {
        #region Delegates & Events
        public delegate void SACEventHandler<TEventArgs>(SimpleAsyncClient client, TEventArgs args);
        public delegate void SACEventHandler(SimpleAsyncClient client);

        /// <summary>
        /// On Connect Event, called when a SimpleAsyncClient connects to a remote host.
        /// </summary>
        public event SACEventHandler OnConnect;

        /// <summary>
        /// On Disconnect Event, called when a SimpleAsyncClient disconnects from the remote host.
        /// </summary>
        public event SACEventHandler OnDisconnect;

        /// <summary>
        /// On Message Received Event, called when a message is completely received from the remote host.
        /// </summary>
        public event SACEventHandler<SACMessageReceivedEventArgs> OnMessageReceived;

        /// <summary>
        /// On Error Event, called when a SimpleAsyncClient encounters an error while managing the connection.
        /// </summary>
        public event SACEventHandler<SACErrorEventArgs> OnError;
        #endregion

        private TcpClient _tcpClient;
        private CancellationTokenSource _cancellation;
        private int _bufferSize;
        private string _hostname;
        private int _port;


        /// <summary>
        /// The remote endpoint the SimpleAsyncClient is connected to.
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return (IPEndPoint)_tcpClient.Client.RemoteEndPoint;
            }
        }

        /// <summary>
        /// The local endpoint of the SimpleAsyncClient
        /// </summary>
        public IPEndPoint LocalEndPoint
        {
            get
            {
                return (IPEndPoint)_tcpClient.Client.LocalEndPoint;
            }
        }

        /// <summary>
        /// Creates a new SimpleAsyncClient with the specified buffer size.
        /// </summary>
        /// <param name="bufferSize"></param>
        public SimpleAsyncClient(int bufferSize = 8192)
        {
            _cancellation = new CancellationTokenSource();
            _bufferSize = bufferSize;
        }

        /// <summary>
        /// Connects to the remote host.
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public void Connect(string hostname, int port)
        {
            _tcpClient = new TcpClient();
            ClientTask(hostname, port).FireAndForget();
        }

        /// <summary>
        /// Disconnects from the remote host.
        /// </summary>
        public void Disconnect()
        {
            _cancellation.Cancel();
        }

        private void Reconnect()
        {
            Connect(_hostname, _port);
        }

        /// <summary>
        /// Sends data to the remote host and automatically frames the message.
        /// </summary>
        /// <param name="message"></param>
        public void Send(byte[] message)
        {
            //var framer = new LengthPrefixPacketFramer(_bufferSize);
            //var framedMessage = framer.Frame(message);
            SendAsync(message).FireAndForget();
        }

        /// <summary>
        /// Asynchronous task for sending data to the remote host
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task SendAsync(byte[] message)
        {
            NetworkStream netStream = _tcpClient.GetStream();
            await netStream.WriteAsync(message, 0, message.Length);

            //if (_tcpClient != null)
            //{
            //    if (_tcpClient.Connected)
            //    {
            //        NetworkStream netStream = _tcpClient.GetStream();
            //        await netStream.WriteAsync(message, 0, message.Length);
            //    }
            //    else
            //    {
            //        Reconnect();
            //    }
            //}
            //else
            //{
            //    Reconnect();  // attempt to reconnect
            //}
        }

        /// <summary>
        /// Asynchronous task for handling the connection (connection, data receive loop, disconnect, error handling).
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private async Task ClientTask(string hostname, int port)
        {
            try
            {
                await _tcpClient.ConnectAsync(hostname, port);

                if (OnConnect != null)
                    OnConnect.Invoke(this);

                using (NetworkStream netStream = _tcpClient.GetStream())
                {
                    //var framer = new LengthPrefixPacketFramer(_bufferSize);

                    while (!_cancellation.Token.IsCancellationRequested)
                    {
                        var buffer = new byte[_bufferSize];
                        var bytesRead = await netStream.ReadAsync(buffer, 0, buffer.Length);

                        if (OnMessageReceived != null)
                        {
                            int length = 0;
                            for (int i = 0; i < buffer.Length; i++)
                            {
                                if (buffer[i] == 13)
                                {
                                    length = i + 1;
                                    break;
                                }
                            }

                            if (length != 0)
                            {
                                byte[] msg = new byte[length];
                                Array.Copy(buffer, msg, length);
                                OnMessageReceived.Invoke(this, new SACMessageReceivedEventArgs(msg, length));
                            }
                        }
                        

                        //bool messageReceived = framer.DataReceived(buffer);
                        //if (messageReceived)
                        //{
                        //    if (OnMessageReceived != null)
                        //        OnMessageReceived.Invoke(this, new SACMessageReceivedEventArgs(framer.GetMessage(), framer.GetMessage().Length));
                        //}
                    }
                }
            }
            catch (Exception exception)
            {
                if (OnError != null)
                    OnError.Invoke(this, new SACErrorEventArgs(exception));
            }
            finally
            {
                if (OnDisconnect != null)
                    OnDisconnect.Invoke(this);

                _tcpClient.Close();
                _tcpClient = null;
                _cancellation.Dispose();
                _cancellation = new CancellationTokenSource();
            }
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            _cancellation.Dispose();

            if (_tcpClient != null)
                _tcpClient.Close();
        }
    }


    public class SACMessageReceivedEventArgs : EventArgs
    {

        /// <summary>
        /// The message data from the received message
        /// </summary>
        public byte[] MessageData { get; private set; }

        /// <summary>
        /// The length of the received message
        /// </summary>
        public long MessageLength { get; private set; }

        public SACMessageReceivedEventArgs(byte[] data, long bytesRead)
        {
            MessageData = data;
            MessageLength = bytesRead;
        }
    }


    public class SACErrorEventArgs : EventArgs
    {
        /// <summary>
        /// The exception thrown while managing the connection.
        /// </summary>
        public Exception Exception { get; private set; }

        public SACErrorEventArgs(Exception ex)
        {
            Exception = ex;
        }
    }



    public static class Extensions
    {
        internal static async void FireAndForget(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Converts an array of bytes into a human readable string of hex values.
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string ToHexString(this byte[] array)
        {
            StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
            string hexAlphabet = "0123456789ABCDEF";

            foreach (byte b in array)
            {
                stringBuilder.Append(hexAlphabet[(int)(b >> 4)]);
                stringBuilder.Append(hexAlphabet[(int)(b & 0xF)]);
                stringBuilder.Append(" ");
            }

            return stringBuilder.ToString();
        }
    }




    /// <summary>
    /// Handles framing and unframing of application packets. Packets are framed by prefixing the packet with the length of the data being sent. 
    /// </summary>
    internal class LengthPrefixPacketFramer : IPacketFramer
    {
        private byte[] _lengthBuffer;
        private byte[] _dataBuffer;
        private byte[] _previousDataBuffer;
        private int _bytesReceived;
        private int _maxMessageSize;

        /// <summary>
        /// Creates a new instance of the packet framer. 
        /// </summary>
        /// <param name="maxMessageSize">The maximum message size</param>
        public LengthPrefixPacketFramer(int maxMessageSize)
        {
            _lengthBuffer = new byte[sizeof(int)];
            _maxMessageSize = maxMessageSize;
        }

        /// <summary>
        /// Frames a packet by prefixing the length of the data being sent.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public byte[] Frame(byte[] message)
        {
            var messageLengthPrefix = BitConverter.GetBytes(message.Length);

            var wrappedMessage = new byte[messageLengthPrefix.Length + message.Length];
            messageLengthPrefix.CopyTo(wrappedMessage, 0);
            message.CopyTo(wrappedMessage, messageLengthPrefix.Length);

            return wrappedMessage;
        }

        /// <summary>
        /// Called whenever data is asyncrhonously received.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>True when a whole application message has been received, otherwise false.</returns>
        public bool DataReceived(byte[] data)
        {
            int i = 0;
            bool result = false;

            while (i != data.Length)
            {
                var bytesAvailable = data.Length - i;

                if (_dataBuffer != null)
                {
                    int bytesRequested = _dataBuffer.Length - _bytesReceived;

                    int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                    Array.Copy(data, i, _dataBuffer, _bytesReceived, bytesTransferred);
                    i += bytesTransferred;

                    result = ReadCompleted(bytesTransferred);
                }
                else
                {
                    int bytesRequested = _lengthBuffer.Length - _bytesReceived;

                    int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                    Array.Copy(data, i, _lengthBuffer, _bytesReceived, bytesTransferred);
                    i += bytesTransferred;

                    result = ReadCompleted(bytesTransferred);
                }

                if (result) break;
            }

            return result;
        }

        /// <summary>
        /// Gets the last fully received message.
        /// </summary>
        /// <returns>The last fully received message, unframed.</returns>
        public byte[] GetMessage()
        {
            if (_previousDataBuffer != null)
                return _previousDataBuffer;
            else
                return new byte[1];
        }

        /// <summary>
        /// Called whenever we've finished processing a section of data.
        /// </summary>
        /// <param name="count"></param>
        /// <returns>True when a whole application message has been received, otherwise false.</returns>
        private bool ReadCompleted(int count)
        {
            _bytesReceived += count;

            if (_dataBuffer == null)
            {
                if (_bytesReceived == sizeof(int))
                {
                    int length = BitConverter.ToInt32(_lengthBuffer, 0);

                    if (length < 0)
                        throw new ProtocolViolationException("Message length cannot be less than zero.");

                    if (_maxMessageSize > 0 && length > _maxMessageSize)
                        throw new ProtocolViolationException(
                            String.Format("Message length {0} is larger than maximum message size {1}.", length, _maxMessageSize)
                        );

                    if (length == 0)
                    {
                        _bytesReceived = 0;
                    }
                    else
                    {
                        _dataBuffer = new byte[length];
                        _bytesReceived = 0;
                    }
                }
            }
            else
            {
                if (_bytesReceived == _dataBuffer.Length)
                {
                    _previousDataBuffer = new byte[_dataBuffer.Length];
                    Array.Copy(_dataBuffer, _previousDataBuffer, _dataBuffer.Length);

                    _dataBuffer = null;
                    _bytesReceived = 0;

                    return true;
                }
            }

            return false;
        }
    }


    internal interface IPacketFramer
    {
        /// <summary>
        /// Frames a message.
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns></returns>
        byte[] Frame(byte[] message);

        /// <summary>
        /// This method is called whenever data is asynchronously received.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>True when the whole message has been received, otherwise false.</returns>
        bool DataReceived(byte[] data);

        /// <summary>
        /// Returns the unframed message, after the whole message has been received.
        /// </summary>
        /// <returns></returns>
        byte[] GetMessage();
    }



}