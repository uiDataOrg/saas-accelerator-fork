using Marketplace.SaaS.Accelerator.DataAccess.Entities;
using System;


namespace Marketplace.SaaS.Accelerator.DataAccess.Contracts;
public interface IDataCentralPurchasesRepository
{
    DataCentralPurchase Get(Guid subscriptionId);
    void Add(DataCentralPurchase tenant);
    void Update(DataCentralPurchase tenant);
}
