using Marketplace.SaaS.Accelerator.DataAccess.Context;
using Marketplace.SaaS.Accelerator.DataAccess.Contracts;
using Marketplace.SaaS.Accelerator.DataAccess.Entities;
using System;
using System.Linq;


namespace Marketplace.SaaS.Accelerator.DataAccess.Services;
public class DataCentralPurchasesRepository : IDataCentralPurchasesRepository
{
    private readonly SaasKitContext context;

    public DataCentralPurchasesRepository(SaasKitContext context)
    {
        this.context = context;
    }

    public void Add(DataCentralPurchase tenant)
    {
        if(tenant != null)
        {
            this.context.DataCentralPurchases.Add(tenant);
            this.context.SaveChanges();
        }
    }

    public DataCentralPurchase Get(Guid subscriptionId)
    {
        return this.context.DataCentralPurchases.Where(dt => dt.SubscriptionId == subscriptionId).FirstOrDefault();
    }

    public void Update(DataCentralPurchase tenant)
    {
        if(tenant != null)
        {
            this.context.DataCentralPurchases.Update(tenant);
            this.context.SaveChanges();
        }
    }
}
