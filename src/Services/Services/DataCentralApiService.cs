using Marketplace.SaaS.Accelerator.Services.Configurations;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Marketplace.SaaS.Accelerator.Services.Contracts;
using Marketplace.SaaS.Accelerator.DataAccess.Contracts;
using Microsoft.Extensions.Logging;

namespace Marketplace.SaaS.Accelerator.Services.Services;
public class DataCentralApiService : IDataCentralApiService
{

    protected SaaSApiClientConfiguration ClientConfiguration { get; set; }
    private readonly IApplicationConfigRepository applicationConfigRepository;
    private readonly ILogger<DataCentralApiService> logger;

    private readonly string ApiUrlInfix = "/api/services/app";

    public DataCentralApiService(
        SaaSApiClientConfiguration clientConfiguration, 
        IApplicationConfigRepository applicationConfigRepository, 
        ILogger<DataCentralApiService> logger)
    {        
        this.ClientConfiguration = clientConfiguration;
        this.applicationConfigRepository = applicationConfigRepository;
        this.logger = logger;
    }

    private string GetApiUrl()
    {
        return this.ClientConfiguration.DataCentralApiBaseUrl;
    }

    private string GetDataCentralApiKey()
    {
        return this.ClientConfiguration.DataCentralApiKey;
    }

    public async Task CreateTenantAsync(string tenantName, string adminEmailAddress, string adminName)
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

    public async Task<int> GetTenantIdByNameAsync(string tenantName)
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

    public async Task EnableTenantAsync(int tenantId)
    {
        var getTenantForEdit = await GetTenantForEdit(tenantId);
        var requestUrl = $"{GetApiUrl()}{ApiUrlInfix}/UpdateTenant";
        var requestBody = new EditTenantDto
        {
            Name = getTenantForEdit.Name,
            IsActive = true,
            TenancyName = getTenantForEdit.TenancyName,
            EditionId = getTenantForEdit.EditionId,
            IsInTrialPeriod = getTenantForEdit.IsInTrialPeriod,
            ConnectionString = getTenantForEdit.ConnectionString,
            SubscriptionEndDateUtc = getTenantForEdit.SubscriptionEndDateUtc
        };

        var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-api-key", GetDataCentralApiKey());
                var response = await client.PutAsync(requestUrl, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Tenant created successfully.");
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Console.WriteLine("Server encountered an internal error (500). Please try again later.");
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to create tenant. Status Code: {(int)response.StatusCode}, Message: {responseContent}");
                }
            }
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"Request failed: {httpEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    public async Task DisableTenantAsync(int tenantId)
    {
        var getTenantForEdit = await GetTenantForEdit(tenantId);
        var requestUrl = $"{GetApiUrl()}{ApiUrlInfix}/UpdateTenant";
        var requestBody = new EditTenantDto
        {
            Name = getTenantForEdit.Name,
            IsActive = false,
            TenancyName = getTenantForEdit.TenancyName,
            EditionId = getTenantForEdit.EditionId,
            IsInTrialPeriod = getTenantForEdit.IsInTrialPeriod,
            ConnectionString = getTenantForEdit.ConnectionString,
            SubscriptionEndDateUtc = getTenantForEdit.SubscriptionEndDateUtc
        };

        var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-api-key", GetDataCentralApiKey());

                var response = await client.PutAsync(requestUrl, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Tenant created successfully.");
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Console.WriteLine("Server encountered an internal error (500). Please try again later.");
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to create tenant. Status Code: {(int)response.StatusCode}, Message: {responseContent}");
                }
            }
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"Request failed: {httpEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    public async Task ChangeEditionForTenantAsync(int tenantId, int editionId)
    {
        var getTenantForEdit = await GetTenantForEdit(tenantId);
        var requestUrl = $"{GetApiUrl()}{ApiUrlInfix}/UpdateTenant";
        var requestBody = new EditTenantDto
        {
            Name = getTenantForEdit.Name,
            IsActive = getTenantForEdit.IsActive,
            TenancyName = getTenantForEdit.TenancyName,
            EditionId = editionId,
            IsInTrialPeriod = getTenantForEdit.IsInTrialPeriod,
            ConnectionString = getTenantForEdit.ConnectionString,
            SubscriptionEndDateUtc = getTenantForEdit.SubscriptionEndDateUtc
        };

        var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-api-key", GetDataCentralApiKey());

                var response = await client.PutAsync(requestUrl, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Tenant created successfully.");
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Console.WriteLine("Server encountered an internal error (500). Please try again later.");
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to create tenant. Status Code: {(int)response.StatusCode}, Message: {responseContent}");
                }
            }
        }
        catch (HttpRequestException httpEx)
        {
            // Handle HTTP request-related exceptions
            Console.WriteLine($"Request failed: {httpEx.Message}");
        }
        catch (Exception ex)
        {
            // Handle other general exceptions
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    
    private async Task<GetTenantForEditDto> GetTenantForEdit(int tenantId)
    {
        // Construct the API URL with the tenant ID as a query parameter
        var requestUrl = $"{GetApiUrl()}{ApiUrlInfix}/GetTenantForEdit?id={tenantId}";

        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("x-api-key", GetDataCentralApiKey());

                // Send the GET request to the API URL
                var response = await client.GetAsync(requestUrl);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    // Read and deserialize the JSON response content
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tenantForEditDto = JsonConvert.DeserializeObject<GetTenantForEditDto>(responseContent);

                    // Return the deserialized object
                    return tenantForEditDto;
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    // Handle 500 internal server error
                    Console.WriteLine("Server encountered an internal error (500). Please try again later.");
                    return null;
                }
                else
                {
                    // Handle other non-success status codes
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to retrieve tenant. Status Code: {(int)response.StatusCode}, Message: {responseContent}");
                    return null;
                }
            }
        }
        catch (HttpRequestException httpEx)
        {
            // Handle HTTP request-related exceptions
            Console.WriteLine($"Request failed: {httpEx.Message}");
            return null;
        }
        catch (Exception ex)
        {
            // Handle other general exceptions
            Console.WriteLine($"An error occurred: {ex.Message}");
            return null;
        }
    }
}

public class GetTenantForEditDto
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

