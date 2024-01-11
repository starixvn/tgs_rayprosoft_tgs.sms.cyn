using System.Text.Json;
using Google.Protobuf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.BrandCertificate.Clients;
using Tgs.Sms.CYN.MessageQueue;
using Tgs.Sms.Protos;
using Valhalla.MessageQueue;
using Xunit.Abstractions;

namespace Tgs.Sms.CYN.IntegrationTests;

public class HandleMessageTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public HandleMessageTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 處理傳送簡訊要求()
    {
        // Arrange
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var mockHttp = new MockHttpMessageHandler();

        await using var web = CYNTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakeBrandServiceClient)
                    .AddMessageQueue()
                    .AddDirectMessageQueue(config => config
                        .AddHandler(ProcessorTypes.CYNReceiveHandlerType, "tgs.sms.cyn"))
                    .ClearAllExchanges()
                    .AddDirectGlobPatternExchange("*")
                    .Services
                    .AddHttpClient<CYNApi>()
                    .ConfigurePrimaryHttpMessageHandler(_ => mockHttp)));

        var brandId = Guid.NewGuid();
        var apiId = "apiId";
        var apiPassword = "apiPassword";
        var senderId = "senderId";
        var smsType = "P";
        var phone = "84898466792";
        var message = "Your ID verification code is : 123456";

        var request = new ShortMessage
        {
            BrandId = brandId,
            Content = message,
            Destination = "+84898466792"
        };

        var messageSender = web.Services.GetRequiredService<IMessageSender>();

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId), Arg.Any<CancellationToken>())
            .Returns(new BrandInfo
            {
                Id = brandId,
                SmsEnabled = true
            });        

        var vendorInfo = new SmsVendorInfo<SmsSettings>(
            "CYN",
            new SmsSettings(apiId, apiPassword, senderId, smsType));
        _ = fakeBrandServiceClient.GetBrandSmsSettingsAsync<SmsSettings>(
            Arg.Is(brandId),
            Arg.Any<JsonSerializerOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(vendorInfo);

        var tcs = new TaskCompletionSource();

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

        mockHttp.Expect("http://localhost/api/SendSMS")
            .WithContent(JsonSerializer.Serialize(dict))
            .Respond(
                "application/json",
                JsonSerializer.Serialize(new CYNSendSMSResponse(
                    107048733,
                    "S",
                    "Message Submitted Sucessfully",
                    null,
                    "84898466792")));

        // Act
        await messageSender.PublishAsync("tgs.sms.cyn", request.ToByteArray());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1D));
    }
}