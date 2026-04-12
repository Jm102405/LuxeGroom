namespace LuxeGroom.Data.Generated
{
    public class Chatbot
    {
        // Primary key
        public int Id { get; set; }

        // System prompt — defines chatbot behavior and personality
        public string Instructions { get; set; }

        // Knowledge base — services, FAQs, pricing, etc.
        public string Data { get; set; }

        // Date this configuration was created
        public DateTime CreatedDate { get; set; }
    }
}