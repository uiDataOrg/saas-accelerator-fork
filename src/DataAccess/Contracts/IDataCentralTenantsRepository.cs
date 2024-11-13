using Marketplace.SaaS.Accelerator.DataAccess.Entities;
using System;


namespace Marketplace.SaaS.Accelerator.DataAccess.Contracts;
public interface IDataCentralTenantsRepository
{
    DataCentralTenant Get(Guid subscriptionId);
    void Add(DataCentralTenant tenant);
    void Update(DataCentralTenant tenant);
}
