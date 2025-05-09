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
                        var onSiteAdditionalService = await GetAddOnAdditionalServiceByAddOnTypeAsync(userBilling, user, AccountTypeEnum.User, AddOnTypeEnum.OnSite);
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
                            var onSiteAdditionalService = await GetAddOnAdditionalServiceByAddOnTypeAsync(userBilling, user, AccountTypeEnum.CM, AddOnTypeEnum.OnSite);
                            if (onSiteAdditionalService != null)
                            {
                                billingRequest.AdditionalServices.Add(onSiteAdditionalService);
                            }
                        }
                    }
                }

                if (userBilling.PushNotificationsAmount > 0 || !string.IsNullOrEmpty(userBilling.PushNotificationsExtra))
                {
                    if (userBilling.PlanType != 0)
                    {
                        var user = await dopplerRepository.GetUserByUserIdAsync(userBilling.Id);
                        var pushNotificationAdditionalService = await GetAddOnAdditionalServiceByAddOnTypeAsync(userBilling, user, AccountTypeEnum.User, AddOnTypeEnum.PushNotification);
                        if (pushNotificationAdditionalService != null)
                        {
                            billingRequest.AdditionalServices.Add(pushNotificationAdditionalService);
                        }
                    }
                    else
                    {
                        var userIds = await dopplerRepository.GetUserIdsByClientManagerIdAsync(userBilling.Id);
                        foreach (var userId in userIds)
                        {
                            var user = await dopplerRepository.GetUserByUserIdAsync(userId);
                            var pushNotificationAdditionalService = await GetAddOnAdditionalServiceByAddOnTypeAsync(userBilling, user, AccountTypeEnum.CM, AddOnTypeEnum.PushNotification);
                            if (pushNotificationAdditionalService != null)
                            {
                                billingRequest.AdditionalServices.Add(pushNotificationAdditionalService);
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
                var extraFee = !string.IsNullOrEmpty(userBilling.ConversationsExtraAmount) ? Convert.ToDouble(userBilling.ConversationsExtraAmount.Replace(".", ",")) : 0;
                var extraQty = !string.IsNullOrEmpty(userBilling.ConversationsExtra) ? Convert.ToInt32(userBilling.ConversationsExtra) : 0;

                if (accountType == AccountTypeEnum.CM)
                {
                    var surplus = await dopplerRepository.GetByUserIdAddOnTypeIdAndPeridoAsync(user.UserId, (int)AddOnTypeEnum.Chat, userBilling.ConversationsExtraMonth);
                    extraFee = surplus != null ? (double)surplus.Total * (double)rate : 0;
                    extraQty = surplus != null ? surplus.Quantity : 0;
                }

                var additionalService = new AdditionalService
                {
                    Type = AdditionalServiceTypeEnum.Chat,
                    Charge = accountType == AccountTypeEnum.User ? 
                                            (double)userBilling.ConversationsAmount : 
                                            chatPlanUser != null ? ((double)chatPlanUser.Fee * totalMonth) * (double)rate : 0,
                    PlanFee = chatPlanUser != null ? ((double)chatPlanUser.Fee * totalMonth) * (double)rate : 0,
                    ExtraPeriodMonth = string.IsNullOrEmpty(userBilling.ConversationsExtraMonth) ? 0 : Convert.ToDateTime(userBilling.ConversationsExtraMonth).Month,
                    ExtraPeriodYear = string.IsNullOrEmpty(userBilling.ConversationsExtraMonth) ? 0 : Convert.ToDateTime(userBilling.ConversationsExtraMonth).Year,
                    ExtraFee = extraFee,
                    ExtraFeePerUnit = chatPlanUser != null ? Math.Round((double)chatPlanUser.AdditionalConversation * (double)rate, 4) : 0,
                    ConversationQty = chatPlanUser != null ? chatPlanUser.ConversationQty : 0,
                    ExtraQty = extraQty,
                    IsCustom = chatPlanUser.IsCustom,
                    UserEmail = user.Email
                };

                return additionalService;
            }

            return null;
        }

        private async Task<AdditionalService> GetAddOnAdditionalServiceByAddOnTypeAsync(UserBilling userBilling, User user, AccountTypeEnum accountType, AddOnTypeEnum addOnType)
        {
            var userAddOn = await dopplerRepository.GetUserAddOnsByUserIdAndTypeAsync(user.UserId, (int)addOnType);
            if (userAddOn != null)
            {
                var addOnPlanUser = await dopplerRepository.GetActiveAddOnPlanByIdBillingCreditAndAddOnType(userAddOn.IdCurrentBillingCredit, addOnType);
                var totalMonth = userBilling.Periodicity == 0 ? 1 :
                                 userBilling.Periodicity == 1 ? 3 :
                                 userBilling.Periodicity == 2 ? 6 :
                                 userBilling.Periodicity == 3 ? 12 :
                                 1;

                var rate = userBilling.Currency > 0 ? await dopplerRepository.GetCurrenyRate(0, userBilling.Currency ?? 0) : 1;
                var extraFee = 0.0;
                var extraFeePerUnit = addOnPlanUser != null ? Math.Round((double)addOnPlanUser.Additional * (double)rate, 4) : 0;
                var extraPeriodMonth = 0;
                var extraPeriodYear = 0;
                var extraQty = 0;
                var charge = 0.0;
                var quantity = addOnPlanUser != null ? addOnPlanUser.Quantity : 0;
                var planFee = addOnPlanUser != null ? ((double)addOnPlanUser.Fee * totalMonth) * (double)rate : 0;
                var additionalServiceType = AdditionalServiceTypeEnum.OnSite;


                switch (addOnType)
                {
                    case AddOnTypeEnum.OnSite:
                        extraFee = !string.IsNullOrEmpty(userBilling.PrintsExtraAmount) ? Convert.ToDouble(userBilling.PrintsExtraAmount.Replace(".", ",")) : 0;
                        extraQty = !string.IsNullOrEmpty(userBilling.PrintsExtra) ? Convert.ToInt32(userBilling.PrintsExtra) : 0;
                        extraPeriodMonth = string.IsNullOrEmpty(userBilling.PrintsExtraMonth) ? 0 : Convert.ToDateTime(userBilling.PrintsExtraMonth).Month;
                        extraPeriodYear = string.IsNullOrEmpty(userBilling.PrintsExtraMonth) ? 0 : Convert.ToDateTime(userBilling.PrintsExtraMonth).Year;

                        if (accountType == AccountTypeEnum.CM)
                        {
                            var surplus = await dopplerRepository.GetByUserIdAddOnTypeIdAndPeridoAsync(user.UserId, (int)addOnType, userBilling.PrintsExtraMonth);
                            extraFee = surplus != null ? (double)surplus.Total * (double)rate : 0;
                            extraQty = surplus != null ? surplus.Quantity : 0;
                        }

                        charge = accountType == AccountTypeEnum.User ?
                                (double)userBilling.PrintsAmount :
                                addOnPlanUser != null ? ((double)addOnPlanUser.Fee * totalMonth) * (double)rate : 0;

                        additionalServiceType = AdditionalServiceTypeEnum.OnSite;

                        break;
                    case AddOnTypeEnum.PushNotification:
                        extraFee = !string.IsNullOrEmpty(userBilling.PushNotificationsExtraAmount) ? Convert.ToDouble(userBilling.PushNotificationsExtraAmount.Replace(".", ",")) : 0;
                        extraQty = !string.IsNullOrEmpty(userBilling.PushNotificationsExtra) ? Convert.ToInt32(userBilling.PushNotificationsExtra) : 0;
                        extraPeriodMonth = string.IsNullOrEmpty(userBilling.PushNotificationsExtraMonth) ? 0 : Convert.ToDateTime(userBilling.PushNotificationsExtraMonth).Month;
                        extraPeriodYear = string.IsNullOrEmpty(userBilling.PushNotificationsExtraMonth) ? 0 : Convert.ToDateTime(userBilling.PushNotificationsExtraMonth).Year;

                        if (accountType == AccountTypeEnum.CM)
                        {
                            var surplus = await dopplerRepository.GetByUserIdAddOnTypeIdAndPeridoAsync(user.UserId, (int)addOnType, userBilling.PushNotificationsExtraMonth);
                            extraFee = surplus != null ? (double)surplus.Total * (double)rate : 0;
                            extraQty = surplus != null ? surplus.Quantity : 0;
                        }

                        charge = accountType == AccountTypeEnum.User ?
                                (double)userBilling.PushNotificationsAmount :
                                addOnPlanUser != null ? ((double)addOnPlanUser.Fee * totalMonth) * (double)rate : 0;

                        additionalServiceType = AdditionalServiceTypeEnum.PushNotification;

                        break;
                }

                var additionalService = new AdditionalService
                {
                    Type = additionalServiceType,
                    Charge = charge,
                    PlanFee = planFee,
                    ExtraPeriodMonth = extraPeriodMonth,
                    ExtraPeriodYear = extraPeriodYear,
                    ExtraFee = extraFee,
                    ExtraFeePerUnit = extraFeePerUnit,
                    Quantity = quantity,
                    ExtraQty = extraQty,
                    IsCustom = addOnPlanUser.IsCustom,
                    UserEmail = user.Email
                };

                return additionalService;
            }

            return null;
        }
    }
}
