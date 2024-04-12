using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PROG280__Remote_Access_App_Data__.Packet;
using System.Windows.Media.Imaging;

namespace PROG280__Remote_Access_App_Data__
{
    public class Client : NetworkConnected
    {
        public async Task<BitmapImage?> ReceiveVideoPackets()
        {
            if (_videoStream == null)
            {
                _videoStream = TcpVideoClient!.GetStream();
            }

            List<byte> frameChunks = new List<byte>();

            int expectedChunks = 0;

            while (IsConnected)
            {
                byte[] buffer = new byte[_packetSize];
                int bytesRead = await _videoStream.ReadAsync(buffer, 0, buffer.Length);
                var stringMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var packet = JsonConvert.DeserializeObject<Packet>(stringMessage);

                if(packet == null)
                {
                    return null;
                }

                if (packet!.ContentType != MessageType.Frame)
                {
                    switch (packet.ContentType)
                    {
                        case MessageType.FrameStart:
                            expectedChunks = JsonConvert.DeserializeObject<int>(packet.Payload!);
                            await SendVideoAckPacket();
                            continue;

                        default:
                            break;
                    }
                }
                else
                {
                    frameChunks.AddRange(JsonConvert.DeserializeObject<byte[]>(packet.Payload!)!);
                    await SendVideoAckPacket();
                    continue;
                }
                break;
            }

            if (frameChunks.Count / _chunkSize != expectedChunks)
            {
                //Something bad happened if you end up here.
            }

            byte[] bitmapBytes = frameChunks.ToArray();

            BitmapImage? frame;

            using (MemoryStream mstream = new(bitmapBytes))
            {
                frame = new BitmapImage();
                frame.BeginInit();
                frame.StreamSource = mstream;
                frame.CacheOption = BitmapCacheOption.OnLoad;
                frame.EndInit();
            }
            return frame;
        }

        private async Task SendVideoAckPacket()
        {
            Packet ackPacket = new Packet()
            {
                ContentType = MessageType.Acknowledgement
            };

            byte[] ackBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ackPacket));

            await _videoStream.WriteAsync(ackBytes, 0, ackBytes.Length);
        }

        
    }
}
