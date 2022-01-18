namespace CrossCutting.EmailSenderService
{
    public class Attachment
    {
        public string ContentType { get; set; }
        public string Filename { get; set; }
        public byte[] Content { get; set; }
    }
}
