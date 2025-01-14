using CrossCutting.DopplerSapService.Entities;
using CrossCutting.DopplerSapService.Enum;
using Doppler.Billing.Job.Database;
using Doppler.Billing.Job.Entities;
using Doppler.Billing.Job.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.Billing.Job.Mappers
{
    public class BillingMapper(IDopplerRepository dopplerRepository) : IBillingMapper
    {
        public async Task<IList<BillingRequest>> MapToListOfBillingRequest(IList<UserBilling> userBillings)
        {
            var result = new List<BillingRequest>();

            foreach (var userBilling in userBillings)
            {
                var billingRequest = new BillingRequest
                {
                    BillingSystemId = userBilling.BillingSystemId,
                    CreditsOrSubscribersQuantity = userBilling.CreditsOrSubscribersQuantity,
                    Currency = userBilling.Currency,
                    Discount = userBilling.Discount,
                    ExtraEmails = userBilling.ExtraEmails,
                    ExtraEmailsFee = userBilling.ExtraEmailsFee,
                    ExtraEmailsFeePerUnit = userBilling.ExtraEmailsFeePerUnit,
                    ExtraEmailsPeriodMonth = userBilling.ExtraEmailsPeriodMonth,
                    ExtraEmailsPeriodYear = userBilling.ExtraEmailsPeriodYear,
                    FiscalID = userBilling.FiscalID,
                    Id = userBilling.Id,
                    IsCustomPlan = userBilling.IsCustomPlan,
                    IsPlanUpgrade = userBilling.IsPlanUpgrade,
                    Periodicity = userBilling.Periodicity,
                    PeriodMonth = userBilling.PeriodMonth,
                    PeriodYear = userBilling.PeriodYear,
                    PlanFee = userBilling.PlanFee,
                    PlanType = userBilling.PlanType,
                    PurchaseOrder = userBilling.PurchaseOrder,
                    AdditionalServices = []
                };

                if (userBilling.LandingsAmount > 0)
                {
                    if (userBilling.PlanType != 0)
                    {
                        var user = await dopplerRepository.GetUserByUserIdAsync(userBilling.Id);
                        var landingAdditionalService = await GetLandingAdditionalServiceAsync(userBilling, user);
                        if (landingAdditionalService != null)
                        {
                            billingRequest.AdditionalServices.Add(landingAdditionalService);
                        }
                    }
                    else
                    {
                        var userIds = await dopplerRepository.GetUserIdsByClientManagerIdAsync(userBilling.Id);
                        foreach (var userId in userIds)
                        {
                            var user = await dopplerRepository.GetUserByUserIdAsync(userId);
                            var landingAdditionalService = await GetLandingAdditionalServiceAsync(userBilling, user);
                            if (landingAdditionalService != null)
                            {
                                billingRequest.AdditionalServices.Add(landingAdditionalService);
                            }
                        }
                    }
                }

                if (userBilling.ConversationsAmount > 0 || !string.IsNullOrEmpty(userBilling.ConversationsExtra))
                {
                    if (userBilling.PlanType != 0)
                    {
                        var user = await dopplerRepository.GetUserByUserIdAsync(userBilling.Id);
                        var conversationAdditionalService = await GetConversationAdditionalServiceAsync(userBilling, user, AccountTypeEnum.User);
                        if (conversationAdditionalService != null)
                        {
                            billingRequest.AdditionalServices.Add(conversationAdditionalService);
                        }
                    }
                    else
                    {
                        var userIds = await dopplerRepository.GetUserIdsByClientManagerIdAsync(userBilling.Id);
                        foreach (var userId in userIds)
                        {
                            var user = await dopplerRepository.GetUserByUserIdAsync(userId);
                            var conversationAdditionalService = await GetConversationAdditionalServiceAsync(userBilling, user, AccountTypeEnum.CM);
                            if (conversationAdditionalService != null)
                            {
                                billingRequest.AdditionalServices.Add(conversationAdditionalService);
                            }
                        }
                    }
                }

                if (userBilling.PrintsAmount > 0 || !string.IsNullOrEmpty(userBilling.PrintsExtra))
                {
                    if (userBilling.PlanType != 0)
                    {
                        var user = await dopplerRepository.GetUserByUserIdAsync(userBilling.Id);
                        var onSiteAdditionalService = await GetOnSiteAdditionalServiceAsync(userBilling, user, AccountTypeEnum.User);
                        if (onSiteAdditionalService != null)
                        {
                            billingRequest.AdditionalServices.Add(onSiteAdditionalService);
                        }
                    }
                    else
                    {
                        var userIds = await dopplerRepository.GetUserIdsByClientManagerIdAsync(userBilling.Id);
                        foreach (var userId in userIds)
                        {
                            var user = await dopplerRepository.GetUserByUserIdAsync(userId);
                            var onSiteAdditionalService = await GetOnSiteAdditionalServiceAsync(userBilling, user, AccountTypeEnum.CM);
                            if (onSiteAdditionalService != null)
                            {
                                billingRequest.AdditionalServices.Add(onSiteAdditionalService);
                            }
                        }
                    }
                }

                result.Add(billingRequest);
            }

            return result;
        }

        private async Task<AdditionalService> GetLandingAdditionalServiceAsync(UserBilling userBilling, User user)
        {
            //Get Addon details
            var landingAddOn = await dopplerRepository.GetUserAddOnsByUserIdAndTypeAsync(user.UserId, (int)AddOnTypeEnum.Landing);

            if (landingAddOn != null)
            {
                var billingCredit = await dopplerRepository.GetCurrentBIllingCreditByUserIdAsync(user.UserId);
                var landings = (await dopplerRepository.GetLandingPlansByBillingCreditIdAsync(landingAddOn.IdCurrentBillingCredit)).ToList();
                var rate = userBilling.Currency > 0 ? await dopplerRepository.GetCurrenyRate(0, userBilling.Currency ?? 0) : 1;

                var additionalService = new AdditionalService
                {
                    UserEmail = user.Email,
                    Type = AdditionalServiceTypeEnum.Landing,
                    Charge = (double)0,
                    Packs = landings.Select(l => new Pack
                    {
                        Amount = Convert.ToDecimal(l.Fee) * (billingCredit.TotalMonthPlan ?? 1) * rate,
                        PackId = l.IdLandingPlan,
                        Quantity = l.PackQty
                    }).ToList()
                };

                return additionalService;
            }

            return null;
        }

        private async Task<AdditionalService> GetConversationAdditionalServiceAsync(UserBilling userBilling, User user, AccountTypeEnum accountType)
        {
            var conversationsAddOn = await dopplerRepository.GetUserAddOnsByUserIdAndTypeAsync(user.UserId, (int)AddOnTypeEnum.Chat);
            if (conversationsAddOn != null)
            {
                var chatPlanUser = await dopplerRepository.GetActiveChatPlanByIdBillingCredit(conversationsAddOn.IdCurrentBillingCredit);
                var totalMonth = userBilling.Periodicity == 0 ? 1 :
                                 userBilling.Periodicity == 1 ? 3 :
                                 userBilling.Periodicity == 2 ? 6 :
                                 userBilling.Periodicity == 3 ? 12 :
                                 1;

                var rate = userBilling.Currency > 0 ? await dopplerRepository.GetCurrenyRate(0, userBilling.Currency ?? 0) : 1;

                var additionalService = new AdditionalService
                {
                    Type = AdditionalServiceTypeEnum.Chat,
                    Charge = accountType == AccountTypeEnum.User ? 
                                            (double)userBilling.ConversationsAmount : 
                                            chatPlanUser != null ? ((double)chatPlanUser.Fee * totalMonth) * (double)rate : 0,
                    PlanFee = chatPlanUser != null ? ((double)chatPlanUser.Fee * totalMonth) * (double)rate : 0,
                    ExtraPeriodMonth = string.IsNullOrEmpty(userBilling.ConversationsExtraMonth) ? 0 : Convert.ToDateTime(userBilling.ConversationsExtraMonth).Month,
                    ExtraPeriodYear = string.IsNullOrEmpty(userBilling.ConversationsExtraMonth) ? 0 : Convert.ToDateTime(userBilling.ConversationsExtraMonth).Year,
                    ExtraFee = !string.IsNullOrEmpty(userBilling.ConversationsExtraAmount) ? Convert.ToDouble(userBilling.ConversationsExtraAmount) : 0,
                    ExtraFeePerUnit = chatPlanUser != null ? (double)chatPlanUser.AdditionalConversation : 0,
                    ConversationQty = chatPlanUser != null ? chatPlanUser.ConversationQty : 0,
                    ExtraQty = !string.IsNullOrEmpty(userBilling.ConversationsExtra) ? Convert.ToInt32(userBilling.ConversationsExtra) : 0,
                    IsCustom = chatPlanUser.IsCustom,
                    UserEmail = user.Email
                };

                return additionalService;
            }

            return null;
        }

        private async Task<AdditionalService> GetOnSiteAdditionalServiceAsync(UserBilling userBilling, User user, AccountTypeEnum accountType)
        {
            var onSiteAddOn = await dopplerRepository.GetUserAddOnsByUserIdAndTypeAsync(user.UserId, (int)AddOnTypeEnum.OnSite);
            if (onSiteAddOn != null)
            {
                var onSitePlanUser = await dopplerRepository.GetActiveOnSitePlanByIdBillingCredit(onSiteAddOn.IdCurrentBillingCredit);
                var totalMonth = userBilling.Periodicity == 0 ? 1 :
                                 userBilling.Periodicity == 1 ? 3 :
                                 userBilling.Periodicity == 2 ? 6 :
                                 userBilling.Periodicity == 3 ? 12 :
                                 1;

                var rate = userBilling.Currency > 0 ? await dopplerRepository.GetCurrenyRate(0, userBilling.Currency ?? 0) : 1;

                var additionalService = new AdditionalService
                {
                    Type = AdditionalServiceTypeEnum.OnSite,
                    Charge = accountType == AccountTypeEnum.User ?
                                            (double)userBilling.PrintsAmount :
                                            onSitePlanUser != null ? ((double)onSitePlanUser.Fee * totalMonth) * (double)rate : 0,
                    PlanFee = onSitePlanUser != null ? ((double)onSitePlanUser.Fee * totalMonth) * (double)rate : 0,
                    ExtraPeriodMonth = string.IsNullOrEmpty(userBilling.PrintsExtraMonth) ? 0 : Convert.ToDateTime(userBilling.PrintsExtraMonth).Month,
                    ExtraPeriodYear = string.IsNullOrEmpty(userBilling.PrintsExtraMonth) ? 0 : Convert.ToDateTime(userBilling.PrintsExtraMonth).Year,
                    ExtraFee = !string.IsNullOrEmpty(userBilling.PrintsExtraAmount) ? Convert.ToDouble(userBilling.PrintsExtraAmount) : 0,
                    ExtraFeePerUnit = onSitePlanUser != null ? (double)onSitePlanUser.AdditionalPrint : 0,
                    PrintQty = onSitePlanUser != null ? onSitePlanUser.PrintQty : 0,
                    ExtraQty = !string.IsNullOrEmpty(userBilling.ConversationsExtra) ? Convert.ToInt32(userBilling.ConversationsExtra) : 0,
                    IsCustom = onSitePlanUser.IsCustom,
                    UserEmail = user.Email
                };

                return additionalService;
            }

            return null;
        }
    }
}
