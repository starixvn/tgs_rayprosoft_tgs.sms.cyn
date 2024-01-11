using System.Text.Json.Serialization;

namespace Tgs.Sms.CYN;
public record SmsSettings(
	[property: JsonPropertyName("apiId")] string ApiId,
	[property: JsonPropertyName("apiPassword")] string ApiPassword,
	[property: JsonPropertyName("senderId")] string SenderId,
	[property: JsonPropertyName("smsType")] string SmsType);