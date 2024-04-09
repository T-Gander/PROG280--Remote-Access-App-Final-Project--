
namespace PROG280__Remote_Access_App_Data__
{
    public class Packet
    {
        public enum MessageType
        {
            Broadcast, Frame, File, Acknowledgement
        }
        
        public MessageType ContentType { get; set; }
        public string? Payload { get; set; }
    }

}
