using Medlix.Backend.API.BAL.AuthenticationService;
using Medlix.Backend.API.BAL.AzureB2CService;
using Medlix.Backend.API.BAL.BlobService;
using Medlix.Backend.API.BAL.ChatService;
using Medlix.Backend.API.BAL.CmsService;
using Medlix.Backend.API.BAL.ConsentServices;
using Medlix.Backend.API.BAL.FhirPatientService;
using Medlix.Backend.API.BAL.FileService;
using Medlix.Backend.API.BAL.HttpService;
using Medlix.Backend.API.BAL.KeyVaultService;
using Medlix.Backend.API.BAL.MessageService;
using Medlix.Backend.API.BAL.SendGridService;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(FileAPI.Startup))]
namespace FileAPI
{
    public class Startup : FunctionsStartup
    {
        
        public Startup()
        {
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IHttpService, HttpService>();
            builder.Services.AddTransient<IFileService, FileService>();
            builder.Services.AddTransient<IBlobService, BlobService>();
            builder.Services.AddTransient<IConsentService, ConsentService>();
            builder.Services.AddTransient<IAuthenticationService, AuthenticationService>();
            builder.Services.AddTransient<IMessageService, MessageService>();
            builder.Services.AddTransient<IChatService, ChatService>();
            builder.Services.AddTransient<IAzureB2CService, AzureB2CService>();
            builder.Services.AddTransient<ISendGridService, SendGridService>();
            builder.Services.AddTransient<IFhirPatientService, FhirPatientService>();
            builder.Services.AddTransient<ICmsService, CmsService>();
            builder.Services.AddTransient<IKeyVaultService, KeyVaultService>();
            builder.Services.AddLogging();
        }
    }
}
