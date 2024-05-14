using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.BrandCertificate.Clients;
using Tgs.Sms.CYN;
using Tgs.Sms.CYN.MessageQueue;
using Tgs.Sms.Protos;

namespace CYN.Core.UnitTests;

public class CYNReceiveHandlerTests
{
    [Fact]
    public async Task 接收到發送簡訊要求()
    {
        // Arrange
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var sut = new CYNReceiveHandler(
            fakeBrandServiceClient,
            new CYNApi(httpClient),
            NullLogger<CYNReceiveHandler>.Instance);

        var brandId = Guid.NewGuid();
        var request = new ShortMessage
        {
            BrandId = brandId,
            Content = "Your ID verification code is : 123456",
            Destination = "+84898466792",
            AreaCode = "+84",
            Phone = "0898466792"
        };

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
            .Returns(new BrandInfo
            {
                Id = brandId,
                SmsEnabled = true
            });

        var vendorInfo = new SmsVendorInfo<SmsSettings>(
            "CYN",
            new SmsSettings("apiId", "apiPassword", "senderId", "P"));
        _ = fakeBrandServiceClient.GetBrandSmsSettingsAsync<SmsSettings>(Arg.Is(brandId))
            .Returns(vendorInfo);

        var apiId = "apiId";
        var apiPassword = "apiPassword";
        var senderId = "senderId";
        var smsType = "P";
        var phone = "84898466792";
        var message = "Your ID verification code is : 123456";

        var dict = new Dictionary<string, object>
        {
            { "api_id", apiId },
            { "api_password", apiPassword },
            { "sms_type", smsType },
            { "encoding", "T" },
            { "sender_id", senderId },
            { "phonenumber", phone },
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
        await sut.HandleAsync(request.ToByteArray());
    }
}