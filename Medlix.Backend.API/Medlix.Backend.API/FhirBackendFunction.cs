using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Medlix.Backend.API.Core.DTO;
using Medlix.Backend.API;
using CareAPI.Core.DTO;
using System.Collections.Generic;
using System.Linq;
using Medlix.Backend.API.BAL.AuthenticationService;
using Medlix.Backend.API.BAL.FhirPatientService;
using Medlix.Backend.API.BAL.CmsService;
using Medlix.Backend.API.BAL.AzureB2CService;
using System.IO;
using Newtonsoft.Json;
using Medlix.Backend.API.BAL.MessageService;
using Medlix.Backend.API.BAL.ChatService;
using Medlix.Backend.API.BAL;
using Microsoft.Extensions.Primitives;
using ExecutionPeriod = CareAPI.Core.DTO.ExecutionPeriod;

namespace CareAPI
{
    public class FhirBackendFunction
    {
        private readonly IAuthenticationService _authenticationService;
        private IFhirPatientService _fhirPatientService;
        private ICmsService _cmsService;
        private IAzureB2CService _azureB2cService;
        private readonly IMessageService _messageService;
        private readonly IChatService _chatService;

        public FhirBackendFunction(
            IMessageService messageService,
            IChatService chatService,
            IAuthenticationService authenticationService,
            IFhirPatientService fhirPatientService,
            ICmsService cmsService,
            IAzureB2CService azureB2cService
        )
        {
            _authenticationService = authenticationService;
            _fhirPatientService = fhirPatientService;
            _cmsService = cmsService;
            _messageService = messageService;
            _chatService = chatService;
            _azureB2cService = azureB2cService;
        }

        /// <summary>
        /// Send message to FHIR url
        /// </summary>
        /// <param name="req">request body is deserialized into SendMsgDto</param>
        /// <param name="log">ILogger is used to log the information or errors</param>
        /// <returns>OkObjectResult</returns>
        /// <exception>ErrorHandler.BadRequestResult</exception>
        [FunctionName("SendMessage")]
        public async Task<IActionResult> SendMessage([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] SendMsgDto sendMsgDto, HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            log.LogInformation("send message function triggered request" + sendMsgDto);
            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);

                if (!authUser.IsAuthenticated)
                {
                    return new UnauthorizedResult();
                }

                if (sendMsgDto.Sender.Contains("Patient/") && (sendMsgDto.Sender.Substring("Patient/".Length) != authUser.PatientId && Environment.GetEnvironmentVariable("Env") != "dev"))
                {
                    return new UnauthorizedObjectResult(new { Message = "Unauthorized for the patient" });
                }
                var responseObject = await _fhirPatientService.SendMessage(sendMsgDto, authUser.ValidJwtToken);

                return new OkObjectResult(responseObject);
            }
            catch (Exception ex)
            {
                log.LogInformation("Returning exception: " + ex.ToString());
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        /// <summary>
        /// Read message to FHIR url (Reset unread-message-count)
        /// </summary>
        /// <param name="req">request body is deserialized into ReadMsgDto</param>
        /// <param name="log">ILogger is used to log the information or errors</param>
        /// <returns>OkObjectResult</returns>
        /// <exception>ErrorHandler.BadRequestResult</exception>
        [FunctionName("ReadMessage")]
        public async Task<IActionResult> ReadMessage([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] ReadMsgDto readMsgDto, HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            log.LogInformation("send message function triggered request" + readMsgDto);
            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);

                if (!authUser.IsAuthenticated)
                {
                    return new UnauthorizedResult();
                }

                if (readMsgDto.Sender.Contains("Patient/") && (readMsgDto.Sender.Substring("Patient/".Length) != authUser.PatientId && Environment.GetEnvironmentVariable("Env") != "dev"))
                {
                    return new UnauthorizedObjectResult(new { Message = "Unauthorized for the patient" });
                }
                var response = await _fhirPatientService.ReadMessage(readMsgDto, authUser.ValidJwtToken);
                return new OkObjectResult(response);

            }
            catch (Exception ex)
            {
                log.LogInformation("Returning exception: " + ex.ToString());
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        /// <summary>
        /// Updates the Task Status in FHIR
        /// </summary>
        /// <param name="fhirTaskStatusUpdateRequest"></param>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns>Returns the response of FHIR call</returns>
        [FunctionName("UpdateTaskStatus")]
        public async Task<IActionResult> UpdateTaskStatus([HttpTrigger(AuthorizationLevel.Function, "post")] FhirTaskStatusUpdateRequest fhirTaskStatusUpdateRequest, HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            log.LogInformation("UpdateTaskStatus function triggered request" + req);

            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
                string patientId = authUser.PatientId;
                if (!authUser.IsAuthenticated || string.IsNullOrEmpty(patientId))
                {
                    return new UnauthorizedResult();
                }

                if (string.IsNullOrEmpty(fhirTaskStatusUpdateRequest.TaskId))
                {
                    return new BadRequestObjectResult(new { Message = "TaskId is required", Error = true });
                }

                if (fhirTaskStatusUpdateRequest.Status != "requested" && fhirTaskStatusUpdateRequest.Status != "completed")
                {
                    return new BadRequestObjectResult(new { Message = "Status should be one of requested, completed", Error = true });
                }

                FhirTaskStatusUpdateResponse fhirTaskStatusUpdateResponse = await _fhirPatientService.UpdateTaskStatus(patientId, fhirTaskStatusUpdateRequest, authUser.ValidJwtToken);
                if (fhirTaskStatusUpdateResponse.Success)
                {
                    return new OkObjectResult(fhirTaskStatusUpdateResponse.UpdatedTask);
                }
                else
                {
                    return new BadRequestObjectResult(new { Message = fhirTaskStatusUpdateResponse.ErrorMessage, Error = true });
                }


            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        /// <summary>
        /// Creates an appointment with a subsequent task into FHIR.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="appointmentId">Route param: CreateTaskFromAppointmentCMS/{appointmentId}</param>
        /// <param name="log"></param>
        /// <returns>Returns patient model, appointment and task model after successful creation in FHIR</returns>
        [FunctionName("CreateTaskFromAppointmentCMS")]
        public async Task<IActionResult> CreateTaskFromAppointmentCMS([HttpTrigger(AuthorizationLevel.Function, "post", Route = "CreateTaskFromAppointmentCMS/{appointmentId}")] HttpRequest req, string appointmentId, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
                if (!authUser.IsAuthenticated)
                {
                    return new UnauthorizedResult();
                }
                if (string.IsNullOrEmpty(appointmentId))
                {
                    return new BadRequestObjectResult(new { Message = "appointmentId is required", Error = true });
                }
                AppointmentData appointmentData = await _fhirPatientService.GetAppointment(appointmentId, authUser.ValidJwtToken, log);

                FhirAppointmentTaskDto appoinmentGroupTask = await _fhirPatientService.GetTasksRelatedAppointment(appointmentId, authUser.ValidJwtToken);

                string appointmentTypeCode = appointmentData?.AppointmentType?.Coding[0]?.Code;

                string extensionValue = null, encounterId = null, patientId = null;

                foreach (var appointment in appointmentData?.Extension)
                {
                    if (appointment.Url.Contains("appointment-schedule-id"))
                    {
                        extensionValue = appointment.ValueString;
                    }
                    if (appointment.Url.Contains("task-encounter-id"))
                    {
                        encounterId = appointment.ValueString;
                    }
                }

                foreach (var participant in appointmentData?.Participant)
                {
                    if (participant.Actor.Reference.Contains("Patient/"))
                    {
                        patientId = participant.Actor.Reference;
                    }
                }
                if (string.IsNullOrEmpty(appointmentTypeCode) || string.IsNullOrEmpty(extensionValue) || string.IsNullOrEmpty(encounterId) || string.IsNullOrEmpty(patientId))
                {
                    return new BadRequestObjectResult(new { Message = "Patient or Extension value or AppointmentType  or Encounter id not present in input message", Error = true });
                }
                var cmsAppointment = await _cmsService.GetCmsAppointment(extensionValue, appointmentTypeCode);

                foreach (var task in cmsAppointment?.Data?[0]?.Tasks)
                {
                    if (!string.IsNullOrEmpty(task.TaskTypes.SchedulingOption) || !string.IsNullOrEmpty(task.TaskTypes.MeasurementSchedulingOption))
                    {
                        string schedulingOption = task.TaskTypes.SchedulingOption ?? task.TaskTypes.MeasurementSchedulingOption;
                        bool isBeforeSchedule = schedulingOption.Contains("before");
                        string scheduleMode = isBeforeSchedule ? schedulingOption.Replace("before", "") : schedulingOption.Replace("after", "");
                        DateTime? executionPeriod = appointmentData.Start;
                        int schedulingAmount = isBeforeSchedule ? -1 * (int)task.TaskTypes.SchedulingAmount : (int)task.TaskTypes.SchedulingAmount;
                        switch (scheduleMode)
                        {
                            case "days":
                                executionPeriod?.AddDays(schedulingAmount);
                                break;
                            case "hours":
                                executionPeriod?.AddHours(schedulingAmount);
                                break;
                            case "minutes":
                                executionPeriod?.AddHours(schedulingAmount);
                                break;
                            default:
                                executionPeriod?.AddHours(7 * schedulingAmount);
                                break;
                        }
                        // TODO check time_since_previous_execution, and set timeSinceLastExecution variable
                        if (task.TaskTypes.TaskType == "simple" || task.TaskTypes.TaskType == "questionnaire" || task.TaskTypes.TaskType == "measurement")
                        {
                            var existEntry = appoinmentGroupTask.Entry?.Where(x => x.Resource.Extension?.Where(y => y.ValueString == task.TaskTypes.Id).Count() > 0).FirstOrDefault();
                            if (existEntry != null)
                            {
                                // update exist task
                                existEntry.Resource.Description = task.TaskTypes.Name;
                                existEntry.Resource.Owner.Reference = patientId;
                                existEntry.Resource.Encounter.Reference = $"Encounter/{encounterId}";
                                existEntry.Resource.ExecutionPeriod.Start = executionPeriod;
                                existEntry.Resource.Code.Coding[0].Code = task.TaskTypes.TaskType;
                                await _fhirPatientService.UpdateTask(existEntry.Resource, authUser.ValidJwtToken);
                            }
                            else
                            {
                                // create new task
                                FhirTaskDto fhirTaskDto = new FhirTaskDto
                                {
                                    AuthoredOn = DateTime.UtcNow,
                                    Description = task.TaskTypes.Name,
                                    Intent = "order",
                                    LastModified = DateTime.UtcNow,
                                    Priority = "routine",
                                    ResourceType = "Task",
                                    Status = "accepted",
                                    StatusReason = new TextModel
                                    {
                                        Text = "Task created"
                                    },
                                    Owner = new ReferenceModel
                                    {
                                        Reference = patientId
                                    },
                                    GroupIdentifier = new ValueModel
                                    {
                                        Value = appointmentData.Id
                                    },
                                    Encounter = new ReferenceModel
                                    {
                                        Reference = $"Encounter/{encounterId}"
                                    },
                                    ExecutionPeriod = new Core.DTO.ExecutionPeriod
                                    {
                                        Start = executionPeriod,
                                    },
                                    Extension = new List<Core.DTO.Extension>()
                                };
                                fhirTaskDto.Extension.Add(new Core.DTO.Extension
                                {
                                    Url = "http://fhir.medlix.org/StructureDefinition/task-cms-id",
                                    ValueString = task.TaskTypes.Id,
                                });
                                var code = new CodeModel
                                {
                                    Code = task.TaskTypes.TaskType
                                };
                                var coding = new CodingModel();
                                coding.Coding = new List<CodeModel>();
                                coding.Coding.Add(code);
                                fhirTaskDto.Code = coding;

                                await _fhirPatientService.CreateTask(fhirTaskDto, authUser.ValidJwtToken);
                            }

                        }

                    }
                }
                return new OkObjectResult(appointmentData);
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        #region task

        [FunctionName("CreateTask")]
        public async Task<IActionResult> SaveTask([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            log.LogInformation("CreateTask function triggered request" + req);
            try
            {
                FhirTask task = new FhirTask();
                var patientId = req.Query["patientId"];
                var encounterId = req.Query["encounter_id"];
                var appoitmentId = req.Query["appointmentId"];
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
                task = JsonConvert.DeserializeObject<FhirTask>(requestBody);

                if (!authUser.IsAuthenticated)
                {
                    log.LogInformation("User Unauthorized");

                    return new UnauthorizedResult();
                }

                if (string.IsNullOrEmpty(encounterId) || encounterId == "null")
                {
                    log.LogInformation("Encounter Id is empty while creating the task");

                    var encounterResponse = await _fhirPatientService.CreateEncounter(authUser.ValidJwtToken, appoitmentId, patientId, log);
                    log.LogInformation("Encounter created and got the response", encounterResponse);


                    AppointmentData appointmentData = await _fhirPatientService.GetAppointment(appoitmentId, authUser.ValidJwtToken, log);
                    log.LogInformation("Get the appointment data.", appointmentData);
                    log.LogInformation("Encounter Id ", encounterResponse.id != null ? encounterResponse.id : "null");

                    if (appointmentData != null)
                    {
                        appointmentData.Extension[0].ValueString = encounterResponse.id;
                    }

                    var appointmentResponse = await _fhirPatientService.UpdateAppointment(appointmentData, authUser.ValidJwtToken, log);
                    log.LogInformation("Updated the appointment data and got the response", appointmentResponse);

                    task.encounter = new Encounter { reference = $"Encounter/{encounterResponse.id}" };
                    var taskResponse = await _fhirPatientService.CreateTaskWithEncounterId(task, authUser.ValidJwtToken, encounterResponse.id, log);
                    log.LogInformation("Created the task and got the response", appointmentResponse);
                    return new OkObjectResult(taskResponse);
                }
                else
                {
                    var taskResponse = await _fhirPatientService.CreateTaskWithEncounterId(task, authUser.ValidJwtToken, encounterId, log);
                    return new OkObjectResult(taskResponse);

                }




            }
            catch (Exception ex)
            {

                throw;
            }
        }
        #endregion task

        /// <summary>
        /// Create search parameter in FHIR
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns>Returns the response of FHIR call</returns>
        [FunctionName("CreateSearchParameter")]
        public async Task<IActionResult> CreateSearchParameter([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            log.LogInformation("CreateSearchParameter function triggered request" + req);

            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
                string patientId = authUser.PatientId;
                if (!authUser.IsAuthenticated || authUser.Role != "admin")
                {
                    return new UnauthorizedResult();
                }


                var response = await _fhirPatientService.CreateSearchParameter(authUser.ValidJwtToken);
                if (response)
                {
                    return new OkObjectResult(new { Message = "Created Search parameter on FHIR successfully" });
                }
                else
                {
                    return new BadRequestObjectResult(new { Message = "Failed Creating search parameter", Error = true });
                }


            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }


        #region Appointment Task Scheduling
        [FunctionName("CreateAppointmentAndTask")]
        public async Task<IActionResult> CreateAppointmentAndTask([HttpTrigger(AuthorizationLevel.Function, "post", Route = "CreateAppointmentAndTask")] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            AppointmentResponse response = new AppointmentResponse();

            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
                if (!authUser.IsAuthenticated)
                {
                    return new UnauthorizedResult();
                }
                var content = await new StreamReader(req.Body).ReadToEndAsync();

                var appt = JsonConvert.DeserializeObject<AppointmentRequest>(content);

                var appointmentData = new AppointmentData()
                {
                    ResourceType = "Appointment",
                    AppointmentType = appt.AppointmentType,
                    MinutesDuration = appt.Duration,
                    ServiceType = appt.ServiceType,
                    Status = appt.Status,
                    Comment = appt.Comment,
                    RequestedPeriod = new List<RequestedPeriodModel>()
                    {
                        new RequestedPeriodModel
                        {
                            Start = appt.RequestedPeriodStart,
                            End = appt.RequestedPeriodEnd
                        }
                    },
                    Extension = new List<Core.DTO.Extension>
                    {
                        new Core.DTO.Extension
                        {
                            Url = "http://fhir.medlix.org/StructureDefinition/appointment-start-date",
                            ValueString = appt.RequestedPeriodStart.ToString(),
                        },
                        new Core.DTO.Extension
                        {
                            Url = "http://fhir.medlix.org/StructureDefinition/appointment-is-virtual",
                            ValueString = appt.VirtualAppointmentAllowed.ToString(),
                        },
                        new Core.DTO.Extension
                        {
                            Url = "http://fhir.medlix.org/StructureDefinition/appointment-virtual-type",
                            ValueString = appt.VirtualAppointmentAppointmentType,
                        },
                        new Core.DTO.Extension
                        {
                            Url = "http://fhir.medlix.org/StructureDefinition/appointment-calendar-code",
                            ValueString = appt.CalendarCode,
                        },
                    },
                    Participant = new List<ParticipantModel>
                    {
                        new ParticipantModel
                        {
                            Status = "accepted", // draft | requested | received | accepted | +
                            Actor = new ReferenceModel {
                                Reference = $"Patient/{appt.PatientId}"
                            },
                            Type = new List<CodingModel>() {
                                new CodingModel()  {
                                    Coding = new List<CodeModel> {
                                        new CodeModel
                                        {
                                            Code = "ATND",
                                            System = "http://terminology.hl7.org/CodeSystem/v3-ParticipationType",
                                        }
                                    }
                                }
                            }
                        },
                        new ParticipantModel
                        {
                            Actor = new ReferenceModel{ Reference = $"Patient/{appt.PatientId}"},
                            Status = "accepted"
                        }
                    }
                };
                response.Request = appt;
                response.Appointment = await _fhirPatientService.CreateAppointment(appointmentData, authUser.ValidJwtToken);
                //if(response.Appointment == null)
                //{
                //    return new NullReferenceException("Appoinment Creation failed");
                //}
                string extensionValue = null;
                string encounterId = null;
                foreach (var extensions in response.Appointment?.Extension)
                {
                    if (extensions.Url.Contains("appointment-schedule-id"))
                    {
                        extensionValue = extensions.ValueString;
                    }
                    if (extensions.Url.Contains("task-encounter-id"))
                    {
                        encounterId = extensions.ValueString;
                    }
                }
                if (response.Appointment != null)
                {
                    var fhirTaskDto = new FhirTaskDto
                    {
                        AuthoredOn = DateTime.UtcNow,
                        Description = $"This task is related to Appointment/{response.Appointment.Id} of Patient/{appt.PatientId}",
                        Intent = "order",
                        LastModified = DateTime.UtcNow,
                        Priority = "routine",
                        ResourceType = "Task",
                        Status = "accepted",
                        Code = new CodingModel()
                        {
                            Coding = new List<CodeModel> {
                                        new CodeModel
                                        {
                                            Code = "measurement", //questionnaire
                                            System = "http://terminology.hl7.org/CodeSystem/v3-ParticipationType",
                                        }
                                    }
                        },
                        StatusReason = new TextModel
                        {
                            Text = "Task created automatically"
                        },
                        Owner = new ReferenceModel
                        {
                            Reference = appt.PatientId
                        },
                        Focus = new ReferenceModel
                        {
                            Reference = $"Appointment/{response.Appointment.Id}"
                        },
                        GroupIdentifier = new ValueModel
                        {
                            Value = appointmentData.Id
                        },
                        Encounter = new ReferenceModel
                        {
                            Reference = $"Encounter/{encounterId}"
                        },
                        ExecutionPeriod = new ExecutionPeriod
                        {
                            Start = appointmentData.Start,
                        },
                        Extension = new List<Core.DTO.Extension>()
                    };

                    response.Task = await _fhirPatientService.CreateTask(fhirTaskDto, authUser.ValidJwtToken);
                    if (response != null && response.Appointment != null && response.Task != null)
                    {
                        return new OkObjectResult(response);
                    }
                }

                return new BadRequestObjectResult(appt);

            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        /// <summary>
        /// Export patient related everything data on FHIR.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="patientId">Route param: ExportPatientEverything/{patientId}</param>
        /// <param name="log"></param>
        /// <returns>Returns everything data to related the patient in FHIR</returns>
        [FunctionName("ExportPatientEverything")]
        public async Task<IActionResult> ExportPatientEverything([HttpTrigger(AuthorizationLevel.Function, "get", Route = "ExportPatientEverything/{patientId}")] HttpRequest req, string patientId, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
                if (!authUser.IsAuthenticated || authUser.Role != "admin")
                {
                    return new UnauthorizedResult();
                }
                if (string.IsNullOrEmpty(patientId))
                {
                    return new BadRequestObjectResult(new { Message = "patientId is required", Error = true });
                }
                dynamic response = await _fhirPatientService.GetPatientEverything(patientId, authUser.ValidJwtToken);
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        /// <summary>
        /// Import patient related everything data on FHIR.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns>Returns success or failed in FHIR</returns>
        [FunctionName("ImportPatientEverything")]
        public async Task<IActionResult> ImportPatientEverything([HttpTrigger(AuthorizationLevel.Function, "put")] List<dynamic> importDto, HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
                if (!authUser.IsAuthenticated || authUser.Role != "admin")
                {
                    return new UnauthorizedResult();
                }
                if (importDto == null || importDto.Count == 0)
                {
                    return new BadRequestObjectResult(new { Message = "Request payload is empty or not array", Error = true });
                }
                string response = await _fhirPatientService.CreateOrUpdatePatientEverything(importDto, authUser.ValidJwtToken);
                return new OkObjectResult( new { Message = response, Error = false });
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        public async Task<IActionResult> GetAvailableTimeSlots([HttpTrigger(AuthorizationLevel.Function, "post", Route = "CreateAppointmentAndTask")] HttpRequest req, ILogger log)
        {
            return null;
        }

        #endregion Appointment Task Scheduling

        #region Timeline API
        [FunctionName("TimelineforPatient")]
        public async Task<IActionResult> TimelineforPatient([HttpTrigger(AuthorizationLevel.Function, "get", Route = "TimelineforPatient/{patientId}")] HttpRequest req, string patientId, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
                if (!authUser.IsAuthenticated || (authUser.Role != "admin" && patientId != authUser.PatientId))
                {
                    return new UnauthorizedResult();
                }
                if (string.IsNullOrEmpty(patientId))
                {
                    return new BadRequestObjectResult(new { Message = "patientId is required", Error = true });
                }
                var startDate = req.Query["startDate"];
                var endDate = req.Query["endDate"];
                var response = await _fhirPatientService.GetPatientCombinedTimelineData(patientId, startDate, endDate, authUser.ValidJwtToken);
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }
        #endregion

        #region Patient Status API
        [FunctionName("PatientStatus")]
        public async Task<IActionResult> PatientStatus([HttpTrigger(AuthorizationLevel.Function, "get", Route = "PatientStatus/{patientId}")] HttpRequest req, string patientId, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
                if (!authUser.IsAuthenticated)
                {
                    return new UnauthorizedResult();
                }
                if (string.IsNullOrEmpty(patientId))
                {
                    return new BadRequestObjectResult(new { Message = "patientId is required", Error = true });
                }

                var patient = await _fhirPatientService.GetPatientByID(patientId, authUser.ValidJwtToken, log);
                var existEmail = patient.telecom?.Where( x => x.system == "email").FirstOrDefault();
                var existPhone = patient.telecom?.Where( x => x.system == "phone").FirstOrDefault();

                if (existEmail == null || existPhone == null || string.IsNullOrEmpty(existEmail.value) || string.IsNullOrEmpty(existPhone.value))
                {
                    return new BadRequestObjectResult(new { 
                        Message = "The patient record needs to be completed before the patient can use the platform.", 
                        Error = true,
                        ErrorCode = 1,
                    });
                }

                var existUser = await _azureB2cService.GetCanUpdateUser(existEmail.value, existPhone.value);
                if (!existUser.CanUpdate)
                {
                    return new BadRequestObjectResult(new { 
                        Message = "The patient has not yet signed up", 
                        Error = true,
                        ErrorCode = 2,
                    });
                }
                if (existUser.PatientId != patientId)
                {
                    return new BadRequestObjectResult(new
                    {
                        Message = "The patient created an account but that linking still needs to be done",
                        Error = true,
                        ErrorCode = 3,
                    });
                }
                
                return new OkObjectResult(new
                {
                    Message = "The patient created an account already linked to Azure B2C ",
                    Error = false,
                });
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }
        #endregion
    }
}
