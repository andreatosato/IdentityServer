using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace is4admin.Custom
{
	public static class AuthorizationCodeReceived
	{
		public static async Task CodeRedemptionAsync(AuthorizationCodeReceivedContext ctx)
		{
			var id_token = ctx.ProtocolMessage.IdToken;
			var code = ctx.ProtocolMessage.Code;
			IConfidentialClientApplication clientapp = ConfidentialClientApplicationBuilder.Create(Globals.ClientId)
			   .WithClientSecret(Globals.ClientSecret)
			   .WithRedirectUri(Globals.RedirectUri)
			   .WithAuthority(new Uri(Globals.Authority))
			   .Build();

			var authResult = await clientapp
				.AcquireTokenByAuthorizationCode(Globals.BasicSignInScopes.Split(" "), code)
				.ExecuteAsync();

			ctx.HandleCodeRedemption(authResult.AccessToken, id_token);
		}

        public static class Globals
        {
            public const string Authority = "https://login.microsoftonline.com/common/v2.0/";
            public const string AdminConsentFormat = "https://login.microsoftonline.com/{0}/adminconsent?client_id={1}&state={2}&redirect_uri={3}";
            public const string BasicSignInScopes = "openid profile email offline_access user.readbasic.all user.read";
            public const string NameClaimType = "name";
			public const string RedirectUri = "http://localhost:5000/signin-oidc";
			public const string ClientId = "50fb4df5-2cb3-4eb0-b50a-398f0f37d034";
			public const string ClientSecret = "Ze/Ktb_kqZS?8vVmi@8uZj6or29CIGyn";
		}
    }
}
