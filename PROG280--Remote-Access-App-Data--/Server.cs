using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PROG280__Remote_Access_App_Data__.Packet;
using Newtonsoft.Json;
using System.Windows;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Data;

namespace PROG280__Remote_Access_App_Data__
{
    public class Server : NetworkConnected
    {
        private async Task<int> SendFrameStartPacket(byte[] bitmapBytes)
        {
            _videoStream = TcpVideoClient!.GetStream();

            int totalChunks = (int)Math.Ceiling((double)bitmapBytes.Length / _chunkSize);

            Packet frameStartPacket = new Packet()
            {
                ContentType = MessageType.FrameStart,
                Payload = JsonConvert.SerializeObject(totalChunks)
            };

            byte[] initialpacket = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(frameStartPacket));

            await _videoStream.WriteAsync(initialpacket, 0, initialpacket.Length);

            return totalChunks;
        }

        private async Task<MessageType> ReceiveVideoAckPacket()
        {
            try
            {
                byte[] buffer = new byte[_packetSize];
                int bytesRead = await _videoStream!.ReadAsync(buffer, 0, buffer.Length);
                var stringMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var packet = JsonConvert.DeserializeObject<Packet>(stringMessage);

                return packet!.ContentType;
            }
            catch
            {
                Messages.Add("Exception: Didn't receive ack packet. And stream is closed.");
                IsConnected = false;

                return MessageType.Failure;
            }
        }

        private Bitmap GrabScreen()
        {
            Bitmap screenshot = new((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(0, 0, 0, 0, screenshot.Size);
            }

            return screenshot;
        }

        private async Task SendVideoFrameChunk(int chunkIndex, byte[] bitmapBytes)
        {
            Packet screenPacket = new();
            screenPacket.ContentType = MessageType.Frame;

            // Determine chunk boundaries
            int offset = chunkIndex * _chunkSize;
            int length = Math.Min(_chunkSize, bitmapBytes.Length - offset);
            byte[] chunk = new byte[_chunkSize];
            Buffer.BlockCopy(bitmapBytes, offset, chunk, 0, length);

            screenPacket.Payload = JsonConvert.SerializeObject(chunk);

            // Serialize packet and send
            byte[] bytepacket = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(screenPacket));
            await TcpVideoClient!.GetStream().WriteAsync(bytepacket, 0, bytepacket.Length);
        }

        public async Task SendVideoPackets()
        {
            try
            {
                byte[] bitmapBytes;
                Bitmap screen = GrabScreen();

                using (MemoryStream mstream = new())
                {
                    screen.Save(mstream, ImageFormat.Png);
                    bitmapBytes = mstream.ToArray();

                    int totalChunks = await SendFrameStartPacket(bitmapBytes);

                    int chunkIndex = 0;

                    while (chunkIndex < totalChunks)
                    {
                        //Wait for acknowledgement packet
                        if (await ReceiveVideoAckPacket() != MessageType.Acknowledgement)
                        {
                            return;
                        }

                        await SendVideoFrameChunk(chunkIndex, bitmapBytes);

                        chunkIndex++;
                    }

                    await SendFrameEndPacket();

                    await ReceiveVideoAckPacket();
                }
                screen.Dispose();
            }
            catch (Exception ex)
            {
                AddToMessagesList(ex.Message);
            }
        }

        private async Task SendFrameEndPacket()
        {
            Packet frameEndPacket = new Packet()
            {
                ContentType = MessageType.FrameEnd,
                Payload = ""
            };

            byte[] endPacket = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(frameEndPacket));

            await _videoStream.WriteAsync(endPacket, 0, endPacket.Length);
        }

    }
}
