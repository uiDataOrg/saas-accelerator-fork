// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.


using System;
using System.Threading.Tasks;
using Marketplace.SaaS.Accelerator.DataAccess.Contracts;
using Marketplace.SaaS.Accelerator.Services.Contracts;
using Marketplace.SaaS.Accelerator.Services.Services;
using Marketplace.SaaS.Accelerator.Services.Utilities;
using Microsoft.AspNetCore.Mvc;


namespace Marketplace.SaaS.Accelerator.CustomerSite.Controllers.WebHook;

/// <summary>
/// Azure Web hook.
/// </summary>
/// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
[Route("api/[controller]")]
[ApiController]
[IgnoreAntiforgeryTokenAttribute]
public class WebNotificationController : ControllerBase
{
    private readonly IApplicationLogRepository applicationLogRepository;
    private readonly ApplicationLogService applicationLogService;
    private readonly SaaSClientLogger<WebNotificationController> logger;

    public WebNotificationController(IApplicationLogRepository applicationLogRepository,
                                     SaaSClientLogger<WebNotificationController> logger)
    {
        this.applicationLogRepository = applicationLogRepository;
        this.applicationLogService = new ApplicationLogService(this.applicationLogRepository);
        this.logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] object request)
    {

        //this gets called on subscribe/activation by the user
        //or 
        //webhook notifications get also sent to this

        try
        {
            string jsonPayload = System.Text.Json.JsonSerializer.Serialize(request);
            await this.applicationLogService.AddApplicationLog($"Web notification received: {jsonPayload}").ConfigureAwait(false);
            this.logger.Info($"Web Notification triggered with payload: {jsonPayload}");

            return Ok();
        }
        catch (Exception ex)
        {
            await this.applicationLogService.AddApplicationLog(
                    $"An error occurred while attempting to process a webhook notification: [{ex.Message}].")
                .ConfigureAwait(false);
            return StatusCode(500);
        }
    }
}
