namespace Marketplace.SaaS.Accelerator.Services.Contracts;
public interface IDataCentralPurchaseHelperService
{
    bool IsCurrentSubscriptionTenantPlan(string offerId, string planId);
}
