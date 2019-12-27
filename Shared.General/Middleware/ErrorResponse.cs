using System;
using System.Runtime.Serialization;

namespace Shared.Middleware
{
    [DataContract(Name = "ErrorResponse")]
    public class ErrorResponse
    {
        [DataMember(Name = "Message")] public String Message { get; set; }

        public ErrorResponse(String message)
        {
            Message = message;
        }
    }    
}