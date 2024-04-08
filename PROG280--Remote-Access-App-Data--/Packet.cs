
namespace PROG280__Remote_Access_App_Data__
{
    public class Packet
    {
        public enum MessageType
        {
            Broadcast, Image, File
        }
        
        public MessageType ContentType { get; set; }
        public string? Payload { get; set; }
    }

}
