
namespace PROG280__Remote_Access_App_Data__
{
    public class Packet
    {
        public enum MessageType
        {
            Broadcast, Frame, File, Acknowledgement, FrameStart, FrameEnd, Failure
        }

        public MessageType ContentType { get; set; } = MessageType.Failure;
        public string? Payload { get; set; }
    }

}
