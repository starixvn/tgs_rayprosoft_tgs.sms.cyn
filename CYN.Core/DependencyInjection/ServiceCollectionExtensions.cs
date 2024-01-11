using Tgs.Sms.CYN;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddCYNSms(this IServiceCollection services, Action<IHttpClientBuilder> configureHttpClient)
	{
		configureHttpClient?.Invoke(services.AddHttpClient<CYNApi>());

		return services;
	}
}