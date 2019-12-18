using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TweetBook.Data;
using TweetBook.Domain;
using TweetBook.Options;

namespace TweetBook.Services
{
    public class IdentitiyService : IIdentitiyService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtSettings _jwtSettings;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly DataContext _DataContext;
        public IdentitiyService(UserManager<IdentityUser> userManager, JwtSettings _jwtSettings, TokenValidationParameters _tokenValidationParameters, DataContext _DataContext)
        {
            _userManager = userManager;
            this._jwtSettings = _jwtSettings;
            this._tokenValidationParameters = _tokenValidationParameters;
            this._DataContext = _DataContext;
        }

        public async Task<AuthenticationResult> LoginAsync(string email, string password)
        {
            var User = await _userManager.FindByEmailAsync(email);

            if (User == null)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "User does  not exist" }
                };
            }
            var userHasValidPassword = await _userManager.CheckPasswordAsync(User, password);
            if (!userHasValidPassword)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "UserId or password mismatch" }
                };
            }

            return await GenerateAuthenticationResultForUserAsync(User);
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken)
        {
            var validatedToken = GetPrincipalFromToken(token);
            if (validatedToken == null)
                return new AuthenticationResult
                {
                    Errors = new[] { "Invalid token" }
                };

            //token expiry datetime is number of seconds since 1970, 1, 1, 0, 0
            //1970, 1, 1, 0, 0 is unix datetime
            var expiryDateUnix = long.Parse(validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

            //We convert unix datetie to utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            //Then we add to it no. of seconds since token got generated -  .AddSeconds(expiryDateUnix)
            //now subtract from it the token life time to check if it is in past i.e, expired  -.Subtract(_jwtSettings.TokenLifeTime);
            var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(expiryDateUnix).Subtract(_jwtSettings.TokenLifeTime);
            if (expiryDateTimeUtc > DateTime.Now)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "This token hasn't expired yet" }
                };
            }

            var Jti = validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

            var StoredRefreshToken = await _DataContext.RefreshTokens.SingleOrDefaultAsync(x => x.Token == refreshToken);
            if (StoredRefreshToken == null)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "This refresh token doesn't exist in DB!" }
                };
            }
            if (DateTime.UtcNow > StoredRefreshToken.ExpiryDate)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "This refresh token has expired" }
                };
            }
            if (StoredRefreshToken.Invalidated)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "This refresh token has been invalidated" }
                };
            }

            if (StoredRefreshToken.Used)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "This refresh token has been used" }
                };
            }

            if (StoredRefreshToken.JwtId != Jti)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "This refresh token does not match this JWT" }
                };
            }
            StoredRefreshToken.Used = true;
            _DataContext.RefreshTokens.Update(StoredRefreshToken);
            await _DataContext.SaveChangesAsync();
            var user = await _userManager.FindByIdAsync(validatedToken.Claims.Single(x => x.Type == "Id").Value);
            return await GenerateAuthenticationResultForUserAsync(user);
        }

        public async Task<AuthenticationResult> RegisterAsync(string email, string password)
        {
            var existingUSer = await _userManager.FindByEmailAsync(email);

            if (existingUSer != null)
            {
                return new AuthenticationResult
                {
                    Errors = new[] { "User already exists" }
                };
            };
            var newUserId = Guid.NewGuid();
            var newUser = new IdentityUser
            {
                Id=newUserId.ToString(),
                Email = email,
                UserName = email
            };

            //we are ading extra claims to created user. We have created policies that will pass only if user 
            //has this claim in the token
            //try
            //{
            //    await _userManager.AddClaimsAsync(newUser, new List<Claim>() { new Claim("tags.view", "true") });
            //}
            //catch (Exception ss) {}
            var createdUser = await _userManager.CreateAsync(newUser, password);

            //CreareAsync returns following stuff
            if (!createdUser.Succeeded)
            {
                return new AuthenticationResult
                {
                    Errors = createdUser.Errors.Select(x => x.Description)
                };
            }
            return await GenerateAuthenticationResultForUserAsync(newUser);
        }

        private async Task<AuthenticationResult> GenerateAuthenticationResultForUserAsync(IdentityUser newUser)
        {
            //user was created successfully. Do automatic login and return a valid token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            var Claims = new List<Claim>
                    {
                     new Claim(type: JwtRegisteredClaimNames.Sub,value: newUser.Email),
                     new Claim( type:JwtRegisteredClaimNames.Jti,value: Guid.NewGuid().ToString() ),
                     new Claim( type:JwtRegisteredClaimNames.Email,value: newUser.Email ),
                     new Claim( type:"Id",value: newUser.Id )
                };

            var userClaims = await _userManager.GetClaimsAsync(newUser);
            Claims.AddRange(userClaims);

            //confiure token options
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(Claims),
                Expires = DateTime.UtcNow.Add(_jwtSettings.TokenLifeTime),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), algorithm: SecurityAlgorithms.HmacSha256Signature)
            };

            //generate token
            var token = tokenHandler.CreateToken(tokenDescriptor);

            //generate refresh token and store in data base and return it alongwith bearer token to user
            var refreshToken = new RefreshToken
            {
                CreationDate = DateTime.UtcNow,
                JwtId = token.Id,
                UserId = newUser.Id,
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            };

            await _DataContext.RefreshTokens.AddAsync(refreshToken);
            await _DataContext.SaveChangesAsync();
            //return authn result with success and token
            return new AuthenticationResult
            {
                Success = true,
                Token = tokenHandler.WriteToken(token),
                RefreshToken = refreshToken.Token
            };
        }

        //Helper method to validate token as to if its same as generated by this app
        //We want to extract principal identity from this token
        public ClaimsPrincipal GetPrincipalFromToken(string Token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                //this method does not check automatically the algorith of token, so we need extra helper method below
                var principal = tokenHandler.ValidateToken(Token, _tokenValidationParameters, out var validatedToken);
                if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
                {
                    return null;
                }
                return principal;
            }
            catch// (Exception ss)
            {
                return null;
            }
        }


        public bool IsJwtWithValidSecurityAlgorithm(SecurityToken ValidatedToken)
        {
            return (ValidatedToken is JwtSecurityToken jwtSecurityToken) &&
                jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}