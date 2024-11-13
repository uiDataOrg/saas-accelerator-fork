using System.Threading.Tasks;

namespace Marketplace.SaaS.Accelerator.Services.Contracts;
public interface IDataCentralApiService
{
    Task CreateTenantAsync(string tenantName, string adminEmailAddress, string adminName);

    Task<int> GetTenantIdByNameAsync(string tenantName);

    Task EnableTenantAsync(int tenantId);

    Task DisableTenantAsync(int tenantId);

    Task ChangeEditionForTenantAsync(int tenantId, int editionId);
}
