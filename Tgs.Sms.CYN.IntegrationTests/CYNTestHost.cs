using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;

namespace Tgs.Sms.CYN.IntegrationTests;

internal static class CYNTestHost
{
	public static WebApplicationFactory<Program> Instance => new WebApplicationFactory<Program>()
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureAppConfiguration((_, configBuilder) =>
				{
					configBuilder.Sources.Clear();

					configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
					{
						["CYN:ApiBaseUrl"] = "http://localhost"
					});
				})
				.ConfigureServices(services => services
					.AddFakeRabbitMessageQueue()
					.AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
						sp => new ConfigureNamedOptions<OtlpExporterOptions>(
							Options.DefaultName,
							exporterOptions => exporterOptions.TimeoutMilliseconds = 0))));
}