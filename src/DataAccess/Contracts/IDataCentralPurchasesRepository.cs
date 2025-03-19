using Marketplace.SaaS.Accelerator.DataAccess.Entities;
using System;
using System.Threading.Tasks;


namespace Marketplace.SaaS.Accelerator.DataAccess.Contracts;
public interface IDataCentralPurchasesRepository
{
    Task<DataCentralPurchase> GetAsync(Guid subscriptionId);
    Task AddAsync(DataCentralPurchase tenant);
    Task UpdateAsync(DataCentralPurchase tenant);
}
