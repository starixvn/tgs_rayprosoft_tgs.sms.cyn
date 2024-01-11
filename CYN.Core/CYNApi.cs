using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Tgs.Sms.CYN;

internal class CYNApi
{
	private readonly HttpClient _httpClient;

	public CYNApi(HttpClient httpClient)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

	public async ValueTask SendMessageAsync(
		string apiId,
		string apiPassword,
		string senderId,
		string smsType,
		string phone,
		string message,
		CancellationToken cancellationToken = default)
	{
		var dict = new Dictionary<string, object>
		{
			{ "api_id", apiId },
			{ "api_password", apiPassword },
			{ "sms_type", smsType },
			{ "encoding", "T" },
			{ "sender_id", senderId },
			{ "phonenumber", phone.TrimStart('+') },
			{ "textmessage", message },
		};

		var requestContent = new StringContent(JsonSerializer.Serialize(dict));

		requestContent.Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json);

		var response = await _httpClient.PostAsync("/api/SendSMS", requestContent, cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<CYNSendSMSResponse>(cancellationToken: cancellationToken)
				.ToAsyncEnumerable()
				.Select(res => res!))
			.FirstAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (response.Status != "S")
			throw new Exception("Send message fail.");
	}
}