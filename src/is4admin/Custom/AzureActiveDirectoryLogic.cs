using IdentityExpress.Identity;
using is4admin.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace is4admin.Custom
{
	public static class AzureActiveDirectoryLogic
	{
		public static async Task<UserAAD> CreateUser(AuthenticateResult result, List<Claim> filtered)
		{

			var user = new ApplicationUser
			{
				UserName = filtered.Any(x => x.Type == "email") ? filtered.FirstOrDefault(x => x.Type == "email")?.Value : Guid.NewGuid().ToString(),
				Email = filtered.FirstOrDefault(x => x.Type == "email")?.Value
			};
			List<Claim> claimAdded = new List<Claim>();

			string access_token = string.Empty;
			if (!string.IsNullOrEmpty(result.Properties.GetTokenValue("access_token")))
			{
				access_token = result.Properties.GetTokenValue("access_token");
				var delegateAuthProvider = new DelegateAuthenticationProvider((requestMessage) =>
				{
					requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", access_token);
					return Task.CompletedTask;
				});

				GraphServiceClient graphClient = new GraphServiceClient(delegateAuthProvider);
				var userMe = await graphClient.Me
					.Request()
					.GetAsync();
				if(!string.IsNullOrEmpty(userMe.City)) claimAdded.Add(new Claim(AADClaims.City, userMe.City));
				if(!string.IsNullOrEmpty(userMe.CompanyName)) claimAdded.Add(new Claim(AADClaims.CompanyName, userMe.CompanyName));
				if(!string.IsNullOrEmpty(userMe.Country)) claimAdded.Add(new Claim(AADClaims.Country, userMe.Country));
				if (!string.IsNullOrEmpty(userMe.JobTitle)) claimAdded.Add(new Claim(AADClaims.JobTitle, userMe.JobTitle));
			}

			return new UserAAD 
			{
				User = user,
				Claims = claimAdded
			};
		}
	}

	public class UserAAD
	{
		public List<Claim> Claims { get; set; } = new List<Claim>();
		public ApplicationUser User { get; set; }
	}

	public class AADClaims
	{
		public const string City = nameof(City);
		public const string CompanyName = nameof(CompanyName);
		public const string Country = nameof(Country);
		public const string JobTitle = nameof(JobTitle);
	}
}
