using System.Text.Json.Serialization;

namespace Tgs.Sms.CYN;
public record CYNSendSMSResponse(
	[property: JsonPropertyName("message_id")] int MessageId,
	[property: JsonPropertyName("status")] string Status,
	[property: JsonPropertyName("remarks")] string Remarks,
	[property: JsonPropertyName("uid")] string? Uid,
	[property: JsonPropertyName("phonenumber")] string PhoneNumber);