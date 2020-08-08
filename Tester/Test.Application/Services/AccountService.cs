using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Girvs.Application;
using Girvs.Domain.Extensions;
using Girvs.Domain.Managers;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Test.Domain.Configuration;
using Test.Domain.Models;
using Test.Domain.Repositories;
using Test.GrpcService.BaseServices.AccountGrpcService;

namespace Test.Application.Services
{
    [AllowAnonymous]
    public class AccountService : AccountGrpcService.AccountGrpcServiceBase, IGrpcService
    {
        private readonly IUserRepository _userRepository;
        private readonly TokenManagement _tokenManagement;
        private readonly IClaimManager _claimManager;

        public AccountService(IUserRepository userRepository, TokenManagement tokenManagement, IClaimManager claimManager)
        {
            _userRepository = userRepository;
            _tokenManagement = tokenManagement ?? throw new ArgumentNullException(nameof(tokenManagement));
            _claimManager = claimManager ?? throw new ArgumentNullException(nameof(claimManager));
        }

        public override async Task<ResponseToken> Login(RequestUserLogin request, ServerCallContext context)
        {
            var user = await _userRepository.GetUserByLoginNameAsync(request.UserAccount);
            if (user != null && user.UserPassword == request.UserPassword.ToMd5())
            {
                return await CreateToken(user);
            }
            else
            {
                throw new RpcException( new Status(StatusCode.NotFound,"用户名或密码错误！"));
            }
        }


        private async Task<ResponseToken> CreateToken(User user)
        {
            string name = user.Id.ToString();
            string pwd = user.UserPassword;
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_tokenManagement.Secret);
            DateTime tokenExpire = DateTime.Now.AddDays(14);
            DateTime refreshTokenExpire = DateTime.Now.AddDays(30);
            var claims = await _claimManager.CreateClaims(user.Id.ToString(), user.UserName, user.TenantId.ToString());
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = tokenExpire,
                IssuedAt = DateTime.Now,
                Audience = _tokenManagement.Audience,
                Issuer = _tokenManagement.Issuer,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // 返回获取到的 Token 对象
            return new ResponseToken()
            {
                Name = name,
                UserName = user.UserName,
                Token = tokenHandler.WriteToken(token),
                TokenExpire = tokenExpire.ToString(CultureInfo.InvariantCulture),
                RefreshToken = Guid.NewGuid().ToString(),
                RefreshTokenExpire = refreshTokenExpire.ToString(CultureInfo.InvariantCulture)
            };
        }
    }
}