using CareAPI.Core.DTO;
using System.Text;
using Newtonsoft.Json;
using Medlix.Backend.API.Core.DTO;
using System.Net.Http.Headers;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Logging;
using CareAPI.Core.DTO.Timeline;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Medlix.Backend.API.BAL.FhirPatientService
{
    public class FhirPatientService : IFhirPatientService
    {
        private static string ApiUrl;

        public FhirPatientService()
        {
            ApiUrl = Environment.GetEnvironmentVariable("ApiUrl");
            if (!string.IsNullOrEmpty(ApiUrl) && !ApiUrl.EndsWith("/"))
            {
                ApiUrl += "/";
            }
        }

        public async Task<bool> UpdatePatientTelecom(string patientId, string registrationId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var apiResponse = await client.GetStringAsync(ApiUrl + "Patient/" + patientId);
                    PatientData patientData = new PatientData();
                    patientData = JsonConvert.DeserializeObject<PatientData>(apiResponse);

                    Telecom telecom = new Telecom();
                    telecom.value = registrationId;
                    patientData.telecom = new List<Telecom>();
                    patientData.telecom.Add(telecom);

                    string serializeData = JsonConvert.SerializeObject(patientData);
                    var content = new StringContent(serializeData, Encoding.UTF8, "application/json");


                    var response = await client.PutAsync(ApiUrl + "Patient/" + patientId, content);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<PatientData> GetPatientByID(string patientId, string jwt, ILogger log)
        {
            patientId = patientId.Trim();
            using (var client = new HttpClient())
            {
                log.LogInformation("Get patient function started.");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                log.LogInformation("querying: " + ApiUrl + "fhir/Patient/" + patientId);
                var apiResponse = await client.GetStringAsync(ApiUrl + "fhir/Patient/" + patientId);
                log.LogInformation("Got the response from patient api.", apiResponse);
                var patientData = JsonConvert.DeserializeObject<PatientData>(apiResponse);
                return patientData;
            }
        }

        public async Task<bool> UpdatePatientEmailPhone(string patientId, string email, string phone, string jwt, ILogger log)
        {
            email = email.Trim();
            phone = phone.Trim();
            patientId = patientId.Trim();
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                    log.LogInformation("querying: " + ApiUrl + "fhir/Patient/" + patientId);
                    var apiResponse = await client.GetStringAsync(ApiUrl + "fhir/Patient/" + patientId);
                    PatientData patientData = new PatientData();
                    patientData = JsonConvert.DeserializeObject<PatientData>(apiResponse);

                    if (patientData.telecom.Where(t => t.system == "email" && t.value == email).Count() == 0)
                    {
                        patientData.telecom.Add(new Telecom
                        {
                            value = email,
                            system = "email",
                        });
                    }

                    if (patientData.telecom.Where(t => t.system == "phone" && t.value == phone).Count() == 0)
                    {
                        patientData.telecom.Add(new Telecom
                        {
                            value = phone,
                            system = "phone",
                        });
                    }


                    string serializeData = JsonConvert.SerializeObject(patientData);
                    var content = new StringContent(serializeData, Encoding.UTF8, "application/json");

                    log.LogInformation("making request to: " + ApiUrl + "fhir/Patient/" + patientId);
                    var response = await client.PutAsync(ApiUrl + "fhir/Patient/" + patientId, content);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<FhirTaskStatusUpdateResponse> UpdateTaskStatus(string patientId, FhirTaskStatusUpdateRequest fhirTaskStatusUpdateRequest, string jwt)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                    var apiResponse = await client.GetStringAsync($"{ApiUrl}fhir/Task/{fhirTaskStatusUpdateRequest.TaskId}");

                    var taskData = JsonConvert.DeserializeObject<FhirTaskModel>(apiResponse);

                    if (taskData?.Owner.Reference == $"Patient/{patientId}")
                    {
                        List<FhirTaskPatchPayload> patchPayloads = new List<FhirTaskPatchPayload>();
                        FhirTaskPatchPayload payload = new FhirTaskPatchPayload
                        {
                            Op = "replace",
                            Path = "/status",
                            Value = fhirTaskStatusUpdateRequest.Status
                        };
                        patchPayloads.Add(payload);
                        string serializeData = JsonConvert.SerializeObject(patchPayloads);
                        var content = new StringContent(serializeData, Encoding.UTF8, "application/json");

                        var response = await client.PatchAsync($"{ApiUrl}fhir/Task/{fhirTaskStatusUpdateRequest.TaskId}", content);
                        var responseJson = await response.Content.ReadAsStringAsync();
                        var json = JsonConvert.DeserializeObject(responseJson);
                        return new FhirTaskStatusUpdateResponse
                        {
                            Success = true,
                            UpdatedTask = json
                        };
                    }
                    else
                    {
                        return new FhirTaskStatusUpdateResponse
                        {
                            Success = false,
                            ErrorMessage = "The Task is not owned by this user"
                        };
                    }

                }
            }
            catch (Exception ex)
            {
                return new FhirTaskStatusUpdateResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<AppointmentData> GetAppointment(string appointmentId, string jwt,ILogger logger)
        {
            using (var client = new HttpClient())
            {
                logger.LogInformation("Get appointment function started.");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                var apiResponse = await client.GetStringAsync($"{ApiUrl}fhir/Appointment/{appointmentId}");
                logger.LogInformation("Got the response from appointment api.",apiResponse);
                var appointmentData = JsonConvert.DeserializeObject<AppointmentData>(apiResponse);
                return appointmentData;
            }
        }

        public async Task<AppointmentData> UpdateAppointment(AppointmentData appointment,string jwt,ILogger logger)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    logger.LogInformation("update appointment function started");

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                    var serializerSettings = new JsonSerializerSettings();
                    serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    string serializeData = JsonConvert.SerializeObject(appointment, serializerSettings);

                    var content = new StringContent(serializeData, Encoding.UTF8, "application/json");

                    var apiResponse = await client.PutAsync($"{ApiUrl}fhir/Appointment/{appointment.Id}",content);
                    logger.LogInformation("Response from update appointment function started");

                    var responseJson = await apiResponse.Content.ReadAsStringAsync();
                    var json = JsonConvert.DeserializeObject<AppointmentData>(responseJson);
                    return json;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<FhirTaskDto> CreateTask(FhirTaskDto fhirTaskDto, string jwt)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                string serializeData = JsonConvert.SerializeObject(fhirTaskDto, serializerSettings);

                var content = new StringContent(serializeData, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{ApiUrl}fhir/Task", content);

                var responseJson = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<FhirTaskDto>(responseJson);
                return json;
            }
        }

        public async Task<FhirTask> CreateTaskWithEncounterId(FhirTask fhirTaskDto, string jwt, string encounterId,ILogger logger)
        {
            using (var client = new HttpClient())
            {
                logger.LogInformation("Create task function started");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                string serializeData = JsonConvert.SerializeObject(fhirTaskDto, serializerSettings);

                var content = new StringContent(serializeData, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{ApiUrl}fhir/Task?encounter={encounterId}", content);
                logger.LogError("Response from created encounter request", response);
                var responseJson = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<FhirTask>(responseJson);
                return json;
            }
        }

        public async Task<EncounterResponse> CreateEncounter(string jwt,string appointmentId,string patientId,ILogger logger)
        {
            try
            {
                logger.LogInformation("Started create encounter.");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                    var serializerSettings = new JsonSerializerSettings();
                    serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    EncounterModel encounterModel = new EncounterModel();
                    encounterModel.resourceType = "Encounter";
                    encounterModel.subject = new Subject { reference= $"Patient/{patientId}" };
                    encounterModel.status = "planned";
                    var appointmentRefernce =new Appointment { reference = $"Appointment/{appointmentId}" };
                    var encounter = new EncounterModel
                    {
                        resourceType = "Encounter",
                        subject = new Subject { reference = $"Patient/{patientId}" },
                        status = "planned",
                        appointment = new List<Appointment>(),
                        encounterClass = new EncounterClass { code = "AMB", display = "ambulatory", system = "http://terminology.hl7.org/CodeSystem/v3-ActCode" }

                    };
                    encounter.appointment.Add(appointmentRefernce);
                    logger.LogInformation("Encounter payload that send with request", encounter);
                    string serializeData = JsonConvert.SerializeObject(encounter, serializerSettings);
                    serializeData = serializeData.Replace("encounterClass", "class");
                    logger.LogInformation($"Url for encounter {ApiUrl}fhir/Encounter?Appointment={appointmentId}", encounter);

                    var content = new StringContent(serializeData, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync($"{ApiUrl}fhir/Encounter?Appointment={appointmentId}", content);
                    logger.LogInformation("Encounter response.", response);

                    var responseJson = await response.Content.ReadAsStringAsync();
                    var json = JsonConvert.DeserializeObject<EncounterResponse>(responseJson);
                    return json;
                }
            }
            catch (Exception ex)
            {

                logger.LogInformation(ex.Message);
                throw ex;
            }
        } 

        public async Task<FhirTaskDto> UpdateTask(FhirTaskDto fhirTaskDto, string jwt)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                string serializeData = JsonConvert.SerializeObject(fhirTaskDto, serializerSettings);

                var content = new StringContent(serializeData, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{ApiUrl}fhir/Task/{fhirTaskDto.Id}", content);

                var responseJson = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<FhirTaskDto>(responseJson);
                return json;
            }
        }

        public async Task<FhirAppointmentTaskDto> GetTasksRelatedAppointment(string appointmentId, string jwt)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                var response = await client.GetAsync($"{ApiUrl}fhir/Task?group-identifier={appointmentId}");

                var responseJson = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<FhirAppointmentTaskDto>(responseJson);
                return json;
            }
        }

        public async Task<PatientSearchResult> GetPatientByIdentifier(string identifierValue, string jwt)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                var response = await client.GetAsync($"{ApiUrl}fhir/Patient?identifier=http://example.com/v2-to-fhir-converter/Identifier/CS|{identifierValue}");

                var responseJson = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<PatientSearchResult>(responseJson);
                return json;
            }
        }

        /// <summary>
        /// Create search parameter in FHIR
        /// </summary>
        /// <returns>Returns true or false</returns>
        public async Task<bool> CreateSearchParameter(string jwt)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                    SearchParameter searchParameter = new SearchParameter();
                    searchParameter.Name = "appointment-sort-start";
                    searchParameter.ResourceType = "SearchParameter";
                    searchParameter.Id = "appointment-sort-start";
                    searchParameter.Url = "http://hl7.org/fhir/SearchParameter/sort-start";
                    searchParameter.Version = "3.1.1";
                    searchParameter.Status = "active";
                    searchParameter.Description = "special datetime to facilitate appointment sorting";
                    searchParameter.Code = "appointment-sort-start";
                    searchParameter.Base = new List<string>();
                    searchParameter.Base.Add("Appointment");
                    searchParameter.Type = "date";
                    searchParameter.Expression = "Appointment.extension.where(url = 'http://fhir.medlix.org/StructureDefinition/appointment-start-sort-date').value";


                    string serializeData = JsonConvert.SerializeObject(searchParameter, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });
                    var content = new StringContent(serializeData, Encoding.UTF8, "application/json");


                    var response = await client.PutAsync($"{ApiUrl}fhir/SearchParameter/appointment-sort-start", content);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Create communication in FHIR
        /// </summary>
        /// <returns>Returns true or false</returns>
        public async Task<ResponseType> SendMessage(SendMsgDto sendMsgDto, string jwt)
        {
            var responseObject = new ResponseType
            {
                communicationResponse = false,
                patchResponse = false
            };
            try
            {
                bool isPatientSender = false;
                string careTeam = "";
                string subject = "";
                string about = "";
                if (sendMsgDto.Sender.Contains("Group/"))
                {
                    careTeam = $"CareTeam/{sendMsgDto.Sender.Substring("Group/".Length)}";
                    subject = sendMsgDto.Receiver;
                    about = sendMsgDto.Sender;
                } else
                {
                    careTeam = $"CareTeam/{sendMsgDto.Receiver.Substring("Group/".Length)}";
                    isPatientSender = true;
                    subject = sendMsgDto.Sender;
                    about = sendMsgDto.Receiver;
                }
                
                
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                    // create one communication
                    CommunicationDto communication = new CommunicationDto();
                    communication.ResourceType = "Communication";
                    communication.Status = "completed";
                    communication.Sent = DateTime.UtcNow;
                    communication.Sender = new Sender { reference = sendMsgDto.Sender };
                    communication.Recipient = new List<Recipient>();
                    communication.Recipient.Add(new Recipient { reference = sendMsgDto.Receiver });
                    communication.Subject = new Subject { reference = subject };
                    communication.About = new List<About> { new About{ reference = about } };
                    communication.Payload = new List<Payload> { new Payload { contentString = sendMsgDto.Message } };
                    communication.Extension = new List<Core.DTO.Extension> { 
                        new Core.DTO.Extension {
                            url=$"http://fhir.medlix.org/StructureDefinition/communication-careteam",
                            valueReference = new Subject {reference = careTeam}
                        }
                    };

                    string payload = JsonConvert.SerializeObject(communication, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"{ApiUrl}fhir/Communication", content);
                    responseObject.communicationResponse = response.IsSuccessStatusCode;

                    // update unread-message-count
                    // get careteam to obtain unread-message-count, patient-unread-message-count
                    var resCareTeam = await client.GetAsync($"{ApiUrl}fhir/{careTeam}");
                    var resJsonCareTeam = await resCareTeam.Content.ReadAsStringAsync();
                    var careTeamDto = JsonConvert.DeserializeObject<CareTeamDto>(resJsonCareTeam);

                    var unreadMsgCount = careTeamDto?.Extension?.Where(x => x.Url == "unread-message-count").FirstOrDefault()?.ValueInteger;
                    var unreadPatientMsgCount = careTeamDto?.Extension?.Where(x => x.Url == "patient-unread-message-count").FirstOrDefault()?.ValueInteger;
                    if (unreadMsgCount != null)
                    {
                        unreadMsgCount += 1;
                    } else
                    {
                        unreadMsgCount = 0;
                    }

                    if (unreadPatientMsgCount != null)
                    {
                        unreadPatientMsgCount += 1;
                    }
                    else
                    {
                        unreadPatientMsgCount = 0;
                    }

                    List<PatchPayload> updateCareTeam = new List<PatchPayload>();
                    updateCareTeam.Add(new PatchPayload
                    {
                        op = "replace",
                        path = "extension/0",
                        value = new Value
                        {
                            url = "extension/0",
                            valueString = sendMsgDto.Message
                        }
                    });
                    updateCareTeam.Add(new PatchPayload
                    {
                        op = "replace",
                        path = "extension/0",
                        value = new Value
                        {
                            url = "last-message",
                            valueString = sendMsgDto.Message
                        }
                    });
                    updateCareTeam.Add(new PatchPayload
                    {
                        op = "replace",
                        path = "/extension/2",
                        value = new Value
                        {
                            url = "last-message-datetime",
                            valueDateTime = DateTime.UtcNow
                        }
                    });
                    if (isPatientSender)
                    {
                        updateCareTeam.Add(new PatchPayload
                        {
                            op = "replace",
                            path = "/extension/4",
                            value = new Value
                            {
                                url = "unread-message-count",
                                valueInteger = unreadMsgCount
                            }
                        });
                    } else
                    {
                        updateCareTeam.Add(new PatchPayload
                        {
                            op = "replace",
                            path = "/extension/5",
                            value = new Value
                            {
                                url = "patient-unread-message-count",
                                valueInteger = unreadPatientMsgCount
                            }
                        });
                    }

                    string serializeData = JsonConvert.SerializeObject(updateCareTeam, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });
                    var patchContent = new StringContent(serializeData, Encoding.UTF8, "application/json");
                    var patchResponse = await client.PatchAsync($"{ApiUrl}fhir/{careTeam}", patchContent);
                    responseObject.patchResponse = patchResponse.IsSuccessStatusCode;
                    return responseObject;
                }
            }
            catch (Exception ex)
            {
                return responseObject;
            }
        }

        /// <summary>
        /// Reset Unread-message-count in FHIR
        /// </summary>
        /// <returns>Returns true or false</returns>
        public async Task<bool> ReadMessage(ReadMsgDto readMsgDto, string jwt)
        {
            try
            {
                bool isPatientSender = false;
                string careTeam = "";
                if (readMsgDto.Sender.Contains("Group/"))
                {
                    careTeam = $"CareTeam/{readMsgDto.Sender.Substring("Group/".Length)}";
                }
                else
                {
                    careTeam = $"CareTeam/{readMsgDto.Receiver.Substring("Group/".Length)}";
                    isPatientSender = true;
                }


                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                    
                    List<PatchPayload> updateCareTeam = new List<PatchPayload>();
                    if (!isPatientSender)
                    {
                        updateCareTeam.Add(new PatchPayload
                        {
                            op = "replace",
                            path = "/extension/4",
                            value = new Value
                            {
                                url = "unread-message-count",
                                valueInteger = 0
                            }
                        });
                    }
                    else
                    {
                        updateCareTeam.Add(new PatchPayload
                        {
                            op = "replace",
                            path = "/extension/5",
                            value = new Value
                            {
                                url = "patient-unread-message-count",
                                valueInteger = 0
                            }
                        });
                    }

                    string serializeData = JsonConvert.SerializeObject(updateCareTeam, new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });
                    var patchContent = new StringContent(serializeData, Encoding.UTF8, "application/json");
                    var patchResponse = await client.PatchAsync($"{ApiUrl}fhir/{careTeam}", patchContent);
                    return patchResponse.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Get Patient everything in FHIR
        /// </summary>
        /// <returns>Returns List<dynamic> appointment or communication, patient, etc </returns>
        public async Task<List<dynamic>> GetPatientEverything(string patientId, string jwt)
        {
            List<dynamic> entries = new List<dynamic>();
            try
            {
                var allReferences = new List<string>();
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                    var response = await client.GetAsync($"{ApiUrl}fhir/Patient/{patientId}/$everything");

                    var responseJson = await response.Content.ReadAsStringAsync();
                    var json = JsonConvert.DeserializeObject<PatientEverythingResult>(responseJson);
                    // Get all Reference
                    var references = GetAllReference(responseJson);
                    if (references.Count > 0)
                    {
                        allReferences.AddRange(references.Distinct().ToList());
                    }

                    var nextLink = json?.Link?.Where(x => x.Relation == "next")?.FirstOrDefault()?.Url;

                    if (json?.Entry?.Count() > 0)
                    {
                        entries.AddRange(json.Entry);
                    }
                    else
                    {
                        nextLink = null;
                    }
                    while (!string.IsNullOrEmpty(nextLink))
                    {
                        string continuationToken = nextLink.Split("?ct=")[1];
                        response = await client.GetAsync($"{ApiUrl}fhir/Patient/{patientId}/$everything?ct={continuationToken}");
                        responseJson = await response.Content.ReadAsStringAsync();
                        // Get all Reference
                        references = GetAllReference(responseJson);
                        if (references.Count > 0)
                        {
                            allReferences.AddRange(references.Distinct().ToList());
                        }

                        json = JsonConvert.DeserializeObject<PatientEverythingResult>(responseJson);
                        nextLink = json?.Link?.Where(x => x.Relation == "next")?.FirstOrDefault()?.Url;
                        if (json?.Entry?.Count() > 0)
                        {
                            entries.AddRange(json.Entry);
                        }
                        else
                        {
                            nextLink = null;
                        }
                    }

                    allReferences = allReferences.Distinct().ToList();
                    var allResourceLinks = new List<string>();

                    foreach (var entry in entries)
                    {
                        allResourceLinks.Add($"{entry.resource.resourceType}/{entry.resource.id}");
                        if (entry.resource.resourceType == "Patient")
                        {
                            Random rnd = new Random();
                            string rndString = rnd.Next(3000).ToString();
                            DateTime start = new DateTime(1975, 1, 1);
                            int range = (DateTime.Today - start).Days;
                            
                            var resource = entry.resource;
                            resource.name[0].family = rndString;
                            resource.name[0].given[0] = "Test Patient";
                            
                            if (((JArray)resource.name[0].given).Count > 1)
                            {
                                resource.name[0].given[1] = rndString;
                            }

                            JToken name = resource.name[0];
                            resource.name = new JArray
                            {
                                name,
                            };

                            var objResource = (JObject)resource;
                            if (objResource.ContainsKey("maritalStatus"))
                                objResource.Property("maritalStatus").Remove();
                            if (objResource.ContainsKey("deceasedBoolean"))
                                objResource.Property("deceasedBoolean").Remove();
                            if (objResource.ContainsKey("multipleBirthBoolean"))
                                objResource.Property("multipleBirthBoolean").Remove();
                            if (objResource.ContainsKey("telecom"))
                                resource.telecom.Clear();
                            if (objResource.ContainsKey("address"))
                                resource.address.Clear();

                            if (objResource.ContainsKey("identifier"))
                            {
                                var identifiers = (JArray)resource.identifier;
                                JToken csIdentifier = null;
                                for (int k = 0; k < identifiers.Count; k++)
                                {
                                    if ($"{resource.identifier[k].system}" == "http://example.com/v2-to-fhir-converter/Identifier/CS")
                                    {
                                        csIdentifier = resource.identifier[k];
                                    }
                                }
                                if (csIdentifier != null)
                                {
                                    resource.identifier = new JArray
                                    {
                                        csIdentifier
                                    };
                                }
                            }
                            
                            resource.birthDate = start.AddDays(rnd.Next(range)).ToString("yyyy-MM-dd");
                        }
                    }
                    // Get Unique Reference from 2 lists
                    var uniqueReference = allReferences.Except(allResourceLinks).ToList();
                    if (uniqueReference.Count > 0)
                    {
                        var tasks = new List<Task<dynamic>>();
                        foreach (var reference in uniqueReference)
                        {
                            tasks.Add(Task.Run(async () => await GetOneResource(reference, jwt)));
                        }
                        var results = await Task.WhenAll(tasks);
                        var availableRefResource = results.Where(x => x != null).ToList();
                        if (availableRefResource.Count > 0)
                        {
                            entries.AddRange(availableRefResource);
                        }
                    }
                    return entries;
                }
            }
            catch (Exception ex)
            {
                return entries;
            }
        }
        private List<string> GetAllReference(string response)
        {
            var m1 = Regex.Matches(response, "\"reference\":\"(.*?)\"");
            var list = new List<string>();
            foreach (Match match in m1)
            {
                list.Add(match.Value.Replace("\"reference\":\"", "").Replace("\"", ""));
            }
            return list;
        }

        private async Task<dynamic> GetOneResource(string resourceUrl, string jwt)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                var response = await client.GetAsync($"{ApiUrl}fhir/{resourceUrl}");

                var responseJson = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<dynamic>(responseJson);
                if (json != null && json.resourceType == resourceUrl.Split("/")[0])
                {
                    return json;
                }
                return null;
            }
        }
        /// <summary>
        /// Create or Update Patient everything in FHIR
        /// </summary>
        /// <returns>Returns string </returns>
        public async Task<string> CreateOrUpdatePatientEverything(List<dynamic> importDto, string jwt)
        {
            int successCount = 0;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                foreach (var entry in importDto)
                {
                    try
                    {
                        string serializeData = JsonConvert.SerializeObject(entry.resource, new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        });
                        var putContent = new StringContent(serializeData, Encoding.UTF8, "application/json");
                        var response = await client.PutAsync($"{ApiUrl}fhir/{entry.resource.resourceType}/{entry.resource.id}", putContent);
                        if (response.IsSuccessStatusCode)
                        {
                            successCount++;
                        }
                    }
                    catch
                    {

                    }
                }
            }
            return $"Created or Updated resource {successCount} of {importDto.Count}";
        }
        public async Task<AppointmentData?> CreateAppointment(AppointmentData appointment, string jwt)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

                string serializeData = JsonConvert.SerializeObject(appointment, serializerSettings);

                var content = new StringContent(serializeData, Encoding.UTF8, "application/json");

                var apiResponse = await client.PostAsync($"{ApiUrl}fhir/Appointment", content);

                if (apiResponse.IsSuccessStatusCode)
                {
                    serializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                    return JsonConvert.DeserializeObject<AppointmentData>(apiResponse.Content.ReadAsStringAsync().Result, serializerSettings);
                }

                return null;
            }
        }
        
        #region Timeline
        public async Task<TimelineResult> GetPatientCombinedTimelineData(string patientId, string startDate, string endDate, string jwt)
        {
            List<dynamic> entries = new List<dynamic>();
            var tasks = new List<Task<PaginationResult>>();
            string queryString = "";
            // Get Appointment data 
            if (!string.IsNullOrEmpty(startDate))
            {
                queryString += $"&date=gt{startDate}";
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                queryString += $"&date=lt{endDate}";
            }

            tasks.Add(Task.Run(async () => await GetNextThreePaginationData($"{ApiUrl}fhir/Appointment?patient=Patient/{patientId}{queryString}&_sort=date", "Appointment", jwt)));
            
            // Get Observation data
            tasks.Add(Task.Run(async () => await GetNextThreePaginationData($"{ApiUrl}fhir/Observation?patient=Patient/{patientId}{queryString}&_sort=date", "Observation", jwt, 30)));

            // Get QuestionnaireResponse data
            string queryString2 = "";
            if (!string.IsNullOrEmpty(startDate))
            {
                queryString2 += $"&authored=gt{startDate}";
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                queryString2 += $"&authored=lt{endDate}";
            }
            
            tasks.Add(Task.Run(async () => await GetNextThreePaginationData($"{ApiUrl}fhir/QuestionnaireResponse?patient=Patient/{patientId}{queryString2}&_sort=authored", "QuestionnaireResponse", jwt)));
            
            // Get Communication data
            string queryString3 = "";
            if (!string.IsNullOrEmpty(startDate))
            {
                queryString3 += $"&sent=gt{startDate}";
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                queryString3 += $"&sent=lt{endDate}";
            }
            
            tasks.Add(Task.Run(async () => await GetNextThreePaginationData($"{ApiUrl}fhir/Communication?patient=Patient/{patientId}{queryString3}&_sort=sent", "Communication", jwt, 50)));
            
            var results = await Task.WhenAll(tasks);
            bool extraAppointment = false;
            bool extraCommunication = false;
            bool extraObservation = false;
            bool extraQR = false;
            foreach(var result in results)
            {
                if (result.ResourceType == "Appointment")
                {
                    extraAppointment = result.ExtraMore;
                    if (result.Entry?.Count > 0)
                    {
                        entries.AddRange(result.Entry);
                    }
                }
                else if (result.ResourceType == "Communication")
                {
                    extraCommunication = result.ExtraMore;
                    if (result.Entry?.Count > 0)
                    {
                        entries.AddRange(AggregationResource(result.Entry));
                    }
                }
                else if (result.ResourceType == "Observation")
                {
                    extraObservation = result.ExtraMore;
                    if (result.Entry?.Count > 0)
                    {
                        entries.AddRange(AggregationResource(result.Entry));
                    }
                }
                else if (result.ResourceType == "QuestionnaireResponse")
                {
                    extraQR = result.ExtraMore;
                    if (result.Entry?.Count > 0)
                    {
                        entries.AddRange(result.Entry);
                    }
                }
            }

            foreach (var entry in entries)
            {
                if (entry.resource.resourceType == "Appointment")
                {
                    entry.resource.orderableDate = entry.resource.start;
                }
                else if (entry.resource.resourceType == "Observation")
                {
                    entry.resource.orderableDate = entry.resource.effectiveDateTime;
                }
                else if (entry.resource.resourceType == "QuestionnaireResponse")
                {
                    entry.resource.orderableDate = entry.resource.authored;
                }
                else
                {
                    entry.resource.orderableDate = entry.resource.sent;
                }
            }

            return new TimelineResult
            {
                Entry = entries.OrderBy(x => x.resource.orderableDate).ToList(),
                ExtraAppointment = extraAppointment,
                ExtraCommunication = extraCommunication,
                ExtraObservation = extraObservation,
                ExtraQuestionnaireResponse = extraQR
            };
        }

        private async Task<PaginationResult> GetNextThreePaginationData(string url, string resourceType, string jwt, int _count=10)
        {
            url += $"&_count={_count}";
            using (var client = new HttpClient())
            {
                List<dynamic> entries = new List<dynamic>();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                var response = await client.GetAsync(url);

                var responseJson = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<SearchBundleResult>(responseJson);

                var nextLink = json?.Link?.Where(x => x.Relation == "next")?.FirstOrDefault()?.Url;

                if (json?.Entry?.Count() > 0)
                {
                    entries.AddRange(json.Entry);
                }
                else
                {
                    nextLink = null;
                }
                int count = 0;
                while (!string.IsNullOrEmpty(nextLink) || count > 1)
                {
                    string continuationToken = nextLink.Split("?ct=")[1];
                    response = await client.GetAsync($"{url}?ct={continuationToken}");
                    responseJson = await response.Content.ReadAsStringAsync();
                    json = JsonConvert.DeserializeObject<SearchBundleResult>(responseJson);
                    nextLink = json?.Link?.Where(x => x.Relation == "next")?.FirstOrDefault()?.Url;
                    if (json?.Entry?.Count() > 0)
                    {
                        entries.AddRange(json.Entry);
                    }
                    else
                    {
                        nextLink = null;
                    }
                    count++;
                }
                
                return new PaginationResult
                {
                    Entry = entries,
                    ExtraMore = count > 1,
                    ResourceType = resourceType
                };
            }
        }
        
        private List<dynamic> AggregationResource(List<dynamic> entries)
        {
            List<dynamic> dates = new List<dynamic>();
            List<dynamic> aggregatedEntries = new List<dynamic>();
            if (entries[0].resource.resourceType == "Observation")
            {
                dates = entries.Select(x => Convert.ToDateTime(x.resource.effectiveDateTime).Date).Distinct().ToList();
                foreach (var date in dates)
                {
                    var groupedEntries = entries.Where(x => Convert.ToDateTime(x.resource.effectiveDateTime).Date == date.Date).ToList();
                    var lastEntry = groupedEntries.LastOrDefault();
                    lastEntry.resource.aggregatedCount = groupedEntries.Count-1;
                    aggregatedEntries.Add(lastEntry);
                }
            } 
            else if (entries[0].resource.resourceType == "Communication")
            {
                dates = entries.Select(x => Convert.ToDateTime(x.resource.sent).Date).Distinct().ToList();
                foreach (var date in dates)
                {
                    var groupedEntries = entries.Where(x => Convert.ToDateTime(x.resource.sent).Date == date.Date).ToList();
                    var lastEntry = groupedEntries.LastOrDefault();
                    lastEntry.resource.aggregatedCount = groupedEntries.Count - 1;
                    aggregatedEntries.Add(lastEntry);
                }
            }
                        
            return aggregatedEntries;
        }
        #endregion
    }
}
