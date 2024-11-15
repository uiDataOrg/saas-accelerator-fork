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
    private readonly IApplicationConfigRepository applicationConfigRepository;
    private readonly ILogger<DataCentralApiService> logger;
    private readonly IDataCentralTenantsRepository dataCentralTenantsRepository;
    private readonly IPlansRepository planRepository;
    private readonly IConfiguration configuration;

    private readonly string ApiUrlInfix = "/api/services/app";

    public DataCentralApiService(
        SaaSApiClientConfiguration clientConfiguration, 
        IApplicationConfigRepository applicationConfigRepository, 
        ILogger<DataCentralApiService> logger,
        IDataCentralTenantsRepository dataCentralTenantsRepository,
        IPlansRepository planRepository,
        IConfiguration configuration)
    {        
        this.ClientConfiguration = clientConfiguration;
        this.applicationConfigRepository = applicationConfigRepository;
        this.logger = logger;
        this.dataCentralTenantsRepository = dataCentralTenantsRepository;
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

    public async Task CreateTenantForNewSubscription(Guid subscriptionId, string customerEmailAddress, string customerName )
    {
        var tenant = this.dataCentralTenantsRepository.Get(subscriptionId);
        if (tenant == null)
        {
            throw new Exception("Tenant not found");
        }

        await CreateTenantAsyncInternal(tenant.Name, customerEmailAddress, customerName);

        var newlyCreatedTenantId = await GetTenantIdByNameInternalAsync(tenant.Name);
        tenant.TenantId = newlyCreatedTenantId;
        this.dataCentralTenantsRepository.Update(tenant);
    }

    public async Task UpdateTenantEditionForPlanChange(Guid subscriptionId, string newPlanId)
    {
        var tenant = this.dataCentralTenantsRepository.Get(subscriptionId);
        var plan = this.planRepository.GetById(newPlanId);

        // Check if plan or tenant is null
        if (tenant == null)
        {
            throw new ArgumentException("Tenant not found for the given subscription ID.", nameof(subscriptionId));
        }

        if (plan == null)
        {
            throw new ArgumentException("Plan not found for the given plan ID.", nameof(newPlanId));
        }

        // Retrieve configuration value and check if it's empty
        var editionIdConfig = configuration[$"DataCentralConfig:{plan.DisplayName}EditionId"];
        if (string.IsNullOrEmpty(editionIdConfig))
        {
            throw new InvalidOperationException($"Configuration for EditionId of plan '{plan.DisplayName}' is missing.");
        }

        // Try parsing the EditionId and check if it's a valid integer
        if (!int.TryParse(editionIdConfig, out int editionId))
        {
            throw new InvalidOperationException($"Configuration for EditionId of plan '{plan.DisplayName}' is not a valid integer.");
        }

        await ChangeEditionForTenantInternalAsync(tenant.TenantId, editionId);
    }


    //"Delete"
    //User or admin unsubscribes
    //Offer suspended due to bill not paid
    public async Task DisableTenant(Guid subscriptionId)
    {
        try
        {
            var dataCentralTenant = this.dataCentralTenantsRepository.Get(subscriptionId);
            await SetTenantStatusInternalAsync(dataCentralTenant.TenantId, false);
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
            var dataCentralTenant = this.dataCentralTenantsRepository.Get(subscriptionId);
            await SetTenantStatusInternalAsync(dataCentralTenant.TenantId, true);
        }
        catch(Exception ex)
        {
            logger.LogError("Error in enable tenant: {0}", ex);
            //do something more here, send email to admins?
            throw;
        }
    }

    private async Task CreateTenantAsyncInternal(string tenantName, string adminEmailAddress, string adminName)
    {
        var requestUrl = $"{GetApiUrl()}{ApiUrlInfix}/Tenant/CreateTenant";
        var editionId = Convert.ToInt32(this.applicationConfigRepository.GetValueByName("DataCentralEditionId_P1"));
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

