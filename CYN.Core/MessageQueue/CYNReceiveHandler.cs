using Microsoft.Extensions.Logging;
using Tgs.BrandCertificate.Clients;
using Tgs.Sms.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Sms.CYN.MessageQueue;

internal class CYNReceiveHandler : IMessageHandler
{
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly CYNApi _cYNApi;
	private readonly ILogger<CYNReceiveHandler> _logger;

	public CYNReceiveHandler(
		IBrandServiceClient brandServiceClient,
		CYNApi cYNApi,
		ILogger<CYNReceiveHandler> logger)
	{
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_cYNApi = cYNApi ?? throw new ArgumentNullException(nameof(cYNApi));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var shortMsg = ShortMessage.Parser.ParseFrom(data.Span);
		var brandId = shortMsg.BrandId.ToGuid();

		_logger.LogInformation(
			"Receive sms: [{brandIdKey}] {brandId} " +
			"[{destinationKey}] {destination} " +
			"[{contentKey}] {content}",
			nameof(shortMsg.BrandId),
			brandId,
			nameof(shortMsg.Destination),
			shortMsg.Destination,
			nameof(shortMsg.Content),
			shortMsg.Content);

		var brandInfo = await _brandServiceClient.GetBrandInfoAsync(
			brandId: brandId,
			cancellationToken).ConfigureAwait(false);

		if (brandInfo == null)
		{
			_logger.LogInformation("Can't find brand info ({brandId})", brandId);

			return;
		}

		if (!brandInfo.SmsEnabled || brandInfo == null)
		{
			_logger.LogWarning("Brand setting warn");

			return;
		}

		var smsInfo = await _brandServiceClient.GetBrandSmsSettingsAsync<SmsSettings>(
			brandId,
			cancellationToken: cancellationToken).ConfigureAwait(false);

		if (smsInfo is null)
		{
			_logger.LogWarning("Can't found sms settings({brandId})", brandId);

			return;
		}

		await _cYNApi.SendMessageAsync(
			smsInfo.SmsSettings.ApiId,
			smsInfo.SmsSettings.ApiPassword,
			smsInfo.SmsSettings.SenderId,
			smsInfo.SmsSettings.SmsType,
			$"{shortMsg.AreaCode.TrimStart('+')}{shortMsg.Phone.TrimStart('0')}",
			shortMsg.Content,
			cancellationToken).ConfigureAwait(false);
	}
}