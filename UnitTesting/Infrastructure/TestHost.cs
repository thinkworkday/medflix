using FileAPI;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTesting.Infrastructure
{
	public class TestHost
	{
		public TestHost()
		{
			ConfigureEnvironmentVariablesFromLocalSettings();
			var startup = new TestStartup();
			var host = new HostBuilder()
				.ConfigureWebJobs(startup.Configure)
				.ConfigureServices(ReplaceTestOverrides)
				.Build();
			

			ServiceProvider = host.Services;
		}

		public IServiceProvider ServiceProvider { get; }

		private void ReplaceTestOverrides(IServiceCollection services)
		{
			// services.Replace(new ServiceDescriptor(typeof(ServiceToReplace), testImplementation));
		}

		private class TestStartup : Startup
		{
			public override void Configure(IFunctionsHostBuilder builder)
			{
				SetExecutionContextOptions(builder);
				base.Configure(builder);
			}

			private static void SetExecutionContextOptions(IFunctionsHostBuilder builder)
			{
				builder.Services.Configure<ExecutionContextOptions>(o => o.AppDirectory = Directory.GetCurrentDirectory());
			}
		}
		static void ConfigureEnvironmentVariablesFromLocalSettings()
		{
			var path = Path.GetDirectoryName(typeof(Startup).Assembly.Location);
			var json = File.ReadAllText(Path.Join(path, "local.settings.json"));
			var parsed = Newtonsoft.Json.Linq.JObject.Parse(json).Value<Newtonsoft.Json.Linq.JObject>("Values");

			foreach (var item in parsed)
			{
				if (item.Key == "Env")
                {
					Environment.SetEnvironmentVariable(item.Key, "test");
				}
                else
                {
					Environment.SetEnvironmentVariable(item.Key, item.Value.ToString());
				}
				
			}
		}
	}
}
