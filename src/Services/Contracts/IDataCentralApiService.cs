using System;
using System.Threading.Tasks;

namespace Marketplace.SaaS.Accelerator.Services.Contracts;
public interface IDataCentralApiService
{
    Task CreateTenantForNewSubscription(Guid subscriptionId, string customerEmailAddress, string customerName, string planId);

    Task DisableTenant(Guid subscriptionId);

    Task EnableTenant(Guid subscriptionId);

    Task UpdateTenantEditionForPlanChange(Guid subscriptionId, string newPlanName);
}
