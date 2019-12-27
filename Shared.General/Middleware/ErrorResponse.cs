namespace Shared.General.Middleware
{
    using System;
    using System.Runtime.Serialization;

    [DataContract(Name = "ErrorResponse")]
    public class ErrorResponse
    {
        [DataMember(Name = "Message")] public String Message { get; set; }

        public ErrorResponse(String message)
        {
            this.Message = message;
        }
    }    
}