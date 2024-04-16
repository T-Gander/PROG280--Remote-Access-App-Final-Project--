
namespace PROG280__Remote_Access_App_Data__
{
    public class Packet
    {
        public enum MessageType
        {
            FrameChunk, FrameEnd, FileChunk, FileEnd, FileAccept, FileDeny, Message, MouseMove, MouseLeft, MouseRight
        }

        public MessageType ContentType { get; set; }
        public string? Payload { get; set; }
    }

}
