using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;

using Medlix.Backend.API.BAL.KeyVaultService;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Medlix.Backend.API
{
    public class DigiDSignInFunction
    {
        private readonly IKeyVaultService _keyVaultService;

        public DigiDSignInFunction(IKeyVaultService keyVaultService)
        {
            _keyVaultService = keyVaultService;
        }

        /// <summary>
        /// Generate signed SAML Metadata XML
        /// </summary>
        /// <param name="req">GET: /SamlMetadata?service_uuid={SERVICE_UUID}</param>
        /// <param name="log">Logging dependency injection</param>
        /// <returns>Signed SAML Metadata XML</returns>
        [FunctionName("SamlMetadata")]
        public async Task<IActionResult> SamlMetadata(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("HTTP trigger function GenerateSamlWithAllEndpoints.");

            var serviceUuid = req.Query["service_uuid"].ToString();
            if (string.IsNullOrWhiteSpace(serviceUuid) &&
                !serviceUuid.Equals(Environment.GetEnvironmentVariable("SamlMetadataServiceUuid"))) {
                    log.LogError("Could not find service UUID as query param and did not find it as env variable. ");
                return new BadRequestResult();
            }

            var samlAppSubscriptionId = Environment.GetEnvironmentVariable("SamlAppSubscriptionId");

            var config = new Saml2Configuration();
            config.Issuer = Environment.GetEnvironmentVariable("ApiUrl");
            config.AudienceRestricted = bool.Parse(Environment.GetEnvironmentVariable("SamlAudienceRestricted")) || false;
            config.SignatureAlgorithm = Environment.GetEnvironmentVariable("SamlSignatureAlgorithm");
            config.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;
            config.RevocationMode = X509RevocationMode.NoCheck;
            config.SignAuthnRequest = true;

            var certificate = await FetchCertificateFromSecrets();
            if (certificate == null) {
                certificate = GenerateCertificate();
            }
            config.SigningCertificate = certificate;

            var entityDescriptor = new EntityDescriptor(config, true);
            entityDescriptor.ValidUntil = Convert.ToInt32(Environment.GetEnvironmentVariable("SamlCertValidityDays"));
            entityDescriptor.SPSsoDescriptor = new SPSsoDescriptor {
                WantAssertionsSigned = true,
                AuthnRequestsSigned = true,
                AssertionConsumerServices = new AssertionConsumerService[] {
                    new AssertionConsumerService() {
                        Binding = ProtocolBindings.HttpPost,
                        Location = new Uri(Environment.GetEnvironmentVariable("SamlAssertionConsumerService")),
                        IsDefault = true
                    }
                },
                AttributeConsumingServices = new AttributeConsumingService[] {
                    new AttributeConsumingService() {
                        ServiceName = new ServiceName(Environment.GetEnvironmentVariable("SamlMetadataAcsServiceName"),
                            Environment.GetEnvironmentVariable("SamlMetadataAcsServiceLanguage")),
                        RequestedAttributes = new RequestedAttribute[] {
                            new RequestedAttribute("urn:nl-eid-gdi:1.0:ServiceUUID"),
                            // TODO: add serviceUuid as child of RequestedAttribute
                            new RequestedAttribute(serviceUuid, true)
                        }
                    }
                },
                SigningCertificates = new X509Certificate2[] { config.SigningCertificate }
            };

            var metadata = new Saml2Metadata(entityDescriptor).CreateMetadata();
            return new OkObjectResult(metadata.ToXml());
        }

        private X509Certificate2 GenerateCertificate()
        {
            var subjectName = Environment.GetEnvironmentVariable("SamlCertSubjectName");
            var validityInDays = Environment.GetEnvironmentVariable("SamlCertValidityDays");

            var request = new CertificateRequest($"CN={subjectName}",
                RSA.Create(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(Convert.ToInt32(validityInDays)));
        }

        private async Task<X509Certificate2> FetchCertificateFromSecrets()
        {
            var certificateString = await _keyVaultService.GetSecretValue(Environment.GetEnvironmentVariable("SamlCertificateSecretKey"));

            if (string.IsNullOrEmpty(certificateString)) {
                return null;
            }
            byte[] privateKeyBytes = Convert.FromBase64String(certificateString);
            var certificate = new X509Certificate2(privateKeyBytes, (string)null, X509KeyStorageFlags.MachineKeySet);
            return certificate;
        }
    }
}
