using FluentdForward.OpenTelemetry.MessagePack;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Sms.CYN;
using Tgs.Sms.CYN.MessageQueue;

var builder = WebApplication.CreateBuilder(args);
{
	builder.Logging
		.AddOpenTelemetry(options =>
		{
			options.IncludeFormattedMessage = true;
			options.ParseStateValues = true;
			_ = options.AddFluentdForwardExporter(fluentOptions =>
			{
				var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
				fluentOptions.Host = nodeIp;
				fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
				fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
					"MachineInfo",
					() => new
					{
						Environment.MachineName
					}));
			});
		});

	builder.Services
		.AddHttpClientLoggingHandler()
		.AddBrandServiceClient((options, _) => options.BrandCertificateBaseUrl =
			builder.Configuration.GetValue<Uri>("CYN:BrandCertificateBaseUrl")!)
		.AddCYNSms(httpBuilder => httpBuilder
			.ConfigureHttpClient(httpClient => httpClient.BaseAddress =
				builder.Configuration.GetValue<Uri>("CYN:ApiBaseUrl")!)
			.AddLoggingHandler<SmsSettings>());

	builder.Services
		.AddMessageQueue()
		.AddRabbitMessageQueue(config => config
			.ConfigQueueOptions((options, _) =>
			{
				builder.Configuration.GetSection("CYN:RabbitMQ").Bind(options);
				options.RabbitMQUrl = new Uri(builder.Configuration.GetConnectionString("RabbitMQ")!);

				options.OnSetupQueueAndExchange = channel =>
				{
					channel.QueueDeclare(
						"tgs.sms.cyn",
						true,
						false,
						false,
						null);

					channel.QueueBind(
						"tgs.sms.cyn",
						"amq.direct",
						"tgs.sms.cyn",
						null);
				};
			})
			.AddHandler(ProcessorTypes.CYNReceiveHandlerType, "tgs.sms.cyn", false))
		.AddRabbitGlobPatternExchange("*", "amq.direct");

	builder.Services
		.AddHealthChecks();

	builder.Services
		.AddOpenTelemetry()
		.WithTracing(tracingBuilder => tracingBuilder
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddSource("Tgs.*")
			.AddSource("TGS.*")
			.AddSource("Valhalla.*")
			.AddAspNetCoreInstrumentation()
			.AddProfileViewExporter()
			.AddOtlpExporter())
		.WithMetrics(meterBuiler => meterBuiler
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
				.AddRuntimeInstrumentation()
				.AddAspNetCoreInstrumentation()
				.AddOtlpExporter())
		.Services
		.AddProfileViewer()
		.Configure<AspNetCoreInstrumentationOptions>(options => options.Filter =
			httpContext => !httpContext.Request.Path.HasValue
				|| !httpContext.Request.Path.StartsWithSegments("/swagger")
				&& !httpContext.Request.Path.StartsWithSegments("/healthz")
				&& !httpContext.Request.Path.StartsWithSegments("/metrics")
				&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
				&& !new[] { "html", "ico", "png", "jpg" }.Any(
					ext => httpContext.Request.Path.Value.EndsWith($".{ext}")));
}

var app = builder.Build();
{
	app.MapHealthChecks("/healthz");
	app.MapProfileViewer();
}

app.Run();