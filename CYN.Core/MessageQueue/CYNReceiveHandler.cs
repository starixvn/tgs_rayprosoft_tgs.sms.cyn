using Microsoft.Extensions.Logging;
using Tgs.BrandCertificate.Clients;
using Tgs.Sms.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Sms.CYN.MessageQueue;

internal class CYNReceiveHandler(
	IBrandServiceClient brandServiceClient,
	CYNApi cYNApi,
	ILogger<CYNReceiveHandler> logger) : IMessageHandler<ReadOnlyMemory<byte>>
{	
	public async ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var shortMsg = ShortMessage.Parser.ParseFrom(data.Span);
		var brandId = shortMsg.BrandId.ToGuid();

		logger.LogInformation(
			"Receive sms: [{brandIdKey}] {brandId} " +
			"[{destinationKey}] {destination} " +
			"[{contentKey}] {content}",
			nameof(shortMsg.BrandId),
			brandId,
			nameof(shortMsg.Destination),
			shortMsg.Destination,
			nameof(shortMsg.Content),
			shortMsg.Content);

		var brandInfo = await brandServiceClient.GetBrandInfoAsync(
			brandId: brandId,
			cancellationToken).ConfigureAwait(false);

		if (brandInfo == null)
		{
			logger.LogInformation("Can't find brand info ({brandId})", brandId);

			return;
		}

		if (!brandInfo.SmsEnabled || brandInfo == null)
		{
			logger.LogWarning("Brand setting warn");

			return;
		}

		var smsInfo = await brandServiceClient.GetBrandSmsSettingsAsync<SmsSettings>(
			brandId,
			cancellationToken: cancellationToken).ConfigureAwait(false);

		if (smsInfo is null)
		{
			logger.LogWarning("Can't found sms settings({brandId})", brandId);

			return;
		}

		await cYNApi.SendMessageAsync(
			smsInfo.SmsSettings.ApiId,
			smsInfo.SmsSettings.ApiPassword,
			smsInfo.SmsSettings.SenderId,
			smsInfo.SmsSettings.SmsType,
			$"{shortMsg.AreaCode.TrimStart('+')}{shortMsg.Phone.TrimStart('0')}",
			shortMsg.Content,
			cancellationToken).ConfigureAwait(false);
	}
}