using Marketplace.SaaS.Accelerator.DataAccess.Context;
using Marketplace.SaaS.Accelerator.DataAccess.Contracts;
using Marketplace.SaaS.Accelerator.DataAccess.Entities;
using System;
using System.Linq;


namespace Marketplace.SaaS.Accelerator.DataAccess.Services;
public class DataCentralTenantsRepository : IDataCentralTenantsRepository
{
    private readonly SaasKitContext context;

    public DataCentralTenantsRepository(SaasKitContext context)
    {
        this.context = context;
    }

    public void Add(DataCentralTenant tenant)
    {
        if(tenant != null)
        {
            this.context.DataCentralTenants.Add(tenant);
            this.context.SaveChanges();
        }
    }

    public DataCentralTenant Get(Guid subscriptionId)
    {
        return this.context.DataCentralTenants.Where(dt => dt.SubscriptionId == subscriptionId).FirstOrDefault();
    }

    public void Update(DataCentralTenant tenant)
    {
        if(tenant != null)
        {
            this.context.DataCentralTenants.Update(tenant);
            this.context.SaveChanges();
        }
    }
}
