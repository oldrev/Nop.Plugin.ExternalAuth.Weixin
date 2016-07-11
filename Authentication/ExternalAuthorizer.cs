using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Services.Authentication;
using Nop.Services.Authentication.External;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.ExternalAuth.Weixin.Authentication {
    public class ExternalAuthorizer : IExternalAuthorizer {
        #region Fields

        private readonly IAuthenticationService _authenticationService;
        private readonly IOpenAuthenticationService _openAuthenticationService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ICustomerRegistrationService _customerRegistrationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly CustomerSettings _customerSettings;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly LocalizationSettings _localizationSettings;
        private readonly IStoreContext _storeContext;
        #endregion

        #region Ctor

        public ExternalAuthorizer(IAuthenticationService authenticationService,
            IOpenAuthenticationService openAuthenticationService,
            IGenericAttributeService genericAttributeService,
            ICustomerRegistrationService customerRegistrationService,
            ICustomerActivityService customerActivityService, ILocalizationService localizationService,
            IWorkContext workContext, CustomerSettings customerSettings,
            ExternalAuthenticationSettings externalAuthenticationSettings,
            IShoppingCartService shoppingCartService,
            IWorkflowMessageService workflowMessageService,
            LocalizationSettings localizationSettings,
            IStoreContext storeContext) {
            this._authenticationService = authenticationService;
            this._openAuthenticationService = openAuthenticationService;
            this._genericAttributeService = genericAttributeService;
            this._customerRegistrationService = customerRegistrationService;
            this._customerActivityService = customerActivityService;
            this._localizationService = localizationService;
            this._workContext = workContext;
            this._customerSettings = customerSettings;
            this._externalAuthenticationSettings = externalAuthenticationSettings;
            this._shoppingCartService = shoppingCartService;
            this._workflowMessageService = workflowMessageService;
            this._localizationSettings = localizationSettings;
            this._storeContext = storeContext;
        }

        #endregion

        private bool AccountIsAssignedToLoggedOnAccount(Customer userFound, Customer userLoggedIn) {
            return userFound.Id.Equals(userLoggedIn.Id);
        }

        private bool AccountAlreadyExists(Customer userFound, Customer userLoggedIn) {
            return userFound != null && userLoggedIn != null;
        }

        private bool AccountDoesNotExistAndUserIsNotLoggedOn(Customer userFound, Customer userLoggedIn) {
            return userFound == null && userLoggedIn == null;
        }

        private bool RegistrationIsEnabled() {
            return _customerSettings.UserRegistrationType != UserRegistrationType.Disabled;
        }

        public virtual AuthorizationResult Authorize(OpenAuthenticationParameters parameters) {
            var userFound = _openAuthenticationService.GetUser(parameters);

            var userLoggedIn = _workContext.CurrentCustomer;

            if (AccountAlreadyExists(userFound, userLoggedIn)) {
                _authenticationService.SignIn(userFound, false);
            } else {
                #region Register user

                var currentCustomer = _workContext.CurrentCustomer;
                var details = new Nop.Plugin.ExternalAuth.Weixin.Authentication.External.RegistrationDetails(parameters);
                var randomPassword = CommonHelper.GenerateRandomDigitCode(20);

                var registrationRequest = new CustomerRegistrationRequest(currentCustomer, string.Empty, details.UserName, randomPassword, PasswordFormat.Clear, _storeContext.CurrentStore.Id, true);

                var registrationResult = _customerRegistrationService.RegisterCustomer(registrationRequest);
                if (registrationResult.Success) {
                    //store other parameters (form fields)
                    if (!String.IsNullOrEmpty(details.NickName))
                        _genericAttributeService.SaveAttribute(currentCustomer, SystemCustomerAttributeNames.FirstName, details.NickName);

                    userFound = currentCustomer;
                    _openAuthenticationService.AssociateExternalAccountWithUser(currentCustomer, parameters);
                    ExternalAuthorizerHelper.RemoveParameters();

                    //authenticate
                    _authenticationService.SignIn(userFound ?? userLoggedIn, false);

                } else {
                    ExternalAuthorizerHelper.RemoveParameters();

                    var result = new AuthorizationResult(OpenAuthenticationStatus.Error);
                    foreach (var error in registrationResult.Errors)
                        result.AddError(string.Format(error));

                    return result;
                }
                #endregion
            }

            return new AuthorizationResult(OpenAuthenticationStatus.Authenticated);
        }
    }
}
