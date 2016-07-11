using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Plugins;
using Nop.Services.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.ExternalAuth.Weixin.Authentication.External
{
    public class OpenAuthenticationService : Nop.Services.Authentication.External.OpenAuthenticationService
    {
        private readonly ICustomerService _customerService;
        private readonly IPluginFinder _pluginFinder;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;
        private readonly IRepository<ExternalAuthenticationRecord> _externalAuthenticationRecordRepository;

        public OpenAuthenticationService(IRepository<ExternalAuthenticationRecord> externalAuthenticationRecordRepository,
            IPluginFinder pluginFinder,
            ExternalAuthenticationSettings externalAuthenticationSettings,
            ICustomerService customerService)
            : base(externalAuthenticationRecordRepository, pluginFinder, externalAuthenticationSettings, customerService)
        {
            this._externalAuthenticationRecordRepository = externalAuthenticationRecordRepository;
            this._pluginFinder = pluginFinder;
            this._externalAuthenticationSettings = externalAuthenticationSettings;
            this._customerService = customerService;
        }

        /// <summary>
        /// 重写此方法用处理昵称(nickname)
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="parameters"></param>
        public override void AssociateExternalAccountWithUser(Nop.Core.Domain.Customers.Customer customer, Nop.Services.Authentication.External.OpenAuthenticationParameters parameters)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");

            //find nick name
            string nickName = null;
            if (parameters.UserClaims != null)
                foreach (var userClaim in parameters.UserClaims
                    .Where(x => x.Name != null && !String.IsNullOrEmpty(x.Name.Nickname)))
                {
                    //found
                    nickName = userClaim.Name.Nickname;
                    break;
                }

            var externalAuthenticationRecord = new ExternalAuthenticationRecord()
            {
                CustomerId = customer.Id,
                Email = string.Empty,
                ExternalIdentifier = parameters.ExternalIdentifier,
                ExternalDisplayIdentifier = nickName,
                OAuthToken = parameters.OAuthToken,
                OAuthAccessToken = parameters.OAuthAccessToken,
                ProviderSystemName = parameters.ProviderSystemName,
            };

            _externalAuthenticationRecordRepository.Insert(externalAuthenticationRecord);
        }
    }
}
