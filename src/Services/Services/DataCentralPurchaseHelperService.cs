using Marketplace.SaaS.Accelerator.DataAccess.Contracts;
using Marketplace.SaaS.Accelerator.Services.Configurations;
using Marketplace.SaaS.Accelerator.Services.Contracts;

namespace Marketplace.SaaS.Accelerator.Services.Services;
public class DataCentralPurchaseHelperService : IDataCentralPurchaseHelperService
{
    private readonly IOffersRepository offerRepository;
    private readonly IPlansRepository planRepository;
    protected SaaSApiClientConfiguration ClientConfiguration { get; set; }
    public DataCentralPurchaseHelperService(
        IOffersRepository offerRepo, 
        IPlansRepository planRepository, 
        SaaSApiClientConfiguration clientConfiguration)
    {
        this.offerRepository = offerRepo;
        this.planRepository = planRepository;
        this.ClientConfiguration = clientConfiguration;
    }

    public bool IsCurrentSubscriptionTenantPlan(string offerId, string planId)
    {
        if (offerId == null)
        {
            //var value = !plan.PlanId.StartsWith(ClientConfiguration.DateCentralInstanceOfferId);
            //return value;

            var plan = planRepository.GetById(planId);
            var offer = offerRepository.GetOfferById(plan.OfferId);
            return offer.OfferId == ClientConfiguration.DataCentralTenantOfferId;
        }
        else
        {
            return offerId == ClientConfiguration.DataCentralTenantOfferId;
        }
    }
}
