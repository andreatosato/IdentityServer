using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
			string access_token = "";

			ctx.HandleCodeRedemption(access_token, id_token);
		}
	}
}
