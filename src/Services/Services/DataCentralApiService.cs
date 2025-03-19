using Marketplace.SaaS.Accelerator.Services.Configurations;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Marketplace.SaaS.Accelerator.Services.Contracts;
using Marketplace.SaaS.Accelerator.DataAccess.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Marketplace.SaaS.Accelerator.Services.Services;
public class DataCentralApiService : IDataCentralApiService
{
    protected SaaSApiClientConfiguration ClientConfiguration { get; set; }
    private readonly ILogger<DataCentralApiService> logger;
    private readonly IDataCentralPurchasesRepository dataCentralPurchaseRepository;
    private readonly IPlansRepository planRepository;
    private readonly IConfiguration configuration;

    private readonly string ApiUrlInfix = "/api/services/app";

    public DataCentralApiService(
        SaaSApiClientConfiguration clientConfiguration, 
        ILogger<DataCentralApiService> logger,
        IDataCentralPurchasesRepository dataCentralPurchaseRepository,
        IPlansRepository planRepository,
        IConfiguration configuration)
    {        
        this.ClientConfiguration = clientConfiguration;
        this.logger = logger;
        this.dataCentralPurchaseRepository = dataCentralPurchaseRepository;
        this.planRepository = planRepository;
        this.configuration = configuration;
    }

    private string GetApiUrl()
    {
        return this.ClientConfiguration.DataCentralApiBaseUrl;
    }

    private string GetDataCentralApiKey()
    {
        return this.ClientConfiguration.DataCentralApiKey;
    }

    private int GetEditionIdByPlanId(string planId)
    {
        var plan = this.planRepository.GetById(planId);
        if (plan == null)
        {
            throw new ArgumentException("Plan not found for the given plan ID.", nameof(planId));
        }

        //var editionIdConfig = configuration[$"DataCentralConfig:{plan.DisplayName}EditionId"];
        var configKey = $"DataCentralConfig:{plan.PlanId}EditionId";
        var editionIdConfig = configuration[configKey];
        if (string.IsNullOrEmpty(editionIdConfig))
        {
            throw new InvalidOperationException($"Configuration for EditionId of plan '{plan.DisplayName}' is missing.");
        }

        if (!int.TryParse(editionIdConfig, out int editionId))
        {
            throw new InvalidOperationException($"Configuration for EditionId of plan '{plan.DisplayName}' is not a valid integer.");
        }
        return editionId;
    }

    public async Task TriggerInstanceAutomation(Guid subscriptionId, string customerEmailAddress, string environmentName)
    {
        var triggerAutomationUrl = configuration[$"DataCentralConfig:TriggerInstanceAutomationApiRoute"];
        var input = new InstanceAutomationInputDto()
        {
            EnvironmentName = environmentName,
            EnvironmentNameInfix = "mp",
            Location = "northeurope",
            EmailAddress = customerEmailAddress,
            MarketplaceSubscriptionDate = DateTime.UtcNow,
            MarketplaceSubscriptionId = subscriptionId,
            TriggerGithubWorkflows = true,
            InsertIntoDb = true,
            UpdateSettings = true,
            UpdateHostAdmin = true
        };

        using (var httpClient = new HttpClient())
        {
            var jsonContent = new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(triggerAutomationUrl, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                // Handle error response
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to trigger instance automation: {errorMessage}");
            }
        }
    }

    public async Task CreateTenantForNewSubscription(Guid subscriptionId, string customerEmailAddress, string customerName, string planId)
    {
        var tenant = await this.dataCentralPurchaseRepository.GetAsync(subscriptionId);
        if (tenant == null)
        {
            throw new Exception("Tenant not found");
        }

        var editionId = GetEditionIdByPlanId(planId);

        await CreateTenantAsyncInternal(tenant.EnvironmentName, customerEmailAddress, customerName, editionId);

        var newlyCreatedTenantId = await GetTenantIdByNameInternalAsync(tenant.EnvironmentName);
        tenant.TenantId = newlyCreatedTenantId;
        await this.dataCentralPurchaseRepository.UpdateAsync(tenant);
    }

    public async Task UpdateTenantEditionForPlanChange(Guid subscriptionId, string newPlanId)
    {
        var tenant = await this.dataCentralPurchaseRepository.GetAsync(subscriptionId);
        if (tenant == null)
        {
            throw new ArgumentException("Tenant not found for the given subscription ID.", nameof(subscriptionId));
        }

        var editionId = GetEditionIdByPlanId(newPlanId);

        await ChangeEditionForTenantInternalAsync((int)tenant.TenantId, editionId);
    }


    //"Delete"
    //User or admin unsubscribes
    //Offer suspended due to bill not paid
    public async Task DisableTenant(Guid subscriptionId)
    {
        try
        {
            var dataCentralTenant = await this.dataCentralPurchaseRepository.GetAsync(subscriptionId);
            if (dataCentralTenant == null)
            {
                logger.LogError("DataCentralPurchase with SubscriptionId: {0} not found", subscriptionId);
                throw new InvalidOperationException($"Subscription {subscriptionId} not found.");
            }
            else if (dataCentralTenant.TenantId == null)
            {
                logger.LogError($"Unable to disable DataCentral tenant for subscription: {subscriptionId}. TenantId for purchase was not found");
                throw new InvalidOperationException($"Tenant ID missing for subscription {subscriptionId}.");
            }
            else
            {
                await SetTenantStatusInternalAsync((int)dataCentralTenant.TenantId, false);
            }
        }
        catch(Exception ex)
        {
            logger.LogError("Error in disable tenant: {0}", ex);
            //do something more here, send email to admins?
            throw;
        }
    }   

    //Offer reinstated, user paid bill
    public async Task EnableTenant(Guid subscriptionId)
    {
        try
        {
            var dataCentralTenant = await this.dataCentralPurchaseRepository.GetAsync(subscriptionId);
            await SetTenantStatusInternalAsync((int)dataCentralTenant.TenantId, true);
        }
        catch(Exception ex)
        {
            logger.LogError("Error in enable tenant: {0}", ex);
            //do something more here, send email to admins?
            throw;
        }
    }

    private async Task CreateTenantAsyncInternal(string tenantName, string adminEmailAddress, string adminName, int editionId)
    {
        var requestUrl = $"{GetApiUrl()}{ApiUrlInfix}/Tenant/CreateTenant";
        var requestBody = new CreateTenantDto()
        {
            TenancyName = tenantName,
            Name = tenantName,
            AdminEmailAddress = adminEmailAddress,
            EditionId = editionId,
            SendActivationEmail = true,
            IsActive = true,
            IsAdminUserAuthenticationSourceAzureAd = true,
            AdminName = adminName
        };

        var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        using (HttpClient client = new HttpClient())
        {
            // Set the x-api-key header
            var key = GetDataCentralApiKey();
            client.DefaultRequestHeaders.Add("x-api-key", GetDataCentralApiKey());

            var response = await client.PostAsync(requestUrl, jsonContent);
            response.EnsureSuccessStatusCode();
        }
    }

    private async Task<int> GetTenantIdByNameInternalAsync(string tenantName)
    {
        var requestUrl = $"{GetApiUrl()}{ApiUrlInfix}/Account/IsTenantAvailable";
        var requestBody = new 
        {
            TenancyName = tenantName,
        };

        var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        using (HttpClient client = new HttpClient())
        {
            // Set the x-api-key header
            var key = GetDataCentralApiKey();
            client.DefaultRequestHeaders.Add("x-api-key", GetDataCentralApiKey());

            var response = await client.PostAsync(requestUrl, jsonContent);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var serialized = JsonConvert.DeserializeObject<DataCentralGenericResponse<IsTenantAvailableOutput>>(content);
            if(serialized.Result.State != 1)
            {
                throw new Exception("Tenant not found");
            }
            else
            {
                return serialized.Result.TenantId;
            }
        }
    }

    private async Task SetTenantStatusInternalAsync(int tenantId, bool isActive)
    {
        var tenantForEdit = await GetTenantForEditInternalAsync(tenantId);
        var requestUrl = $"{GetApiUrl()}{ApiUrlInfix}/Tenant/UpdateTenant";

        tenantForEdit.IsActive = isActive;

        var jsonContent = new StringContent(JsonConvert.SerializeObject(tenantForEdit), Encoding.UTF8, "application/json");

        using (HttpClient client = new HttpClient())
        {
            // Set the x-api-key header
            var key = GetDataCentralApiKey();
            client.DefaultRequestHeaders.Add("x-api-key", GetDataCentralApiKey());

            var response = await client.PutAsync(requestUrl, jsonContent);
            var content = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
        }
    }

    private async Task ChangeEditionForTenantInternalAsync(int tenantId, int editionId)
    {
        var tenantForEdit = await GetTenantForEditInternalAsync(tenantId);
        var requestUrl = $"{GetApiUrl()}{ApiUrlInfix}/Tenant/UpdateTenant";

        tenantForEdit.EditionId = editionId;

        var jsonContent = new StringContent(JsonConvert.SerializeObject(tenantForEdit), Encoding.UTF8, "application/json");

        using (HttpClient client = new HttpClient())
        {
            var key = GetDataCentralApiKey();
            client.DefaultRequestHeaders.Add("x-api-key", GetDataCentralApiKey());

            var response = await client.PutAsync(requestUrl, jsonContent);
            var content = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
        }
    }

    
    private async Task<EditTenantDto> GetTenantForEditInternalAsync(int tenantId)
    {
        var requestUrl = $"{GetApiUrl()}{ApiUrlInfix}/Tenant/GetTenantForEdit?id={tenantId}";

        using (HttpClient client = new HttpClient())
        {
            var key = GetDataCentralApiKey();
            client.DefaultRequestHeaders.Add("x-api-key", GetDataCentralApiKey());

            var response = await client.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var serialized = JsonConvert.DeserializeObject<DataCentralGenericResponse<EditTenantDto>>(content);
            return serialized.Result;
        }

    }



}

public class CreateTenantDto
{
    public string TenancyName { get; set; }
    public string Name { get; set; }
    public string AdminEmailAddress { get; set; }
    public string AdminName { get; set; }
    public string AdminPassword { get; set; }
    public string ConnectionString { get; set; }
    public bool ShouldChangePasswordOnNextLogin { get; set; }
    public bool SendActivationEmail { get; set; }
    public int? EditionId { get; set; }
    public bool IsActive { get; set; }
    public DateTime? SubscriptionEndDateUtc { get; set; }
    public bool IsInTrialPeriod { get; set; }

    public bool IsAdminUserAuthenticationSourceAzureAd { get; set; }
}

public class EditTenantDto
{
    public int Id { get; set; }
    public string TenancyName { get; set; }
    public string Name { get; set; }
    public string ConnectionString { get; set; }
    public int? EditionId { get; set; }
    public bool IsActive { get; set; }
    public DateTime? SubscriptionEndDateUtc { get; set; }
    public bool IsInTrialPeriod { get; set; }
}

public class IsTenantAvailableOutput
{
    public int State { get; set; }

    public int TenantId { get; set; }

    public string ServerRootAddress { get; set; }
}

public class DataCentralGenericResponse<T>
{
    public T Result { get; set; }
}

public class InstanceAutomationInputDto()
{
    public string EnvironmentName { get; set; }
    public string Location { get; set; }
    public string EnvironmentNameInfix { get; set; }
    public string EmailAddress { get; set; }
    public DateTime MarketplaceSubscriptionDate { get; set; }
    public Guid? MarketplaceSubscriptionId { get; set; }
    public bool? TriggerGithubWorkflows { get; set; } = false;
    public bool? InsertIntoDb { get; set; } = false;
    public bool? UpdateSettings { get; set; } = false;
    public bool? UpdateHostAdmin { get; set; } = false;
    public bool? IsRetry { get; set; } = false;
}

