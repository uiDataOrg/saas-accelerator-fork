using System;

namespace Marketplace.SaaS.Accelerator.DataAccess.Entities;
public class DataCentralTenant
{
    public int Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string Name { get; set; }
    public int TenantId { get; set; }
}
