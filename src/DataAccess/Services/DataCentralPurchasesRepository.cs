using Marketplace.SaaS.Accelerator.DataAccess.Context;
using Marketplace.SaaS.Accelerator.DataAccess.Contracts;
using Marketplace.SaaS.Accelerator.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace Marketplace.SaaS.Accelerator.DataAccess.Services;
public class DataCentralPurchasesRepository : IDataCentralPurchasesRepository
{
    private readonly SaasKitContext context;

    public DataCentralPurchasesRepository(SaasKitContext context)
    {
        this.context = context;
    }

    public async Task<DataCentralPurchase> GetAsync(Guid subscriptionId)
    {
        return await this.context.DataCentralPurchases
            .Where(dt => dt.SubscriptionId == subscriptionId)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(DataCentralPurchase tenant)
    {
        if(tenant != null)
        {
            await this.context.DataCentralPurchases.AddAsync(tenant);
            await this.context.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(DataCentralPurchase tenant)
    {
        if (tenant != null)
        {
            this.context.DataCentralPurchases.Update(tenant);
            await this.context.SaveChangesAsync();
        }
    }
}
