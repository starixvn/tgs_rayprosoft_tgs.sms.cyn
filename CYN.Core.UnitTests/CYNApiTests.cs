using System.Text.Json;
using RichardSzalay.MockHttp;
using Tgs.Sms.CYN;

namespace CYN.Core.UnitTests;

public class CYNApiTests
{
    [Fact]
    public async Task 傳送簡訊()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var sut = new CYNApi(httpClient);

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
        await sut.SendMessageAsync(apiId, apiPassword, senderId, smsType, phone, message);
    }

    [Fact]
    public async Task 傳送簡訊_傳信回應內容_Status不是F_視為錯誤()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var sut = new CYNApi(httpClient);

        var apiId = "apiId";
        var apiPassword = "apiPassword";
        var senderId = "senderId";
        var smsType = "P";
        var phone = "84898466792";
        var message = "Your ID verification code is : 123456";

        mockHttp.Expect("http://localhost/api/SendSMS")
            .Respond(
                "application/json",
                JsonSerializer.Serialize(new CYNSendSMSResponse(
                    0,
                    "F",
                    "Transactional SMS is Not Allowed For Your Account",
                    null,
                    "84898466792")));

        // Act
        _ = await Assert.ThrowsAsync<Exception>(async () => await sut.SendMessageAsync(apiId, apiPassword, senderId, smsType, phone, message));
    }

    [Fact]
    public async Task 傳送簡訊時_會把電話號碼前面的加號去掉()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var sut = new CYNApi(httpClient);

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
        await sut.SendMessageAsync(apiId, apiPassword, senderId, smsType, phone, message);
    }
}