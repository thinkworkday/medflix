using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Medlix.Backend.API.Core.DTO
{
    public class CommunicationModel
    {
        public PostExtension postExtension { get; set; }
        public PatchExtension patchExtension { get; set; }

    }
    public class Subject
    {
        public string? reference { get; set; }
    }

    public class Extension
    {
        public string url { get; set; }
        public Subject valueReference { get; set; }
    }
    
    public class About
    {
        public string reference { get; set; }
    }

    public class Recipient
    {
        public string reference { get; set; }
    }

    public class Sender
    {
        public string reference { get; set; }
    }

    public class Payload
    {
        public string contentString { get; set; }
    }
    public class PatchPayload
    {
        public string op { get; set; }
        public string path { get; set; }
        public Value value { get; set; }

    }
    public class Value
    {

        public string url { get; set; }
        public string valueString { get; set; }
        public int? valueInteger { get; set; }
        public DateTime? valueDateTime { get; set; }

    }
    public class ResponseType
    {
        public bool communicationResponse { get; set; }
        public bool patchResponse { get; set; }


    }

    public class PostExtension
    {
        public string resourceType { get; set; }
        public List<Extension> extension { get; set; }
        public string status { get; set; }
        public Subject subject { get; set; }
        public List<About> about { get; set; }
        public DateTime sent { get; set; }
        public List<Recipient> recipient { get; set; }
        public Sender sender { get; set; }
        public List<Payload> payload { get; set; }
    }
    public class PatchExtension
    {
        public string urlPatchType { get; set; }
        public List<PatchPayload> patchExtension { get; set; }
    }
    public class KeyVault
    {
        public string patientId { get; set; } 
        public string dateTime { get; set; } = DateTime.UtcNow.ToString();
        public string organizationId { get; set; } 
        public string userId { get; set; } 
        public string secret { get; set; } 
    }

    public class ReadMsgDto
    {
        public string? Sender { get; set; }
        public string? Receiver { get; set; }
    }

    public class SendMsgDto
    {
        public string? Sender { get; set; }
        public string? Receiver { get; set; }
        public string? Message { get; set; }
    }

    public class CommunicationDto
    {
        public string? ResourceType { get; set; }
        public List<Extension>? Extension { get; set; }
        public string? Status { get; set; }
        public Subject? Subject { get; set; }
        public List<About>? About { get; set; }
        public DateTime Sent { get; set; }
        public List<Recipient>? Recipient { get; set; }
        public Sender? Sender { get; set; }
        public List<Payload>? Payload { get; set; }
    }

    public class ExtensionDto
    {
        public string? Url { get; set; }
        public string? ValueString { get; set; }
        public int? ValueInteger { get; set; }
    }

    public class CareTeamDto
    {
        public string? ResourceType { get; set; }
        public string? Id { get; set; }
        public Meta? Meta { get; set; }
        public List<ExtensionDto>? Extension { get; set; }
        public string? Status { get; set; }
        public string? Name { get; set; }

        public Subject? Subject { get; set; }
    }
}
