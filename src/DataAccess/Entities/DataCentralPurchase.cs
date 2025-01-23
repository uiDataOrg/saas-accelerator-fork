using System;

namespace Marketplace.SaaS.Accelerator.DataAccess.Entities;
public class DataCentralPurchase
{
    public int Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string EnvironmentName { get; set; }
    public string TypeOfPurchase { get; set; }
    public int? TenantId { get; set; }
    public int? EnvironmentId { get; set; }
}
