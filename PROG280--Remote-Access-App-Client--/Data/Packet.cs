
namespace PROG280__Remote_Access_App_Data__
{
    public class Packet
    {
        public enum MessageType
        {
            Broadcast, FrameChunk, FrameEnd, FileName, FileChunk, FileEnd, FileAccept, FileDeny, Acknowledgement, Failure, Message, 
        }

        public MessageType ContentType { get; set; } = MessageType.Failure;
        public string? Payload { get; set; }
    }

}
